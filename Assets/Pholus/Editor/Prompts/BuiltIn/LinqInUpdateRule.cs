using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class LinqInUpdateRule : IDetectionRule
    {
        public string IssueType => "linq_in_update";
        public string Title => "LINQ in hot path";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- LINQ methods (Where, Select, ToList, OrderBy, etc.) in hot paths - hidden allocations";

        public string FixInstructions => @"Fix by caching the result or using a loop:
- If the collection doesn't change, cache the LINQ result in Start/Awake
- If it must run each frame, replace LINQ with explicit for/foreach loop
- For Where(): use if statement inside loop
- For Select(): build result manually
- For ToList()/ToArray(): use pre-allocated collection
- For FirstOrDefault(): use loop with break
- Keep exact same logic, just avoid LINQ allocations
- Do not modify any other code";
    }
}
