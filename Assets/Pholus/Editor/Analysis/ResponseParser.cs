using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Pholus.Editor.Analysis.Interfaces;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;

namespace Pholus.Editor.Analysis
{
    /// <summary>
    /// Parses AI responses into AnalysisResult objects.
    /// Single Responsibility: Only handles response parsing.
    /// </summary>
    public class ResponseParser : IResponseParser
    {
        /// <summary>
        /// Parses a JSON response into an AnalysisResult.
        /// </summary>
        public AnalysisResult Parse(string jsonResponse)
        {
            if (!TryParse(jsonResponse, out var result, out var error))
            {
                throw new Exception($"Failed to parse response: {error}");
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse without throwing.
        /// </summary>
        public bool TryParse(string jsonResponse, out AnalysisResult result, out string error)
        {
            result = null;
            error = null;

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                error = "Response is empty";
                return false;
            }

            // Log raw response for debugging (truncated)
            var truncatedResponse = jsonResponse.Length > 500
                ? jsonResponse.Substring(0, 500) + "..."
                : jsonResponse;
            PholusLogger.Log($"Raw AI response ({jsonResponse.Length} chars):\n{truncatedResponse}");

            try
            {
                // Extract JSON from response (might be wrapped in markdown code blocks)
                var json = ExtractJson(jsonResponse);

                if (string.IsNullOrEmpty(json))
                {
                    PholusLogger.LogError($"No JSON found in response. Full response:\n{jsonResponse}");
                    error = "No JSON found in response";
                    return false;
                }

                PholusLogger.Log($"Extracted JSON ({json.Length} chars)");

                // Parse using Unity's JsonUtility (limited but built-in)
                // For complex nested objects, we'll parse manually
                result = ParseManually(json);
                result.AnalyzedAt = DateTime.Now;

                // Assign categories based on list membership
                foreach (var issue in result.DefiniteIssues)
                {
                    issue.Category = IssueCategory.Definite;
                }
                foreach (var issue in result.ContextualIssues)
                {
                    issue.Category = IssueCategory.Contextual;
                }
                foreach (var issue in result.Suggestions)
                {
                    issue.Category = IssueCategory.Suggestion;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Extracts code from a fix response.
        /// Handles markdown code blocks (Claude, Gemini) and CLI-formatted output (OpenRouter).
        /// </summary>
        public string ExtractCode(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            // First, check for markdown code blocks (Claude, Gemini format)
            var codeBlockMatch = Regex.Match(response, @"```(?:csharp|cs)?\s*\n([\s\S]*?)\n```", RegexOptions.Multiline);
            if (codeBlockMatch.Success)
            {
                return StripMarkdownArtifacts(codeBlockMatch.Groups[1].Value.Trim());
            }

            // Check for OpenRouter CLI formatting (line numbers, ASCII boxes, nvm messages)
            if (response.Contains("Now using node") ||
                Regex.IsMatch(response, @"^\s+\d+\s", RegexOptions.Multiline) ||
                (response.Contains("Tokens:") && response.Contains("Cost:")) ||
                response.Contains("┌──") || response.Contains("└──"))
            {
                var stripped = StripCLIFormatting(response);
                if (!string.IsNullOrWhiteSpace(stripped))
                {
                    PholusLogger.Log($"Stripped CLI formatting, extracted {stripped.Length} chars of code");
                    return StripMarkdownArtifacts(stripped);
                }
            }

            // Fallback: return as-is (might be raw code)
            var code = response.Trim();

            // Strip markdown artifacts that could break C# compilation
            code = StripMarkdownArtifacts(code);

            return code;
        }

        /// <summary>
        /// Strips markdown formatting artifacts that could break C# code.
        /// Handles: inline backticks, bold/italic markers, etc.
        /// </summary>
        private string StripMarkdownArtifacts(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            // Remove inline backticks (used for inline code in markdown)
            // But preserve string literals by only removing backticks outside of quotes
            var result = new System.Text.StringBuilder();
            var inString = false;
            var inVerbatim = false;
            var prevChar = '\0';

            for (int i = 0; i < code.Length; i++)
            {
                var c = code[i];

                // Track string state
                if (c == '"' && prevChar != '\\')
                {
                    if (prevChar == '@')
                        inVerbatim = true;
                    else if (!inVerbatim)
                        inString = !inString;
                    else if (inVerbatim && i + 1 < code.Length && code[i + 1] == '"')
                    {
                        // Escaped quote in verbatim string
                        result.Append(c);
                        prevChar = c;
                        continue;
                    }
                    else
                        inVerbatim = false;
                }

                // Skip backticks outside of strings
                if (c == '`' && !inString && !inVerbatim)
                {
                    PholusLogger.Log($"Stripped markdown backtick at position {i}");
                    prevChar = c;
                    continue;
                }

                result.Append(c);
                prevChar = c;
            }

            return result.ToString();
        }

        /// <summary>
        /// Strips OpenRouter CLI formatting from the response.
        /// Handles: nvm messages, ASCII boxes, line numbers, token counts, AI explanations.
        /// Also handles wrapped line continuations (lines without numbers in code section).
        /// </summary>
        private string StripCLIFormatting(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            var lines = response.Split('\n');
            var codeLines = new List<string>();
            var inCodeSection = false;
            var lastLineNumber = 0;

            foreach (var line in lines)
            {
                // Skip nvm messages
                if (line.StartsWith("Now using node"))
                    continue;

                // Skip ASCII box borders and headers (both +-- and Unicode ┌── styles)
                if (line.StartsWith("+--") || line.StartsWith("| ") ||
                    line.StartsWith("└") || line.StartsWith("┌") || line.StartsWith("│"))
                    continue;

                // Skip token/cost info
                if (line.Contains("Tokens:") && line.Contains("Cost:"))
                    continue;

                // Skip model info lines
                if (line.Contains("model:") && line.Contains("domain:"))
                    continue;

                // Detect line-numbered code: "  1 using UnityEngine;" or " 10 {" etc.
                var lineNumberMatch = Regex.Match(line, @"^\s+(\d+)\s(.*)$");
                if (lineNumberMatch.Success)
                {
                    inCodeSection = true;
                    lastLineNumber = int.Parse(lineNumberMatch.Groups[1].Value);
                    codeLines.Add(lineNumberMatch.Groups[2].Value);
                    continue;
                }

                // If we're in code section and hit a non-numbered line
                if (inCodeSection)
                {
                    // Empty lines - preserve code formatting
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        codeLines.Add("");
                        continue;
                    }

                    var trimmedLine = line.Trim();

                    // Check if this looks like AI explanation paragraph (not code) - stop collecting
                    // Must be a complete sentence pattern, not just starting words that could be in comments
                    // Only trigger on lines that are clearly outside the code (after all numbered lines)
                    if (!trimmedLine.Contains("{") && !trimmedLine.Contains("}") &&
                        !trimmedLine.Contains(";") && !trimmedLine.Contains("//") &&
                        (trimmedLine.StartsWith("Note that ") || trimmedLine.StartsWith("I've ") ||
                         trimmedLine.StartsWith("Please ") || trimmedLine.StartsWith("Remember ") ||
                         trimmedLine.StartsWith("Make sure ") || trimmedLine.StartsWith("I ") ||
                         trimmedLine.StartsWith("The code ") || trimmedLine.StartsWith("The above ")))
                    {
                        PholusLogger.Log($"StripCLI: Stopped at explanation line (after line {lastLineNumber}): '{trimmedLine.Substring(0, Math.Min(50, trimmedLine.Length))}'");
                        break;
                    }

                    // In CLI wrapped output, non-numbered lines are continuations of the previous line
                    // Always append to previous line to reconstruct the original
                    if (codeLines.Count > 0)
                    {
                        codeLines[codeLines.Count - 1] = codeLines[codeLines.Count - 1] + " " + trimmedLine;
                    }
                }
            }

            PholusLogger.Log($"StripCLI: Extracted {codeLines.Count} lines, last numbered line was {lastLineNumber}");
            return string.Join("\n", codeLines).Trim();
        }

        private string ExtractJson(string response)
        {
            // Try to find JSON in code blocks first
            var codeBlockMatch = Regex.Match(response, @"```(?:json)?\s*\n([\s\S]*?)\n```", RegexOptions.Multiline);
            if (codeBlockMatch.Success)
            {
                return codeBlockMatch.Groups[1].Value.Trim();
            }

            // Try to find raw JSON (starts with {)
            var jsonStart = response.IndexOf('{');
            if (jsonStart >= 0)
            {
                // Find matching closing brace, accounting for strings
                var braceCount = 0;
                var inString = false;
                var escapeNext = false;

                for (var i = jsonStart; i < response.Length; i++)
                {
                    var c = response[i];

                    if (escapeNext)
                    {
                        escapeNext = false;
                        continue;
                    }

                    if (c == '\\' && inString)
                    {
                        escapeNext = true;
                        continue;
                    }

                    if (c == '"' && !escapeNext)
                    {
                        inString = !inString;
                        continue;
                    }

                    if (!inString)
                    {
                        if (c == '{') braceCount++;
                        if (c == '}') braceCount--;

                        if (braceCount == 0)
                        {
                            return response.Substring(jsonStart, i - jsonStart + 1);
                        }
                    }
                }
            }

            return null;
        }

        private AnalysisResult ParseManually(string json)
        {
            // Simple JSON parsing for our specific format
            // Unity's JsonUtility doesn't handle all our nested structures well
            var result = new AnalysisResult
            {
                DefiniteIssues = new List<PerformanceIssue>(),
                ContextualIssues = new List<PerformanceIssue>(),
                Suggestions = new List<PerformanceIssue>(),
                PositiveNotes = new List<string>()
            };

            // Parse score
            result.Score = ParseInt(json, "score", 50);

            // Parse summary
            result.Summary = ParseString(json, "summary") ?? "Analysis complete";

            // Parse issue arrays
            result.DefiniteIssues = ParseIssueArray(json, "definite_issues");
            result.ContextualIssues = ParseIssueArray(json, "contextual_issues");
            result.Suggestions = ParseIssueArray(json, "suggestions");

            // Parse positive notes
            result.PositiveNotes = ParseStringArray(json, "positive_notes");

            return result;
        }

        private int ParseInt(string json, string key, int defaultValue)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        private string ParseString(string json, string key)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
            if (match.Success)
            {
                return UnescapeString(match.Groups[1].Value);
            }
            return null;
        }

        private List<PerformanceIssue> ParseIssueArray(string json, string arrayKey)
        {
            var issues = new List<PerformanceIssue>();

            // Find the array
            var arrayMatch = Regex.Match(json, $@"""{arrayKey}""\s*:\s*\[([\s\S]*?)\](?=\s*[,}}])");
            if (!arrayMatch.Success)
            {
                return issues;
            }

            var arrayContent = arrayMatch.Groups[1].Value;

            // Split into individual objects
            var objectMatches = Regex.Matches(arrayContent, @"\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");

            foreach (Match objMatch in objectMatches)
            {
                var objContent = "{" + objMatch.Groups[1].Value + "}";
                var issue = ParseIssue(objContent);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }

            return issues;
        }

        private PerformanceIssue ParseIssue(string json)
        {
            try
            {
                // Skip objects that are too short to be valid issues (placeholders like {platform})
                if (json.Length < 50)
                {
                    return null;
                }

                // Skip template/schema objects (contain pipe characters like "critical|high")
                if (json.Contains("|"))
                {
                    return null;
                }

                // Skip if doesn't contain any recognizable issue field
                if (!json.Contains("\"title\"") && !json.Contains("\"name\"") &&
                    !json.Contains("\"issue\"") && !json.Contains("\"severity\"") &&
                    !json.Contains("\"line\"") && !json.Contains("\"explanation\""))
                {
                    return null;
                }

                // Try multiple possible field names for robustness across providers
                var title = ParseString(json, "title")
                    ?? ParseString(json, "name")
                    ?? ParseString(json, "issue")
                    ?? ParseString(json, "description");

                var line = ParseInt(json, "line", -1);
                if (line == -1)
                {
                    line = ParseInt(json, "lineNumber", 0);
                    if (line == -1) line = ParseInt(json, "line_number", 0);
                }

                var confidencePercent = ParseInt(json, "confidence_percent", -1);
                if (confidencePercent == -1)
                {
                    confidencePercent = ParseInt(json, "confidencePercent", -1);
                    if (confidencePercent == -1) confidencePercent = ParseInt(json, "confidence_score", 50);
                }

                // Skip if no title found - not a valid issue object
                if (string.IsNullOrEmpty(title))
                {
                    PholusLogger.Log($"Skipping non-issue object: {json.Substring(0, Math.Min(100, json.Length))}...");
                    return null;
                }

                var issue = new PerformanceIssue
                {
                    Title = NormalizeTitle(title),
                    Line = line,
                    ConfidencePercent = confidencePercent,
                    CodeSnippet = ParseString(json, "code_snippet") ?? ParseString(json, "codeSnippet") ?? ParseString(json, "code"),
                    Explanation = ParseString(json, "explanation") ?? ParseString(json, "reason") ?? ParseString(json, "details"),
                    Impact = ParseString(json, "impact") ?? ParseString(json, "performance_impact"),
                    WhyIMightBeWrong = ParseString(json, "why_i_might_be_wrong") ?? ParseString(json, "whyIMightBeWrong"),
                    ProblemIf = ParseString(json, "problem_if") ?? ParseString(json, "problemIf"),
                    AcceptableIf = ParseString(json, "acceptable_if") ?? ParseString(json, "acceptableIf"),
                    IssueType = ParseString(json, "issue_type") ?? ParseString(json, "issueType") ?? ParseString(json, "type")
                };

                // Parse severity - try multiple variations
                var severityStr = (ParseString(json, "severity") ?? ParseString(json, "level") ?? "medium").ToLower();
                issue.Severity = severityStr switch
                {
                    "critical" => IssueSeverity.Critical,
                    "high" => IssueSeverity.High,
                    "medium" => IssueSeverity.Medium,
                    "low" => IssueSeverity.Low,
                    _ => IssueSeverity.Medium
                };

                // Parse confidence
                var confidenceStr = (ParseString(json, "confidence") ?? "medium").ToLower();
                issue.Confidence = confidenceStr switch
                {
                    "high" => IssueConfidence.High,
                    "medium" => IssueConfidence.Medium,
                    "low" => IssueConfidence.Low,
                    _ => IssueConfidence.Medium
                };

                return issue;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"ParseIssue failed: {ex.Message}");
                return null;
            }
        }

        private List<string> ParseStringArray(string json, string arrayKey)
        {
            var result = new List<string>();

            var arrayMatch = Regex.Match(json, $@"""{arrayKey}""\s*:\s*\[([\s\S]*?)\]");
            if (!arrayMatch.Success)
            {
                return result;
            }

            var arrayContent = arrayMatch.Groups[1].Value;
            var stringMatches = Regex.Matches(arrayContent, @"""([^""\\]*(?:\\.[^""\\]*)*)""");

            foreach (Match match in stringMatches)
            {
                result.Add(UnescapeString(match.Groups[1].Value));
            }

            return result;
        }

        private string UnescapeString(string str)
        {
            var unescaped = str
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");

            // Clean up artificial line breaks from CLI output formatting
            return CleanupLineBreaks(unescaped);
        }

        /// <summary>
        /// Normalizes issue titles to be concise and consistent across providers.
        /// Some providers (e.g., OpenRouter) return verbose titles with prefixes or explanations.
        /// </summary>
        private string NormalizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            var normalized = title.Trim();

            // Remove snake_case prefixes like "allocation_in_update: ..."
            var colonIndex = normalized.IndexOf(':');
            if (colonIndex > 0 && colonIndex < normalized.Length - 1)
            {
                var prefix = normalized.Substring(0, colonIndex);
                // Only remove if prefix looks like a category (snake_case or short)
                if (prefix.Contains("_") || prefix.Length < 25)
                {
                    normalized = normalized.Substring(colonIndex + 1).Trim();
                }
            }

            // Remove parenthetical suffixes like "... (should be refactored)"
            var parenIndex = normalized.LastIndexOf('(');
            if (parenIndex > 10 && normalized.EndsWith(")"))
            {
                normalized = normalized.Substring(0, parenIndex).Trim();
            }

            // Truncate very long titles (keep first ~60 chars at word boundary)
            if (normalized.Length > 70)
            {
                var truncateAt = normalized.LastIndexOf(' ', 60);
                if (truncateAt > 30)
                {
                    normalized = normalized.Substring(0, truncateAt).Trim();
                }
                else
                {
                    normalized = normalized.Substring(0, 60).Trim();
                }
            }

            // Capitalize first letter
            if (normalized.Length > 0 && char.IsLower(normalized[0]))
            {
                normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);
            }

            return normalized;
        }

        /// <summary>
        /// Removes artificial line breaks added by CLI markdown rendering.
        /// Preserves intentional paragraph breaks (double newlines).
        /// </summary>
        private string CleanupLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // First, normalize line endings
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // Preserve double newlines (paragraph breaks) by replacing with placeholder
            text = text.Replace("\n\n", "<<<PARA>>>");

            // Replace single newlines with spaces (these are CLI word-wrap artifacts)
            text = text.Replace("\n", " ");

            // Restore paragraph breaks
            text = text.Replace("<<<PARA>>>", "\n\n");

            // Clean up multiple spaces
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text.Trim();
        }
    }
}
