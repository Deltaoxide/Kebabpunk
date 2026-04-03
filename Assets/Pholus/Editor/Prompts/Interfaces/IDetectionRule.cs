using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Prompts.Interfaces
{
    /// <summary>
    /// Interface for defining a detection rule.
    /// Implement this to create custom performance detection rules that Pholus will use.
    /// </summary>
    public interface IDetectionRule
    {
        /// <summary>
        /// Unique identifier for this issue type. Used for fix lookup.
        /// Example: "cache_getcomponent", "my_custom_pattern"
        /// </summary>
        string IssueType { get; }

        /// <summary>
        /// Short human-readable title shown in reports.
        /// Example: "GetComponent in Update"
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Default severity level for this issue type.
        /// </summary>
        IssueSeverity DefaultSeverity { get; }

        /// <summary>
        /// Description for the AI system prompt explaining what to detect.
        /// This should describe the pattern to look for and when it's a problem.
        /// Example: "- GetComponent&lt;T&gt;() in Update/FixedUpdate/LateUpdate - searches components every frame"
        /// </summary>
        string DetectionDescription { get; }

        /// <summary>
        /// Instructions for fixing this issue type.
        /// This is used when generating fix suggestions.
        /// </summary>
        string FixInstructions { get; }

        /// <summary>
        /// Priority for ordering and override. Higher priority wins.
        /// Built-in rules use 0. Custom rules should use 100+ to override.
        /// </summary>
        int Priority { get; }
    }
}
