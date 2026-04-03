using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class CacheWaitForSecondsRule : IDetectionRule
    {
        public string IssueType => "cache_waitforseconds";
        public string Title => "new WaitForSeconds in loop";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- yield return new WaitForSeconds() in loops - should cache the WaitForSeconds";

        public string FixInstructions => @"Fix by caching the WaitForSeconds instance:
- Add private readonly field at class level
- Use same delay value from the original
- Name it descriptively (e.g., _spawnDelay, _waitOneSecond, _attackCooldown)
- Replace ""new WaitForSeconds(X)"" with the cached field
- Do not modify any other code

Example:
private readonly WaitForSeconds _spawnDelay = new WaitForSeconds(2f);
// Then in coroutine: yield return _spawnDelay;";
    }
}
