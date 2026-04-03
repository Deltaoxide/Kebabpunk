using System;
using System.Collections.Generic;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Factory for creating provider instances.
    /// Follows Open/Closed Principle - add new providers without modifying existing code.
    /// </summary>
    public class ProviderFactory : IProviderFactory
    {
        private readonly Dictionary<ProviderType, Func<ICLIProvider>> _providerFactories;
        private readonly Dictionary<ProviderType, Func<ICLIDetector>> _detectorFactories;
        private readonly Dictionary<ProviderType, Func<ICLIInstaller>> _installerFactories;

        // Cached instances (providers are stateless, safe to reuse)
        private readonly Dictionary<ProviderType, ICLIProvider> _providerCache = new();
        private readonly Dictionary<ProviderType, ICLIDetector> _detectorCache = new();

        public ProviderFactory()
        {
            _providerFactories = new Dictionary<ProviderType, Func<ICLIProvider>>
            {
                { ProviderType.Claude, () => new ClaudeProvider() },
                { ProviderType.Codex, () => new CodexProvider() },
                { ProviderType.Gemini, () => new GeminiProvider() },
                { ProviderType.Cursor, () => new CursorProvider() },
                { ProviderType.OpenRouter, () => new OpenRouterProvider() },
            };

            _detectorFactories = new Dictionary<ProviderType, Func<ICLIDetector>>
            {
                { ProviderType.Claude, () => new ClaudeProvider() },
                { ProviderType.Codex, () => new CodexProvider() },
                { ProviderType.Gemini, () => new GeminiProvider() },
                { ProviderType.Cursor, () => new CursorProvider() },
                { ProviderType.OpenRouter, () => new OpenRouterProvider() },
            };

            _installerFactories = new Dictionary<ProviderType, Func<ICLIInstaller>>
            {
                { ProviderType.Claude, () => new CLIInstaller(ProviderType.Claude) },
                { ProviderType.Codex, () => new CLIInstaller(ProviderType.Codex) },
                { ProviderType.Gemini, () => new CLIInstaller(ProviderType.Gemini) },
                { ProviderType.Cursor, () => new CLIInstaller(ProviderType.Cursor) },
                { ProviderType.OpenRouter, () => new CLIInstaller(ProviderType.OpenRouter) },
            };
        }

        public ProviderType[] AvailableProviders => new[]
        {
            ProviderType.Claude,
            ProviderType.Codex,
            ProviderType.Gemini,
            ProviderType.Cursor,
            ProviderType.OpenRouter,
        };

        public ICLIProvider CreateProvider(ProviderType type)
        {
            if (_providerCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            if (!_providerFactories.TryGetValue(type, out var factory))
            {
                throw new NotSupportedException($"Provider type {type} is not supported yet.");
            }

            var provider = factory();
            _providerCache[type] = provider;
            return provider;
        }

        public ICLIDetector CreateDetector(ProviderType type)
        {
            if (_detectorCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            if (!_detectorFactories.TryGetValue(type, out var factory))
            {
                throw new NotSupportedException($"Detector for {type} is not supported yet.");
            }

            var detector = factory();
            _detectorCache[type] = detector;
            return detector;
        }

        public ICLIInstaller CreateInstaller(ProviderType type)
        {
            // Installers are not cached - they may hold state during installation
            if (!_installerFactories.TryGetValue(type, out var factory))
            {
                throw new NotSupportedException($"Installer for {type} is not supported.");
            }

            return factory();
        }

        /// <summary>
        /// Clears cached instances (useful for testing or when settings change).
        /// </summary>
        public void ClearCache()
        {
            _providerCache.Clear();
            _detectorCache.Clear();
        }
    }
}
