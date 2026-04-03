namespace Pholus.Editor.Providers.Interfaces
{
    /// <summary>
    /// Interface for detecting CLI installation and authentication status.
    /// Follows Interface Segregation - only detection responsibility.
    /// </summary>
    public interface ICLIDetector
    {
        /// <summary>
        /// Checks if the CLI tool is installed on the system.
        /// </summary>
        /// <returns>True if installed, false otherwise.</returns>
        bool IsInstalled();

        /// <summary>
        /// Checks if the user is authenticated with the CLI.
        /// </summary>
        /// <returns>True if authenticated, false otherwise.</returns>
        bool IsAuthenticated();

        /// <summary>
        /// Gets the installed CLI version.
        /// </summary>
        /// <returns>Version string, or null if not installed.</returns>
        string GetVersion();

        /// <summary>
        /// Gets a human-readable status message.
        /// </summary>
        /// <returns>Status message for display in UI.</returns>
        string GetStatusMessage();

        /// <summary>
        /// Clears the cached detection results, forcing fresh checks.
        /// </summary>
        void ClearCache();
    }
}
