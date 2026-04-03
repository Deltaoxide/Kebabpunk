using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class DebugLogInUpdateRule : IDetectionRule
    {
        public string IssueType => "debug_log_in_update";
        public string Title => "Debug.Log in Update";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Debug.Log/LogWarning/LogError in Update/FixedUpdate/LateUpdate - expensive in builds";

        public string FixInstructions => @"Fix by wrapping in editor-only conditional:
- Add #if UNITY_EDITOR before the Debug.Log line
- Add #endif after the Debug.Log line
- Keep indentation consistent with surrounding code
- If multiple consecutive Debug calls exist, wrap them together in one block
- Do not modify any other code";
    }
}
