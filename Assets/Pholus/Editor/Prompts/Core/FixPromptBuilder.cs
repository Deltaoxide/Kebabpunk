using Pholus.Editor.Prompts.Registry;

namespace Pholus.Editor.Prompts.Core
{
    /// <summary>
    /// Builds fix prompts using registered fix providers.
    /// </summary>
    public static class FixPromptBuilder
    {
        /// <summary>
        /// Gets fix instructions for a specific issue type.
        /// </summary>
        public static string GetFixInstructions(string issueType)
        {
            return PromptRegistry.GetFixInstructions(issueType);
        }

        /// <summary>
        /// Gets the full fix prompt for an issue.
        /// </summary>
        public static string GetFixPrompt(
            string scriptContent,
            string fileName,
            int lineNumber,
            string issueTitle,
            string issueType)
        {
            var instructions = GetFixInstructions(issueType);

            return $@"You are applying a specific fix to Unity C# code.

ORIGINAL FILE:
{fileName}

ORIGINAL CODE:
```csharp
{scriptContent}
```

ISSUE TO FIX:
- Line: {lineNumber}
- Issue: {issueTitle}
- Type: {issueType}

FIX INSTRUCTIONS:
{instructions}

CRITICAL REQUIREMENTS:
- Output the COMPLETE file from start to end - every single line
- NEVER use placeholder comments like '// ... rest of code', '// ... remains the same', '// ... other methods', etc.
- NEVER summarize or skip any code - output ALL of it
- Apply ONLY this specific fix, keep everything else exactly as-is
- Preserve all formatting, comments, and whitespace elsewhere
- Ensure the code compiles
- Do not add explanations before or after the code

Output ONLY the complete fixed C# code, nothing else.";
        }
    }
}
