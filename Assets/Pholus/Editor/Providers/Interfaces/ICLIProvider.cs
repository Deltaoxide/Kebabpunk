using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Providers.Interfaces
{
    /// <summary>
    /// Interface for AI CLI providers (Claude Code, Codex).
    /// Follows Interface Segregation - only prompt sending responsibility.
    /// </summary>
    public interface ICLIProvider
    {
        /// <summary>
        /// Display name of the provider (e.g., "Claude Code").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// CLI command name (e.g., "claude").
        /// </summary>
        string CLICommand { get; }

        /// <summary>
        /// Provider type enum value.
        /// </summary>
        ProviderType ProviderType { get; }

        /// <summary>
        /// Sends a prompt to the AI and returns the response.
        /// </summary>
        /// <param name="prompt">The prompt to send.</param>
        /// <param name="model">Optional model name. Empty/null uses CLI default.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The CLI response containing the AI output.</returns>
        Task<CLIResponse> SendPromptAsync(string prompt, string model = null, CancellationToken cancellationToken = default);
    }
}
