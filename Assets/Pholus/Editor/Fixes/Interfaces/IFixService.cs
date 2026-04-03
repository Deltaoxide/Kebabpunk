using System;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes.Interfaces
{
    /// <summary>
    /// Interface for the fix service.
    /// Orchestrates the full fix workflow.
    /// </summary>
    public interface IFixService
    {
        /// <summary>
        /// Generates a fix preview without applying it.
        /// </summary>
        /// <param name="scriptPath">Path to the script.</param>
        /// <param name="issue">The issue to fix.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Fix preview with diff.</returns>
        Task<FixPreview> GenerateFixPreviewAsync(
            string scriptPath,
            PerformanceIssue issue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a previously generated fix.
        /// </summary>
        /// <param name="preview">The fix preview to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of applying the fix.</returns>
        Task<FixResult> ApplyFixAsync(
            FixPreview preview,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Undoes the most recent fix.
        /// </summary>
        /// <returns>True if undo succeeded.</returns>
        bool UndoLastFix();

        /// <summary>
        /// Whether there's a fix that can be undone.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Event raised when a fix is applied.
        /// </summary>
        event Action<FixResult> OnFixApplied;

        /// <summary>
        /// Event raised when a fix is undone.
        /// </summary>
        event Action<UndoAction> OnFixUndone;
    }
}
