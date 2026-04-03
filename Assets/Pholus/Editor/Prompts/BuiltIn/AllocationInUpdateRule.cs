using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class AllocationInUpdateRule : IDetectionRule
    {
        public string IssueType => "allocation_in_update";
        public string Title => "Allocation in Update";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;
        public int Priority => 0;

        public string DetectionDescription =>
            "- new List<T>(), new Dictionary<T,V>(), new HashSet<T>() in Update - GC allocations every frame";

        public string FixInstructions => @"Fix by pre-allocating the collection:
- Move the new List/Dictionary/etc to a class field
- Initialize the field at declaration or in Awake
- Call Clear() at the start of the method instead of creating new
- Keep exact same behavior
- Do not modify any other code

Example:
private readonly List<Enemy> _nearbyEnemies = new List<Enemy>();
// Then in Update: _nearbyEnemies.Clear(); instead of = new List<Enemy>();";
    }
}
