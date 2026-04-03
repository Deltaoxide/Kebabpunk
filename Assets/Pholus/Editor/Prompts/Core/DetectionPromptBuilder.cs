using System.Linq;
using System.Text;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Registry;

namespace Pholus.Editor.Prompts.Core
{
    /// <summary>
    /// Builds system prompts for detection analysis from registered rules.
    /// </summary>
    public static class DetectionPromptBuilder
    {
        /// <summary>
        /// Gets the system prompt for performance analysis.
        /// </summary>
        /// <param name="platform">Target platform for threshold tuning.</param>
        public static string GetSystemPrompt(TargetPlatform platform)
        {
            var platformDescription = GetPlatformDescription(platform);
            var detectionRules = BuildDetectionRulesSection();
            var issueTypes = BuildIssueTypesSection();

            return $@"You are Pholus, a Unity and C# performance analysis system applying senior performance engineering standards to real world runtime code paths.
YOUR ROLE:
Analyze C# Unity scripts for performance issues. Be contextually aware — not every pattern is always a problem.

TARGET PLATFORM: {platform}
{platformDescription}

FOR EACH POTENTIAL ISSUE, ASSESS:
1. SEVERITY - How bad is it IF it's a problem?
2. CONFIDENCE - How sure are you it IS a problem in this context?
3. REASONING - What context clues informed your assessment?

CONFIDENCE MODIFIERS:

REDUCE confidence if:
- Class name suggests singleton (Player, Manager, Camera, Controller, GameManager, Singleton)
- Early return statements exist before the problematic code
- Method is event-driven (OnClick, OnTrigger, OnCollision, OnEnable, OnDisable)
- Code is inside #if UNITY_EDITOR or [Conditional(""DEBUG"")]
- Small, fixed-size collections (< 20 items)
- Comment indicates intentional choice

INCREASE confidence if:
- Class name suggests mass instantiation (Enemy, Bullet, Projectile, Item, Spawned, Pooled)
- Issue is inside a loop (for, foreach, while)
- Multiple performance issues compound in same method
- No conditional guards before expensive operations
- Dynamic or unbounded collections
- High-frequency methods (Update, FixedUpdate, LateUpdate)

DETECTION RULES:
{detectionRules}
OUTPUT FORMAT (JSON):
{{
  ""score"": 0-100,
  ""summary"": ""one sentence overview"",
  ""definite_issues"": [
    {{
      ""severity"": ""critical|high"",
      ""confidence"": ""high"",
      ""confidence_percent"": 90,
      ""line"": 47,
      ""title"": ""short description"",
      ""code_snippet"": ""the problematic code"",
      ""explanation"": ""why this is a problem"",
      ""impact"": ""estimated performance impact"",
      ""issue_type"": ""cache_getcomponent|cache_camera_main|string_concat_in_update|etc"",
      ""why_i_might_be_wrong"": null
    }}
  ],
  ""contextual_issues"": [
    {{
      ""severity"": ""critical|high|medium"",
      ""confidence"": ""medium|low"",
      ""confidence_percent"": 65,
      ""line"": 23,
      ""title"": ""short description"",
      ""code_snippet"": ""the code in question"",
      ""explanation"": ""why this could be a problem"",
      ""impact"": ""estimated impact if it is a problem"",
      ""issue_type"": ""cache_getcomponent|cache_find|etc"",
      ""why_i_might_be_wrong"": ""reasons this might be acceptable"",
      ""problem_if"": ""conditions that make this a real problem"",
      ""acceptable_if"": ""conditions that make this okay""
    }}
  ],
  ""suggestions"": [
    {{
      ""severity"": ""low"",
      ""confidence"": ""medium"",
      ""confidence_percent"": 50,
      ""line"": 89,
      ""title"": ""optional improvement"",
      ""issue_type"": ""suggestion"",
      ""explanation"": ""why this could be better""
    }}
  ],
  ""positive_notes"": [""things they did well""]
}}

ISSUE TYPES (for fix lookup):
{issueTypes}
CRITICAL GUIDELINES:
- Be specific. Say ""allocates ~40 bytes per frame"" not ""this is slow""
- Always explain your confidence reasoning
- If uncertain, say so and explain what would change your assessment
- Don't invent issues. Only report what you actually see in the code.
- Acknowledge good practices when you see them
- If code is already well-optimized, say so with high score (90+)
- Return ONLY valid JSON, no markdown code blocks or extra text";
        }

        /// <summary>
        /// Gets the user prompt that wraps the script content.
        /// </summary>
        public static string GetUserPrompt(string scriptContent, string fileName)
        {
            return $@"Analyze this Unity C# script for performance issues:

FILE: {fileName}

```csharp
{scriptContent}
```

Return your analysis as JSON following the exact format specified.";
        }

        private static string BuildDetectionRulesSection()
        {
            var rules = PromptRegistry.GetAllRules();
            if (!rules.Any())
            {
                return "No detection rules configured.";
            }

            var sb = new StringBuilder();
            var critical = rules.Where(r => r.DefaultSeverity == IssueSeverity.Critical).ToList();
            var high = rules.Where(r => r.DefaultSeverity == IssueSeverity.High).ToList();
            var medium = rules.Where(r => r.DefaultSeverity == IssueSeverity.Medium).ToList();
            var low = rules.Where(r => r.DefaultSeverity == IssueSeverity.Low).ToList();

            if (critical.Any())
            {
                sb.AppendLine();
                sb.AppendLine("CRITICAL (causes frame drops):");
                foreach (var rule in critical)
                {
                    sb.AppendLine(rule.DetectionDescription);
                }
            }

            if (high.Any())
            {
                sb.AppendLine();
                sb.AppendLine("HIGH (measurable impact):");
                foreach (var rule in high)
                {
                    sb.AppendLine(rule.DetectionDescription);
                }
            }

            if (medium.Any())
            {
                sb.AppendLine();
                sb.AppendLine("MEDIUM (code quality / minor impact):");
                foreach (var rule in medium)
                {
                    sb.AppendLine(rule.DetectionDescription);
                }
            }

            if (low.Any())
            {
                sb.AppendLine();
                sb.AppendLine("LOW (optional improvements):");
                foreach (var rule in low)
                {
                    sb.AppendLine(rule.DetectionDescription);
                }
            }

            return sb.ToString();
        }

        private static string BuildIssueTypesSection()
        {
            var rules = PromptRegistry.GetAllRules();
            if (!rules.Any())
            {
                return "No issue types configured.";
            }

            var sb = new StringBuilder();
            foreach (var rule in rules)
            {
                sb.AppendLine($"- {rule.IssueType}: {rule.Title}");
            }

            return sb.ToString();
        }

        private static string GetPlatformDescription(TargetPlatform platform)
        {
            return platform switch
            {
                TargetPlatform.Mobile => @"- Strict thresholds. Every allocation matters.
- 16ms frame budget (60 FPS target)
- Memory pressure is critical
- GC spikes cause visible stutters
- Be aggressive about caching and pooling recommendations",

                TargetPlatform.PCConsole => @"- Standard thresholds. More lenient on minor allocations.
- 16ms frame budget but more headroom
- Memory is less constrained
- Focus on critical issues, be lenient on minor ones
- Still flag allocation patterns in tight loops",

                TargetPlatform.VR => @"- Latency-focused. Consistent frame timing is priority.
- 11ms frame budget (90 FPS target)
- Frame timing consistency matters more than occasional spikes
- Any stutter is highly noticeable
- Be very strict about Update-time allocations",

                _ => "- Standard thresholds"
            };
        }
    }
}
