using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class CacheCameraMainRule : IDetectionRule
    {
        public string IssueType => "cache_camera_main";
        public string Title => "Camera.main not cached";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- Camera.main not cached - does FindGameObjectWithTag internally";

        public string FixInstructions => @"Fix by caching Camera.main reference:
- Add private field: private Camera _mainCamera;
- Initialize in Awake(): _mainCamera = Camera.main;
- Replace all Camera.main usages with _mainCamera
- If Camera.main could legitimately be null and original had no check, add: if (_mainCamera == null) return;
- Keep exact same behavior otherwise
- Do not modify any other code";
    }
}
