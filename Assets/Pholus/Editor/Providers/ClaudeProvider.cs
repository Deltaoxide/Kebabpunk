using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Claude Code CLI provider implementation.
    /// Implements both ICLIProvider and ICLIDetector (can be split if needed).
    /// </summary>
    public class ClaudeProvider : ICLIProvider, ICLIDetector
    {
        private const string CLI_COMMAND = "claude";
        private const string PROVIDER_NAME = "Claude Code";

        private string _cachedVersion;
        private bool? _cachedInstalled;
        private DateTime _cacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);


        #region ICLIProvider

        public string ProviderName => PROVIDER_NAME;
        public string CLICommand => CLI_COMMAND;
        public ProviderType ProviderType => ProviderType.Claude;

        public async Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default)
        {
            if (!IsInstalled())
            {
                return CLIResponse.Failed(
                    "Claude Code CLI is not installed. Please install it first.",
                    -1,
                    TimeSpan.Zero);
            }

            // Build arguments with optional model flag
            var args = "--print -p - --output-format json";
            if (!string.IsNullOrEmpty(model))
            {
                args += $" --model {model}";
            }

            // Use stdin to pass long prompts (avoids command line length limits)
            // Use --output-format json to get token usage info
            var response = await ProcessRunner.RunWithStdinAsync(
                CLI_COMMAND,
                args,
                prompt,
                timeoutMs: 300000, // 5 minute timeout for AI response
                cancellationToken: cancellationToken);

            if (!response.Success)
            {
                return response;
            }

            // Parse JSON response to extract result and usage
            return ParseJsonResponse(response);
        }

        /// <summary>
        /// Parses Claude's JSON response to extract the actual result and token usage.
        /// </summary>
        private CLIResponse ParseJsonResponse(CLIResponse jsonResponse)
        {
            var json = jsonResponse.Output;

            try
            {
                // Extract the "result" field which contains the actual AI response
                var resultMatch = System.Text.RegularExpressions.Regex.Match(
                    json,
                    @"""result""\s*:\s*""((?:[^""\\]|\\.)*)""",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                string actualResult;
                if (resultMatch.Success)
                {
                    // Unescape the JSON string
                    actualResult = resultMatch.Groups[1].Value
                        .Replace("\\n", "\n")
                        .Replace("\\r", "\r")
                        .Replace("\\t", "\t")
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
                }
                else
                {
                    // Fallback: return raw output if parsing fails
                    PholusLogger.LogWarning("Could not parse 'result' from Claude JSON response");
                    actualResult = json;
                }

                // Parse token usage
                var usage = TokenUsage.ParseFromClaudeJson(json);

                // Log token usage with cache details
                var cacheInfo = "";
                if (usage.CacheCreationTokens > 0)
                    cacheInfo = $" | cache_write: {usage.CacheCreationTokens}";
                if (usage.CacheReadTokens > 0)
                    cacheInfo = $" | cache_read: {usage.CacheReadTokens} (90% savings!)";

                PholusLogger.Log($"Claude: {usage.InputTokens} in + {usage.OutputTokens} out = {usage.TotalTokens} tokens{cacheInfo}");

                return CLIResponse.Successful(actualResult, jsonResponse.Duration, usage);
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Failed to parse Claude JSON response: {ex.Message}");
                return CLIResponse.Successful(json, jsonResponse.Duration);
            }
        }

        #endregion

        #region ICLIDetector

        public bool IsInstalled()
        {
            // Check cache
            if (_cachedInstalled.HasValue && DateTime.Now - _cacheTime < _cacheDuration)
            {
                return _cachedInstalled.Value;
            }

            _cachedInstalled = ProcessRunner.CommandExists(CLI_COMMAND);
            _cacheTime = DateTime.Now;

            return _cachedInstalled.Value;
        }

        public bool IsAuthenticated()
        {
            if (!IsInstalled())
            {
                return false;
            }

            try
            {
                // Method 1: Check for credential files in common locations
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var possiblePaths = new[]
                {
                    Path.Combine(home, ".claude", ".credentials.json"),
                    Path.Combine(home, ".claude", "credentials.json"),
                    Path.Combine(home, ".claude", "settings.json"),
                    Path.Combine(home, ".claude.json"),
                    Path.Combine(home, ".config", "claude", "credentials.json"),
                    Path.Combine(home, ".config", "claude", "settings.json")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }

                // Method 2: Check if ~/.claude directory exists with any auth-related files
                var claudeDir = Path.Combine(home, ".claude");
                if (Directory.Exists(claudeDir))
                {
                    // Check for any JSON files that might contain credentials
                    var files = Directory.GetFiles(claudeDir, "*.json");
                    if (files.Length > 0)
                    {
                        return true;
                    }
                }

                // Method 3: Run claude command to check auth status (fallback)
                return CheckAuthViaCommand();
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Error checking Claude auth: {ex.Message}");
                return false;
            }
        }

        private bool CheckAuthViaCommand()
        {
            try
            {
#if UNITY_EDITOR_OSX
                var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var pathPrefix = $"export PATH='{home}/.local/bin:/opt/homebrew/bin:/usr/local/bin:'$PATH && ";

                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = shell,
                    // Run 'claude --version' which shows account info if logged in
                    Arguments = $"-l -c \"{pathPrefix}claude --version 2>&1\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                bool exited = process.WaitForExit(5000);
                if (!exited)
                {
                    try { process.Kill(); } catch { }
                    return false;
                }
                var output = process.StandardOutput.ReadToEnd();

                // If claude runs successfully and shows version, it's likely authenticated
                // (Claude Code requires auth to run most commands)
                return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
#elif UNITY_EDITOR_LINUX
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var pathPrefix = $"export PATH='{home}/.local/bin:/usr/local/bin:'$PATH && ";

                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-l -c \"{pathPrefix}claude --version 2>&1\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                bool exited = process.WaitForExit(5000);
                if (!exited)
                {
                    try { process.Kill(); } catch { }
                    return false;
                }
                var output = process.StandardOutput.ReadToEnd();
                return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
#else
                // Windows: use cmd
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c claude --version 2>&1",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(5000);
                var output = process.StandardOutput.ReadToEnd();
                return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
#endif
            }
            catch
            {
                return false;
            }
        }

        public string GetVersion()
        {
            if (!string.IsNullOrEmpty(_cachedVersion) && DateTime.Now - _cacheTime < _cacheDuration)
            {
                return _cachedVersion;
            }

            _cachedVersion = ProcessRunner.GetVersion(CLI_COMMAND);
            return _cachedVersion;
        }

        public string GetStatusMessage()
        {
            if (!IsInstalled())
            {
                return "Not installed";
            }

            var version = GetVersion();
            var versionText = !string.IsNullOrEmpty(version) ? $" ({version})" : "";

            if (!IsAuthenticated())
            {
                return $"Installed{versionText} - Not authenticated";
            }

            return $"Connected{versionText}";
        }

        public void ClearCache()
        {
            _cachedInstalled = null;
            _cachedVersion = null;
            _cacheTime = DateTime.MinValue;
        }

        #endregion

        #region Private Helpers

        private static string GetCredentialsPath()
        {
            // Claude Code stores credentials in ~/.claude/.credentials.json
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".claude", ".credentials.json");
        }

        private static string EscapeForCommandLine(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Escape special characters for command line
            return input
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        #endregion
    }
}
