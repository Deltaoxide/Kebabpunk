using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pholus.Editor.Core;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.Registry
{
    /// <summary>
    /// Registry that discovers and manages all prompt providers.
    /// Scans assemblies for classes with [PromptProvider] attribute.
    /// </summary>
    public static class PromptRegistry
    {
        private static List<IDetectionRule> _detectionRules;
        private static List<IFixProvider> _fixProviders;
        private static Dictionary<string, IDetectionRule> _rulesByType;
        private static Dictionary<string, IFixProvider> _fixProvidersByType;
        private static bool _initialized;

        /// <summary>
        /// Gets all registered detection rules, sorted by priority (highest first).
        /// </summary>
        public static IReadOnlyList<IDetectionRule> GetAllRules()
        {
            EnsureInitialized();
            return _detectionRules;
        }

        /// <summary>
        /// Gets a detection rule by its issue type.
        /// Returns the highest priority rule if multiple exist.
        /// </summary>
        public static IDetectionRule GetRule(string issueType)
        {
            EnsureInitialized();
            return _rulesByType.TryGetValue(issueType, out var rule) ? rule : null;
        }

        /// <summary>
        /// Gets fix instructions for an issue type.
        /// Checks IFixProvider first (higher priority), then falls back to IDetectionRule.
        /// </summary>
        public static string GetFixInstructions(string issueType)
        {
            EnsureInitialized();

            // First check dedicated fix providers (they can override rule fix instructions)
            if (_fixProvidersByType.TryGetValue(issueType, out var fixProvider))
            {
                return fixProvider.GetFixInstructions();
            }

            // Fall back to the rule's fix instructions
            if (_rulesByType.TryGetValue(issueType, out var rule))
            {
                return rule.FixInstructions;
            }

            // Generic fallback
            return GetGenericFixInstructions();
        }

        /// <summary>
        /// Forces a refresh of all registered providers.
        /// Call this after domain reload or when new assemblies are loaded.
        /// </summary>
        public static void Refresh()
        {
            _initialized = false;
            _detectionRules = null;
            _fixProviders = null;
            _rulesByType = null;
            _fixProvidersByType = null;
            EnsureInitialized();
        }

        /// <summary>
        /// Gets unique issue types across all rules for building category lists.
        /// </summary>
        public static IEnumerable<string> GetAllIssueTypes()
        {
            EnsureInitialized();
            return _rulesByType.Keys;
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            _detectionRules = new List<IDetectionRule>();
            _fixProviders = new List<IFixProvider>();
            _rulesByType = new Dictionary<string, IDetectionRule>(StringComparer.OrdinalIgnoreCase);
            _fixProvidersByType = new Dictionary<string, IFixProvider>(StringComparer.OrdinalIgnoreCase);

            ScanAssemblies();

            // Sort by priority descending (highest priority first)
            _detectionRules = _detectionRules.OrderByDescending(r => r.Priority).ToList();
            _fixProviders = _fixProviders.OrderByDescending(p => p.Priority).ToList();

            // Build lookup dictionaries (first entry wins due to priority sorting)
            foreach (var rule in _detectionRules)
            {
                if (!_rulesByType.ContainsKey(rule.IssueType))
                {
                    _rulesByType[rule.IssueType] = rule;
                }
            }

            foreach (var provider in _fixProviders)
            {
                if (!_fixProvidersByType.ContainsKey(provider.IssueType))
                {
                    _fixProvidersByType[provider.IssueType] = provider;
                }
            }

            _initialized = true;

            PholusLogger.Log($"PromptRegistry initialized: {_detectionRules.Count} rules, {_fixProviders.Count} fix providers");
        }

        private static void ScanAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    // Skip system and Unity assemblies for performance
                    var assemblyName = assembly.GetName().Name;
                    if (IsSystemAssembly(assemblyName)) continue;

                    try
                    {
                        ScanAssembly(assembly);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Some types couldn't be loaded, but try to scan what we can
                        foreach (var type in ex.Types.Where(t => t != null))
                        {
                            TryRegisterType(type);
                        }
                    }
                    catch (Exception ex)
                    {
                        PholusLogger.LogWarning($"Failed to scan assembly {assemblyName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"PromptRegistry scan failed: {ex.Message}");
            }
        }

        private static void ScanAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                TryRegisterType(type);
            }
        }

        private static void TryRegisterType(Type type)
        {
            if (type == null || type.IsAbstract || type.IsInterface) return;

            // Check for PromptProvider attribute
            var attribute = type.GetCustomAttribute<PromptProviderAttribute>();
            if (attribute == null) return;

            // Check if it implements our interfaces
            var isDetectionRule = typeof(IDetectionRule).IsAssignableFrom(type);
            var isFixProvider = typeof(IFixProvider).IsAssignableFrom(type);

            if (!isDetectionRule && !isFixProvider) return;

            try
            {
                var instance = Activator.CreateInstance(type);

                if (isDetectionRule && instance is IDetectionRule rule)
                {
                    _detectionRules.Add(rule);
                }

                if (isFixProvider && instance is IFixProvider provider)
                {
                    _fixProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Failed to instantiate prompt provider {type.Name}: {ex.Message}");
            }
        }

        private static bool IsSystemAssembly(string name)
        {
            return name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Mono", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetGenericFixInstructions()
        {
            return @"Fix this performance issue by:
- Caching any repeated lookups
- Moving allocations out of hot paths
- Pre-computing values where possible
- Keep exact same behavior
- Do not modify any other code";
        }
    }
}
