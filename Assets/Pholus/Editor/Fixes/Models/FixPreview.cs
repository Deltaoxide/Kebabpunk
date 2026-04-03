using System;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Fixes.Models
{
    /// <summary>
    /// Preview of a fix before applying.
    /// </summary>
    [Serializable]
    public class FixPreview
    {
        /// <summary>
        /// The issue being fixed.
        /// </summary>
        public PerformanceIssue Issue { get; set; }

        /// <summary>
        /// Original file content.
        /// </summary>
        public string OriginalContent { get; set; }

        /// <summary>
        /// Fixed file content.
        /// </summary>
        public string FixedContent { get; set; }

        /// <summary>
        /// Diff between original and fixed.
        /// </summary>
        public DiffResult Diff { get; set; }

        /// <summary>
        /// Path to the script file.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// Whether the fix generation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if generation failed.
        /// </summary>
        public string Error { get; set; }

        public static FixPreview Successful(
            PerformanceIssue issue,
            string scriptPath,
            string original,
            string fixedContent,
            DiffResult diff)
        {
            return new FixPreview
            {
                Issue = issue,
                ScriptPath = scriptPath,
                OriginalContent = original,
                FixedContent = fixedContent,
                Diff = diff,
                Success = true
            };
        }

        public static FixPreview Failed(PerformanceIssue issue, string scriptPath, string error)
        {
            return new FixPreview
            {
                Issue = issue,
                ScriptPath = scriptPath,
                Success = false,
                Error = error
            };
        }
    }

    /// <summary>
    /// Result of applying a fix.
    /// </summary>
    [Serializable]
    public class FixResult
    {
        /// <summary>
        /// Whether the fix was applied successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if application failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Path to the backup file (for undo).
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Path to the fixed script.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// The issue that was fixed.
        /// </summary>
        public PerformanceIssue Issue { get; set; }

        /// <summary>
        /// When the fix was applied.
        /// </summary>
        public DateTime AppliedAt { get; set; }

        public static FixResult Successful(string scriptPath, string backupPath, PerformanceIssue issue)
        {
            return new FixResult
            {
                Success = true,
                ScriptPath = scriptPath,
                BackupPath = backupPath,
                Issue = issue,
                AppliedAt = DateTime.Now
            };
        }

        public static FixResult Failed(string error)
        {
            return new FixResult
            {
                Success = false,
                Error = error,
                AppliedAt = DateTime.Now
            };
        }
    }
}
