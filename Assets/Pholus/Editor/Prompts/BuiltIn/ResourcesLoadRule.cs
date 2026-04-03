using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class ResourcesLoadRule : IDetectionRule
    {
        public string IssueType => "resources_load_runtime";
        public string Title => "Resources.Load at runtime";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Resources.Load() at runtime - should pre-load or use Addressables";

        public string FixInstructions => @"Fix by pre-loading the resource:
- Add a private field to store the loaded resource
- Load in Awake() or Start() instead of at runtime
- Or better: use [SerializeField] and assign in Inspector
- Replace runtime Resources.Load with the cached reference
- Add null check if the resource might not exist
- Do not modify any other code";
    }
}
