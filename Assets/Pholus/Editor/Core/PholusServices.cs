using System;
using System.Collections.Generic;
using Pholus.Editor.Providers;
using Pholus.Editor.Providers.Interfaces;
using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Simple service locator for dependency management.
    /// Unity Editor doesn't support constructor injection, so we use this pattern.
    /// Follows Dependency Inversion - components request interfaces, not implementations.
    /// </summary>
    public static class PholusServices
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();
        private static bool _initialized;

        /// <summary>
        /// Event raised when services are (re)initialized.
        /// </summary>
        public static event Action OnServicesInitialized;

        /// <summary>
        /// Whether services have been initialized.
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Initialize all services. Call this on Editor load.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                // Register core services
                var providerFactory = new ProviderFactory();
                Register<IProviderFactory>(providerFactory);

                // Register provider based on settings
                var settings = PholusSettings.Instance;
                var provider = providerFactory.CreateProvider(settings.ActiveProvider);
                var detector = providerFactory.CreateDetector(settings.ActiveProvider);

                Register<ICLIProvider>(provider);
                Register<ICLIDetector>(detector);

                // Listen for settings changes to update provider
                PholusSettings.OnSettingsChanged += OnSettingsChanged;

                _initialized = true;
                OnServicesInitialized?.Invoke();

                PholusLogger.Log("Services initialized");
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to initialize services: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Register a service instance.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Register a service factory for lazy instantiation.
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// Get a registered service.
        /// </summary>
        public static T Get<T>() where T : class
        {
            EnsureInitialized();

            var type = typeof(T);

            // Check for existing instance
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            // Check for factory
            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = (T)factory();
                _services[type] = instance;
                return instance;
            }

            throw new InvalidOperationException(
                $"Service {type.Name} is not registered. " +
                "Make sure PholusServices.Initialize() has been called.");
        }

        /// <summary>
        /// Try to get a service without throwing.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;

            if (!_initialized)
            {
                return false;
            }

            var type = typeof(T);

            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }

            if (_factories.TryGetValue(type, out var factory))
            {
                service = (T)factory();
                _services[type] = service;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all services (for testing or reinitialization).
        /// </summary>
        public static void Clear()
        {
            PholusSettings.OnSettingsChanged -= OnSettingsChanged;

            _services.Clear();
            _factories.Clear();
            _initialized = false;
        }

        /// <summary>
        /// Reinitialize services (e.g., after provider change).
        /// </summary>
        public static void Reinitialize()
        {
            Clear();
            Initialize();
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static void OnSettingsChanged(PholusSettings settings)
        {
            // Update provider when settings change
            if (TryGet<IProviderFactory>(out var factory))
            {
                try
                {
                    var provider = factory.CreateProvider(settings.ActiveProvider);
                    var detector = factory.CreateDetector(settings.ActiveProvider);

                    Register<ICLIProvider>(provider);
                    Register<ICLIDetector>(detector);

                    PholusLogger.Log($"Switched to provider: {provider.ProviderName}");
                }
                catch (Exception ex)
                {
                    PholusLogger.LogError($"Failed to switch provider: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Initializes Pholus services when Unity Editor loads.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public static class PholusServicesInitializer
    {
        static PholusServicesInitializer()
        {
            // Delay initialization to after domain reload
            UnityEditor.EditorApplication.delayCall += () =>
            {
                PholusServices.Initialize();

                // Show setup wizard on first run
                PholusSetupWizard.ShowIfFirstRun();
            };
        }
    }
}
