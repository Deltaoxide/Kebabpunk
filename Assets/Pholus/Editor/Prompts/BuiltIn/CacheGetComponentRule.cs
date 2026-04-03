using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class CacheGetComponentRule : IDetectionRule
    {
        public string IssueType => "cache_getcomponent";
        public string Title => "GetComponent in Update";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- GetComponent<T>() in Update/FixedUpdate/LateUpdate - searches components every frame\n" +
            "- TryGetComponent<T>() in Update/FixedUpdate/LateUpdate - same issue\n" +
            "- GetComponentsInChildren/GetComponentsInParent in Update - repeated traversal";

        public string FixInstructions => @"Fix this GetComponent issue by caching the component reference:
- Add a private field with underscore prefix naming (e.g., _rigidbody, _collider)
- Use the exact component type from the GetComponent call
- Initialize in Awake() method (prefer Awake over Start for dependency setup)
- If Awake() already exists, add the cache line to it
- If Awake() doesn't exist, create it before Start() or Update()
- Replace the GetComponent call with the cached field reference
- Replace all usages of the local variable in that method with the cached field
- Keep exact same behavior, just cache the lookup
- Do not modify any other code
- Do not add null checks unless the original code had them";
    }
}
