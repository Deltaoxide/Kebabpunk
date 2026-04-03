namespace Pholus.Editor.Prompts.Interfaces
{
    /// <summary>
    /// Interface for providing fix instructions for a specific issue type.
    /// Use this to override fix instructions without creating a full detection rule.
    /// </summary>
    public interface IFixProvider
    {
        /// <summary>
        /// The issue type this provider gives fix instructions for.
        /// Must match an existing IDetectionRule.IssueType.
        /// Example: "cache_getcomponent"
        /// </summary>
        string IssueType { get; }

        /// <summary>
        /// Priority for override. Higher priority wins.
        /// Built-in providers use 0. Custom providers should use 100+ to override.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the fix instructions for this issue type.
        /// </summary>
        /// <returns>Detailed instructions for fixing the issue.</returns>
        string GetFixInstructions();
    }
}
