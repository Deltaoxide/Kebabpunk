using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class CompareTagRule : IDetectionRule
    {
        public string IssueType => "compare_tag";
        public string Title => "Tag comparison allocates";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- gameObject.tag == \"string\" or .tag != \"string\" - allocates string, use CompareTag instead";

        public string FixInstructions => @"Fix by using CompareTag method:
- Replace .tag == ""X"" with .CompareTag(""X"")
- Replace .tag != ""X"" with !.CompareTag(""X"")
- Keep the exact same string value
- Do not modify any other code";
    }
}
