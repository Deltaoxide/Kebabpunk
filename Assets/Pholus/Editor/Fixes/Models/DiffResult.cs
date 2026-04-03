using System;
using System.Collections.Generic;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Fixes.Models
{
    /// <summary>
    /// Result of generating a diff between two versions of code.
    /// </summary>
    [Serializable]
    public class DiffResult
    {
        /// <summary>
        /// Original file content.
        /// </summary>
        public string Original { get; set; }

        /// <summary>
        /// Modified file content.
        /// </summary>
        public string Modified { get; set; }

        /// <summary>
        /// List of diff lines with change information.
        /// </summary>
        public List<DiffLine> Lines { get; set; } = new List<DiffLine>();

        /// <summary>
        /// Number of lines added.
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        /// Number of lines removed.
        /// </summary>
        public int LinesRemoved { get; set; }

        /// <summary>
        /// Whether there are any changes.
        /// </summary>
        public bool HasChanges => LinesAdded > 0 || LinesRemoved > 0;

        /// <summary>
        /// Summary of changes (e.g., "+5 lines, -2 lines").
        /// </summary>
        public string Summary => $"+{LinesAdded} lines, -{LinesRemoved} lines";
    }

    /// <summary>
    /// Represents a single line in a diff.
    /// </summary>
    [Serializable]
    public class DiffLine
    {
        /// <summary>
        /// Type of change for this line.
        /// </summary>
        public DiffLineType Type { get; set; }

        /// <summary>
        /// Line number in the original file (null for added lines).
        /// </summary>
        public int? OriginalLineNumber { get; set; }

        /// <summary>
        /// Line number in the modified file (null for removed lines).
        /// </summary>
        public int? ModifiedLineNumber { get; set; }

        /// <summary>
        /// The content of the line.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Prefix for display ('+', '-', or ' ').
        /// </summary>
        public char Prefix => Type switch
        {
            DiffLineType.Added => '+',
            DiffLineType.Removed => '-',
            _ => ' '
        };

        public static DiffLine Unchanged(int originalLine, int modifiedLine, string content)
        {
            return new DiffLine
            {
                Type = DiffLineType.Unchanged,
                OriginalLineNumber = originalLine,
                ModifiedLineNumber = modifiedLine,
                Content = content
            };
        }

        public static DiffLine Added(int modifiedLine, string content)
        {
            return new DiffLine
            {
                Type = DiffLineType.Added,
                OriginalLineNumber = null,
                ModifiedLineNumber = modifiedLine,
                Content = content
            };
        }

        public static DiffLine Removed(int originalLine, string content)
        {
            return new DiffLine
            {
                Type = DiffLineType.Removed,
                OriginalLineNumber = originalLine,
                ModifiedLineNumber = null,
                Content = content
            };
        }
    }
}
