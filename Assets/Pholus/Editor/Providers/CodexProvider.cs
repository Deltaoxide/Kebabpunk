using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// OpenAI Codex CLI provider implementation.
    /// Implements both ICLIProvider and ICLIDetector.
    /// </summary>
    public class CodexProvider : ICLIProvider, ICLIDetector
    {
        private const string CLI_COMMAND = "codex";
        private const string PROVIDER_NAME = "OpenAI Codex";

        private string _cachedVersion;
        private bool? _cachedInstalled;
        private DateTime _cacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        #region ICLIProvider

        public string ProviderName => PROVIDER_NAME;
        public string CLICommand => CLI_COMMAND;
        public ProviderType ProviderType => ProviderType.Codex;

        public async Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default)
        {
            if (!IsInstalled())
            {
                return CLIResponse.Failed(
                    "OpenAI Codex CLI is not installed. Run: npm install -g @openai/codex",
                    -1,
                    TimeSpan.Zero);
            }

            // Build arguments with optional model flag
            // -c mcp_servers={} disables any user-configured MCP servers that could
            // block startup if the MCP server isn't running (connection refused errors)
            var args = "-c mcp_servers={} exec --yolo";
            if (!string.IsNullOrEmpty(model))
            {
                args += $" --model {model}";
            }

            // OpenAI Codex CLI uses 'codex exec' for non-interactive mode
            // See: https://developers.openai.com/codex/cli/reference/
            // Use stdin to pass the prompt (avoids command line length limits)

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

            // OpenAI Codex CLI stores config in ~/.codex/config.toml
            // Authentication is done on first run via browser
            try
            {
                var configPath = GetConfigPath();
                if (string.IsNullOrEmpty(configPath))
                {
                    return false;
                }

                // Check if auth.json exists (created after login)
                return File.Exists(Path.Combine(configPath, "auth.json"));
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

        private static string GetConfigPath()
        {
            // OpenAI Codex CLI stores config in ~/.codex/
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".codex");
        }

        #endregion
    }
}
