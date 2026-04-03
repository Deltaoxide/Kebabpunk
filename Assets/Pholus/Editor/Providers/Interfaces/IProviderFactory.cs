using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Providers.Interfaces
{
    /// <summary>
    /// Factory for creating provider instances.
    /// Follows Dependency Inversion - clients depend on this abstraction.
    /// </summary>
    public interface IProviderFactory
    {
        /// <summary>
        /// Creates a CLI provider for the specified type.
        /// </summary>
        /// <param name="type">The provider type.</param>
        /// <returns>A CLI provider instance.</returns>
        ICLIProvider CreateProvider(ProviderType type);

        /// <summary>
        /// Creates a CLI detector for the specified type.
        /// </summary>
        /// <param name="type">The provider type.</param>
        /// <returns>A CLI detector instance.</returns>
        ICLIDetector CreateDetector(ProviderType type);

        /// <summary>
        /// Creates a CLI installer for the specified type.
        /// </summary>
        /// <param name="type">The provider type.</param>
        /// <returns>A CLI installer instance.</returns>
        ICLIInstaller CreateInstaller(ProviderType type);

        /// <summary>
        /// Gets all available provider types.
        /// </summary>
        ProviderType[] AvailableProviders { get; }
    }
}
