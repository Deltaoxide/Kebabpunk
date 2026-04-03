using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class EmptyUnityMethodRule : IDetectionRule
    {
        public string IssueType => "empty_unity_method";
        public string Title => "Empty Unity method";
        public IssueSeverity DefaultSeverity => IssueSeverity.Medium;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Empty Update/FixedUpdate/LateUpdate methods - still have Unity overhead";

        public string FixInstructions => @"Remove the empty method entirely:
- Delete the entire method including signature and braces
- These methods have overhead even when empty (Unity still calls them)
- Do not modify any other code
- Do not leave any trace of the removed method";
    }
}
