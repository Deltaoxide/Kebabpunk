using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pholus.Editor.Analysis.Models;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// Builds prompts for the director to review analyzer results.
    /// </summary>
    public static class DirectorPromptBuilder
    {
        /// <summary>
        /// Builds a prompt for the director to review all analyzer results.
        /// </summary>
        public static string BuildReviewPrompt(
            string originalCode,
            Dictionary<ProviderType, AnalysisResult> analyzerResults)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are the Director reviewing Unity performance analysis from multiple AI providers.");
            sb.AppendLine("Your job is to: 1) Deduplicate similar issues, 2) Dismiss FALSE POSITIVES, 3) Set final severity.");
            sb.AppendLine();
            sb.AppendLine("## FALSE POSITIVES (DISMISS these):");
            sb.AppendLine("- Code inside comments or #if UNITY_EDITOR blocks");
            sb.AppendLine("- Camera.main/Find calls in Start()/Awake()/OnEnable() flagged as 'per-frame' - these run once, not per-frame");
            sb.AppendLine("- Already-cached references flagged as 'should cache' - check if there's a field storing it");
            sb.AppendLine("- Method only called once flagged as 'called every frame'");
            sb.AppendLine("- Allocation in a method that isn't called frequently");
            sb.AppendLine();
            sb.AppendLine("## VALID ISSUES (keep these):");
            sb.AppendLine("- Code actually in Update()/FixedUpdate()/LateUpdate() or coroutine loops");
            sb.AppendLine("- Multiple providers independently found the same issue");
            sb.AppendLine("- Pattern clearly matches the issue type (GetComponent in Update = real issue)");
            sb.AppendLine();
            sb.AppendLine("## SEVERITY CALIBRATION:");
            sb.AppendLine("- Critical: Allocations/expensive calls EVERY FRAME (Update, FixedUpdate, while loops)");
            sb.AppendLine("- High: Allocations in frequent callbacks, uncached lookups in hot paths");
            sb.AppendLine("- Medium: Suboptimal patterns that may cause issues at scale");
            sb.AppendLine("- Low: Minor improvements, style suggestions");
            sb.AppendLine();

            // Add the original code
            sb.AppendLine("## Original Code");
            sb.AppendLine("```csharp");
            sb.AppendLine(TruncateCode(originalCode, 3000));
            sb.AppendLine("```");
            sb.AppendLine();

            // Add each analyzer's findings
            sb.AppendLine("## Analyzer Results");
            sb.AppendLine();

            foreach (var (providerType, result) in analyzerResults)
            {
                if (result == null) continue;

                sb.AppendLine($"### {GetProviderDisplayName(providerType)} Found:");

                var allIssues = result.DefiniteIssues
                    .Concat(result.ContextualIssues)
                    .Concat(result.Suggestions)
                    .ToList();

                if (allIssues.Count == 0)
                {
                    sb.AppendLine("- No issues found");
                }
                else
                {
                    foreach (var issue in allIssues)
                    {
                        var severity = issue.Severity.ToString();
                        var opinion = GetIssueOpinion(issue);
                        sb.AppendLine($"- Line {issue.Line}: {issue.Title} ({severity}) - \"{opinion}\"");
                    }
                }
                sb.AppendLine();
            }

            // Add the task instructions
            sb.AppendLine("## Your Task");
            sb.AppendLine("1. DEDUPLICATE: Group similar issues (same problem, different wording) by line number");
            sb.AppendLine("2. VALIDATE/DISMISS: Check against false positive criteria above");
            sb.AppendLine("3. SET SEVERITY: Use the calibration guide - your final_severity is the authoritative rating");
            sb.AppendLine("4. DOCUMENT: Note what each provider said for transparency");
            sb.AppendLine();

            sb.AppendLine("## Response Format");
            sb.AppendLine("Respond ONLY with valid JSON (no markdown code blocks):");
            sb.AppendLine(@"{
  ""verdict"": [
    {
      ""issue"": ""Deduplicated issue title"",
      ""line"": 47,
      ""is_valid"": true,
      ""final_severity"": ""critical|high|medium|low"",
      ""director_reasoning"": ""Why validated or dismissed (reference false positive criteria if dismissing)"",
      ""provider_opinions"": {");

            // Add provider names dynamically
            var providerNames = analyzerResults.Keys.Select(GetProviderDisplayName).ToList();
            for (int i = 0; i < providerNames.Count; i++)
            {
                var comma = i < providerNames.Count - 1 ? "," : "";
                sb.AppendLine($@"        ""{providerNames[i]}"": {{""found"": true, ""opinion"": ""What they said""}}{comma}");
            }

            sb.AppendLine(@"      }
    }
  ],
  ""summary"": ""Overall assessment: X valid issues, Y dismissed""
}");

            return sb.ToString();
        }

        private static string GetProviderDisplayName(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => "Claude",
                ProviderType.Codex => "Codex",
                ProviderType.Gemini => "Gemini",
                ProviderType.Cursor => "Cursor",
                ProviderType.OpenRouter => "OpenRouter",
                _ => type.ToString()
            };
        }

        private static string GetIssueOpinion(PerformanceIssue issue)
        {
            // Try to get a short opinion from the issue
            if (!string.IsNullOrEmpty(issue.Explanation))
            {
                // Take first sentence or first 100 chars
                var explanation = issue.Explanation;
                var periodIndex = explanation.IndexOf('.');
                if (periodIndex > 0 && periodIndex < 100)
                {
                    return explanation.Substring(0, periodIndex + 1);
                }
                return explanation.Length > 100
                    ? explanation.Substring(0, 97) + "..."
                    : explanation;
            }

            // Fall back to impact
            if (!string.IsNullOrEmpty(issue.Impact))
            {
                return issue.Impact.Length > 100
                    ? issue.Impact.Substring(0, 97) + "..."
                    : issue.Impact;
            }

            return "Issue identified";
        }

        private static string TruncateCode(string code, int maxLength)
        {
            if (code.Length <= maxLength)
            {
                return code;
            }

            return code.Substring(0, maxLength) + "\n// ... (code truncated)";
        }
    }
}
