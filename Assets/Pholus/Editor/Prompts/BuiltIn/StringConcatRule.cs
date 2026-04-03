using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class StringConcatRule : IDetectionRule
    {
        public string IssueType => "string_concat_in_update";
        public string Title => "String concatenation in Update";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- String concatenation with + in Update or loops - creates garbage";

        public string FixInstructions => @"Fix by using cached StringBuilder:
- Add private field: private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(64);
- Add using statement for System.Text if not present
- Replace string concatenation with StringBuilder pattern:
  - _sb.Clear();
  - _sb.Append(""..."").Append(value).Append(""..."");
  - Use _sb.ToString() where the final string is needed
- Choose initial capacity based on expected string length
- Keep exact same output string format
- Do not modify any other code";
    }
}
