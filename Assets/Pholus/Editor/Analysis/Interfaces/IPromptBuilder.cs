using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Analysis.Interfaces
{
    /// <summary>
    /// Interface for building analysis prompts.
    /// Single Responsibility: Only builds detection prompts.
    /// </summary>
    public interface IPromptBuilder
    {
        /// <summary>
        /// Builds a prompt for analyzing a script.
        /// </summary>
        /// <param name="scriptContent">The C# script content to analyze.</param>
        /// <param name="context">Analysis context (platform, options).</param>
        /// <returns>The complete prompt string.</returns>
        string BuildPrompt(string scriptContent, AnalysisContext context);
    }

    /// <summary>
    /// Interface for building fix prompts.
    /// Separated from detection prompt building (Interface Segregation).
    /// </summary>
    public interface IFixPromptBuilder
    {
        /// <summary>
        /// Builds a prompt for fixing a specific issue.
        /// </summary>
        /// <param name="scriptContent">The original script content.</param>
        /// <param name="issue">The issue to fix.</param>
        /// <param name="fileName">Name of the script file.</param>
        /// <returns>The complete fix prompt string.</returns>
        string BuildFixPrompt(string scriptContent, PerformanceIssue issue, string fileName);
    }
}
