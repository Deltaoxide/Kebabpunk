using System;

namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Represents a detected performance issue in a script.
    /// </summary>
    [Serializable]
    public class PerformanceIssue
    {
        /// <summary>
        /// Unique identifier for this issue instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// How severe this issue is if it's actually a problem.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// How confident we are that this is actually a problem.
        /// </summary>
        public IssueConfidence Confidence { get; set; }

        /// <summary>
        /// Numeric confidence percentage (0-100).
        /// </summary>
        public int ConfidencePercent { get; set; }

        /// <summary>
        /// Line number where the issue occurs.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Short title describing the issue.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The problematic code snippet.
        /// </summary>
        public string CodeSnippet { get; set; }

        /// <summary>
        /// Detailed explanation of why this is a problem.
        /// </summary>
        public string Explanation { get; set; }

        /// <summary>
        /// Estimated performance impact (e.g., "~0.3ms per frame").
        /// </summary>
        public string Impact { get; set; }

        /// <summary>
        /// Reasons why this detection might be wrong (for contextual issues).
        /// </summary>
        public string WhyIMightBeWrong { get; set; }

        /// <summary>
        /// Conditions that make this definitely a problem.
        /// </summary>
        public string ProblemIf { get; set; }

        /// <summary>
        /// Conditions that make this acceptable.
        /// </summary>
        public string AcceptableIf { get; set; }

        /// <summary>
        /// Issue type key used to look up fix instructions (e.g., "cache_getcomponent").
        /// </summary>
        public string IssueType { get; set; }

        /// <summary>
        /// Category for UI grouping.
        /// </summary>
        public IssueCategory Category { get; set; }

        /// <summary>
        /// Whether this issue has been skipped by the user.
        /// </summary>
        public bool IsSkipped { get; set; }

        /// <summary>
        /// Whether this issue has been fixed.
        /// </summary>
        public bool IsFixed { get; set; }

        public PerformanceIssue()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        /// <summary>
        /// Creates a deep copy of this issue.
        /// </summary>
        public PerformanceIssue Clone()
        {
            return new PerformanceIssue
            {
                Id = Id,
                Severity = Severity,
                Confidence = Confidence,
                ConfidencePercent = ConfidencePercent,
                Line = Line,
                Title = Title,
                CodeSnippet = CodeSnippet,
                Explanation = Explanation,
                Impact = Impact,
                WhyIMightBeWrong = WhyIMightBeWrong,
                ProblemIf = ProblemIf,
                AcceptableIf = AcceptableIf,
                IssueType = IssueType,
                Category = Category,
                IsSkipped = IsSkipped,
                IsFixed = IsFixed
            };
        }
    }
}
