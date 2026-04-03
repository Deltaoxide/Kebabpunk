using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Cursor CLI provider implementation.
    /// Implements both ICLIProvider and ICLIDetector.
    /// Uses background threads for WSL checks to avoid blocking Unity main thread.
    /// </summary>
    public class CursorProvider : ICLIProvider, ICLIDetector
    {
        private const string CLI_COMMAND = "cursor-agent";
        private const string PROVIDER_NAME = "Cursor";

        // Static cache shared across all instances
        private static string _cachedVersion;
        private static bool? _cachedInstalled;
        private static bool? _cachedAuthenticated;
        private static DateTime _cacheTime;
        private static DateTime _authCacheTime;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        // Background refresh state
        private static bool _isRefreshing;
        private static event Action _onRefreshComplete;

        /// <summary>
        /// Event fired when background refresh completes. Use to repaint UI.
        /// </summary>
        public static event Action OnRefreshComplete
        {
            add => _onRefreshComplete += value;
            remove => _onRefreshComplete -= value;
        }

        /// <summary>
        /// Whether a background refresh is currently in progress.
        /// </summary>
        public static bool IsRefreshing => _isRefreshing;

        /// <summary>
        /// Checks if running on Windows (uses WSL on Windows).
        /// Compile-time constant via preprocessor symbols.
        /// </summary>
#if UNITY_EDITOR_WIN
        public static bool IsWindowsPlatform => true;
#else
        public static bool IsWindowsPlatform => false;
#endif

        #region ICLIProvider

        public string ProviderName => PROVIDER_NAME;
        public string CLICommand => CLI_COMMAND;
        public ProviderType ProviderType => ProviderType.Cursor;

        public async Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default)
        {
            if (!IsInstalled())
            {
                return CLIResponse.Failed(GetInstallErrorMessage(), -1, TimeSpan.Zero);
            }

            // Build arguments: -p for print mode (non-interactive)
            var args = "-p --output-format text";
            if (!string.IsNullOrEmpty(model))
            {
                args += $" --model {model}";
            }

            // On Windows, run through WSL; on Linux/Mac run natively
            if (IsWindowsPlatform)
            {
                return await ProcessRunner.RunWSLWithStdinAsync(
                    CLI_COMMAND,
                    args,
                    prompt,
                    timeoutMs: 300000,
                    cancellationToken: cancellationToken);
            }
            else
            {
                return await ProcessRunner.RunWithStdinAsync(
                    CLI_COMMAND,
                    args,
                    prompt,
                    timeoutMs: 300000,
                    cancellationToken: cancellationToken);
            }
        }

        private string GetInstallErrorMessage()
        {
            if (IsWindowsPlatform)
            {
                if (!ProcessRunner.IsWSLAvailable())
                {
                    return "Cursor Agent CLI requires WSL on Windows. Install WSL: wsl --install";
                }
                return "Cursor Agent CLI not found in WSL. Run in WSL terminal: curl https://cursor.com/install -fsSL | bash";
            }
            return "Cursor Agent CLI is not installed. Run: curl https://cursor.com/install -fsSL | bash";
        }

        #endregion

        #region ICLIDetector

        public bool IsInstalled()
        {
            // Return cached value if available
            if (_cachedInstalled.HasValue)
            {
                // Trigger background refresh if cache expired
                if (DateTime.Now - _cacheTime >= _cacheDuration)
                {
                    RefreshInBackground();
                }
                return _cachedInstalled.Value;
            }

            // No cached value - do synchronous check (with timeout)
            // This ensures accurate status on first check / after cache clear
#if UNITY_EDITOR_WIN
            _cachedInstalled = ProcessRunner.WSLCommandExists(CLI_COMMAND);
#else
            _cachedInstalled = ProcessRunner.CommandExists(CLI_COMMAND);
#endif
            _cacheTime = DateTime.Now;
            return _cachedInstalled.Value;
        }

        public bool IsAuthenticated()
        {
            // Return cached value if available
            if (_cachedAuthenticated.HasValue)
            {
                // Trigger background refresh if cache expired
                if (DateTime.Now - _authCacheTime >= _cacheDuration)
                {
                    RefreshInBackground();
                }
                return _cachedAuthenticated.Value;
            }

            // No cached value - do synchronous check
            if (!IsInstalled())
            {
                _cachedAuthenticated = false;
                _authCacheTime = DateTime.Now;
                return false;
            }

#if UNITY_EDITOR_WIN
            _cachedAuthenticated = CheckWSLAuthentication();
#else
            _cachedAuthenticated = CheckNativeAuthentication();
#endif
            _authCacheTime = DateTime.Now;
            return _cachedAuthenticated.Value;
        }

        /// <summary>
        /// Triggers a background refresh of installation and authentication status.
        /// Subscribe to OnRefreshComplete to be notified when done.
        /// </summary>
        public void RefreshInBackground()
        {
            if (_isRefreshing) return;

            _isRefreshing = true;
            Task.Run(() =>
            {
                try
                {
                    // Check installation
                    if (IsWindowsPlatform)
                    {
                        _cachedInstalled = ProcessRunner.WSLCommandExists(CLI_COMMAND);
                    }
                    else
                    {
                        _cachedInstalled = ProcessRunner.CommandExists(CLI_COMMAND);
                    }
                    _cacheTime = DateTime.Now;

                    // Check authentication if installed
                    if (_cachedInstalled == true)
                    {
                        if (IsWindowsPlatform)
                        {
                            _cachedAuthenticated = CheckWSLAuthentication();
                        }
                        else
                        {
                            _cachedAuthenticated = CheckNativeAuthentication();
                        }
                        _authCacheTime = DateTime.Now;
                    }
                    else
                    {
                        _cachedAuthenticated = false;
                    }

                    // Get version if installed
                    if (_cachedInstalled == true)
                    {
                        string version;
                        if (IsWindowsPlatform)
                        {
                            version = ProcessRunner.GetWSLCommandVersion(CLI_COMMAND);
                        }
                        else
                        {
                            version = ProcessRunner.GetVersion(CLI_COMMAND);
                        }

                        // Prefix with "cursor-agent" for consistency
                        if (!string.IsNullOrEmpty(version) && !version.Contains("cursor"))
                        {
                            version = $"cursor-agent {version}";
                        }
                        _cachedVersion = version;
                    }
                }
                catch (Exception ex)
                {
                    PholusLogger.LogWarning($"Cursor status check failed: {ex.Message}");
                }
                finally
                {
                    _isRefreshing = false;
                    // Notify on main thread
                    UnityMainThreadDispatcher.Enqueue(() => _onRefreshComplete?.Invoke());
                }
            });
        }

        private bool CheckWSLAuthentication()
        {
            // Run cursor-agent status to check if logged in
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "bash -lc \"cursor-agent status 2>&1\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10000); // 10 sec timeout for status check

                // Check if output contains "Logged in" indicator
                return output.Contains("Logged in") || output.Contains("✓");
            }
            catch
            {
                return false;
            }
        }

        private bool CheckNativeAuthentication()
        {
            // Run cursor-agent status to check if logged in
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#if UNITY_EDITOR_OSX
                var shell = System.Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
                var pathPrefix = $"export PATH=\"{home}/.local/bin:/opt/homebrew/bin:/usr/local/bin:$PATH\" && ";
#else
                var shell = "/bin/bash";
                var pathPrefix = $"export PATH=\"{home}/.local/bin:/usr/local/bin:$PATH\" && ";
#endif
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = $"-c \"{pathPrefix}cursor-agent status 2>&1\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                bool exited = process.WaitForExit(5000);
                if (!exited)
                {
                    try { process.Kill(); } catch (Exception ex) { PholusLogger.LogWarning($"Failed to kill process: {ex.Message}"); }
                    return false;
                }
                var output = process.StandardOutput.ReadToEnd();
                return output.Contains("Logged in") || output.Contains("✓");
            }
            catch
            {
                return false;
            }
        }

        public string GetVersion()
        {
            // Return cached value immediately (never block)
            // Version is refreshed in background along with install/auth status
            if (!string.IsNullOrEmpty(_cachedVersion))
            {
                return _cachedVersion;
            }

            // Trigger background refresh if no cached version
            RefreshInBackground();
            return null;
        }

        public string GetStatusMessage()
        {
            // Show checking status if refresh is in progress and no cached data
            if (_isRefreshing && !_cachedInstalled.HasValue)
            {
                return "Checking...";
            }

            if (!IsInstalled())
            {
                if (IsWindowsPlatform)
                {
                    if (!ProcessRunner.IsWSLAvailable())
                    {
                        return "WSL not installed";
                    }
                    return _isRefreshing ? "Checking WSL..." : "Not installed in WSL";
                }
                return _isRefreshing ? "Checking..." : "Not installed";
            }

            var version = GetVersion();
            var versionText = !string.IsNullOrEmpty(version) ? $" ({version})" : "";
            var viaWSL = IsWindowsPlatform ? " via WSL" : "";

            if (!IsAuthenticated())
            {
                return $"Installed{viaWSL}{versionText} - Not authenticated";
            }

            return $"Connected{viaWSL}{versionText}";
        }

        public void ClearCache()
        {
            _cachedInstalled = null;
            _cachedVersion = null;
            _cachedAuthenticated = null;
            _cacheTime = DateTime.MinValue;
            _authCacheTime = DateTime.MinValue;
        }

        #endregion

        #region Private Helpers

        private static string GetConfigPath()
        {
            // Cursor CLI stores config in ~/.cursor/
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".cursor");
        }

        #endregion
    }
}
