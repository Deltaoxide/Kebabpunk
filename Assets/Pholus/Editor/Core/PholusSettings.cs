using System;
using System.Collections.Generic;
using System.Linq;
using Pholus.Editor.Analysis.Models;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Persistent settings for Pholus.
    /// Stored in EditorPrefs for persistence across sessions.
    /// </summary>
    [Serializable]
    public class PholusSettings
    {
        public const string PrefsKeyPrefix = "Pholus_";

        // Keys
        private const string ActiveProviderKey = PrefsKeyPrefix + "ActiveProvider";
        private const string TargetPlatformKey = PrefsKeyPrefix + "TargetPlatform";
        private const string ShowWhyMightBeWrongKey = PrefsKeyPrefix + "ShowWhyMightBeWrong";
        private const string GroupByCertaintyKey = PrefsKeyPrefix + "GroupByCertainty";
        private const string CreateBackupBeforeFixKey = PrefsKeyPrefix + "CreateBackupBeforeFix";
        private const string MaxBackupsToKeepKey = PrefsKeyPrefix + "MaxBackupsToKeep";
        private const string AutoApplyFixesKey = PrefsKeyPrefix + "AutoApplyFixes";
        private const string FirstRunCompleteKey = PrefsKeyPrefix + "FirstRunComplete";

        // Consensus Mode Keys
        private const string EnableConsensusModeKey = PrefsKeyPrefix + "EnableConsensusMode";
        private const string ConsensusAnalyzersKey = PrefsKeyPrefix + "ConsensusAnalyzers";
        private const string ConsensusDirectorKey = PrefsKeyPrefix + "ConsensusDirector";
        private const string ShowProviderBreakdownKey = PrefsKeyPrefix + "ShowProviderBreakdown";

        // Model Selection Keys (per-provider)
        private const string ClaudeSelectedModelKey = PrefsKeyPrefix + "ClaudeSelectedModel";
        private const string CodexSelectedModelKey = PrefsKeyPrefix + "CodexSelectedModel";
        private const string GeminiSelectedModelKey = PrefsKeyPrefix + "GeminiSelectedModel";
        private const string CursorSelectedModelKey = PrefsKeyPrefix + "CursorSelectedModel";
        private const string OpenRouterSelectedModelKey = PrefsKeyPrefix + "OpenRouterSelectedModel";
        private const string ClaudeDiscoveredModelsKey = PrefsKeyPrefix + "ClaudeDiscoveredModels";
        private const string CodexDiscoveredModelsKey = PrefsKeyPrefix + "CodexDiscoveredModels";
        private const string GeminiDiscoveredModelsKey = PrefsKeyPrefix + "GeminiDiscoveredModels";
        private const string CursorDiscoveredModelsKey = PrefsKeyPrefix + "CursorDiscoveredModels";
        private const string OpenRouterDiscoveredModelsKey = PrefsKeyPrefix + "OpenRouterDiscoveredModels";
        private const string ClaudeCustomModelsKey = PrefsKeyPrefix + "ClaudeCustomModels";
        private const string CodexCustomModelsKey = PrefsKeyPrefix + "CodexCustomModels";
        private const string GeminiCustomModelsKey = PrefsKeyPrefix + "GeminiCustomModels";
        private const string CursorCustomModelsKey = PrefsKeyPrefix + "CursorCustomModels";
        private const string OpenRouterCustomModelsKey = PrefsKeyPrefix + "OpenRouterCustomModels";
        private const string OpenRouterApiKeyKey = PrefsKeyPrefix + "OpenRouterApiKey";

        // Logging Keys
        private const string LogDebugMessagesKey = PrefsKeyPrefix + "LogDebugMessages";
        private const string LogWarningMessagesKey = PrefsKeyPrefix + "LogWarningMessages";
        private const string LogErrorMessagesKey = PrefsKeyPrefix + "LogErrorMessages";

        /// <summary>
        /// Event raised when settings are changed.
        /// </summary>
        public static event Action<PholusSettings> OnSettingsChanged;

        private static PholusSettings _instance;

        /// <summary>
        /// Singleton instance of settings.
        /// </summary>
        public static PholusSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        #region Properties

        /// <summary>
        /// Currently active AI provider.
        /// </summary>
        public ProviderType ActiveProvider
        {
            get => (ProviderType)EditorPrefs.GetInt(ActiveProviderKey, (int)ProviderType.Claude);
            set
            {
                if (ActiveProvider == value) return;
                EditorPrefs.SetInt(ActiveProviderKey, (int)value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Target platform for analysis thresholds.
        /// </summary>
        public TargetPlatform TargetPlatform
        {
            get => (TargetPlatform)EditorPrefs.GetInt(TargetPlatformKey, (int)TargetPlatform.Mobile);
            set
            {
                if (TargetPlatform == value) return;
                EditorPrefs.SetInt(TargetPlatformKey, (int)value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to show "why I might be wrong" explanations.
        /// </summary>
        public bool ShowWhyMightBeWrong
        {
            get => EditorPrefs.GetBool(ShowWhyMightBeWrongKey, true);
            set
            {
                if (ShowWhyMightBeWrong == value) return;
                EditorPrefs.SetBool(ShowWhyMightBeWrongKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to group issues by certainty category.
        /// </summary>
        public bool GroupByCertainty
        {
            get => EditorPrefs.GetBool(GroupByCertaintyKey, true);
            set
            {
                if (GroupByCertainty == value) return;
                EditorPrefs.SetBool(GroupByCertaintyKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to create backup before applying fixes.
        /// </summary>
        public bool CreateBackupBeforeFix
        {
            get => EditorPrefs.GetBool(CreateBackupBeforeFixKey, true);
            set
            {
                if (CreateBackupBeforeFix == value) return;
                EditorPrefs.SetBool(CreateBackupBeforeFixKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Maximum number of backups to keep per file.
        /// </summary>
        public int MaxBackupsToKeep
        {
            get => EditorPrefs.GetInt(MaxBackupsToKeepKey, 10);
            set
            {
                var clamped = Mathf.Clamp(value, 1, 100);
                if (MaxBackupsToKeep == clamped) return;
                EditorPrefs.SetInt(MaxBackupsToKeepKey, clamped);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to automatically apply fixes without showing diff preview.
        /// </summary>
        public bool AutoApplyFixes
        {
            get => EditorPrefs.GetBool(AutoApplyFixesKey, false);
            set
            {
                if (AutoApplyFixes == value) return;
                EditorPrefs.SetBool(AutoApplyFixesKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether the first-run setup wizard has been completed.
        /// </summary>
        public bool FirstRunComplete
        {
            get => EditorPrefs.GetBool(FirstRunCompleteKey, false);
            set => EditorPrefs.SetBool(FirstRunCompleteKey, value);
        }

        #region Logging Settings

        /// <summary>
        /// Whether to log debug messages to the Unity console.
        /// </summary>
        public bool LogDebugMessages
        {
            get => EditorPrefs.GetBool(LogDebugMessagesKey, true);
            set
            {
                if (LogDebugMessages == value) return;
                EditorPrefs.SetBool(LogDebugMessagesKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to log warning messages to the Unity console.
        /// </summary>
        public bool LogWarningMessages
        {
            get => EditorPrefs.GetBool(LogWarningMessagesKey, true);
            set
            {
                if (LogWarningMessages == value) return;
                EditorPrefs.SetBool(LogWarningMessagesKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to log error messages to the Unity console.
        /// </summary>
        public bool LogErrorMessages
        {
            get => EditorPrefs.GetBool(LogErrorMessagesKey, true);
            set
            {
                if (LogErrorMessages == value) return;
                EditorPrefs.SetBool(LogErrorMessagesKey, value);
                NotifyChanged();
            }
        }

        #endregion

        #region Consensus Mode Settings

        /// <summary>
        /// Whether multi-provider consensus mode is enabled.
        /// </summary>
        public bool EnableConsensusMode
        {
            get => EditorPrefs.GetBool(EnableConsensusModeKey, false);
            set
            {
                if (EnableConsensusMode == value) return;
                EditorPrefs.SetBool(EnableConsensusModeKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// List of providers to use as analyzers in consensus mode.
        /// Stored as comma-separated integers in EditorPrefs.
        /// </summary>
        public List<ProviderType> ConsensusAnalyzers
        {
            get
            {
                var stored = EditorPrefs.GetString(ConsensusAnalyzersKey, "");
                if (string.IsNullOrEmpty(stored))
                {
                    // Default: Claude and Codex
                    return new List<ProviderType> { ProviderType.Claude, ProviderType.Codex };
                }

                return stored.Split(',')
                    .Where(s => int.TryParse(s, out _))
                    .Select(s => (ProviderType)int.Parse(s))
                    .ToList();
            }
            set
            {
                var serialized = string.Join(",", value.Select(p => ((int)p).ToString()));
                EditorPrefs.SetString(ConsensusAnalyzersKey, serialized);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Provider to use as the director/judge in consensus mode.
        /// </summary>
        public ProviderType ConsensusDirector
        {
            get => (ProviderType)EditorPrefs.GetInt(ConsensusDirectorKey, (int)ProviderType.Claude);
            set
            {
                if (ConsensusDirector == value) return;
                EditorPrefs.SetInt(ConsensusDirectorKey, (int)value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Whether to show provider breakdown (debate) in consensus results.
        /// </summary>
        public bool ShowProviderBreakdown
        {
            get => EditorPrefs.GetBool(ShowProviderBreakdownKey, true);
            set
            {
                if (ShowProviderBreakdown == value) return;
                EditorPrefs.SetBool(ShowProviderBreakdownKey, value);
                NotifyChanged();
            }
        }

        #endregion

        #region Model Selection Settings

        /// <summary>
        /// Currently selected model for Claude provider.
        /// Empty string means use CLI default.
        /// </summary>
        public string ClaudeSelectedModel
        {
            get => EditorPrefs.GetString(ClaudeSelectedModelKey, "");
            set
            {
                if (ClaudeSelectedModel == value) return;
                EditorPrefs.SetString(ClaudeSelectedModelKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Currently selected model for Codex provider.
        /// </summary>
        public string CodexSelectedModel
        {
            get => EditorPrefs.GetString(CodexSelectedModelKey, "");
            set
            {
                if (CodexSelectedModel == value) return;
                EditorPrefs.SetString(CodexSelectedModelKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Currently selected model for Gemini provider.
        /// </summary>
        public string GeminiSelectedModel
        {
            get => EditorPrefs.GetString(GeminiSelectedModelKey, "");
            set
            {
                if (GeminiSelectedModel == value) return;
                EditorPrefs.SetString(GeminiSelectedModelKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Currently selected model for Cursor provider.
        /// </summary>
        public string CursorSelectedModel
        {
            get => EditorPrefs.GetString(CursorSelectedModelKey, "");
            set
            {
                if (CursorSelectedModel == value) return;
                EditorPrefs.SetString(CursorSelectedModelKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Currently selected model for OpenRouter provider.
        /// Syncs with the CLI config file.
        /// </summary>
        public string OpenRouterSelectedModel
        {
            get
            {
                var stored = EditorPrefs.GetString(OpenRouterSelectedModelKey, "");
                // If no stored value, check the CLI config file
                if (string.IsNullOrEmpty(stored))
                {
                    var configModel = Providers.OpenRouterProvider.GetModelFromConfig();
                    if (!string.IsNullOrEmpty(configModel))
                    {
                        // Sync to EditorPrefs
                        EditorPrefs.SetString(OpenRouterSelectedModelKey, configModel);
                        return configModel;
                    }
                }
                return stored;
            }
            set
            {
                if (OpenRouterSelectedModel == value) return;
                EditorPrefs.SetString(OpenRouterSelectedModelKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// OpenRouter API key (from openrouter.ai/keys).
        /// </summary>
        public string OpenRouterApiKey
        {
            get => EditorPrefs.GetString(OpenRouterApiKeyKey, "");
            set
            {
                if (OpenRouterApiKey == value) return;
                EditorPrefs.SetString(OpenRouterApiKeyKey, value);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Models discovered from Claude CLI.
        /// </summary>
        public List<string> ClaudeDiscoveredModels
        {
            get => ParseModelList(EditorPrefs.GetString(ClaudeDiscoveredModelsKey, ""));
            set => EditorPrefs.SetString(ClaudeDiscoveredModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Models discovered from Codex CLI.
        /// </summary>
        public List<string> CodexDiscoveredModels
        {
            get => ParseModelList(EditorPrefs.GetString(CodexDiscoveredModelsKey, ""));
            set => EditorPrefs.SetString(CodexDiscoveredModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Models discovered from Gemini CLI.
        /// </summary>
        public List<string> GeminiDiscoveredModels
        {
            get => ParseModelList(EditorPrefs.GetString(GeminiDiscoveredModelsKey, ""));
            set => EditorPrefs.SetString(GeminiDiscoveredModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Models discovered from Cursor CLI.
        /// </summary>
        public List<string> CursorDiscoveredModels
        {
            get => ParseModelList(EditorPrefs.GetString(CursorDiscoveredModelsKey, ""));
            set => EditorPrefs.SetString(CursorDiscoveredModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Models discovered from OpenRouter CLI.
        /// </summary>
        public List<string> OpenRouterDiscoveredModels
        {
            get => ParseModelList(EditorPrefs.GetString(OpenRouterDiscoveredModelsKey, ""));
            set => EditorPrefs.SetString(OpenRouterDiscoveredModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Custom models added by user for Claude.
        /// </summary>
        public List<string> ClaudeCustomModels
        {
            get => ParseModelList(EditorPrefs.GetString(ClaudeCustomModelsKey, ""));
            set => EditorPrefs.SetString(ClaudeCustomModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Custom models added by user for Codex.
        /// </summary>
        public List<string> CodexCustomModels
        {
            get => ParseModelList(EditorPrefs.GetString(CodexCustomModelsKey, ""));
            set => EditorPrefs.SetString(CodexCustomModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Custom models added by user for Gemini.
        /// </summary>
        public List<string> GeminiCustomModels
        {
            get => ParseModelList(EditorPrefs.GetString(GeminiCustomModelsKey, ""));
            set => EditorPrefs.SetString(GeminiCustomModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Custom models added by user for Cursor.
        /// </summary>
        public List<string> CursorCustomModels
        {
            get => ParseModelList(EditorPrefs.GetString(CursorCustomModelsKey, ""));
            set => EditorPrefs.SetString(CursorCustomModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Custom models added by user for OpenRouter.
        /// </summary>
        public List<string> OpenRouterCustomModels
        {
            get => ParseModelList(EditorPrefs.GetString(OpenRouterCustomModelsKey, ""));
            set => EditorPrefs.SetString(OpenRouterCustomModelsKey, string.Join(",", value));
        }

        /// <summary>
        /// Gets the selected model for the specified provider.
        /// Returns the provider's default model if none is selected.
        /// </summary>
        public string GetSelectedModel(ProviderType provider)
        {
            var selected = provider switch
            {
                ProviderType.Claude => ClaudeSelectedModel,
                ProviderType.Codex => CodexSelectedModel,
                ProviderType.Gemini => GeminiSelectedModel,
                ProviderType.Cursor => CursorSelectedModel,
                ProviderType.OpenRouter => OpenRouterSelectedModel,
                _ => ""
            };

            // Validate: if selected model is not in available list, reset to default
            if (!string.IsNullOrEmpty(selected))
            {
                var available = GetAvailableModels(provider);
                if (!available.Contains(selected))
                {
                    // Invalid model - reset to default
                    SetSelectedModel(provider, "");
                    selected = "";
                }
            }

            // If no model selected, return provider's default (e.g., Cursor needs "auto")
            if (string.IsNullOrEmpty(selected))
            {
                return GetDefaultModel(provider);
            }

            return selected;
        }

        /// <summary>
        /// Sets the selected model for the specified provider.
        /// </summary>
        public void SetSelectedModel(ProviderType provider, string model)
        {
            switch (provider)
            {
                case ProviderType.Claude:
                    ClaudeSelectedModel = model;
                    break;
                case ProviderType.Codex:
                    CodexSelectedModel = model;
                    break;
                case ProviderType.Gemini:
                    GeminiSelectedModel = model;
                    break;
                case ProviderType.Cursor:
                    CursorSelectedModel = model;
                    break;
                case ProviderType.OpenRouter:
                    OpenRouterSelectedModel = model;
                    // Also update the CLI config file so the CLI uses this model
                    Providers.OpenRouterProvider.SetModelInConfig(model);
                    break;
            }
        }

        /// <summary>
        /// Gets all available models for a provider (discovered + custom + defaults as fallback).
        /// </summary>
        public List<string> GetAvailableModels(ProviderType provider)
        {
            var discovered = GetDiscoveredModels(provider);
            var custom = GetCustomModels(provider);

            // Start with CLI Default option (always works)
            var models = new List<string> { "" };

            // Only show discovered models (no hardcoded defaults that may be outdated)
            if (discovered.Count > 0)
            {
                models.AddRange(discovered);
            }

            // Add custom models that aren't already in the list
            foreach (var customModel in custom)
            {
                if (!models.Contains(customModel))
                {
                    models.Add(customModel);
                }
            }

            return models;
        }

        /// <summary>
        /// Returns true if models have been discovered for this provider.
        /// </summary>
        public bool HasDiscoveredModels(ProviderType provider)
        {
            return GetDiscoveredModels(provider).Count > 0;
        }

        /// <summary>
        /// Gets discovered models for the specified provider.
        /// </summary>
        public List<string> GetDiscoveredModels(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.Claude => ClaudeDiscoveredModels,
                ProviderType.Codex => CodexDiscoveredModels,
                ProviderType.Gemini => GeminiDiscoveredModels,
                ProviderType.Cursor => CursorDiscoveredModels,
                ProviderType.OpenRouter => OpenRouterDiscoveredModels,
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Sets discovered models for the specified provider.
        /// </summary>
        public void SetDiscoveredModels(ProviderType provider, List<string> models)
        {
            switch (provider)
            {
                case ProviderType.Claude:
                    ClaudeDiscoveredModels = models;
                    break;
                case ProviderType.Codex:
                    CodexDiscoveredModels = models;
                    break;
                case ProviderType.Gemini:
                    GeminiDiscoveredModels = models;
                    break;
                case ProviderType.Cursor:
                    CursorDiscoveredModels = models;
                    break;
                case ProviderType.OpenRouter:
                    OpenRouterDiscoveredModels = models;
                    break;
            }
            NotifyChanged();
        }

        /// <summary>
        /// Gets custom models for the specified provider.
        /// </summary>
        public List<string> GetCustomModels(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.Claude => ClaudeCustomModels,
                ProviderType.Codex => CodexCustomModels,
                ProviderType.Gemini => GeminiCustomModels,
                ProviderType.Cursor => CursorCustomModels,
                ProviderType.OpenRouter => OpenRouterCustomModels,
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Adds a custom model for the specified provider.
        /// </summary>
        public void AddCustomModel(ProviderType provider, string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return;

            var custom = GetCustomModels(provider);
            if (!custom.Contains(model))
            {
                custom.Add(model);
                SetCustomModels(provider, custom);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Removes a custom model for the specified provider.
        /// </summary>
        public void RemoveCustomModel(ProviderType provider, string model)
        {
            var custom = GetCustomModels(provider);
            if (custom.Remove(model))
            {
                SetCustomModels(provider, custom);

                // If removed model was selected, reset to default
                if (GetSelectedModel(provider) == model)
                {
                    SetSelectedModel(provider, "");
                }
                NotifyChanged();
            }
        }

        /// <summary>
        /// Checks if a model is a custom model (user-added).
        /// </summary>
        public bool IsCustomModel(ProviderType provider, string model)
        {
            if (string.IsNullOrEmpty(model)) return false;
            return GetCustomModels(provider).Contains(model);
        }

        private void SetCustomModels(ProviderType provider, List<string> models)
        {
            switch (provider)
            {
                case ProviderType.Claude:
                    ClaudeCustomModels = models;
                    break;
                case ProviderType.Codex:
                    CodexCustomModels = models;
                    break;
                case ProviderType.Gemini:
                    GeminiCustomModels = models;
                    break;
                case ProviderType.Cursor:
                    CursorCustomModels = models;
                    break;
                case ProviderType.OpenRouter:
                    OpenRouterCustomModels = models;
                    break;
            }
        }

        private static List<string> ParseModelList(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return new List<string>();

            return stored.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Creates an AnalysisContext from current settings.
        /// </summary>
        public AnalysisContext CreateAnalysisContext()
        {
            return new AnalysisContext
            {
                Platform = TargetPlatform,
                ShowWhyMightBeWrong = ShowWhyMightBeWrong,
                GroupByCertainty = GroupByCertainty
            };
        }

        /// <summary>
        /// Resets all settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(ActiveProviderKey);
            EditorPrefs.DeleteKey(TargetPlatformKey);
            EditorPrefs.DeleteKey(ShowWhyMightBeWrongKey);
            EditorPrefs.DeleteKey(GroupByCertaintyKey);
            EditorPrefs.DeleteKey(CreateBackupBeforeFixKey);
            EditorPrefs.DeleteKey(MaxBackupsToKeepKey);
            EditorPrefs.DeleteKey(AutoApplyFixesKey);

            // Consensus Mode
            EditorPrefs.DeleteKey(EnableConsensusModeKey);
            EditorPrefs.DeleteKey(ConsensusAnalyzersKey);
            EditorPrefs.DeleteKey(ConsensusDirectorKey);
            EditorPrefs.DeleteKey(ShowProviderBreakdownKey);

            // Model Selection
            EditorPrefs.DeleteKey(ClaudeSelectedModelKey);
            EditorPrefs.DeleteKey(CodexSelectedModelKey);
            EditorPrefs.DeleteKey(GeminiSelectedModelKey);
            EditorPrefs.DeleteKey(CursorSelectedModelKey);
            EditorPrefs.DeleteKey(OpenRouterSelectedModelKey);
            EditorPrefs.DeleteKey(ClaudeDiscoveredModelsKey);
            EditorPrefs.DeleteKey(CodexDiscoveredModelsKey);
            EditorPrefs.DeleteKey(GeminiDiscoveredModelsKey);
            EditorPrefs.DeleteKey(CursorDiscoveredModelsKey);
            EditorPrefs.DeleteKey(OpenRouterDiscoveredModelsKey);
            EditorPrefs.DeleteKey(ClaudeCustomModelsKey);
            EditorPrefs.DeleteKey(CodexCustomModelsKey);
            EditorPrefs.DeleteKey(GeminiCustomModelsKey);
            EditorPrefs.DeleteKey(CursorCustomModelsKey);
            EditorPrefs.DeleteKey(OpenRouterCustomModelsKey);
            EditorPrefs.DeleteKey(OpenRouterApiKeyKey);

            // Logging
            EditorPrefs.DeleteKey(LogDebugMessagesKey);
            EditorPrefs.DeleteKey(LogWarningMessagesKey);
            EditorPrefs.DeleteKey(LogErrorMessagesKey);

            NotifyChanged();
        }

        /// <summary>
        /// Clears only the model discovery cache and selected models.
        /// </summary>
        public void ClearModelCache()
        {
            // Clear selected models
            EditorPrefs.DeleteKey(ClaudeSelectedModelKey);
            EditorPrefs.DeleteKey(CodexSelectedModelKey);
            EditorPrefs.DeleteKey(GeminiSelectedModelKey);
            EditorPrefs.DeleteKey(CursorSelectedModelKey);
            EditorPrefs.DeleteKey(OpenRouterSelectedModelKey);

            // Clear discovered models
            EditorPrefs.DeleteKey(ClaudeDiscoveredModelsKey);
            EditorPrefs.DeleteKey(CodexDiscoveredModelsKey);
            EditorPrefs.DeleteKey(GeminiDiscoveredModelsKey);
            EditorPrefs.DeleteKey(CursorDiscoveredModelsKey);
            EditorPrefs.DeleteKey(OpenRouterDiscoveredModelsKey);

            // Clear custom models
            EditorPrefs.DeleteKey(ClaudeCustomModelsKey);
            EditorPrefs.DeleteKey(CodexCustomModelsKey);
            EditorPrefs.DeleteKey(GeminiCustomModelsKey);
            EditorPrefs.DeleteKey(CursorCustomModelsKey);
            EditorPrefs.DeleteKey(OpenRouterCustomModelsKey);

            // Note: OpenRouterApiKey is NOT cleared here - user must re-enter it manually

            NotifyChanged();
        }

        private static PholusSettings Load()
        {
            return new PholusSettings();
        }

        private void NotifyChanged()
        {
            OnSettingsChanged?.Invoke(this);
        }

        /// <summary>
        /// Gets the default model for a provider when no model is selected or after reset.
        /// Some providers like Cursor require a model to be specified.
        /// </summary>
        public static string GetDefaultModel(ProviderType provider)
        {
            return provider switch
            {
                ProviderType.Cursor => "auto",  // Cursor requires a model, "auto" is the default
                _ => null  // Other providers work with no model specified
            };
        }

        /// <summary>
        /// Detects CLI errors about unavailable models and auto-resets the selection.
        /// Also updates the discovered models cache from the error message if available.
        /// Returns true if a stale model was detected and cleared.
        /// </summary>
        public bool HandleStaleModelError(string error, ProviderType provider, string selectedModel)
        {
            if (string.IsNullOrEmpty(error) || string.IsNullOrEmpty(selectedModel))
                return false;

            // Pattern: "Cannot use this model: <model>. Available models: <list>"
            if (!error.Contains("Cannot use this model"))
                return false;

            PholusLogger.LogWarning($"Stale model detected for {provider}: '{selectedModel}' is no longer available. Resetting to CLI Default.");

            // Clear the stale model selection
            SetSelectedModel(provider, "");

            // Try to parse and update available models from the error message
            var availableIndex = error.IndexOf("Available models:", StringComparison.OrdinalIgnoreCase);
            if (availableIndex >= 0)
            {
                var modelsString = error.Substring(availableIndex + "Available models:".Length).Trim();
                var models = modelsString.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                if (models.Count > 0)
                {
                    PholusLogger.Log($"Updated {provider} discovered models from CLI error: {models.Count} models");
                    SetDiscoveredModels(provider, models);
                }
            }

            return true;
        }

        #endregion
    }
}
