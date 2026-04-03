using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.UI
{
    /// <summary>
    /// Centralized UI styles for Pholus.
    /// Single Responsibility: Only handles visual styling.
    /// </summary>
    public static class Styles
    {
        private static bool _initialized;

        // Colors
        public static readonly Color CriticalColor = new Color(1f, 0.3f, 0.3f);
        public static readonly Color HighColor = new Color(1f, 0.6f, 0.2f);
        public static readonly Color MediumColor = new Color(1f, 0.9f, 0.3f);
        public static readonly Color LowColor = new Color(0.5f, 0.9f, 0.5f);
        public static readonly Color SuccessColor = new Color(0.3f, 0.9f, 0.3f);

        public static readonly Color AddedLineColor = new Color(0.2f, 0.5f, 0.2f, 0.3f);
        public static readonly Color RemovedLineColor = new Color(0.5f, 0.2f, 0.2f, 0.3f);

        public static readonly Color HeaderBackground = new Color(0.2f, 0.2f, 0.2f);
        public static readonly Color CardBackground = new Color(0.25f, 0.25f, 0.25f);

        // Styles
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle SubHeaderStyle { get; private set; }
        public static GUIStyle CardStyle { get; private set; }
        public static GUIStyle IssueRowStyle { get; private set; }
        public static GUIStyle CodeStyle { get; private set; }
        public static GUIStyle LabelBold { get; private set; }
        public static GUIStyle LabelMuted { get; private set; }
        public static GUIStyle ScoreStyle { get; private set; }
        public static GUIStyle ButtonPrimary { get; private set; }
        public static GUIStyle ButtonSecondary { get; private set; }
        public static GUIStyle ButtonDanger { get; private set; }
        public static GUIStyle SeverityBadge { get; private set; }
        public static GUIStyle FoldoutStyle { get; private set; }
        public static GUIStyle RichTextLabel { get; private set; }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            Initialize();
            _initialized = true;
        }

        private static void Initialize()
        {
            // Header style
            HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                padding = new RectOffset(5, 5, 5, 5)
            };

            // Sub header
            SubHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                padding = new RectOffset(5, 5, 3, 3)
            };

            // Card container
            CardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            // Issue row
            IssueRowStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Code display
            CodeStyle = new GUIStyle(EditorStyles.textArea)
            {
                font = GetMonospaceFont(),
                fontSize = 11,
                wordWrap = false,
                padding = new RectOffset(5, 5, 5, 5)
            };

            // Bold label
            LabelBold = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };

            // Muted label
            LabelMuted = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            // Score display
            ScoreStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };

            // Primary button
            ButtonPrimary = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(15, 15, 5, 5)
            };

            // Secondary button
            ButtonSecondary = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 4, 4)
            };

            // Danger button
            ButtonDanger = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 4, 4)
            };

            // Severity badge
            SeverityBadge = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2)
            };

            // Foldout
            FoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            // Rich text label
            RichTextLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };
        }

        private static Font GetMonospaceFont()
        {
            // Try to get a monospace font
            var font = Font.CreateDynamicFontFromOSFont("Consolas", 11);
            if (font != null) return font;

            font = Font.CreateDynamicFontFromOSFont("Monaco", 11);
            if (font != null) return font;

            font = Font.CreateDynamicFontFromOSFont("Courier New", 11);
            if (font != null) return font;

            return null; // Fall back to default
        }

        public static Color GetSeverityColor(Analysis.Models.IssueSeverity severity)
        {
            return severity switch
            {
                Analysis.Models.IssueSeverity.Critical => CriticalColor,
                Analysis.Models.IssueSeverity.High => HighColor,
                Analysis.Models.IssueSeverity.Medium => MediumColor,
                Analysis.Models.IssueSeverity.Low => LowColor,
                _ => Color.white
            };
        }

        public static string GetSeverityIcon(Analysis.Models.IssueSeverity severity)
        {
            return severity switch
            {
                Analysis.Models.IssueSeverity.Critical => "\u26a0", // ⚠
                Analysis.Models.IssueSeverity.High => "\u26a0",    // ⚠
                Analysis.Models.IssueSeverity.Medium => "\u25cf",  // ●
                Analysis.Models.IssueSeverity.Low => "\u25cb",     // ○
                _ => "\u25cf"
            };
        }

        public static string GetCategoryIcon(Analysis.Models.IssueCategory category)
        {
            return category switch
            {
                Analysis.Models.IssueCategory.Definite => "\ud83c\udfaf",   // 🎯
                Analysis.Models.IssueCategory.Contextual => "\ud83e\udd14", // 🤔
                Analysis.Models.IssueCategory.Suggestion => "\ud83d\udca1", // 💡
                _ => "\u25cf"
            };
        }

        public static Color GetScoreColor(int score)
        {
            if (score >= 90) return SuccessColor;
            if (score >= 70) return LowColor;
            if (score >= 50) return MediumColor;
            if (score >= 30) return HighColor;
            return CriticalColor;
        }
    }
}
