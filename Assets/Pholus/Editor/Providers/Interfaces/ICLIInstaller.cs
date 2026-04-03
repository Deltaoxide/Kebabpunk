using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pholus.Editor.Providers.Interfaces
{
    /// <summary>
    /// Result of a CLI installation attempt.
    /// </summary>
    public class InstallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }

        public static InstallResult Successful(string message = "Installation completed successfully.")
        {
            return new InstallResult { Success = true, Message = message };
        }

        public static InstallResult Failed(string error)
        {
            return new InstallResult { Success = false, Error = error };
        }
    }

    /// <summary>
    /// Interface for installing CLI tools.
    /// Follows Interface Segregation - only installation responsibility.
    /// </summary>
    public interface ICLIInstaller
    {
        /// <summary>
        /// The npm install command for this CLI.
        /// </summary>
        string InstallCommand { get; }

        /// <summary>
        /// The login command for authentication.
        /// </summary>
        string LoginCommand { get; }

        /// <summary>
        /// Checks if Node.js/npm is available (prerequisite).
        /// </summary>
        /// <returns>True if prerequisites are met.</returns>
        bool CheckPrerequisites();

        /// <summary>
        /// Gets prerequisite status message.
        /// </summary>
        string GetPrerequisiteStatus();

        /// <summary>
        /// Installs the CLI tool via npm.
        /// </summary>
        /// <param name="progress">Progress reporter (0-1).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Installation result.</returns>
        Task<InstallResult> InstallAsync(IProgress<float> progress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens terminal for user to login.
        /// </summary>
        void OpenLoginTerminal();
    }
}
