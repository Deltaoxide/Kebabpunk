using Pholus.Editor.Analysis.Interfaces;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Core;

namespace Pholus.Editor.Analysis
{
    /// <summary>
    /// Builds prompts for performance analysis.
    /// Single Responsibility: Only handles prompt construction.
    /// Uses the modular DetectionPromptBuilder for rule-based prompts.
    /// </summary>
    public class PromptBuilder : IPromptBuilder
    {
        /// <summary>
        /// Builds a complete prompt for analyzing a script.
        /// </summary>
        public string BuildPrompt(string scriptContent, AnalysisContext context)
        {
            var systemPrompt = DetectionPromptBuilder.GetSystemPrompt(context.Platform);
            var userPrompt = DetectionPromptBuilder.GetUserPrompt(scriptContent, "Script");

            // Combine system and user prompts
            // For Claude CLI, we send both as a single prompt
            return $@"{systemPrompt}

---

{userPrompt}";
        }

        /// <summary>
        /// Builds a prompt with file name context.
        /// </summary>
        public string BuildPromptWithFileName(string scriptContent, string fileName, AnalysisContext context)
        {
            var systemPrompt = DetectionPromptBuilder.GetSystemPrompt(context.Platform);
            var userPrompt = DetectionPromptBuilder.GetUserPrompt(scriptContent, fileName);

            return $@"{systemPrompt}

---

{userPrompt}";
        }
    }

    /// <summary>
    /// Builds prompts for fixing specific issues.
    /// Uses the modular FixPromptBuilder for rule-based fix instructions.
    /// </summary>
    public class AnalysisFixPromptBuilder : IFixPromptBuilder
    {
        /// <summary>
        /// Builds a prompt for fixing a specific issue.
        /// </summary>
        public string BuildFixPrompt(string scriptContent, PerformanceIssue issue, string fileName)
        {
            return FixPromptBuilder.GetFixPrompt(
                scriptContent,
                fileName,
                issue.Line,
                issue.Title,
                issue.IssueType ?? "unknown");
        }
    }
}
