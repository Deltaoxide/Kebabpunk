using System;
using System.Collections.Generic;
using System.Linq;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Consensus;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.UI
{
    /// <summary>
    /// Renders the list of issues grouped by category.
    /// </summary>
    public class IssueListView
    {
#pragma warning disable CS0067 // Event is never used (reserved for future use)
        public event Action<PerformanceIssue> OnIssueSelected;
#pragma warning restore CS0067
        public event Action<PerformanceIssue> OnFixRequested;
        public event Action<PerformanceIssue> OnSkipRequested;

        private Vector2 _scrollPosition;
        private PerformanceIssue _selectedIssue;
        private bool _definiteFoldout = true;
        private bool _contextualFoldout = true;
        private bool _suggestionsFoldout = true;
        private bool _dismissedFoldout = false; // Collapsed by default

        private readonly Dictionary<string, bool> _issueFoldouts = new();
        private readonly HashSet<string> _expandedIssues = new();

        // Consensus mode support
        private ConsensusResult _consensusResult;
        private bool _showProviderBreakdown = true;

        // Fix generation state
        private bool _isFixInProgress;

        public PerformanceIssue SelectedIssue => _selectedIssue;

        /// <summary>
        /// Sets whether a fix is currently being generated.
        /// When true, all Fix buttons are disabled.
        /// </summary>
        public void SetFixInProgress(bool inProgress)
        {
            _isFixInProgress = inProgress;
        }

        /// <summary>
        /// Sets the consensus result for showing provider debate breakdown.
        /// </summary>
        public void SetConsensusResult(ConsensusResult result, bool showBreakdown = true)
        {
            _consensusResult = result;
            _showProviderBreakdown = showBreakdown;
        }

        /// <summary>
        /// Clears the consensus result.
        /// </summary>
        public void ClearConsensusResult()
        {
            _consensusResult = null;
        }

        public void Draw(AnalysisResult result)
        {
            Styles.EnsureInitialized();

            // Check if there are any issues OR dismissed issues to show
            var hasContent = result != null &&
                (result.HasIssues || (result.DismissedIssues?.Count > 0));

            if (!hasContent)
            {
                DrawNoIssues(result);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Only show groups that have active (non-fixed, non-skipped) issues
            var activeDefinite = result.DefiniteIssues.Where(i => !i.IsFixed && !i.IsSkipped).ToList();
            var activeContextual = result.ContextualIssues.Where(i => !i.IsFixed && !i.IsSkipped).ToList();
            var activeSuggestions = result.Suggestions.Where(i => !i.IsFixed && !i.IsSkipped).ToList();

            // Definite issues
            if (activeDefinite.Count > 0)
            {
                DrawIssueGroup(
                    "DEFINITE ISSUES",
                    Styles.GetCategoryIcon(IssueCategory.Definite),
                    result.DefiniteIssues,
                    ref _definiteFoldout);
            }

            // Contextual issues
            if (activeContextual.Count > 0)
            {
                DrawIssueGroup(
                    "DEPENDS ON CONTEXT",
                    Styles.GetCategoryIcon(IssueCategory.Contextual),
                    result.ContextualIssues,
                    ref _contextualFoldout);
            }

            // Suggestions
            if (activeSuggestions.Count > 0)
            {
                DrawIssueGroup(
                    "SUGGESTIONS",
                    Styles.GetCategoryIcon(IssueCategory.Suggestion),
                    result.Suggestions,
                    ref _suggestionsFoldout);
            }

            // Show success message if all issues have been fixed
            var totalActive = activeDefinite.Count + activeContextual.Count + activeSuggestions.Count;
            var totalOriginal = result.DefiniteIssues.Count + result.ContextualIssues.Count + result.Suggestions.Count;
            if (totalActive == 0 && totalOriginal > 0)
            {
                PholusEditorGUI.BeginVerticalBackground();
                var prevColor = GUI.color;
                GUI.color = Styles.SuccessColor;
                GUILayout.Label($"✓ All {totalOriginal} issues fixed!", Styles.HeaderStyle);
                GUI.color = prevColor;
                EditorGUILayout.Space(5);
                var noteStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    fontStyle = FontStyle.Italic
                };
                GUILayout.Label("Re-analyze to verify changes or find new issues.", noteStyle);
                PholusEditorGUI.EndVerticalBackground();
            }

            // Show "all clear" if only dismissed issues exist
            if (!result.HasIssues && result.DismissedIssues?.Count > 0)
            {
                PholusEditorGUI.BeginVerticalBackground();
                var prevColor = GUI.color;
                GUI.color = Styles.SuccessColor;
                GUILayout.Label("\u2713 No active performance issues!", Styles.HeaderStyle);
                GUI.color = prevColor;
                GUILayout.Label("All detected issues were dismissed as false positives by consensus.", Styles.LabelMuted);
                PholusEditorGUI.EndVerticalBackground();
            }

            // Dismissed (consensus mode only)
            if (result.DismissedIssues?.Count > 0)
            {
                DrawDismissedGroup(result.DismissedIssues);
            }

            // Positive notes
            if (result.PositiveNotes.Count > 0)
            {
                DrawPositiveNotes(result.PositiveNotes);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawNoIssues(AnalysisResult result)
        {
            PholusEditorGUI.BeginVerticalBackground();

            var prevColor = GUI.color;
            GUI.color = Styles.SuccessColor;
            GUILayout.Label("\u2713 No performance issues detected!", Styles.HeaderStyle);
            GUI.color = prevColor;

            if (result?.PositiveNotes.Count > 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label("Good practices observed:", Styles.LabelBold);
                foreach (var note in result.PositiveNotes)
                {
                    GUILayout.Label($"  \u2022 {note}", Styles.RichTextLabel);
                }
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawIssueGroup(
            string title,
            string icon,
            List<PerformanceIssue> issues,
            ref bool foldout)
        {
            var activeIssues = issues.Where(i => !i.IsSkipped && !i.IsFixed).ToList();
            var resolvedCount = issues.Count - activeIssues.Count;

            var countText = resolvedCount > 0
                ? $"{title} ({activeIssues.Count}/{issues.Count})"
                : $"{title} ({activeIssues.Count})";

            // Get icon and color based on category
            Texture2D foldoutIcon;
            Color foldoutColor;
            if (title.Contains("DEFINITE"))
            {
                foldoutIcon = PholusEditorGUI.GetIcon("Error");
                foldoutColor = Styles.CriticalColor;
            }
            else if (title.Contains("CONTEXT"))
            {
                foldoutIcon = PholusEditorGUI.GetIcon("Zoom");
                foldoutColor = Styles.HighColor;
            }
            else
            {
                foldoutIcon = PholusEditorGUI.GetIcon("Edit");
                foldoutColor = Styles.SuccessColor;
            }

            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            // Tint the foldout icon with category color
            var prevColor = GUI.color;
            GUI.color = foldoutColor;
            var isOpen = PholusEditorGUI.DrawFoldout(ref foldout, countText, foldoutIcon);
            GUI.color = prevColor;

            if (isOpen)
            {
                PholusEditorGUI.BeginContainer();

                // Draw issues
                foreach (var issue in issues)
                {
                    if (issue.IsSkipped || issue.IsFixed)
                    {
                        continue; // Skip resolved issues
                    }

                    DrawIssueDetailed(issue);
                }

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawIssueDetailed(PerformanceIssue issue)
        {
            var isSelected = _selectedIssue == issue;
            var severityColor = Styles.GetSeverityColor(issue.Severity);
            var issueId = issue.Id ?? $"{issue.Line}_{issue.Title}";
            var isExpanded = _expandedIssues.Contains(issueId);

            // Set background color based on selection
            var defaultBG = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0f, 0.5f, 1f, 0.5f);
            }

            // Start tracking the rect for color strip
            var itemRect = EditorGUILayout.BeginVertical();
            PholusEditorGUI.BeginVerticalBackground();
            GUI.backgroundColor = defaultBG;

            // Content area with left padding for color strip
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12); // Space for color strip
            EditorGUILayout.BeginVertical();

            // Header row (entire row is clickable to expand/collapse)
            var headerRect = EditorGUILayout.BeginHorizontal();

            // Expand/collapse arrow - use a visible style
            var arrowIcon = isExpanded ? "\u25BC" : "\u25B6"; // ▼ or ▶
            var arrowStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                hover = { textColor = Color.white }
            };
            GUILayout.Label(arrowIcon, arrowStyle, GUILayout.Width(16));

            // Severity badge with color
            var severityPrevColor = GUI.color;
            GUI.color = severityColor;
            GUILayout.Label($"{Styles.GetSeverityIcon(issue.Severity)} {issue.Severity}", Styles.SeverityBadge, GUILayout.Width(80));
            GUI.color = severityPrevColor;

            // Title
            GUILayout.Label(issue.Title, Styles.LabelBold);

            GUILayout.FlexibleSpace();

            // Line & Confidence
            GUILayout.Label($"Line {issue.Line} | {issue.ConfidencePercent}%", Styles.LabelMuted);

            EditorGUILayout.EndHorizontal();

            // Make entire header row clickable
            if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
            {
                if (isExpanded)
                    _expandedIssues.Remove(issueId);
                else
                    _expandedIssues.Add(issueId);
                Event.current.Use();
            }

            // Change cursor to hand when hovering over header
            EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);

            // Expanded content
            if (isExpanded)
            {
                // Code snippet
                if (!string.IsNullOrEmpty(issue.CodeSnippet))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.SelectableLabel(issue.CodeSnippet, Styles.CodeStyle, GUILayout.Height(36));
                }

                // Explanation
                if (!string.IsNullOrEmpty(issue.Explanation))
                {
                    EditorGUILayout.Space(4);
                    GUILayout.Label(issue.Explanation, Styles.RichTextLabel);
                }

                // Why I might be wrong
                if (!string.IsNullOrEmpty(issue.WhyIMightBeWrong))
                {
                    EditorGUILayout.Space(4);
                    PholusEditorGUI.DrawInfoBox($"Why I might be wrong: {issue.WhyIMightBeWrong}");
                }

                // Provider Debate (Consensus Mode)
                if (_showProviderBreakdown && _consensusResult?.Verdict != null)
                {
                    DrawProviderDebate(issue);
                }

                // Impact
                if (!string.IsNullOrEmpty(issue.Impact))
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Impact:", Styles.LabelBold, GUILayout.Width(50));
                    GUILayout.Label(issue.Impact, Styles.RichTextLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(6);

                // Action buttons - inside content area so they stay within bounds
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // Disable Fix button when a fix is in progress
                GUI.enabled = !_isFixInProgress;
                var fixLabel = _isFixInProgress ? "Generating..." : "Fix This Issue";
                var fixColor = _isFixInProgress
                    ? new Color(0.5f, 0.5f, 0.5f, 0.25f)
                    : new Color(0.2f, 1.0f, 0.2f, 0.25f);
                if (PholusEditorGUI.DrawButton(fixLabel, PholusEditorGUI.GetIcon("CodeGen"), reverse: true, width: 130, height: 28, iconSize: 14, textSize: 13, tintColor: fixColor))
                {
                    OnFixRequested?.Invoke(issue);
                }
                GUI.enabled = true;

                GUILayout.Space(8);

                if (PholusEditorGUI.DrawButton("Skip", PholusEditorGUI.GetIcon("ArrowMinimize"), reverse: true, width: 70, height: 28, iconSize: 14, textSize: 13))
                {
                    issue.IsSkipped = true;
                    OnSkipRequested?.Invoke(issue);
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Collapsed: show compact buttons
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // Disable Fix button when a fix is in progress
                GUI.enabled = !_isFixInProgress;
                var compactFixColor = _isFixInProgress
                    ? new Color(0.5f, 0.5f, 0.5f, 0.25f)
                    : new Color(0.2f, 1.0f, 0.2f, 0.25f);
                if (PholusEditorGUI.DrawButton("Fix", PholusEditorGUI.GetIcon("CodeGen"), reverse: true, width: 55, height: 22, iconSize: 12, textSize: 11, tintColor: compactFixColor))
                {
                    OnFixRequested?.Invoke(issue);
                }
                GUI.enabled = true;

                GUILayout.Space(4);

                if (PholusEditorGUI.DrawButton("Skip", PholusEditorGUI.GetIcon("ArrowMinimize"), reverse: true, width: 55, height: 22, iconSize: 12, textSize: 11))
                {
                    issue.IsSkipped = true;
                    OnSkipRequested?.Invoke(issue);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            PholusEditorGUI.EndVerticalBackground();
            EditorGUILayout.EndVertical();

            // Draw color strip overlay on the left edge (full height)
            if (Event.current.type == EventType.Repaint && itemRect.height > 0)
            {
                var colorStripRect = new Rect(itemRect.x, itemRect.y, 6, itemRect.height);
                EditorGUI.DrawRect(colorStripRect, severityColor);

                // Draw separator line at the bottom
                var separatorRect = new Rect(itemRect.x, itemRect.y + itemRect.height + 2, itemRect.width, 1);
                EditorGUI.DrawRect(separatorRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            }

            GUILayout.Space(8); // Spacing between items
        }

        private void DrawDismissedGroup(List<PerformanceIssue> dismissedIssues)
        {
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            // Muted color for dismissed section header
            var prevColor = GUI.color;
            GUI.color = new Color(0.7f, 0.65f, 0.6f); // Warm muted tone
            var isOpen = PholusEditorGUI.DrawFoldout(ref _dismissedFoldout, $"DISMISSED ({dismissedIssues.Count})", PholusEditorGUI.GetIcon("ArrowMinimize"));
            GUI.color = prevColor;

            if (isOpen)
            {
                PholusEditorGUI.BeginContainer();

                // Info text
                var infoStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.65f, 0.6f, 0.55f) },
                    fontStyle = FontStyle.Italic,
                    wordWrap = true
                };
                EditorGUILayout.LabelField("These issues were dismissed by the Director as false positives:", infoStyle);
                EditorGUILayout.Space(4);

                foreach (var issue in dismissedIssues)
                {
                    DrawDismissedIssue(issue);
                }

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawDismissedIssue(PerformanceIssue issue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with strikethrough effect via color
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.75f, 0.7f, 0.65f) }
            };
            GUILayout.Label($"\u2717 {issue.Title}", titleStyle); // ✗ symbol
            GUILayout.FlexibleSpace();
            var lineStyle = new GUIStyle(Styles.LabelMuted)
            {
                normal = { textColor = new Color(0.6f, 0.55f, 0.5f) }
            };
            GUILayout.Label($"Line {issue.Line}", lineStyle);
            EditorGUILayout.EndHorizontal();

            // Reason for dismissal
            if (!string.IsNullOrEmpty(issue.Explanation))
            {
                var reasonStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    wordWrap = true,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
                EditorGUILayout.LabelField($"Reason: {issue.Explanation}", reasonStyle);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(4);
        }

        private bool _positiveNotesFoldout = true;

        private void DrawPositiveNotes(List<string> notes)
        {
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            // Tint the foldout icon with success color
            var prevColor = GUI.color;
            GUI.color = Styles.SuccessColor;
            var isOpen = PholusEditorGUI.DrawFoldout(ref _positiveNotesFoldout, $"GOOD PRACTICES ({notes.Count})", PholusEditorGUI.GetIcon("Animation"));
            GUI.color = prevColor;

            if (isOpen)
            {
                PholusEditorGUI.BeginContainer();

                foreach (var note in notes)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label($"\u2022 {note}", Styles.RichTextLabel);
                    GUILayout.Space(6);
                    EditorGUILayout.EndHorizontal();
                }

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        public void ClearSelection()
        {
            _selectedIssue = null;
        }

        #region Provider Debate (Consensus Mode)

        private void DrawProviderDebate(PerformanceIssue issue)
        {
            // Find matching verdict item for this issue
            var verdictItem = FindVerdictItem(issue);
            if (verdictItem == null || verdictItem.ProviderOpinions == null || verdictItem.ProviderOpinions.Count == 0)
                return;

            EditorGUILayout.Space(6);

            // Draw debate box with dark background
            var debateRect = EditorGUILayout.BeginVertical();
            var bgColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(debateRect, bgColor);
            }

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();

            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUILayout.Label("Provider Opinions:", headerStyle);
            GUILayout.Space(4);

            // Draw each provider's opinion
            foreach (var (providerType, opinion) in verdictItem.ProviderOpinions)
            {
                DrawProviderOpinion(providerType, opinion);
            }

            // Director verdict
            GUILayout.Space(6);
            DrawDirectorVerdict(verdictItem);

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);

            EditorGUILayout.EndVertical();
        }

        private void DrawProviderOpinion(ProviderType providerType, ProviderOpinion opinion)
        {
            EditorGUILayout.BeginHorizontal();

            // Provider badge with color
            var color = GetProviderColor(providerType);
            var icon = GetProviderIcon(providerType);
            var name = GetProviderShortName(providerType);

            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = color },
                alignment = TextAnchor.MiddleLeft
            };

            GUILayout.Label($"{icon} {name}:", badgeStyle, GUILayout.Width(90));

            // Opinion text
            var opinionStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true
            };

            if (opinion.FoundIssue)
            {
                var opinionText = !string.IsNullOrEmpty(opinion.Opinion)
                    ? $"\"{TruncateText(opinion.Opinion, 80)}\""
                    : "Found this issue";
                opinionStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                GUILayout.Label(opinionText, opinionStyle);
            }
            else
            {
                opinionStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                opinionStyle.fontStyle = FontStyle.Italic;
                GUILayout.Label("(did not find this issue)", opinionStyle);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDirectorVerdict(VerdictItem verdictItem)
        {
            EditorGUILayout.BeginHorizontal();

            // Director icon
            var verdictStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            var foundCount = verdictItem.ProviderOpinions.Count(o => o.Value.FoundIssue);
            var totalCount = verdictItem.ProviderOpinions.Count;

            if (verdictItem.IsValid)
            {
                verdictStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
                GUILayout.Label($"\u2696 Director:", verdictStyle, GUILayout.Width(90));

                var reasonStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
                };
                GUILayout.Label($"VALID ({foundCount}/{totalCount} agree)", reasonStyle);
            }
            else
            {
                verdictStyle.normal.textColor = new Color(0.7f, 0.5f, 0.3f);
                GUILayout.Label($"\u2696 Director:", verdictStyle, GUILayout.Width(90));

                var reasonStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    normal = { textColor = new Color(0.7f, 0.5f, 0.3f) }
                };
                GUILayout.Label("DISMISSED", reasonStyle);
            }

            EditorGUILayout.EndHorizontal();

            // Director reasoning if available
            if (!string.IsNullOrEmpty(verdictItem.DirectorReasoning))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(90);
                var reasoningStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Italic,
                    wordWrap = true,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                GUILayout.Label($"\"{verdictItem.DirectorReasoning}\"", reasoningStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        private VerdictItem FindVerdictItem(PerformanceIssue issue)
        {
            if (_consensusResult?.Verdict?.Items == null)
                return null;

            // Try to find by line number and title similarity
            return _consensusResult.Verdict.Items.FirstOrDefault(v =>
                Math.Abs(v.Line - issue.Line) <= 3 &&
                (issue.Title?.Contains(v.IssueTitle ?? "", StringComparison.OrdinalIgnoreCase) == true ||
                 v.IssueTitle?.Contains(issue.Title ?? "", StringComparison.OrdinalIgnoreCase) == true ||
                 // Also check if the title starts with the agreement tag like "[2/3]"
                 issue.Title?.Contains(v.IssueTitle?.Split(']').LastOrDefault()?.Trim() ?? "", StringComparison.OrdinalIgnoreCase) == true));
        }

        private static Color GetProviderColor(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => new Color(0.4f, 0.6f, 1f),             // Blue
                ProviderType.Codex => new Color(0.4f, 0.8f, 0.4f),            // Green
                ProviderType.Gemini => new Color(1f, 0.7f, 0.3f),             // Orange
                ProviderType.Cursor => new Color(0.8f, 0.4f, 0.8f),           // Purple
                ProviderType.OpenRouter => new Color(1.0f, 0.1f, 0.1f, 0.65f),// Red
                _ => Color.white
            };
        }

        private static string GetProviderIcon(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => "\u25cf",  // ●
                ProviderType.Codex => "\u25cf",   // ●
                ProviderType.Gemini => "\u25cf",  // ●
                ProviderType.Cursor => "\u25cf",  // ●
                ProviderType.OpenRouter => "\u25cf", // ●
                _ => "\u25cb"                      // ○
            };
        }

        private static string GetProviderShortName(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => "Claude",
                ProviderType.Codex => "Codex",
                ProviderType.Gemini => "Gemini",
                ProviderType.Cursor => "Cursor",
                ProviderType.OpenRouter => "OpenRouter",
                _ => type.ToString()
            };
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
