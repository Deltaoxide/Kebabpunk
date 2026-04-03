using System;
using System.Collections.Generic;
using System.Linq;

namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Result of analyzing a script for performance issues.
    /// </summary>
    [Serializable]
    public class AnalysisResult
    {
        /// <summary>
        /// Overall score from 0-100 (higher is better).
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// One-sentence summary of the analysis.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// High-confidence issues that are definitely problems.
        /// </summary>
        public List<PerformanceIssue> DefiniteIssues { get; set; } = new List<PerformanceIssue>();

        /// <summary>
        /// Context-dependent issues that may or may not be problems.
        /// </summary>
        public List<PerformanceIssue> ContextualIssues { get; set; } = new List<PerformanceIssue>();

        /// <summary>
        /// Optional improvement suggestions.
        /// </summary>
        public List<PerformanceIssue> Suggestions { get; set; } = new List<PerformanceIssue>();

        /// <summary>
        /// Issues dismissed by consensus Director as false positives.
        /// </summary>
        public List<PerformanceIssue> DismissedIssues { get; set; } = new List<PerformanceIssue>();

        /// <summary>
        /// Positive notes about things done well.
        /// </summary>
        public List<string> PositiveNotes { get; set; } = new List<string>();

        /// <summary>
        /// Path to the analyzed script.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// When the analysis was performed.
        /// </summary>
        public DateTime AnalyzedAt { get; set; }

        /// <summary>
        /// Estimated token usage and cost for this analysis.
        /// </summary>
        public TokenUsage Usage { get; set; }

        /// <summary>
        /// Total number of issues found.
        /// </summary>
        public int TotalIssueCount => DefiniteIssues.Count + ContextualIssues.Count + Suggestions.Count;

        /// <summary>
        /// All issues combined into a single list.
        /// </summary>
        public IEnumerable<PerformanceIssue> AllIssues =>
            DefiniteIssues.Concat(ContextualIssues).Concat(Suggestions);

        /// <summary>
        /// Issues that haven't been skipped or fixed.
        /// </summary>
        public IEnumerable<PerformanceIssue> ActiveIssues =>
            AllIssues.Where(i => !i.IsSkipped && !i.IsFixed);

        /// <summary>
        /// Whether the analysis found any issues.
        /// </summary>
        public bool HasIssues => TotalIssueCount > 0;

        /// <summary>
        /// Whether all issues have been resolved (fixed or skipped).
        /// </summary>
        public bool IsFullyResolved => !ActiveIssues.Any();

        public static AnalysisResult Empty(string scriptPath)
        {
            return new AnalysisResult
            {
                Score = 100,
                Summary = "No performance issues detected.",
                ScriptPath = scriptPath,
                AnalyzedAt = DateTime.Now
            };
        }

        public static AnalysisResult Error(string scriptPath, string error)
        {
            return new AnalysisResult
            {
                Score = 0,
                Summary = $"Analysis failed: {error}",
                ScriptPath = scriptPath,
                AnalyzedAt = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Result of scanning multiple scripts.
    /// </summary>
    [Serializable]
    public class ProjectScanResult
    {
        /// <summary>
        /// Results for each analyzed script.
        /// </summary>
        public List<AnalysisResult> Results { get; set; } = new List<AnalysisResult>();

        /// <summary>
        /// Total number of scripts analyzed.
        /// </summary>
        public int ScriptsAnalyzed => Results.Count;

        /// <summary>
        /// Total number of issues across all scripts.
        /// </summary>
        public int TotalIssues => Results.Sum(r => r.TotalIssueCount);

        /// <summary>
        /// Number of scripts with at least one issue.
        /// </summary>
        public int ScriptsWithIssues => Results.Count(r => r.HasIssues);

        /// <summary>
        /// Average score across all scripts.
        /// </summary>
        public int AverageScore => Results.Count > 0 ? (int)Results.Average(r => r.Score) : 100;

        /// <summary>
        /// When the scan was performed.
        /// </summary>
        public DateTime ScannedAt { get; set; }

        /// <summary>
        /// How long the scan took.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Progress information during a project scan.
    /// </summary>
    [Serializable]
    public class ScanProgress
    {
        /// <summary>
        /// Currently processing file path.
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// Current file index (1-based).
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// Total number of files to scan.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Progress percentage (0-100).
        /// </summary>
        public float Percent => TotalFiles > 0 ? (float)CurrentIndex / TotalFiles * 100f : 0f;
    }
}
