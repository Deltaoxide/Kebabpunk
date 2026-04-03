using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// OpenRouter CLI provider implementation.
    /// Uses @letuscode/openrouter-cli package for multi-model access.
    /// Implements both ICLIProvider and ICLIDetector.
    /// </summary>
    public class OpenRouterProvider : ICLIProvider, ICLIDetector
    {
        private const string CLI_COMMAND = "openrouter";
        private const string PROVIDER_NAME = "OpenRouter";

        private string _cachedVersion;
        private bool? _cachedInstalled;
        private DateTime _cacheTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        #region ICLIProvider

        public string ProviderName => PROVIDER_NAME;
        public string CLICommand => CLI_COMMAND;
        public ProviderType ProviderType => ProviderType.OpenRouter;

        public async Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default)
        {
            if (!IsInstalled())
            {
                return CLIResponse.Failed(
                    "OpenRouter CLI is not installed. Run: npm install -g @letuscode/openrouter-cli",
                    -1,
                    TimeSpan.Zero);
            }

            // Set API key environment variable before running
            var storedApiKey = PholusSettings.Instance.OpenRouterApiKey;
            if (!string.IsNullOrEmpty(storedApiKey))
            {
                Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", storedApiKey);
            }

            // Write prompt to temp file to avoid command-line escaping issues
            var tempFile = Path.Combine(Path.GetTempPath(), $"pholus_prompt_{Guid.NewGuid():N}.txt");
            try
            {
                await File.WriteAllTextAsync(tempFile, prompt, cancellationToken);

                string command;
                string args;

                // Note: OpenRouter CLI doesn't support --model flag on 'ask' command
                // Model is set via 'openrouter' init or config file
                // If a model is specified, we ignore it (user should configure via CLI)

#if UNITY_EDITOR_WIN
                // Write a PowerShell script file to handle the prompt properly
                    var scriptFile = Path.Combine(Path.GetTempPath(), $"pholus_ask_{Guid.NewGuid():N}.ps1");
                    var scriptContent = @"
$promptFile = $args[0]
$prompt = Get-Content -Raw -LiteralPath $promptFile
$prompt = $prompt -replace ""`r`n"", "" "" -replace ""`n"", "" "" -replace ""`r"", "" ""
$prompt = $prompt -replace '""', '\""'
& openrouter ask ""$prompt""
";
                    await File.WriteAllTextAsync(scriptFile, scriptContent, cancellationToken);

                    try
                    {
                        command = "powershell.exe";
                        args = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptFile}\" \"{tempFile}\"";

                        // Run PowerShell directly, not through cmd.exe wrapper
                        // OpenRouter streams output which cmd.exe buffers incorrectly
                        var psi = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = args,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };

                        var stopwatch = Stopwatch.StartNew();
                        using var process = new Process();
                        process.StartInfo = psi;

                        var outputBuilder = new StringBuilder();
                        var errorBuilder = new StringBuilder();

                        process.OutputDataReceived += (_, e) =>
                        {
                            if (e.Data != null)
                            {
                                outputBuilder.AppendLine(e.Data);
                                UnityEngine.Debug.Log($"[Pholus] > {e.Data}");
                            }
                        };

                        process.ErrorDataReceived += (_, e) =>
                        {
                            if (e.Data != null)
                            {
                                errorBuilder.AppendLine(e.Data);
                                UnityEngine.Debug.Log($"[Pholus] ! {e.Data}");
                            }
                        };

                        process.EnableRaisingEvents = true;
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // Use event-based wait for proper async with cancellation
                        var tcs = new TaskCompletionSource<bool>();
                        process.Exited += (_, _) => tcs.TrySetResult(true);

                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(300000);
                        await using var registration = cts.Token.Register(() => tcs.TrySetResult(false));

                        var completed = process.HasExited || await tcs.Task;
                        stopwatch.Stop();

                        if (!completed || cts.IsCancellationRequested)
                        {
                            try { process.Kill(); } catch (Exception ex) { PholusLogger.LogWarning($"Failed to kill process: {ex.Message}"); }
                            return CLIResponse.Failed("Process timed out or cancelled", -1, stopwatch.Elapsed);
                        }

                        var output = outputBuilder.ToString().Trim();
                        var error = errorBuilder.ToString().Trim();

                        if (process.ExitCode == 0)
                        {
                            return CLIResponse.Successful(output, stopwatch.Elapsed);
                        }
                        else
                        {
                            return CLIResponse.Failed(
                                string.IsNullOrEmpty(error) ? output : error,
                                process.ExitCode,
                                stopwatch.Elapsed);
                        }
                    }
                    finally
                    {
                        try { File.Delete(scriptFile); } catch (Exception ex) { PholusLogger.LogWarning($"Failed to delete script file: {ex.Message}"); }
                    }
#elif UNITY_EDITOR_OSX
                // macOS: Run directly with user's shell (login shell for PATH)
                var shell = System.Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
                command = shell;
                args = $"-l -c 'openrouter ask \"$(cat \"{tempFile}\")\"'";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var process = new System.Diagnostics.Process();
                process.StartInfo = psi;

                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        UnityEngine.Debug.Log($"[Pholus] > {e.Data}");
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        UnityEngine.Debug.Log($"[Pholus] ! {e.Data}");
                    }
                };

                process.EnableRaisingEvents = true;
                process.Start();
                UnityEngine.Debug.Log($"[Pholus] OpenRouter process started (PID: {process.Id})");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                process.Exited += (_, _) => tcs.TrySetResult(true);

                using var cts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(300000);
                await using var registration = cts.Token.Register(() => tcs.TrySetResult(false));

                var completed = process.HasExited || await tcs.Task;
                stopwatch.Stop();

                if (!completed || cts.IsCancellationRequested)
                {
                    try { process.Kill(); } catch { }
                    return CLIResponse.Failed("Process timed out or cancelled", -1, stopwatch.Elapsed);
                }

                var macOutput = outputBuilder.ToString().Trim();
                var macError = errorBuilder.ToString().Trim();

                if (process.ExitCode == 0)
                {
                    return CLIResponse.Successful(macOutput, stopwatch.Elapsed);
                }
                else
                {
                    return CLIResponse.Failed(
                        string.IsNullOrEmpty(macError) ? macOutput : macError,
                        process.ExitCode,
                        stopwatch.Elapsed);
                }
#else
                // Linux: Use bash with login shell
                command = "/bin/bash";
                args = $"-l -c 'openrouter ask \"$(cat \"{tempFile}\")\"'";

                return await ProcessRunner.RunAsync(
                    command,
                    args,
                    timeoutMs: 300000,
                    cancellationToken: cancellationToken);
#endif
            }
            finally
            {
                // Clean up temp file
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch (Exception ex) { PholusLogger.LogWarning($"Failed to delete temp file: {ex.Message}"); }
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
            // OpenRouter authenticates via API key - check this FIRST before CLI installation
            // because having an API key is the primary authentication method
            try
            {
                // Check Pholus stored API key first (most common case)
                var storedApiKey = PholusSettings.Instance.OpenRouterApiKey;
                if (!string.IsNullOrEmpty(storedApiKey))
                {
                    return true;
                }

                // Check environment variable
                var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return true;
                }

                // Also check OPENAI_API_KEY as fallback (OpenRouter is OpenAI-compatible)
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return true;
                }

                // Check config file (only if CLI is installed)
                if (IsInstalled())
                {
                    var configPath = GetConfigPath();
                    if (File.Exists(configPath))
                    {
                        return true;
                    }
                }

                return false;
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

            // Prefix with "openrouter-cli" for consistency
            if (!string.IsNullOrEmpty(version) && !version.Contains("openrouter"))
            {
                version = $"openrouter-cli {version}";
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
                return $"Installed{versionText} - API key not set";
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
            // OpenRouter CLI stores config in ~/.config/openrouter-cli/config.json
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "openrouter-cli", "config.json");
        }

        #endregion

        #region Model Selection

        /// <summary>
        /// Sets the model in the OpenRouter CLI config file.
        /// </summary>
        public static bool SetModelInConfig(string model)
        {
            try
            {
                var configPath = GetConfigPath();
                var configDir = Path.GetDirectoryName(configPath);

                // Ensure config directory exists
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // Read existing config or create new one
                Dictionary<string, object> config;
                if (File.Exists(configPath))
                {
                    var existingJson = File.ReadAllText(configPath);
                    config = ParseSimpleJson(existingJson);
                }
                else
                {
                    config = new Dictionary<string, object>
                    {
                        { "domain", "https://openrouter.ai/api/v1" }
                    };
                }

                // Set the model
                if (string.IsNullOrEmpty(model))
                {
                    // Remove model key to use CLI default
                    config.Remove("model");
                }
                else
                {
                    config["model"] = model;
                }

                // Write back to file
                var json = SerializeSimpleJson(config);
                File.WriteAllText(configPath, json);

                PholusLogger.Log($"OpenRouter model set to: {(string.IsNullOrEmpty(model) ? "CLI Default" : model)}");
                return true;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to set OpenRouter model: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the currently configured model from the config file.
        /// </summary>
        public static string GetModelFromConfig()
        {
            try
            {
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    return "";
                }

                var json = File.ReadAllText(configPath);
                var config = ParseSimpleJson(json);

                if (config.TryGetValue("model", out var model) && model is string modelStr)
                {
                    return modelStr;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Simple JSON parser for config file (key-value string pairs only).
        /// </summary>
        private static Dictionary<string, object> ParseSimpleJson(string json)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            // Remove braces and whitespace
            json = json.Trim().TrimStart('{').TrimEnd('}').Trim();
            if (string.IsNullOrEmpty(json)) return result;

            // Split by comma (simple approach, won't handle nested objects)
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var colonIndex = pair.IndexOf(':');
                if (colonIndex <= 0) continue;

                var key = pair.Substring(0, colonIndex).Trim().Trim('"');
                var value = pair.Substring(colonIndex + 1).Trim().Trim('"');

                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Simple JSON serializer for config file.
        /// </summary>
        private static string SerializeSimpleJson(Dictionary<string, object> dict)
        {
            var pairs = new List<string>();
            foreach (var kvp in dict)
            {
                var value = kvp.Value?.ToString() ?? "";
                pairs.Add($"  \"{kvp.Key}\": \"{value}\"");
            }
            return "{\n" + string.Join(",\n", pairs) + "\n}";
        }

        #endregion
    }
}
