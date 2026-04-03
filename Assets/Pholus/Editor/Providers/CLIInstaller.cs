using System;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Handles installation of CLI tools via npm.
    /// Single Responsibility: Only handles installation.
    /// </summary>
    public class CLIInstaller : ICLIInstaller
    {
        private readonly ProviderType _providerType;

        public CLIInstaller(ProviderType providerType)
        {
            _providerType = providerType;
        }

        public string InstallCommand => _providerType switch
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            // Mac/Linux need sudo for global npm installs
            ProviderType.Claude => "sudo npm install -g @anthropic-ai/claude-code",
            ProviderType.Codex => "sudo npm install -g @openai/codex",
            ProviderType.Gemini => "sudo npm install -g @google/gemini-cli",
            ProviderType.Cursor => "curl https://cursor.com/install -fsSL | bash",
            ProviderType.OpenRouter => "sudo npm install -g @letuscode/openrouter-cli",
#else
            // Windows doesn't need sudo
            ProviderType.Claude => "npm install -g @anthropic-ai/claude-code",
            ProviderType.Codex => "npm install -g @openai/codex",
            ProviderType.Gemini => "npm install -g @google/gemini-cli",
            ProviderType.Cursor => "wsl -e bash -c \"curl https://cursor.com/install -fsSL | bash\"",
            ProviderType.OpenRouter => "npm install -g @letuscode/openrouter-cli",
#endif
            _ => throw new ArgumentOutOfRangeException()
        };

        public string LoginCommand => _providerType switch
        {
            ProviderType.Claude => "claude /login",
            ProviderType.Codex => "codex",
            ProviderType.Gemini => "gemini",  // Opens interactive login
#if UNITY_EDITOR_WIN
            ProviderType.Cursor => "wsl bash -lc \"cursor-agent login\"",
#else
            // Mac/Linux: Add ~/.local/bin to PATH for cursor-agent
            ProviderType.Cursor => "export PATH=\"$HOME/.local/bin:$PATH\" && cursor-agent login",
#endif
            ProviderType.OpenRouter => "openrouter config --api-key",  // User pastes API key from openrouter.ai/keys
            _ => throw new ArgumentOutOfRangeException()
        };

        public bool CheckPrerequisites()
        {
            // Cursor Agent CLI uses WSL on Windows, curl on Linux/Mac
            if (_providerType == ProviderType.Cursor)
            {
                if (CursorProvider.IsWindowsPlatform)
                {
                    return ProcessRunner.IsWSLAvailable();
                }
                return ProcessRunner.CommandExists("curl");
            }

            // Check for Node.js and npm for other providers
            if (!ProcessRunner.CommandExists("node") || !ProcessRunner.CommandExists("npm"))
            {
                return false;
            }

            // Check Node version compatibility (CLIs require Node 20+)
            var nodeVersion = GetNodeMajorVersion();
            if (nodeVersion > 0)
            {
                // Node 20 (LTS) and 22+ are supported
                // Node 18 has ESM compatibility issues with some CLIs
                // Odd versions like 19, 21 are not LTS
                if (nodeVersion >= 20)
                {
                    return true;
                }
                // Incompatible version - fail the check
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the major version number of Node.js (e.g., 20 for v20.11.0)
        /// </summary>
        private int GetNodeMajorVersion()
        {
            try
            {
                var versionStr = ProcessRunner.GetVersion("node");
                if (string.IsNullOrEmpty(versionStr)) return 0;

                // Version format: v20.11.0 or 20.11.0
                versionStr = versionStr.TrimStart('v', 'V');
                var dotIndex = versionStr.IndexOf('.');
                if (dotIndex > 0)
                {
                    versionStr = versionStr.Substring(0, dotIndex);
                }
                return int.TryParse(versionStr, out var major) ? major : 0;
            }
            catch
            {
                return 0;
            }
        }

        public string GetPrerequisiteStatus()
        {
            // Cursor Agent CLI uses WSL on Windows
            if (_providerType == ProviderType.Cursor)
            {
                if (CursorProvider.IsWindowsPlatform)
                {
                    if (!ProcessRunner.IsWSLAvailable())
                    {
                        return "WSL not installed. Run: wsl --install";
                    }
                    return "WSL available";
                }
                var hasCurl = ProcessRunner.CommandExists("curl");
                return hasCurl ? "curl available" : "curl is not installed";
            }

            var hasNode = ProcessRunner.CommandExists("node");
            var hasNpm = ProcessRunner.CommandExists("npm");

            if (hasNode && hasNpm)
            {
                var nodeVersion = ProcessRunner.GetVersion("node") ?? "unknown";
                var npmVersion = ProcessRunner.GetVersion("npm") ?? "unknown";

                // Check for version compatibility
                var majorVersion = GetNodeMajorVersion();
                if (majorVersion > 0 && majorVersion < 20)
                {
                    // Node 18 and below have ESM compatibility issues
                    return $"Node.js {nodeVersion} (⚠ Requires v20+, use: nvm use 22)";
                }

                return $"Node.js {nodeVersion}, npm {npmVersion}";
            }

            if (!hasNode && !hasNpm)
            {
                return "Node.js is not installed. Please install from nodejs.org";
            }

            if (!hasNode)
            {
                return "Node.js is not installed";
            }

            return "npm is not installed";
        }

        public async Task<InstallResult> InstallAsync(
            IProgress<float> progress,
            CancellationToken cancellationToken = default)
        {
            if (!CheckPrerequisites())
            {
                return InstallResult.Failed(GetPrerequisiteStatus());
            }

            progress?.Report(0.1f);

            try
            {
                if (_providerType == ProviderType.Cursor)
                {
                    // Cursor uses curl/bash for installation
                    // On Windows, runs through WSL - keep terminal open to see output
                    ProcessRunner.OpenTerminal(InstallCommand, keepOpen: true);
                    progress?.Report(1.0f);
                    var msg = CursorProvider.IsWindowsPlatform
                        ? "Installation started in WSL terminal. Keep the window open until complete."
                        : "Please complete the installation in the opened terminal.";
                    return InstallResult.Successful(msg);
                }

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                // On Mac/Linux, open terminal for npm install so user can enter sudo password if needed
                ProcessRunner.OpenTerminal(InstallCommand, keepOpen: true);
                progress?.Report(1.0f);
                await Task.CompletedTask; // Satisfy async requirement
                return InstallResult.Successful("Installation started in Terminal. Enter your password if prompted, then return here when complete.");
#else
                // On Windows, run npm install in background (no sudo needed)
                var response = await ProcessRunner.RunAsync(
                    "npm",
                    GetInstallArguments(),
                    timeoutMs: 300000, // 5 minutes for install
                    cancellationToken: cancellationToken);

                progress?.Report(0.9f);

                if (response.Success)
                {
                    progress?.Report(1.0f);
                    return InstallResult.Successful(GetSuccessMessage());
                }
                else
                {
                    return InstallResult.Failed(response.Error ?? "Installation failed");
                }
#endif
            }
            catch (OperationCanceledException)
            {
                return InstallResult.Failed("Installation was cancelled");
            }
            catch (Exception ex)
            {
                return InstallResult.Failed($"Installation error: {ex.Message}");
            }
        }

        public void OpenLoginTerminal()
        {
            // Keep terminal open for Cursor (WSL) so user can complete login
            var keepOpen = _providerType == ProviderType.Cursor && CursorProvider.IsWindowsPlatform;
            ProcessRunner.OpenTerminal(LoginCommand, keepOpen);
        }

        private string GetInstallArguments()
        {
            return _providerType switch
            {
                ProviderType.Claude => "install -g @anthropic-ai/claude-code",
                ProviderType.Codex => "install -g @openai/codex",
                ProviderType.Gemini => "install -g @google/gemini-cli",
                ProviderType.Cursor => "", // Cursor uses curl, not npm
                ProviderType.OpenRouter => "install -g @letuscode/openrouter-cli",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private string GetSuccessMessage()
        {
            return _providerType switch
            {
                ProviderType.Claude => "Claude Code CLI installed successfully!",
                ProviderType.Codex => "OpenAI Codex CLI installed successfully!",
                ProviderType.Gemini => "Google Gemini CLI installed successfully!",
                ProviderType.Cursor => "Cursor CLI installed successfully!",
                ProviderType.OpenRouter => "OpenRouter CLI installed! Get your API key from openrouter.ai/keys",
                _ => "CLI installed successfully!"
            };
        }

        /// <summary>
        /// Whether this provider uses npm for installation.
        /// </summary>
        public bool UsesNpmInstall => _providerType != ProviderType.Cursor;
    }
}
