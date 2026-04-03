using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class CacheFindRule : IDetectionRule
    {
        public string IssueType => "cache_find";
        public string Title => "GameObject.Find not cached";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- GameObject.Find(), FindWithTag(), FindObjectOfType() anywhere - expensive scene search\n" +
            "- Instantiate() in loops without pooling context - allocates memory repeatedly";

        public string FixInstructions => @"Fix by caching the find result:
- Add private field for the found object/component
- Cache in Awake() or Start() (Awake preferred)
- Replace Find call with cached reference
- Add null check if the original didn't have one and the object might not exist
- Consider adding [SerializeField] as alternative (mention in comment)
- Do not modify any other code";
    }
}
