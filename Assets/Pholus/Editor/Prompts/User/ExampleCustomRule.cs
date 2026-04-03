// =============================================================================
// EXAMPLE: How to Add Your Own Custom Detection Rules
// =============================================================================
// Uncomment and modify to add new performance patterns that Pholus will detect.
// Great for project-specific patterns or team coding standards.
// =============================================================================

/*
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.User
{
    /// <summary>
    /// Example: Detect usage of your deprecated API.
    /// </summary>
    [PromptProvider(Priority = 100)]
    public class DeprecatedApiRule : IDetectionRule
    {
        public string IssueType => "deprecated_api_usage";
        public string Title => "Deprecated API Usage";
        public IssueSeverity DefaultSeverity => IssueSeverity.Medium;
        public int Priority => 100;

        public string DetectionDescription =>
            "- OldManager.DoSomething() is deprecated - use NewManager.DoSomethingBetter()\n" +
            "- LegacyPool.Spawn() is deprecated - use ObjectPoolManager.Get()";

        public string FixInstructions => @"Replace deprecated API calls:
- Replace OldManager.DoSomething() with NewManager.DoSomethingBetter()
- Replace LegacyPool.Spawn() with ObjectPoolManager.Get()
- Replace LegacyPool.Despawn() with ObjectPoolManager.Release()
- Keep exact same parameters and behavior
- Do not modify any other code";
    }

    /// <summary>
    /// Example: Detect direct field access that should use properties.
    /// </summary>
    [PromptProvider(Priority = 100)]
    public class DirectFieldAccessRule : IDetectionRule
    {
        public string IssueType => "direct_field_access";
        public string Title => "Direct Field Access";
        public IssueSeverity DefaultSeverity => IssueSeverity.Low;
        public int Priority => 100;

        public string DetectionDescription =>
            "- Direct access to public fields that have corresponding properties\n" +
            "- player.health instead of player.Health (if property exists)";

        public string FixInstructions => @"Replace direct field access with property:
- Check if a property exists with the same name (different casing)
- Replace field access with property access
- Do not modify any other code";
    }

    /// <summary>
    /// Example: Detect your game's specific anti-pattern.
    /// </summary>
    [PromptProvider(Priority = 100)]
    public class GameSpecificPatternRule : IDetectionRule
    {
        public string IssueType => "game_specific_antipattern";
        public string Title => "Game-Specific Anti-Pattern";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 100;

        public string DetectionDescription =>
            "- Creating new Damage() objects in combat loops - use DamagePool\n" +
            "- Calling InventoryManager.Refresh() every frame - batch updates";

        public string FixInstructions => @"Fix game-specific patterns:

For Damage allocation:
- Replace 'new Damage(...)' with 'DamagePool.Get()'
- Call damage.Reset(...) to initialize
- Ensure damage is returned to pool after use

For Inventory refresh:
- Mark inventory as dirty instead of refreshing
- Call Refresh() only when dirty flag is set
- Clear dirty flag after refresh

Do not modify any other code";
    }
}
*/
