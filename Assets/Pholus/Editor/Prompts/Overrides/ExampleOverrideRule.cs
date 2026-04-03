// =============================================================================
// EXAMPLE: How to Override Built-in Fix Instructions
// =============================================================================
// Uncomment and modify to override how Pholus fixes specific issue types.
// This is useful when your team has specific coding standards or preferences.
// =============================================================================

/*
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.Overrides
{
    /// <summary>
    /// Example: Override the GetComponent caching fix with your team's preferred approach.
    /// Priority 100 ensures this takes precedence over the built-in rule (Priority 0).
    /// </summary>
    [PromptProvider(Priority = 100)]
    public class CustomGetComponentFix : IFixProvider
    {
        public string IssueType => "cache_getcomponent";  // Must match built-in issue type
        public int Priority => 100;

        public string GetFixInstructions()
        {
            return @"Fix this GetComponent issue using our team's standard pattern:

1. Add [RequireComponent(typeof(YourComponent))] attribute to the class
2. Create a private field: private YourComponent _component;
3. Cache in Awake() using TryGetComponent for safety:

   private void Awake()
   {
       if (!TryGetComponent(out _component))
       {
           Debug.LogError($""Missing required component on {gameObject.name}"");
           enabled = false;
           return;
       }
   }

4. Replace all GetComponent calls with the cached field
5. Do not modify any other code";
        }
    }

    /// <summary>
    /// Example: Override Camera.main caching with a singleton pattern.
    /// </summary>
    [PromptProvider(Priority = 100)]
    public class CustomCameraMainFix : IFixProvider
    {
        public string IssueType => "cache_camera_main";
        public int Priority => 100;

        public string GetFixInstructions()
        {
            return @"Fix by using our CameraManager singleton:

1. Replace Camera.main with CameraManager.Instance.MainCamera
2. If CameraManager doesn't exist in the project, fall back to:
   - Add private Camera _mainCamera field
   - Cache in Awake(): _mainCamera = Camera.main;
   - Replace all Camera.main usages
3. Do not modify any other code";
        }
    }
}
*/
