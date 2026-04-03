using System;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Fixes.Models;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.UI
{
    /// <summary>
    /// Renders a diff preview for code changes.
    /// </summary>
    public class DiffPreviewView
    {
        public event Action OnApply;
        public event Action OnApplyAndDontAsk;
        public event Action OnCancel;

        private Vector2 _scrollPosition;
        private FixPreview _preview;

        public void SetPreview(FixPreview preview)
        {
            _preview = preview;
            _scrollPosition = Vector2.zero;
        }

        public void Draw()
        {
            Styles.EnsureInitialized();

            if (_preview == null)
            {
                GUILayout.Label("No preview available", Styles.LabelMuted);
                return;
            }

            if (!_preview.Success)
            {
                DrawError();
                return;
            }

            DrawHeader();
            DrawDiff();
            DrawFooter();
        }

        private void DrawHeader()
        {
            PholusEditorGUI.BeginVerticalBackground();

            PholusEditorGUI.DrawHeader("Preview Changes", "Edit");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"File: {System.IO.Path.GetFileName(_preview.ScriptPath)}", Styles.LabelBold);
            GUILayout.FlexibleSpace();
            GUILayout.Label(_preview.Diff.Summary, Styles.LabelMuted);
            EditorGUILayout.EndHorizontal();

            if (_preview.Issue != null)
            {
                GUILayout.Label($"Fix: {_preview.Issue.Title}", Styles.LabelMuted);
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawDiff()
        {
            PholusEditorGUI.BeginVerticalBackground();

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.MinHeight(200),
                GUILayout.MaxHeight(400));

            foreach (var line in _preview.Diff.Lines)
            {
                DrawDiffLine(line);
            }

            EditorGUILayout.EndScrollView();

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawDiffLine(DiffLine line)
        {
            var bgColor = line.Type switch
            {
                DiffLineType.Added => Styles.AddedLineColor,
                DiffLineType.Removed => Styles.RemovedLineColor,
                _ => Color.clear
            };

            var textColor = line.Type switch
            {
                DiffLineType.Added => Styles.SuccessColor,
                DiffLineType.Removed => Styles.CriticalColor,
                _ => GUI.skin.label.normal.textColor
            };

            // Draw background
            var rect = EditorGUILayout.BeginHorizontal();
            if (bgColor != Color.clear)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }

            // Line number
            var lineNum = line.Type switch
            {
                DiffLineType.Added => line.ModifiedLineNumber?.ToString() ?? "",
                DiffLineType.Removed => line.OriginalLineNumber?.ToString() ?? "",
                _ => line.OriginalLineNumber?.ToString() ?? ""
            };

            GUILayout.Label(lineNum, Styles.LabelMuted, GUILayout.Width(35));

            // Prefix
            var prefix = line.Prefix.ToString();
            var prevColor = GUI.color;
            GUI.color = textColor;
            GUILayout.Label(prefix, GUILayout.Width(15));

            // Content
            var style = new GUIStyle(Styles.CodeStyle)
            {
                normal = { textColor = textColor }
            };
            GUILayout.Label(line.Content, style);

            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawError()
        {
            PholusEditorGUI.BeginVerticalBackground();

            var prevColor = GUI.color;
            GUI.color = Styles.CriticalColor;
            GUILayout.Label("Fix generation failed", Styles.HeaderStyle);
            GUI.color = prevColor;

            PholusEditorGUI.DrawInfoBox(_preview.Error, PholusEditorGUI.InfoBoxType.Error);

            EditorGUILayout.Space(10);

            if (PholusEditorGUI.DrawButton("Close", PholusEditorGUI.GetIcon("ArrowMinimize"), reverse: true, width: 90, height: 28, iconSize: 14, textSize: 13))
            {
                OnCancel?.Invoke();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawFooter()
        {
            PholusEditorGUI.BeginHorizontalBackground();
            GUILayout.FlexibleSpace();

            if (PholusEditorGUI.DrawButton("Cancel", PholusEditorGUI.GetIcon("ArrowMinimize"), reverse: true, width: 100, height: 30, iconSize: 14, textSize: 13))
            {
                OnCancel?.Invoke();
            }

            GUILayout.Space(10);

            // Apply & Don't Ask - enables auto-apply for future fixes
            if (PholusEditorGUI.DrawButton("Apply & Don't Ask", PholusEditorGUI.GetIcon("Forward"), reverse: true, width: 150, height: 30, iconSize: 14, textSize: 13, tintColor: new Color(1.0f, 0.6f, 0.2f, 0.3f)))
            {
                OnApplyAndDontAsk?.Invoke();
            }

            GUILayout.Space(10);

            if (PholusEditorGUI.DrawButton("Apply Changes", PholusEditorGUI.GetIcon("Save"), reverse: true, width: 130, height: 30, iconSize: 14, textSize: 13, tintColor: new Color(0.2f, 1.0f, 0.2f, 0.25f)))
            {
                OnApply?.Invoke();
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        public void Clear()
        {
            _preview = null;
        }
    }
}
