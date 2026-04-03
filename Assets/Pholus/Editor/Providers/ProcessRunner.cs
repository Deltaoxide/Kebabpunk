using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using UnityEngine;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Utility for running CLI processes and capturing output.
    /// Single Responsibility: Only handles process execution.
    /// Uses Strategy Pattern with preprocessor symbols for platform-specific implementations.
    /// </summary>
    public static class ProcessRunner
    {
        private const int DefaultTimeoutMs = 120000; // 2 minutes

        #region Platform Strategy Pattern

        /// <summary>
        /// Platform-specific execution strategy interface.
        /// </summary>
        private interface IPlatformStrategy
        {
            ProcessStartInfo CreateStartInfo(string command, string arguments, bool redirectStdin);
            bool CommandExists(string command);
            string GetVersion(string command);
            void OpenTerminal(string command, bool keepOpen);
        }

        /// <summary>
        /// Singleton platform strategy instance, selected at compile time.
        /// </summary>
        private static readonly IPlatformStrategy _platform = CreatePlatformStrategy();

        private static IPlatformStrategy CreatePlatformStrategy()
        {
#if UNITY_EDITOR_WIN
            return new WindowsStrategy();
#elif UNITY_EDITOR_OSX
            return new MacOSStrategy();
#else
            return new LinuxStrategy();
#endif
        }

        #region Windows Strategy
#if UNITY_EDITOR_WIN
        private class WindowsStrategy : IPlatformStrategy
        {
            public ProcessStartInfo CreateStartInfo(string command, string arguments, bool redirectStdin)
            {
                // On Windows, always use cmd.exe - npm packages like 'claude' are .cmd scripts
                return new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command} {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = redirectStdin,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
            }

            public bool CommandExists(string command)
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c where {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }

            public string GetVersion(string command)
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo = CreateStartInfo(command, "--version", false);
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        return output.Trim().Split('\n')[0];
                    }
                }
                catch { }
                return null;
            }

            public void OpenTerminal(string command, bool keepOpen)
            {
                string wrappedCommand;
                if (keepOpen)
                {
                    wrappedCommand = $"/k \"{command}\"";
                }
                else
                {
                    wrappedCommand = $"/c \"{command} && echo. && echo Complete! You can close this window. && timeout /t 3 >nul\"";
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = wrappedCommand,
                    UseShellExecute = true
                });
            }
        }
#endif
        #endregion

        #region MacOS Strategy
#if UNITY_EDITOR_OSX
        private class MacOSStrategy : IPlatformStrategy
        {
            // Common paths where CLI tools are installed on Mac
            private static readonly string[] MacPaths = new[]
            {
                "/opt/homebrew/bin",      // Homebrew on Apple Silicon
                "/usr/local/bin",         // Homebrew on Intel / manual installs
                "/usr/bin",
                "/bin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin"),         // Cursor, pipx, local installs
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nvm/current/bin"),   // NVM
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".npm-global/bin")     // npm global
            };

            private string GetUserShell()
            {
                return Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
            }

            /// <summary>
            /// Gets the nvm initialization script that:
            /// 1. Sources nvm if available
            /// 2. Tries to use Node 22 or 20 (silently fails if already on compatible version or not available)
            /// </summary>
            private string GetNvmInitScript(string home)
            {
                // Source nvm and try to switch to Node 22 or 20
                // nvm use fails silently if version not installed, succeeds silently if already on it
                return $"[ -s '{home}/.nvm/nvm.sh' ] && source '{home}/.nvm/nvm.sh' 2>/dev/null && (nvm use 22 2>/dev/null || nvm use 20 2>/dev/null || true); ";
            }

            public ProcessStartInfo CreateStartInfo(string command, string arguments, bool redirectStdin)
            {
                var shell = GetUserShell();
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var fullCommand = string.IsNullOrEmpty(arguments) ? command : $"{command} {arguments}";
                var escapedCmd = EscapeForShell(fullCommand);

                // Source nvm and auto-switch to compatible Node version if needed
                var nvmInit = GetNvmInitScript(home);

                // Add common CLI paths to PATH before running command
                var pathPrefix = $"export PATH='{home}/.local/bin:/opt/homebrew/bin:/usr/local/bin:'$PATH && ";
                var finalCmd = nvmInit + pathPrefix + escapedCmd;

                return new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments = $"-l -c \"{finalCmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = redirectStdin,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
            }

            public bool CommandExists(string command)
            {
                // Method 1: Direct path check for common locations (fast)
                foreach (var path in MacPaths)
                {
                    var fullPath = Path.Combine(path, command);
                    if (File.Exists(fullPath))
                    {
                        return true;
                    }
                }

                // Method 2: Use shell with nvm (auto-switch to compatible version) and extended PATH
                try
                {
                    var shell = GetUserShell();
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                    // Source nvm and auto-switch to compatible Node version if needed
                    var nvmInit = GetNvmInitScript(home);
                    var pathPrefix = $"export PATH='{home}/.local/bin:/opt/homebrew/bin:/usr/local/bin:'$PATH && ";

                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = $"-l -c \"{nvmInit}{pathPrefix}which {command}\"",
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
                    if (process.ExitCode == 0)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Silently fail - command doesn't exist
                }

                return false;
            }

            public string GetVersion(string command)
            {
                try
                {
                    var shell = GetUserShell();
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                    // Source nvm and auto-switch to compatible Node version if needed
                    var nvmInit = GetNvmInitScript(home);
                    var pathPrefix = $"export PATH='{home}/.local/bin:/opt/homebrew/bin:/usr/local/bin:'$PATH && ";

                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = shell,
                        Arguments = $"-l -c \"{nvmInit}{pathPrefix}{command} --version\"",
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
                        return null;
                    }
                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        return output.Trim().Split('\n')[0];
                    }
                }
                catch { }
                return null;
            }

            public void OpenTerminal(string command, bool keepOpen)
            {
                try
                {
                    PholusLogger.Log($"Opening Terminal with command: {command}");

                    // Write AppleScript to a temp file to avoid shell escaping issues
                    var scriptFile = Path.Combine(Path.GetTempPath(), $"pholus_terminal_{Guid.NewGuid():N}.scpt");
                    var escapedCommand = command.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    var appleScript = $"tell application \"Terminal\"\n  activate\n  do script \"{escapedCommand}\"\nend tell";
                    File.WriteAllText(scriptFile, appleScript);

                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/osascript",
                        Arguments = scriptFile,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    process.Start();
                    process.WaitForExit(5000);

                    // Clean up script file
                    try { File.Delete(scriptFile); } catch { }
                }
                catch (Exception ex)
                {
                    PholusLogger.LogError($"Failed to open Terminal: {ex.Message}");
                }
            }

            private static string EscapeForShell(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                // Escape backslashes first, then double quotes
                return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }

            private static string EscapeForAppleScript(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                // AppleScript requires escaping backslashes and double quotes
                return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }
        }
#endif
        #endregion

        #region Linux Strategy
#if !UNITY_EDITOR_WIN && !UNITY_EDITOR_OSX
        private class LinuxStrategy : IPlatformStrategy
        {
            public ProcessStartInfo CreateStartInfo(string command, string arguments, bool redirectStdin)
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var fullCommand = string.IsNullOrEmpty(arguments) ? command : $"{command} {arguments}";
                var escapedCmd = EscapeForShell(fullCommand);

                // Add common CLI paths to PATH before running command
                var pathPrefix = $"export PATH='{home}/.local/bin:/usr/local/bin:'$PATH && ";
                var finalCmd = pathPrefix + escapedCmd;

                return new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    // Use -l for login shell to load ~/.bashrc, ~/.profile (NVM, npm, etc.)
                    Arguments = $"-l -c \"{finalCmd}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = redirectStdin,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
            }

            public bool CommandExists(string command)
            {
                try
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var pathPrefix = $"export PATH='{home}/.local/bin:/usr/local/bin:'$PATH && ";

                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        // Use -l for login shell to load ~/.bashrc, ~/.profile (NVM, npm, etc.)
                        Arguments = $"-l -c \"{pathPrefix}which {command}\"",
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
                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }

            public string GetVersion(string command)
            {
                try
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var pathPrefix = $"export PATH='{home}/.local/bin:/usr/local/bin:'$PATH && ";

                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        // Use -l for login shell to load ~/.bashrc, ~/.profile (NVM, npm, etc.)
                        Arguments = $"-l -c \"{pathPrefix}{command} --version\"",
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
                        return null;
                    }
                    if (process.ExitCode == 0)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        return output.Trim().Split('\n')[0];
                    }
                }
                catch { }
                return null;
            }

            public void OpenTerminal(string command, bool keepOpen)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "x-terminal-emulator",
                    Arguments = $"-e \"{command}\"",
                    UseShellExecute = true
                });
            }

            private static string EscapeForShell(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }
        }
#endif
        #endregion

        #endregion

        /// <summary>
        /// Runs a command and returns the result.
        /// </summary>
        /// <param name="command">The command to run (e.g., "claude").</param>
        /// <param name="arguments">Command arguments.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>CLI response with output or error.</returns>
        public static async Task<CLIResponse> RunAsync(
            string command,
            string arguments,
            int timeoutMs = DefaultTimeoutMs,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            PholusLogger.Log($"Starting process: {command} {arguments}");
            PholusLogger.Log($"Timeout: {timeoutMs / 1000}s");

            try
            {
                using var process = new Process();
                process.StartInfo = CreateStartInfo(command, arguments, redirectStdin: false);

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                        // Log output in real-time so user can see progress
                        PholusLogger.Log($"> {e.Data}");
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                        // Log errors/warnings in real-time
                        if (e.Data.Contains("ERR") || e.Data.Contains("error"))
                            PholusLogger.LogError($"! {e.Data}");
                        else if (e.Data.Contains("WARN") || e.Data.Contains("warn"))
                            PholusLogger.LogWarning($"! {e.Data}");
                        else
                            PholusLogger.Log($"! {e.Data}");
                    }
                };

                // Enable raising events BEFORE starting to avoid race conditions
                process.EnableRaisingEvents = true;

                process.Start();
                PholusLogger.Log($"Process started (PID: {process.Id}). Waiting for completion...");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait with timeout and cancellation
                var completed = await WaitForExitAsync(process, timeoutMs, cancellationToken);

                stopwatch.Stop();

                if (!completed)
                {
                    TryKillProcess(process);
                    PholusLogger.LogError($"Process timed out after {stopwatch.Elapsed.TotalSeconds:F1}s");
                    return CLIResponse.Failed(
                        $"Process timed out after {timeoutMs / 1000} seconds",
                        -1,
                        stopwatch.Elapsed);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    TryKillProcess(process);
                    PholusLogger.LogWarning("Process cancelled by user");
                    return CLIResponse.Failed(
                        "Operation cancelled",
                        -1,
                        stopwatch.Elapsed);
                }

                var output = outputBuilder.ToString().Trim();
                var error = errorBuilder.ToString().Trim();

                PholusLogger.Log($"Process completed in {stopwatch.Elapsed.TotalSeconds:F1}s with exit code {process.ExitCode}");

                if (process.ExitCode == 0)
                {
                    PholusLogger.Log("Process succeeded!");
                    return CLIResponse.Successful(output, stopwatch.Elapsed);
                }
                else
                {
                    var errorMsg = string.IsNullOrEmpty(error) ? output : error;
                    PholusLogger.LogError($"Process failed with exit code {process.ExitCode}: {errorMsg}");
                    return CLIResponse.Failed(
                        errorMsg,
                        process.ExitCode,
                        stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PholusLogger.LogError($"Process execution failed: {ex.Message}\n{ex.StackTrace}");
                return CLIResponse.Failed(ex.Message, -1, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Checks if a command is available on the system.
        /// Uses platform-specific strategy for proper PATH resolution.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <returns>True if the command exists.</returns>
        public static bool CommandExists(string command)
        {
            return _platform.CommandExists(command);
        }

        #region WSL Support

        /// <summary>
        /// Checks if WSL (Windows Subsystem for Linux) is available.
        /// Only relevant on Windows platforms.
        /// </summary>
        public static bool IsWSLAvailable()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor &&
                Application.platform != RuntimePlatform.WindowsPlayer)
            {
                return false; // WSL only exists on Windows
            }

            return CommandExists("wsl");
        }

        /// <summary>
        /// Checks if a command exists inside WSL.
        /// Uses login shell to ensure PATH is properly loaded.
        /// </summary>
        /// <param name="command">The command to check in WSL.</param>
        /// <returns>True if the command exists in WSL.</returns>
        public static bool WSLCommandExists(string command)
        {
            if (!IsWSLAvailable())
                return false;

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    // Use login shell (-l) to load ~/.profile and ~/.bashrc for proper PATH
                    Arguments = $"bash -lc \"which {command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Runs a command through WSL with stdin input.
        /// Uses login shell and sources .bashrc to ensure PATH is properly loaded.
        /// Note: Bypasses cmd.exe wrapper to avoid quote escaping issues with WSL.
        /// </summary>
        public static async Task<CLIResponse> RunWSLWithStdinAsync(
            string command,
            string arguments,
            string stdinContent,
            int timeoutMs = DefaultTimeoutMs,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Build the bash command that sources .bashrc for PATH, then runs the command
            // Using -i makes it interactive which sources .bashrc, but we also source it explicitly
            var bashCommand = $"source ~/.bashrc 2>/dev/null; {command} {arguments}";

            try
            {
                using var process = new Process();
                // Invoke wsl.exe directly (not through cmd.exe) to avoid quote escaping issues
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"bash -lc \"{bashCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        errorBuilder.AppendLine(e.Data);
                };

                PholusLogger.Log($"Executing WSL: wsl.exe {process.StartInfo.Arguments}");
                process.EnableRaisingEvents = true;
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Write to stdin and close it
                await process.StandardInput.WriteAsync(stdinContent);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();

                // Wait with timeout and cancellation
                var completed = await WaitForExitAsync(process, timeoutMs, cancellationToken);

                stopwatch.Stop();

                if (!completed)
                {
                    TryKillProcess(process);
                    return CLIResponse.Failed("Process timed out", -1, stopwatch.Elapsed);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    TryKillProcess(process);
                    return CLIResponse.Failed("Operation cancelled", -1, stopwatch.Elapsed);
                }

                var output = outputBuilder.ToString().Trim();
                var error = errorBuilder.ToString().Trim();

                if (process.ExitCode == 0)
                {
                    return CLIResponse.Successful(output, stopwatch.Elapsed);
                }
                else
                {
                    var errorMsg = !string.IsNullOrEmpty(error) ? error
                        : !string.IsNullOrEmpty(output) ? output
                        : $"Process exited with code {process.ExitCode}";
                    PholusLogger.LogError($"CLI error: {errorMsg}");
                    return CLIResponse.Failed(errorMsg, process.ExitCode, stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PholusLogger.LogError($"WSL execution failed: {ex.Message}");
                return CLIResponse.Failed(ex.Message, -1, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Gets the version of a command inside WSL.
        /// Uses login shell to ensure PATH is properly loaded.
        /// </summary>
        public static string GetWSLCommandVersion(string command)
        {
            if (!IsWSLAvailable())
                return null;

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    // Use login shell for proper PATH
                    Arguments = $"bash -lc \"{command} --version\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    return output.Trim().Split('\n')[0];
                }
            }
            catch
            {
                // Command not found or failed
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Gets the version of a command.
        /// Uses platform-specific strategy for proper PATH resolution.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <returns>Version string or null.</returns>
        public static string GetVersion(string command)
        {
            return _platform.GetVersion(command);
        }

        /// <summary>
        /// Opens a terminal window with the specified command.
        /// Uses platform-specific strategy for proper terminal handling.
        /// </summary>
        /// <param name="command">Command to run in the terminal.</param>
        /// <param name="keepOpen">Keep terminal open after command completes (for installs).</param>
        public static void OpenTerminal(string command, bool keepOpen = false)
        {
            try
            {
                _platform.OpenTerminal(command, keepOpen);
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to open terminal: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs a command with input piped through stdin.
        /// </summary>
        public static async Task<CLIResponse> RunWithStdinAsync(
            string command,
            string arguments,
            string stdinContent,
            int timeoutMs = DefaultTimeoutMs,
            CancellationToken cancellationToken = default,
            System.Collections.Generic.Dictionary<string, string> environmentVariables = null)
        {
            var stopwatch = Stopwatch.StartNew();
            PholusLogger.Log($"Starting CLI: {command} {arguments}");
            PholusLogger.Log($"Stdin content length: {stdinContent?.Length ?? 0} chars");

            try
            {
                using var process = new Process();
                process.StartInfo = CreateStartInfo(command, arguments, redirectStdin: true);

                // Add custom environment variables if provided
                if (environmentVariables != null)
                {
                    foreach (var kvp in environmentVariables)
                    {
                        process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                    }
                }

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        errorBuilder.AppendLine(e.Data);
                };

                PholusLogger.Log($"Executing: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                // Enable raising events BEFORE starting to avoid race conditions
                process.EnableRaisingEvents = true;

                process.Start();
                PholusLogger.Log($"Process started (PID: {process.Id})");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                PholusLogger.Log("Output/Error streams attached, writing to stdin...");

                // Write to stdin and close it
                await process.StandardInput.WriteAsync(stdinContent);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
                PholusLogger.Log("Stdin written and closed. Waiting for AI response (30-90 seconds)...");

                // Wait with timeout and cancellation
                var completed = await WaitForExitAsync(process, timeoutMs, cancellationToken);

                stopwatch.Stop();
                PholusLogger.Log($"CLI finished in {stopwatch.Elapsed.TotalSeconds:F1}s");

                if (!completed)
                {
                    TryKillProcess(process);
                    PholusLogger.LogWarning("Process timed out!");
                    return CLIResponse.Failed("Process timed out", -1, stopwatch.Elapsed);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    TryKillProcess(process);
                    return CLIResponse.Failed("Operation cancelled", -1, stopwatch.Elapsed);
                }

                var output = outputBuilder.ToString().Trim();
                var error = errorBuilder.ToString().Trim();

                PholusLogger.Log($"Exit code: {process.ExitCode}, Output length: {output.Length}, Error length: {error.Length}");

                if (process.ExitCode == 0)
                {
                    return CLIResponse.Successful(output, stopwatch.Elapsed);
                }
                else
                {
                    var errorMsg = !string.IsNullOrEmpty(error) ? error
                        : !string.IsNullOrEmpty(output) ? output
                        : $"Process exited with code {process.ExitCode} (no output)";
                    PholusLogger.LogError($"CLI error: {errorMsg}");
                    return CLIResponse.Failed(errorMsg, process.ExitCode, stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PholusLogger.LogError($"Process execution failed: {ex.Message}\n{ex.StackTrace}");
                return CLIResponse.Failed(ex.Message, -1, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Creates ProcessStartInfo using platform-specific strategy.
        /// </summary>
        private static ProcessStartInfo CreateStartInfo(string command, string arguments, bool redirectStdin = false)
        {
            return _platform.CreateStartInfo(command, arguments, redirectStdin);
        }

        private static async Task<bool> WaitForExitAsync(
            Process process,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Note: EnableRaisingEvents must be set before Process.Start() by the caller
            process.Exited += (_, _) => tcs.TrySetResult(true);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            using var registration = cts.Token.Register(() => tcs.TrySetResult(false));

            if (process.HasExited)
            {
                return true;
            }

            return await tcs.Task;
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Best effort kill
            }
        }
    }
}
