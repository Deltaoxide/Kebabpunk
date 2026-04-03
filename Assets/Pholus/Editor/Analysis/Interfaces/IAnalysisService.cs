using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Analysis.Interfaces
{
    /// <summary>
    /// Interface for the analysis service.
    /// Orchestrates the full analysis workflow.
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// Analyzes a single script for performance issues.
        /// </summary>
        /// <param name="scriptPath">Path to the script file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis result with detected issues.</returns>
        Task<AnalysisResult> AnalyzeScriptAsync(
            string scriptPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes a single script with custom context.
        /// </summary>
        /// <param name="scriptPath">Path to the script file.</param>
        /// <param name="context">Custom analysis context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis result with detected issues.</returns>
        Task<AnalysisResult> AnalyzeScriptAsync(
            string scriptPath,
            AnalysisContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans multiple scripts for performance issues.
        /// </summary>
        /// <param name="scriptPaths">Paths to script files.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Project scan result with all analysis results.</returns>
        Task<ProjectScanResult> ScanProjectAsync(
            IEnumerable<string> scriptPaths,
            IProgress<ScanProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when analysis starts.
        /// </summary>
        event Action<string> OnAnalysisStarted;

        /// <summary>
        /// Event raised when analysis completes.
        /// </summary>
        event Action<AnalysisResult> OnAnalysisCompleted;

        /// <summary>
        /// Event raised when analysis fails.
        /// </summary>
        event Action<string, Exception> OnAnalysisFailed;
    }
}
