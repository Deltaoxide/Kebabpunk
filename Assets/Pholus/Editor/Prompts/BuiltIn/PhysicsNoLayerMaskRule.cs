using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class PhysicsNoLayerMaskRule : IDetectionRule
    {
        public string IssueType => "physics_no_layermask";
        public string Title => "Physics query without layer mask";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Physics.Raycast/SphereCast/etc. without layer masks - checks all layers";

        public string FixInstructions => @"Fix by adding a layer mask parameter:
- Add a [SerializeField] private LayerMask _targetLayerMask; field
- Or define a constant: private const int TargetLayer = 1 << 8; (adjust layer number)
- Add the layer mask as the final parameter to the Physics method
- Keep all other parameters the same
- Do not modify any other code";
    }
}
