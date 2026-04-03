using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis;
using Pholus.Editor.Analysis.Interfaces;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using Pholus.Editor.Fixes.Interfaces;
using Pholus.Editor.Fixes.Models;
using Pholus.Editor.Providers.Interfaces;

namespace Pholus.Editor.Fixes
{
    /// <summary>
    /// Orchestrates the fix workflow.
    /// Coordinates between prompt building, AI, diff generation, backup, and file modification.
    /// </summary>
    public class FixService : IFixService
    {
        private readonly ICLIProvider _provider;
        private readonly IFixPromptBuilder _fixPromptBuilder;
        private readonly IResponseParser _responseParser;
        private readonly IDiffGenerator _diffGenerator;
        private readonly IBackupManager _backupManager;
        private readonly IFileModifier _fileModifier;
        private readonly IUndoManager _undoManager;

        public event Action<FixResult> OnFixApplied;
        public event Action<UndoAction> OnFixUndone;

        public bool CanUndo => _undoManager.CanUndo;

        public FixService(
            ICLIProvider provider,
            IFixPromptBuilder fixPromptBuilder,
            IResponseParser responseParser,
            IDiffGenerator diffGenerator,
            IBackupManager backupManager,
            IFileModifier fileModifier,
            IUndoManager undoManager)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _fixPromptBuilder = fixPromptBuilder ?? throw new ArgumentNullException(nameof(fixPromptBuilder));
            _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
            _diffGenerator = diffGenerator ?? throw new ArgumentNullException(nameof(diffGenerator));
            _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
            _fileModifier = fileModifier ?? throw new ArgumentNullException(nameof(fileModifier));
            _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));
        }

        /// <summary>
        /// Creates a FixService with all dependencies.
        /// </summary>
        public static FixService Create()
        {
            var provider = PholusServices.Get<ICLIProvider>();
            var fixPromptBuilder = new AnalysisFixPromptBuilder();
            var responseParser = new ResponseParser();
            var diffGenerator = new DiffGenerator();
            var backupManager = new BackupManager();
            var fileModifier = new FileModifier();
            var undoManager = new UndoManager(backupManager);

            return new FixService(
                provider,
                fixPromptBuilder,
                responseParser,
                diffGenerator,
                backupManager,
                fileModifier,
                undoManager);
        }

        public async Task<FixPreview> GenerateFixPreviewAsync(
            string scriptPath,
            PerformanceIssue issue,
            CancellationToken cancellationToken = default)
        {
            var fileName = Path.GetFileName(scriptPath);

            try
            {
                PholusLogger.Log($"Generating fix for: {issue.Title}");

                // Read original content
                if (!File.Exists(scriptPath))
                {
                    return FixPreview.Failed(issue, scriptPath, "Script file not found");
                }

                var originalContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);

                // Build fix prompt
                var prompt = _fixPromptBuilder.BuildFixPrompt(originalContent, issue, fileName);

                // Get selected model for the provider
                var selectedModel = PholusSettings.Instance.GetSelectedModel(_provider.ProviderType);

                // Send to AI
                var response = await _provider.SendPromptAsync(prompt, selectedModel, cancellationToken);

                if (!response.Success)
                {
                    // Check if this is a stale model error - auto-clear and retry with default
                    if (!string.IsNullOrEmpty(selectedModel) && PholusSettings.Instance.HandleStaleModelError(response.Error, _provider.ProviderType, selectedModel))
                    {
                        var defaultModel = PholusSettings.GetDefaultModel(_provider.ProviderType);
                        PholusLogger.Log($"Retrying {_provider.ProviderType} with model: {defaultModel ?? "CLI Default"}...");
                        response = await _provider.SendPromptAsync(prompt, defaultModel, cancellationToken);

                        if (!response.Success)
                        {
                            return FixPreview.Failed(issue, scriptPath, $"AI request failed: {response.Error}");
                        }
                    }
                    else
                    {
                        return FixPreview.Failed(issue, scriptPath, $"AI request failed: {response.Error}");
                    }
                }

                // Extract code from response
                var fixedContent = _responseParser.ExtractCode(response.Output);

                if (string.IsNullOrWhiteSpace(fixedContent))
                {
                    return FixPreview.Failed(issue, scriptPath, "AI returned empty response");
                }

                // Validate the fixed content
                var validation = _fileModifier.ValidateContent(fixedContent);
                if (!validation.IsValid)
                {
                    return FixPreview.Failed(issue, scriptPath, $"Invalid fix: {validation.Error}");
                }

                // Generate diff
                var diff = _diffGenerator.GenerateDiff(originalContent, fixedContent);

                if (!diff.HasChanges)
                {
                    return FixPreview.Failed(issue, scriptPath, "No changes detected in the fix");
                }

                PholusLogger.Log($"Fix preview ready: {diff.Summary}");

                return FixPreview.Successful(issue, scriptPath, originalContent, fixedContent, diff);
            }
            catch (OperationCanceledException)
            {
                return FixPreview.Failed(issue, scriptPath, "Operation cancelled");
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Fix preview failed: {ex.Message}");
                return FixPreview.Failed(issue, scriptPath, ex.Message);
            }
        }

        public async Task<FixResult> ApplyFixAsync(
            FixPreview preview,
            CancellationToken cancellationToken = default)
        {
            if (preview == null || !preview.Success)
            {
                return FixResult.Failed("Invalid fix preview");
            }

            try
            {
                PholusLogger.Log($"Applying fix: {preview.Issue.Title}");

                // Create backup if enabled
                string backupPath = null;
                if (PholusSettings.Instance.CreateBackupBeforeFix)
                {
                    backupPath = _backupManager.CreateBackup(preview.ScriptPath);

                    // Clean up old backups
                    _backupManager.CleanupOldBackups(
                        preview.ScriptPath,
                        PholusSettings.Instance.MaxBackupsToKeep);
                }

                // Apply the fix
                var result = await _fileModifier.ApplyFixAsync(
                    preview.ScriptPath,
                    preview.FixedContent,
                    cancellationToken);

                if (result.Success)
                {
                    result.BackupPath = backupPath;
                    result.Issue = preview.Issue;

                    // Register for undo
                    if (!string.IsNullOrEmpty(backupPath))
                    {
                        var undoAction = new UndoAction(
                            preview.ScriptPath,
                            backupPath,
                            $"Fixed: {preview.Issue.Title}",
                            preview.Issue.IssueType);

                        _undoManager.RegisterUndo(undoAction);
                    }

                    // Mark issue as fixed
                    preview.Issue.IsFixed = true;

                    PholusLogger.Log($"Fix applied successfully: {preview.Diff.Summary}");
                    OnFixApplied?.Invoke(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to apply fix: {ex.Message}");
                return FixResult.Failed(ex.Message);
            }
        }

        public bool UndoLastFix()
        {
            if (!CanUndo)
            {
                PholusLogger.LogWarning("Nothing to undo");
                return false;
            }

            var action = _undoManager.PeekUndo();

            if (_undoManager.Undo())
            {
                PholusLogger.Log($"Fix undone: {action?.Description}");
                OnFixUndone?.Invoke(action);
                return true;
            }

            return false;
        }
    }
}
