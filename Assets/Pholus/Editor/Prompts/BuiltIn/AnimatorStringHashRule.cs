using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class AnimatorStringHashRule : IDetectionRule
    {
        public string IssueType => "animator_string_hash";
        public string Title => "Animator string parameter";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Animator.SetFloat/SetBool/SetInteger/SetTrigger/ResetTrigger/GetFloat/GetBool/GetInteger with string parameter - should use cached hash";

        public string FixInstructions => @"Fix by caching the Animator parameter hash:
- Add a static readonly int field at class level: private static readonly int SpeedHash = Animator.StringToHash(""Speed"");
- Name the field based on the parameter name with Hash suffix (e.g., ""Speed"" becomes SpeedHash)
- Replace the string parameter in the Animator call with the hash field
- If multiple calls use the same string parameter, reuse the same hash field
- Do not modify any other code

Example:
private static readonly int SpeedHash = Animator.StringToHash(""Speed"");
// Then: animator.SetFloat(SpeedHash, value); instead of animator.SetFloat(""Speed"", value);";
    }
}
