using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class UncachedTransformRule : IDetectionRule
    {
        public string IssueType => "uncached_transform";
        public string Title => "Uncached transform access";
        public IssueSeverity DefaultSeverity => IssueSeverity.Medium;
        public int Priority => 0;

        public string DetectionDescription =>
            "- foreach on non-cached collections in Update - potential allocator call\n" +
            "- Accessing .gameObject or .transform repeatedly - cache the reference";

        public string FixInstructions => @"Fix by caching the transform reference:
- If accessing .transform multiple times, cache it: private Transform _transform;
- Initialize in Awake: _transform = transform;
- Replace all .transform accesses with _transform
- Same applies to .gameObject if accessed repeatedly
- Do not modify any other code";
    }
}
