using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Core;
using UnityEngine;

namespace Pholus.Editor.Consensus
{
    /// <summary>
    /// Parses the director's verdict response into a DirectorVerdict object.
    /// </summary>
    public static class DirectorVerdictParser
    {
        /// <summary>
        /// Parses a JSON response from the director into a DirectorVerdict.
        /// </summary>
        public static DirectorVerdict Parse(string response)
        {
            var verdict = new DirectorVerdict
            {
                Items = new List<VerdictItem>()
            };

            if (string.IsNullOrWhiteSpace(response))
            {
                verdict.Summary = "Empty response from director";
                return verdict;
            }

            try
            {
                // Log the raw director response for debugging
                var truncated = response.Length > 1000 ? response.Substring(0, 1000) + "..." : response;
                PholusLogger.Log($"Consensus: Director raw response ({response.Length} chars):\n{truncated}");

                // Extract JSON from response (might be wrapped in markdown)
                var json = ExtractJson(response);

                if (string.IsNullOrEmpty(json))
                {
                    PholusLogger.LogWarning("Consensus: No JSON found in director response");
                    verdict.Summary = "Failed to parse director response";
                    return verdict;
                }

                // Parse the verdict array - find the array bounds properly
                var verdictKeyIndex = json.IndexOf("\"verdict\"");
                if (verdictKeyIndex >= 0)
                {
                    var arrayStart = json.IndexOf('[', verdictKeyIndex);
                    if (arrayStart >= 0)
                    {
                        var arrayContent = ExtractArrayContent(json, arrayStart);
                        if (!string.IsNullOrEmpty(arrayContent))
                        {
                            var items = ParseVerdictItems(arrayContent);
                            verdict.Items = items;
                            PholusLogger.Log($"Consensus: Extracted {items.Count} verdict items from array");
                        }
                    }
                }

                // Parse summary
                var summaryMatch = Regex.Match(json, @"""summary""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
                if (summaryMatch.Success)
                {
                    verdict.Summary = UnescapeString(summaryMatch.Groups[1].Value);
                }
                else
                {
                    verdict.Summary = $"Reviewed {verdict.Items.Count} issues";
                }

                PholusLogger.Log($"Consensus: Parsed verdict: {verdict.ValidCount} valid, {verdict.DismissedCount} dismissed");
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Consensus: Failed to parse verdict: {ex.Message}");
                verdict.Summary = $"Parse error: {ex.Message}";
            }

            return verdict;
        }

        private static string ExtractJson(string response)
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

        private static List<VerdictItem> ParseVerdictItems(string arrayContent)
        {
            var items = new List<VerdictItem>();

            // Find top-level objects in the array by tracking braces properly
            var objectStrings = ExtractTopLevelObjects(arrayContent);

            foreach (var objContent in objectStrings)
            {
                var item = ParseVerdictItem(objContent);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static string ExtractObjectContent(string json, int objectStart)
        {
            var braceCount = 0;
            var inString = false;
            var escapeNext = false;

            for (var i = objectStart; i < json.Length; i++)
            {
                var c = json[i];

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
                        // Return content between braces (excluding the braces themselves)
                        return json.Substring(objectStart + 1, i - objectStart - 1);
                    }
                }
            }

            return null;
        }

        private static string ExtractArrayContent(string json, int arrayStart)
        {
            var bracketCount = 0;
            var inString = false;
            var escapeNext = false;

            for (var i = arrayStart; i < json.Length; i++)
            {
                var c = json[i];

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
                    if (c == '[') bracketCount++;
                    if (c == ']') bracketCount--;

                    if (bracketCount == 0)
                    {
                        // Return content between brackets (excluding the brackets themselves)
                        return json.Substring(arrayStart + 1, i - arrayStart - 1);
                    }
                }
            }

            return null;
        }

        private static List<string> ExtractTopLevelObjects(string arrayContent)
        {
            var objects = new List<string>();
            var braceCount = 0;
            var inString = false;
            var escapeNext = false;
            var objectStart = -1;

            for (var i = 0; i < arrayContent.Length; i++)
            {
                var c = arrayContent[i];

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
                    if (c == '{')
                    {
                        if (braceCount == 0)
                        {
                            objectStart = i;
                        }
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && objectStart >= 0)
                        {
                            var objStr = arrayContent.Substring(objectStart, i - objectStart + 1);
                            objects.Add(objStr);
                            objectStart = -1;
                        }
                    }
                }
            }

            return objects;
        }

        private static VerdictItem ParseVerdictItem(string json)
        {
            try
            {
                var item = new VerdictItem
                {
                    ProviderOpinions = new Dictionary<ProviderType, ProviderOpinion>()
                };

                // Parse issue title
                item.IssueTitle = ParseString(json, "issue") ?? ParseString(json, "title") ?? "Unknown Issue";

                // Parse line
                item.Line = ParseInt(json, "line", 0);

                // Parse is_valid - default to TRUE if not found (validate by default)
                var isValidResult = ParseBoolWithDefault(json, "is_valid", true);
                if (!isValidResult.HasValue)
                {
                    isValidResult = ParseBoolWithDefault(json, "isValid", true);
                }
                item.IsValid = isValidResult ?? true;

                PholusLogger.Log($"Consensus: Parsed verdict item: '{item.IssueTitle}' line {item.Line}, is_valid={item.IsValid}");

                // Parse final_severity
                var severityStr = ParseString(json, "final_severity") ?? ParseString(json, "finalSeverity");
                if (!string.IsNullOrEmpty(severityStr))
                {
                    item.FinalSeverity = ParseSeverity(severityStr);
                }

                // Parse director_reasoning
                item.DirectorReasoning = ParseString(json, "director_reasoning")
                    ?? ParseString(json, "directorReasoning")
                    ?? ParseString(json, "reasoning");

                // Parse provider_opinions - find the object bounds properly
                var opinionsKeyIndex = json.IndexOf("\"provider_opinions\"");
                if (opinionsKeyIndex >= 0)
                {
                    var opinionsObjStart = json.IndexOf('{', opinionsKeyIndex);
                    if (opinionsObjStart >= 0)
                    {
                        var opinionsContent = ExtractObjectContent(json, opinionsObjStart);
                        if (!string.IsNullOrEmpty(opinionsContent))
                        {
                            PholusLogger.Log($"Consensus: Provider opinions content ({opinionsContent.Length} chars): {opinionsContent.Substring(0, Math.Min(200, opinionsContent.Length))}...");
                            ParseProviderOpinions(opinionsContent, item.ProviderOpinions);
                            // Re-assign to sync backing lists for Unity serialization
                            item.ProviderOpinions = item.ProviderOpinions;
                            PholusLogger.Log($"Consensus: Parsed {item.ProviderOpinions.Count} provider opinions");
                        }
                        else
                        {
                            PholusLogger.LogWarning("Consensus: Failed to extract provider_opinions content");
                        }
                    }
                }
                else
                {
                    PholusLogger.LogWarning("Consensus: No provider_opinions key found in verdict item");
                }

                return item;
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Consensus: Failed to parse verdict item: {ex.Message}");
                return null;
            }
        }

        private static void ParseProviderOpinions(string content, Dictionary<ProviderType, ProviderOpinion> opinions)
        {
            // Parse each provider's opinion using proper object extraction
            var providers = new[] { ("Claude", ProviderType.Claude), ("Codex", ProviderType.Codex), ("Gemini", ProviderType.Gemini), ("Cursor", ProviderType.Cursor), ("OpenRouter", ProviderType.OpenRouter) };

            foreach (var (name, type) in providers)
            {
                // Find the provider key
                var keyIndex = content.IndexOf($"\"{name}\"", StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) continue;

                // Find the opening brace of the provider's opinion object
                var objStart = content.IndexOf('{', keyIndex);
                if (objStart < 0) continue;

                // Extract the opinion object content (pass the full content and the start position)
                var opinionContent = ExtractObjectContent(content, objStart);
                if (string.IsNullOrEmpty(opinionContent)) continue;

                var found = ParseBool(opinionContent, "found");
                var opinionText = ParseString(opinionContent, "opinion");

                PholusLogger.Log($"Consensus: Provider {name}: found={found}, opinion={opinionText?.Substring(0, Math.Min(50, opinionText?.Length ?? 0))}...");

                opinions[type] = new ProviderOpinion
                {
                    FoundIssue = found,
                    Opinion = opinionText
                };
            }
        }

        private static string ParseString(string json, string key)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
            if (match.Success)
            {
                return UnescapeString(match.Groups[1].Value);
            }

            // Try null value
            var nullMatch = Regex.Match(json, $@"""{key}""\s*:\s*null");
            if (nullMatch.Success)
            {
                return null;
            }

            return null;
        }

        private static int ParseInt(string json, string key, int defaultValue)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        private static bool ParseBool(string json, string key)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*(true|false)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.ToLower() == "true";
            }
            return false;
        }

        private static bool? ParseBoolWithDefault(string json, string key, bool defaultValue)
        {
            var match = Regex.Match(json, $@"""{key}""\s*:\s*(true|false)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.ToLower() == "true";
            }
            return null; // Return null to indicate not found
        }

        private static IssueSeverity? ParseSeverity(string severityStr)
        {
            return severityStr?.ToLower() switch
            {
                "critical" => IssueSeverity.Critical,
                "high" => IssueSeverity.High,
                "medium" => IssueSeverity.Medium,
                "low" => IssueSeverity.Low,
                _ => null
            };
        }

        private static string UnescapeString(string str)
        {
            return str
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }
}
