using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Core;
using Pholus.Editor.Fixes.Interfaces;
using Pholus.Editor.Fixes.Models;
using UnityEditor;

namespace Pholus.Editor.Fixes
{
    /// <summary>
    /// Handles writing fixed content to files.
    /// Single Responsibility: Only handles file modification.
    /// </summary>
    public class FileModifier : IFileModifier
    {
        public async Task<FixResult> ApplyFixAsync(
            string filePath,
            string fixedContent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate content first
                var validation = ValidateContent(fixedContent);
                if (!validation.IsValid)
                {
                    PholusLogger.LogError($"Fix validation failed: {validation.Error}");
                    return FixResult.Failed(validation.Error);
                }

                // Check if file can be modified
                if (!CanModify(filePath))
                {
                    return FixResult.Failed($"Cannot modify file: {filePath}");
                }

                // Write the fixed content
                await File.WriteAllTextAsync(filePath, fixedContent, cancellationToken);

                // Refresh Unity's asset database
                AssetDatabase.Refresh();

                PholusLogger.Log($"Fixed content applied to: {filePath}");

                return new FixResult
                {
                    Success = true,
                    ScriptPath = filePath,
                    AppliedAt = DateTime.Now
                };
            }
            catch (OperationCanceledException)
            {
                return FixResult.Failed("Operation cancelled");
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to apply fix: {ex.Message}");
                return FixResult.Failed(ex.Message);
            }
        }

        public ValidationResult ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ValidationResult.Invalid("Content is empty");
            }

            // Check for CLI output that leaked through parsing
            if (content.Contains("Now using node"))
            {
                return ValidationResult.Invalid("CLI output detected: nvm message leaked through");
            }
            if (content.Contains("Tokens:") && content.Contains("Cost:"))
            {
                return ValidationResult.Invalid("CLI output detected: token count leaked through");
            }
            if (Regex.IsMatch(content, @"^\s+\d+\s+using\s", RegexOptions.Multiline))
            {
                return ValidationResult.Invalid("CLI output detected: line numbers leaked through");
            }

            // Check for lazy placeholder comments that would delete code
            if (Regex.IsMatch(content, @"//\s*\.\.\..*remain|//\s*\.\.\..*same|//\s*\.\.\..*rest|//\s*\.\.\..*other|//\s*\.\.\.\s*\(", RegexOptions.IgnoreCase))
            {
                return ValidationResult.Invalid("AI used placeholder comments (e.g., '// ... remains the same') which would delete code. Try again.");
            }

            // Check for common issues that indicate parsing problems
            if (content.Contains("```"))
            {
                return ValidationResult.Invalid("Content appears to contain markdown code blocks");
            }

            // Basic C# syntax checks - count braces for detailed error
            var (isBalanced, openCount, closeCount) = CountBraces(content);
            if (!isBalanced)
            {
                var diff = openCount - closeCount;
                var msg = diff > 0
                    ? $"Unbalanced braces: {diff} more '{{' than '}}' (missing closing braces)"
                    : $"Unbalanced braces: {-diff} more '}}' than '{{' (missing opening braces)";
                PholusLogger.LogError($"{msg}. Content preview:\n{content.Substring(0, Math.Min(500, content.Length))}...");
                return ValidationResult.Invalid(msg);
            }

            // Check for class/struct declaration (basic sanity check)
            if (!Regex.IsMatch(content, @"\b(class|struct|interface|enum)\s+\w+"))
            {
                // Might still be valid (e.g., partial class in another file), so just warn
                PholusLogger.LogWarning("No class/struct declaration found - content might be incomplete");
            }

            return ValidationResult.Valid();
        }

        public bool CanModify(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (!File.Exists(filePath))
            {
                return false;
            }

            // Check if file is read-only
            var attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                PholusLogger.LogWarning($"File is read-only: {filePath}");
                return false;
            }

            // Check if it's a C# file
            if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                PholusLogger.LogWarning($"Not a C# file: {filePath}");
                return false;
            }

            // Check if it's in the Assets folder (Unity project)
            if (!filePath.Contains("Assets"))
            {
                PholusLogger.LogWarning($"File is not in Assets folder: {filePath}");
                return false;
            }

            return true;
        }

        private (bool isBalanced, int openCount, int closeCount) CountBraces(string content)
        {
            var openCount = 0;
            var closeCount = 0;
            var inString = false;
            var inChar = false;
            var inSingleLineComment = false;
            var inMultiLineComment = false;
            var prevChar = '\0';

            for (int i = 0; i < content.Length; i++)
            {
                var c = content[i];
                var nextChar = i + 1 < content.Length ? content[i + 1] : '\0';

                // Handle comments
                if (!inString && !inChar && !inMultiLineComment && c == '/' && nextChar == '/')
                {
                    inSingleLineComment = true;
                }
                if (inSingleLineComment && c == '\n')
                {
                    inSingleLineComment = false;
                }
                if (!inString && !inChar && !inSingleLineComment && c == '/' && nextChar == '*')
                {
                    inMultiLineComment = true;
                }
                if (inMultiLineComment && c == '*' && nextChar == '/')
                {
                    inMultiLineComment = false;
                    i++; // Skip the '/'
                    continue;
                }

                // Skip if in comment
                if (inSingleLineComment || inMultiLineComment)
                {
                    prevChar = c;
                    continue;
                }

                // Skip string literals
                if (c == '"' && prevChar != '\\' && !inChar)
                {
                    inString = !inString;
                }

                // Skip char literals
                if (c == '\'' && prevChar != '\\' && !inString)
                {
                    inChar = !inChar;
                }

                if (!inString && !inChar)
                {
                    if (c == '{') openCount++;
                    if (c == '}') closeCount++;
                }

                prevChar = c;
            }

            return (openCount == closeCount, openCount, closeCount);
        }
    }
}
