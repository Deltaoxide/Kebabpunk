namespace Pholus.Editor.Analysis.Models
{
    /// <summary>
    /// Supported AI provider types.
    /// </summary>
    public enum ProviderType
    {
        Claude,
        Codex,
        Gemini,
        Cursor,
        OpenRouter
    }

    /// <summary>
    /// Target platform for analysis thresholds.
    /// </summary>
    public enum TargetPlatform
    {
        /// <summary>Strict thresholds. Every allocation matters. 16ms frame budget.</summary>
        Mobile,

        /// <summary>Standard thresholds. More lenient on minor allocations.</summary>
        PCConsole,

        /// <summary>Latency-focused. Consistent frame timing priority.</summary>
        VR
    }

    /// <summary>
    /// Severity level of a performance issue.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>Causes frame drops, immediate fix needed.</summary>
        Critical,

        /// <summary>Measurable performance impact.</summary>
        High,

        /// <summary>Code quality concern, minor impact.</summary>
        Medium,

        /// <summary>Optional improvement suggestion.</summary>
        Low
    }

    /// <summary>
    /// Confidence level in the issue detection.
    /// </summary>
    public enum IssueConfidence
    {
        /// <summary>Definitely a problem (90%+).</summary>
        High,

        /// <summary>Likely a problem depending on context (60-89%).</summary>
        Medium,

        /// <summary>Might be a problem, context-dependent (below 60%).</summary>
        Low
    }

    /// <summary>
    /// Category for grouping issues in the UI.
    /// </summary>
    public enum IssueCategory
    {
        /// <summary>High confidence issues that are definitely problems.</summary>
        Definite,

        /// <summary>Context-dependent issues that may or may not be problems.</summary>
        Contextual,

        /// <summary>Optional improvements and best practices.</summary>
        Suggestion
    }

    /// <summary>
    /// Type of diff line change.
    /// </summary>
    public enum DiffLineType
    {
        /// <summary>Line was not changed.</summary>
        Unchanged,

        /// <summary>Line was added in the new version.</summary>
        Added,

        /// <summary>Line was removed from the original.</summary>
        Removed
    }
}
