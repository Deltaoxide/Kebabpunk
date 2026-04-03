using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Google Gemini CLI provider implementation.
    /// Implements both ICLIProvider and ICLIDetector.
    /// </summary>
    public class GeminiProvider : ICLIProvider, ICLIDetector
    {
        private const string CLI_COMMAND = "gemini";
        private const string PROVIDER_NAME = "Google Gemini";

        private string _cachedVersion;
        private bool? _cachedInstalled;
        private DateTime _cacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        #region ICLIProvider

        public string ProviderName => PROVIDER_NAME;
        public string CLICommand => CLI_COMMAND;
        public ProviderType ProviderType => ProviderType.Gemini;

        public async Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default)
        {
            if (!IsInstalled())
            {
                return CLIResponse.Failed(
                    "Google Gemini CLI is not installed. Run: npm install -g @google/gemini-cli",
                    -1,
                    TimeSpan.Zero);
            }

            // Build arguments with optional model flag
            // --allowed-mcp-server-names with a non-existent name effectively disables
            // all user-configured MCP servers that could block startup
            var args = "-y --allowed-mcp-server-names __pholus_none__";
            if (!string.IsNullOrEmpty(model))
            {
                args += $" --model {model}";
            }

            // Gemini CLI uses stdin for prompts in non-interactive mode
            return await ProcessRunner.RunWithStdinAsync(
                CLI_COMMAND,
                args,
                prompt,
                timeoutMs: 300000, // 5 minute timeout for AI response
                cancellationToken: cancellationToken);
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

            // Gemini CLI authenticates via Google account
            // Config stored in ~/.gemini/ directory
            try
            {
                var configPath = GetConfigPath();
                if (string.IsNullOrEmpty(configPath) || !Directory.Exists(configPath))
                {
                    return false;
                }

                // Check for settings.json which contains auth info
                var settingsFile = Path.Combine(configPath, "settings.json");
                if (File.Exists(settingsFile))
                {
                    return true;
                }

                // Also check for credentials file
                var credentialsFile = Path.Combine(configPath, "credentials.json");
                return File.Exists(credentialsFile);
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

            var version = ProcessRunner.GetVersion(CLI_COMMAND);

            // Gemini CLI returns just version number (e.g., "0.22.5")
            // Prefix with "gemini-cli" for consistency with other providers
            if (!string.IsNullOrEmpty(version) && !version.Contains("gemini"))
            {
                version = $"gemini-cli {version}";
            }

            _cachedVersion = version;
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

        private static string GetConfigPath()
        {
            // Gemini CLI stores config in ~/.gemini/
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".gemini");
        }

        #endregion
    }
}
