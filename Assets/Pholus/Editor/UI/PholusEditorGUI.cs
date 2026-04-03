using System;
using System.Collections.Generic;
using Pholus.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.UI
{
    /// <summary>
    /// Utility class for creating consistent, styled Unity Editor UI elements.
    /// Provides methods for drawing backgrounds, buttons, toggles, dropdowns, and other common editor controls.
    /// </summary>
    public class PholusEditorGUI : UnityEditor.Editor
    {
        #region Constants - Dimensions and Sizing
        private const int ElementSpace = 3;
        private const double EditorRepaintInterval = 0.016; // Repaint at ~60 FPS
        private const int DefaultPadding = 4;
        private const int ContainerPadding = 5;
        private const int HeaderFontSize = 15;
        private const int FoldoutFontSize = 14;
        private const int ButtonFontSize = 13;
        private const int DropdownFontSize = 13;
        private const float DefaultButtonHeight = 26f;
        private const float DropdownHeight = 22f;
        private const float ToggleWidth = 24f;
        private const float ToggleHeight = 18f;
        private const float SeparatorHeight = 1.5f;
        private const float IconTextSpacing = 4f;
        private const int LabelWidth = 100;
        private const int FoldoutIconSize = 18;
        private const int HeaderIconSize = 16;
        private const float TextSpacing = 2.5f;
        private const float FoldoutTextSpacing = 2f;
        private const int BorderSize = 5;
        private const int ButtonBorderSize = 5;
        private const int ButtonPadding = 8;
        private const int DropdownPadding = 8;
        private const int DropdownRightPadding = 25;
        #endregion

        #region Constants - Layout Offsets and Spacing
        private const int ToggleOverflowLeft = 4;
        private const int ToggleOverflowRight = -4;
        private const int ToggleOverflowTop = -2;
        private const int ToggleOverflowBottom = -2;
        private const int TogglePaddingLeft = -33;
        private const int TogglePaddingRight = 33;
        private const int ToggleMargin = 2;
        private const int ArrayPropertyIndent = 16;
        private const int ArrayPropertyRightSpace = 7;
        private const int HeaderTopSpace = 16;
        private const int HeaderLeftSpace = -2;
        private const int HeaderIconWidth = 19;
        private const int HeaderBottomSpace = 6;
        private const int FoldoutPaddingHorizontal = 8;
        private const int FoldoutPaddingVertical = 7;
        private const int FoldoutIconLeftOffset = 27;
        private const int FoldoutIconTopOffset = 7;
        private const int FoldoutTextLeftOffset = 26;
        private const int FoldoutTextLeftOffsetWithIcon = 49;
        private const int FoldoutTextRightPadding = 6;
        private const int CenteredInspectorLeftSpace = -10;
        private const int TopSpacing = 6;
        private const int FoldoutSpacing = 5;
        #endregion

        #region Constants - InfoBox
        private const float InfoBoxMinHeight = 32;
        private const float InfoBoxIconSize = 20;
        private const int InfoBoxFontSize = 11;
        #endregion

        #region Constants - Color Alpha Values
        private const float ProSkinDynamicBgAlpha = 0.4f;
        private const float PersonalSkinDynamicBgAlpha = 0.2f;
        private const float ProSkinDynamicBgReverseAlpha = 0.16f;
        private const float PersonalSkinDynamicBgReverseAlpha = 0.5f;
        private const float ProSkinStaticBgAlpha = 0.2f;
        private const float PersonalSkinStaticBgAlpha = 0.1f;
        private const float ProSkinStaticBgReverseAlpha = 0.08f;
        private const float PersonalSkinStaticBgReverseAlpha = 0.25f;
        private const float ProSkinSeparatorAlpha = 0.2f;
        private const float PersonalSkinSeparatorAlpha = 0.25f;
        private const float ProSkinDropdownTextBrightness = 0.9f;
        private const float ButtonHoverMultiplier = 0.9f;
        private const float ButtonActiveMultiplier = 0.8f;
        #endregion

        #region Static Color Instances
        static private readonly Color ProSkinDynamicBgColor = new Color(0f, 0f, 0f, ProSkinDynamicBgAlpha);
        static private readonly Color PersonalSkinDynamicBgColor = new Color(0f, 0f, 0f, PersonalSkinDynamicBgAlpha);
        static private readonly Color ProSkinDynamicBgReverseColor = new Color(1f, 1f, 1f, ProSkinDynamicBgReverseAlpha);
        static private readonly Color PersonalSkinDynamicBgReverseColor = new Color(1f, 1f, 1f, PersonalSkinDynamicBgReverseAlpha);
        static private readonly Color ProSkinStaticBgColor = new Color(0f, 0f, 0f, ProSkinStaticBgAlpha);
        static private readonly Color PersonalSkinStaticBgColor = new Color(0f, 0f, 0f, PersonalSkinStaticBgAlpha);
        static private readonly Color ProSkinStaticBgReverseColor = new Color(1f, 1f, 1f, ProSkinStaticBgReverseAlpha);
        static private readonly Color PersonalSkinStaticBgReverseColor = new Color(1f, 1f, 1f, PersonalSkinStaticBgReverseAlpha);
        static private readonly Color ProSkinSeparatorColor = new Color(1f, 1f, 1f, ProSkinSeparatorAlpha);
        static private readonly Color PersonalSkinSeparatorColor = new Color(0f, 0f, 0f, PersonalSkinSeparatorAlpha);
        static private readonly Color ProSkinDropdownTextColor = new Color(ProSkinDropdownTextBrightness, ProSkinDropdownTextBrightness, ProSkinDropdownTextBrightness);
        #endregion

        #region Static RectOffset Instances
        static private readonly RectOffset BorderRectOffset = new RectOffset(BorderSize, BorderSize, BorderSize, BorderSize);
        static private readonly RectOffset ButtonBorderRectOffset = new RectOffset(ButtonBorderSize, ButtonBorderSize, ButtonBorderSize, ButtonBorderSize);
        static private readonly RectOffset ButtonPaddingRectOffset = new RectOffset(ButtonPadding, ButtonPadding, DefaultPadding, DefaultPadding);
        static private readonly RectOffset ToggleOverflowRectOffset = new RectOffset(ToggleOverflowLeft, ToggleOverflowRight, ToggleOverflowTop, ToggleOverflowBottom);
        static private readonly RectOffset ToggleMarginRectOffset = new RectOffset(ToggleMargin, ToggleMargin, ToggleMargin, ToggleMargin);
        static private readonly RectOffset TogglePaddingRectOffset = new RectOffset(TogglePaddingLeft, TogglePaddingRight, 0, 0);
        static private readonly RectOffset DefaultPaddingRectOffset = new RectOffset(DefaultPadding, DefaultPadding, DefaultPadding, DefaultPadding);
        static private readonly RectOffset DropdownPaddingRectOffset = new RectOffset(DropdownPadding, DropdownRightPadding, DefaultPadding, DefaultPadding);
        static private readonly RectOffset ZeroMarginRectOffset = new RectOffset(0, 0, 0, 0);
        static private readonly RectOffset ZeroPaddingRectOffset = new RectOffset(0, 0, 0, 0);
        static private readonly RectOffset HeaderMarginRectOffset = new RectOffset(0, 0, 2, 0);
        static private readonly RectOffset FoldoutPaddingRectOffset = new RectOffset(FoldoutPaddingHorizontal, FoldoutPaddingHorizontal, FoldoutPaddingVertical, FoldoutPaddingVertical);
        static private readonly RectOffset CenteredInspectorPaddingMore = new RectOffset(6, 8, 7, 0);
        static private readonly RectOffset CenteredInspectorPaddingNormal = new RectOffset(-8, 3, 3, 0);
        static private readonly RectOffset DefaultContainerPadding = new RectOffset(ContainerPadding, ContainerPadding, ContainerPadding, ContainerPadding);
        static private readonly RectOffset InfoBoxPadding = new RectOffset(8, 8, 6, 6);
        #endregion

        #region Cached GUI Styles
        static private GUIStyle _backgroundStyle;
        static private GUIStyle _dynamicBackgroundStyle;
        static private GUIStyle _buttonStyle;
        static private GUIStyle _toggleStyle;
        static private GUIStyle _foldoutStyle;
        static private GUIStyle _dropdownStyle;
        static private GUIStyle _dropdownButtonStyle;
        static private GUIStyle _infoBoxStyle;
        static private GUIStyle _infoBoxTextStyle;
        #endregion

        #region Cached Color Variables
        static private Color? _cachedTextColor;
        static private Color? _cachedDynamicBgColor;
        static private Color? _cachedDynamicBgColorReverse;
        static private Color? _cachedBgColor;
        static private Color? _cachedBgColorReverse;
        static private bool _lastProSkinState;
        #endregion

        #region Editor Repaint Management
        static private readonly HashSet<UnityEditor.Editor> _registeredEditors = new HashSet<UnityEditor.Editor>();
        static private double _lastRepaintTime;
        static private bool _needsRepaint;
        static private bool _updateHandlerRegistered;
        #endregion

        /// <summary>
        /// Enum representing different font weights available for the editor.
        /// </summary>
        public enum CustomFontType
        {
            Light = 0,
            Regular = 1,
            Medium = 2,
            Bold = 3
        }

        public enum IconPosition 
        { 
            Normal, 
            Left,
            Right
        }

        public enum InfoBoxType
        {
            Default = 0,
            Warning = 1,
            Error = 2
        }

        static PholusEditorGUI()
        {
            _lastProSkinState = EditorGUIUtility.isProSkin;
            EditorApplication.update += CheckForSkinChange;
        }

        // Check for skin changes to invalidate color cache
        static private void CheckForSkinChange()
        {
            if (_lastProSkinState != EditorGUIUtility.isProSkin)
            {
                _lastProSkinState = EditorGUIUtility.isProSkin;
                InvalidateColorCache();
                InvalidateStyles();
            }
        }

        // Force color cache to be calculated again
        static private void InvalidateColorCache()
        {
            _cachedTextColor = null;
            _cachedDynamicBgColor = null;
            _cachedDynamicBgColorReverse = null;
            _cachedBgColor = null;
            _cachedBgColorReverse = null;
        }

        // Force styles to be recreated when skin changes
        static private void InvalidateStyles()
        {
            _toggleStyle = null;
            _buttonStyle = null;
            _foldoutStyle = null;
            _dropdownStyle = null;
            _dropdownButtonStyle = null;
            _infoBoxStyle = null;  // Add this line
            _infoBoxTextStyle = null;  // Add this line
        }

        static private void InitializeBackgroundStyle()
        {
            if (_backgroundStyle != null)
                return;

            Texture2D bgSolid = null;
            Texture2D bgTransparent = null;
            
            try
            {
                bgSolid = Resources.Load<Texture2D>("EditorBackground-Solid");
                bgTransparent = Resources.Load<Texture2D>("EditorBackground-Transparent");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load background textures: {e.Message}");
            }

            _backgroundStyle = new GUIStyle()
            {
                normal = { background = bgSolid },
                border = BorderRectOffset
            };

            _dynamicBackgroundStyle = new GUIStyle()
            {
                normal = { background = bgTransparent },
                hover = { background = bgSolid },
                border = BorderRectOffset
            };
        }

        static private void InitializeButtonStyle()
        {
            if (_buttonStyle != null)
                return;

            Texture2D bgTexture = null;
            
            try
            {
                bgTexture = Resources.Load<Texture2D>("EditorBackground-Solid");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load button background texture: {e.Message}");
            }

            _buttonStyle = new GUIStyle()
            {
                normal = { background = bgTexture, textColor = GetTextColor() },
                hover = { background = bgTexture, textColor = GetTextColor() },
                active = { background = bgTexture, textColor = GetTextColor() },
                border = ButtonBorderRectOffset,
                padding = ButtonPaddingRectOffset,
                alignment = TextAnchor.MiddleCenter,
                fontSize = ButtonFontSize
            };
        }

        static private void InitializeInfoBoxStyle()
        {
            if (_infoBoxStyle != null && _infoBoxTextStyle != null)
                return;

            Texture2D bgSolid = null;

            try
            {
                bgSolid = Resources.Load<Texture2D>("EditorBackground-Solid");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load info box background texture: {e.Message}");
            }

            _infoBoxStyle = new GUIStyle()
            {
                normal = { background = bgSolid },
                hover = { background = bgSolid },    // Add this line - same as normal
                active = { background = bgSolid },   // Add this line - same as normal
                border = BorderRectOffset,
                padding = InfoBoxPadding
            };

            _infoBoxTextStyle = new GUIStyle()
            {
                fontSize = InfoBoxFontSize,
                wordWrap = true,
                normal = { textColor = GetTextColor() },
                hover = { textColor = GetTextColor() },     // Add this line
                active = { textColor = GetTextColor() },    // Add this line
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static Color GetInfoBoxBackgroundColor(InfoBoxType type, bool reverseColor)
        {
            Color baseColor = GetBackgroundColor(reverseColor);

            return type switch
            {
                InfoBoxType.Warning => new Color(0.8f, 0.6f, 0.1f, 0.2f),
                InfoBoxType.Error => new Color(0.8f, 0.2f, 0.2f, 0.2f),
                _ => baseColor
            };
        }

        static private void InitializeToggleStyle()
        {
            if (_toggleStyle != null)
                return;

            Texture2D toggleOff = null;
            Texture2D toggleOn = null;
            
            try
            {
                toggleOff = Resources.Load<Texture2D>("Toggle-Off");
                toggleOn = Resources.Load<Texture2D>("Toggle-On");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load toggle textures: {e.Message}");
            }

            Color tColor = GetTextColor();

            _toggleStyle = new GUIStyle()
            {
                fixedWidth = ToggleWidth,
                fixedHeight = ToggleHeight,
                overflow = ToggleOverflowRectOffset,
                margin = ToggleMarginRectOffset,
                padding = TogglePaddingRectOffset,
                alignment = TextAnchor.MiddleRight,
                normal = { background = toggleOff, textColor = tColor },
                hover = { background = toggleOff, textColor = tColor },
                active = { background = toggleOff, textColor = tColor, },
                onNormal = { background = toggleOn, textColor = tColor },
                onHover = { background = toggleOn, textColor = tColor },
                onActive = { background = toggleOn, textColor = tColor }
            };
        }

        static private void InitializeFoldoutStyle()
        {
            if (_foldoutStyle != null)
                return;

            Texture2D bgSolid = null;
            Texture2D bgTransparent = null;
            
            try
            {
                bgSolid = Resources.Load<Texture2D>("EditorBackground-Solid");
                bgTransparent = Resources.Load<Texture2D>("EditorBackground-Transparent");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load foldout background textures: {e.Message}");
            }

            _foldoutStyle = new GUIStyle()
            {
                normal = { textColor = GetTextColor() },
                hover = { background = bgTransparent, textColor = GetTextColor() },
                active = { background = bgSolid },
                border = BorderRectOffset
            };
        }

        /// <summary>
        /// Loads an icon texture from Resources with the "Icon-" prefix.
        /// </summary>
        /// <param name="icon">The icon name without the prefix</param>
        /// <returns>The loaded texture or null if not found</returns>
        public static Texture2D GetIcon(string icon, bool bypassPrefix = false)
        {
            if (string.IsNullOrEmpty(icon))
                return null;
                
            try
            {
                return Resources.Load<Texture2D>(bypassPrefix ? $"{icon}" : $"Icon-{icon}");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load icon '{icon}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a raw texture from Resources without any prefix.
        /// </summary>
        /// <param name="icon">The texture name</param>
        /// <returns>The loaded texture or null if not found</returns>
        public static Texture2D GetRawIcon(string icon)
        {
            if (string.IsNullOrEmpty(icon))
                return null;
                
            try
            {
                return Resources.Load<Texture2D>(icon);
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load raw icon '{icon}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a custom font from Resources based on the font type.
        /// </summary>
        /// <param name="type">The font weight type to load</param>
        /// <returns>The loaded font or null if not found</returns>
        public static Font GetCustomFont(CustomFontType type)
        {
            try
            {
                return Resources.Load<Font>($"EditorFont-{type}");
            }
            catch (Exception e)
            {
                PholusLogger.LogWarning($"Failed to load custom font '{type}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the appropriate text color based on the current editor skin (Pro/Personal).
        /// </summary>
        /// <returns>White for Pro skin, black for Personal skin</returns>
        public static Color GetTextColor()
        {
            _cachedTextColor ??= EditorGUIUtility.isProSkin ? Color.white : Color.black;
            return _cachedTextColor.Value;
        }

        /// <summary>
        /// Gets a dynamic background color that changes on hover, adjusted for the current skin.
        /// </summary>
        /// <param name="reverse">If true, returns a lighter variant for contrast</param>
        /// <returns>Semi-transparent color for dynamic backgrounds</returns>
        public static Color GetDynamicBackgroundColor(bool reverse = false)
        {
            if (reverse) {
                _cachedDynamicBgColorReverse ??= EditorGUIUtility.isProSkin ? ProSkinDynamicBgReverseColor : PersonalSkinDynamicBgReverseColor;
                return _cachedDynamicBgColorReverse.Value;
            }
            
            _cachedDynamicBgColor ??= EditorGUIUtility.isProSkin ? ProSkinDynamicBgColor : PersonalSkinDynamicBgColor;
            return _cachedDynamicBgColor.Value;
        }

        /// <summary>
        /// Gets a static background color adjusted for the current skin.
        /// </summary>
        /// <param name="reverse">If true, returns a lighter variant for contrast</param>
        /// <returns>Semi-transparent color for static backgrounds</returns>
        public static Color GetBackgroundColor(bool reverse = false)
        {
            if (reverse) {
                _cachedBgColorReverse ??= EditorGUIUtility.isProSkin ? ProSkinStaticBgReverseColor : PersonalSkinStaticBgReverseColor;
                return _cachedBgColorReverse.Value;
            }
            _cachedBgColor ??= EditorGUIUtility.isProSkin ? ProSkinStaticBgColor : PersonalSkinStaticBgColor;
            return _cachedBgColor.Value;
        }

        /// <summary>
        /// Begins a horizontal layout with a styled background.
        /// </summary>
        /// <param name="revertColor">If true, uses reverse color scheme for contrast</param>
        /// <param name="isDynamic">If true, background changes on hover</param>
        public static void BeginHorizontalBackground(bool revertColor = false, bool isDynamic = false)
        {
            // Initialize custom style
            InitializeBackgroundStyle();

            // Cache GUI color
            Color cachedGUIColor = GUI.color;

            // Use cached colors
            Color targetColor = isDynamic ? GetDynamicBackgroundColor(revertColor) : GetBackgroundColor(revertColor);

            // Start drawing GUI color
            GUI.color = targetColor;

            // Begin layout
            GUILayout.BeginHorizontal(isDynamic ? _dynamicBackgroundStyle : _backgroundStyle);

            // Revert GUI color for other content
            GUI.color = cachedGUIColor;
        }

        /// <summary>
        /// Ends a horizontal background layout and optionally adds spacing.
        /// </summary>
        /// <param name="addSpace">If true, adds standard element spacing</param>
        public static void EndHorizontalBackground(bool addSpace = true)
        {
            GUILayout.EndHorizontal();
            if (addSpace) { GUILayout.Space(ElementSpace); }
        }

        /// <summary>
        /// Begins a vertical layout with a styled background.
        /// </summary>
        /// <param name="isContainerItem">If true, uses container-appropriate styling</param>
        public static void BeginVerticalBackground(bool isContainerItem = false, bool expandHeight = false)
        {
            // Initialize custom style only once
            InitializeBackgroundStyle();

            // Cache GUI color
            Color cachedGUIColor = GUI.color;

            // Use cached color
            GUI.color = GetBackgroundColor(isContainerItem);

            // Begin layout
            GUILayout.BeginVertical(_backgroundStyle, GUILayout.ExpandHeight(expandHeight));

            // Revert GUI color for other content
            GUI.color = cachedGUIColor;
        }

        /// <summary>
        /// Ends a vertical background layout and optionally adds spacing.
        /// </summary>
        /// <param name="addSpace">If true, adds standard element spacing</param>
        public static void EndVerticalBackground(bool addSpace = false)
        {
            GUILayout.EndVertical();
            if (addSpace) { GUILayout.Space(ElementSpace); }
        }

        /// <summary>
        /// Begins a padded layout container.
        /// </summary>
        /// <param name="excludeTop">If true, excludes top padding</param>
        public static void BeginPadding(bool excludeTop = false)
        {
            // Use static instance when possible, create new one only when top value differs
            RectOffset padding = excludeTop ? 
                new RectOffset(DefaultPadding, DefaultPadding, 0, DefaultPadding) : 
                DefaultPaddingRectOffset;

            GUILayout.BeginVertical(new GUIStyle()
            {
                padding = padding
            });
        }

        /// <summary>
        /// Begins a padded layout container with custom padding values.
        /// </summary>
        /// <param name="value">Custom padding values</param>
        public static void BeginPadding(RectOffset value)
        {
            GUILayout.BeginVertical(new GUIStyle()
            {
                padding = value
            });
        }

        /// <summary>
        /// Ends a padded layout container.
        /// </summary>
        public static void EndPadding()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Begins a centered inspector layout with adjusted margins.
        /// </summary>
        /// <param name="addMoreSpace">If true, adds extra spacing for better visual separation</param>
        public static void BeginCenteredInspector(bool addMoreSpace = false)
        {
            GUILayout.BeginHorizontal(new GUIStyle()
            {
                padding = addMoreSpace ? CenteredInspectorPaddingMore : CenteredInspectorPaddingNormal
            });
            GUILayout.Space(CenteredInspectorLeftSpace);
            GUILayout.BeginVertical();
        }

        /// <summary>
        /// Ends a centered inspector layout.
        /// </summary>
        public static void EndCenteredInspector()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Begins a container layout with uniform padding.
        /// </summary>
        /// <param name="paddingValue">Padding value for all sides</param>
        
        public static void BeginContainer(int paddingValue = ContainerPadding)
        {
            // Use static instance when using default padding, create new one only when different
            RectOffset padding = paddingValue == ContainerPadding ? 
                DefaultContainerPadding : 
                new RectOffset(paddingValue, paddingValue, paddingValue, paddingValue);

            // Begin content area with background
            GUILayout.BeginVertical(new GUIStyle()
            {
                padding = padding
            });
        }

        public static void BeginContainer(RectOffset padding)
        {
            // Begin content area with background
            GUILayout.BeginVertical(new GUIStyle()
            {
                padding = padding
            });
        }

        /// <summary>
        /// Ends a container layout.
        /// </summary>
        public static void EndContainer()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a standard label with the given text.
        /// </summary>
        /// <param name="text">The text to display</param>
        public static void DrawLabel(string text)
        {
            EditorGUILayout.LabelField(new GUIContent(text));
        }
        
        /// <summary>
        /// Draws a compact label with optional fixed width.
        /// </summary>
        /// <param name="text">The text to display</param>
        /// <param name="width">Optional fixed width, -1 for auto width</param>
        public static void DrawCompactLabel(string text, int width = -1)
        {
            if (width > 0)
            {
                GUILayout.Label(new GUIContent(text), GUILayout.Width(width));
            }
            else
            {
                GUILayout.Label(new GUIContent(text));
            }
        }

        // For SerializedProperty
        public static void DrawProperty(SerializedProperty property, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            EndHorizontalBackground(addSpace);
        }

        // For float
        public static float DrawProperty(float value, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            float newValue = EditorGUILayout.FloatField(new GUIContent(label, tooltip), value);

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        // For string
        public static string DrawProperty(string value, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            string newValue = EditorGUILayout.TextField(new GUIContent(label, tooltip), value);

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        // For int
        public static int DrawProperty(int value, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            int newValue = EditorGUILayout.IntField(new GUIContent(label, tooltip), value);

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        // For bool
        public static bool DrawProperty(bool value, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            bool newValue = EditorGUILayout.Toggle(new GUIContent(label, tooltip), value);

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        // For dropdown
        public static int DrawProperty(int selectedIndex, string[] options, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            int newSelectedIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), selectedIndex, options);

            EndHorizontalBackground(addSpace);
            return newSelectedIndex;
        }

        // For enum
        public static T DrawProperty<T>(T enumValue, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0) where T : Enum
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            T newEnumValue = (T)EditorGUILayout.EnumPopup(new GUIContent(label, tooltip), enumValue);

            EndHorizontalBackground(addSpace);
            return newEnumValue;
        }

        // For object
        public static T DrawProperty<T>(T objectValue, string label, string tooltip = null, bool allowSceneObjects = true, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0) where T : UnityEngine.Object
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            T newValue = EditorGUILayout.ObjectField(new GUIContent(label, tooltip), objectValue, typeof(T), allowSceneObjects) as T;

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        // For color
        public static Color DrawProperty(Color colorValue, string label, string tooltip = null, bool showEyedropper = true, bool showAlpha = true, bool hdr = false, bool addSpace = true, bool customBackground = true, bool revertColor = false, int propertyPadding = 0)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }
            if (propertyPadding > 0) { GUILayout.Space(propertyPadding); }

            Color newColor = EditorGUILayout.ColorField(new GUIContent(label, tooltip), colorValue, showEyedropper, showAlpha, hdr);

            EndHorizontalBackground(addSpace);
            return newColor;
        }

        public static bool DrawToggleButton(bool value, string textWhenTrue, string textWhenFalse, Texture2D icon = null, string tooltip = null, float width = 0, float height = DefaultButtonHeight, float iconTextSpacing = IconTextSpacing, Color? tintColor = null, Color? toggleColor = null, bool reverse = false, bool addSpace = true, bool hideNormal = false, bool centered = true, float iconSize = 0, float textSize = 0, IconPosition iconPosition = IconPosition.Normal)
        {
            string displayText = value ? textWhenTrue : textWhenFalse;

            InitializeButtonStyle();

            // Create GUIContent with tooltip
            GUIContent buttonContent = new GUIContent("", tooltip);

            // Get button rect and control ID early
            GUILayoutOption[] layoutOptions = width > 0
                ? new[] { GUILayout.Width(width), GUILayout.Height(height) }
                : new[] { GUILayout.Height(height) };
            Rect buttonRect = GUILayoutUtility.GetRect(buttonContent, _buttonStyle, layoutOptions);
            int controlID = GUIUtility.GetControlID(FocusType.Passive, buttonRect);

            // Cache event type to avoid multiple property accesses
            EventType eventType = Event.current.GetTypeForControl(controlID);
            bool isHover = buttonRect.Contains(Event.current.mousePosition);
            bool isActive = GUIUtility.hotControl == controlID;
            bool toggled = false;

            // Handle events
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (isHover && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isActive)
                    {
                        GUIUtility.hotControl = 0;
                        if (isHover) { toggled = true; }
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isActive) { Event.current.Use(); }
                    break;

                case EventType.Repaint:
                    // Request repaint for hover effects
                    if (isHover || isActive)
                        RequestRepaint();

                    // Calculate and apply button color
                    Color originalBgColor = GUI.backgroundColor;

                    // Determine which color to use as base
                    Color? baseColorToUse = value && toggleColor.HasValue ? toggleColor : tintColor;

                    // Handle hideNormal option
                    if (hideNormal && !isHover && !isActive && !value)
                    {
                        // Make button transparent in normal state (when not toggled)
                        GUI.backgroundColor = Color.clear;
                    }
                    else if (baseColorToUse.HasValue)
                    {
                        // Use toggle color when toggled, or tint color otherwise
                        Color targetColor = baseColorToUse.Value;

                        if (isActive && isHover)
                        {
                            // Pressed state
                            GUI.backgroundColor = targetColor * ButtonActiveMultiplier;
                        }
                        else if (isHover)
                        {
                            // Hover state (works even when toggled)
                            GUI.backgroundColor = targetColor * ButtonHoverMultiplier;
                        }
                        else
                        {
                            // Normal/toggled state
                            GUI.backgroundColor = targetColor;
                        }
                    }
                    else
                    {
                        // No custom colors - use default background colors
                        Color baseColor;

                        if (value)
                        {
                            // When toggled, use pressed-like appearance as base
                            baseColor = GetDynamicBackgroundColor(reverse);
                            if (isActive && isHover)
                            {
                                baseColor *= ButtonActiveMultiplier;
                            }
                            else if (isHover)
                            {
                                baseColor *= ButtonHoverMultiplier;
                            }
                        }
                        else
                        {
                            // When not toggled, normal behavior
                            if (isActive && isHover)
                            {
                                baseColor = GetDynamicBackgroundColor(reverse) * ButtonActiveMultiplier;
                            }
                            else if (isHover)
                            {
                                baseColor = GetDynamicBackgroundColor(reverse);
                            }
                            else
                            {
                                baseColor = GetBackgroundColor(reverse);
                            }
                        }

                        GUI.backgroundColor = baseColor;
                    }

                    // Draw button background with tooltip
                    _buttonStyle.Draw(buttonRect, buttonContent, controlID);
                    GUI.backgroundColor = originalBgColor;

                    // Draw content - FIXED: Added tooltip parameter
                    DrawButtonContent(buttonRect, displayText, icon, tooltip, iconTextSpacing, false, centered, iconSize, textSize, iconPosition);
                    break;
            }

            if (addSpace)
                GUILayout.Space(ElementSpace);

            return toggled ? !value : value;
        }

        public static void DrawSlider(SerializedProperty value, float leftValue, float rightValue, bool addSpace = true, bool customBackground = true, bool revertColor = false)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }

            EditorGUILayout.Slider(value, leftValue, rightValue);
            EndHorizontalBackground(addSpace);
        }

        public static float DrawSlider(string label, float value, float leftValue, float rightValue, bool addSpace = true, bool customBackground = true, bool revertColor = false)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }

            float tempValue = EditorGUILayout.Slider(label, value, leftValue, rightValue);
            EndHorizontalBackground(addSpace);

            return tempValue;
        }

        public static void DrawArrayProperty(SerializedProperty property, string label, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }

            GUILayout.Space(ArrayPropertyIndent);
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            GUILayout.Space(ArrayPropertyRightSpace);
            EndHorizontalBackground(addSpace);
        }

        /// <summary>
        /// Draws a styled button with optional icon and custom styling.
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="icon">Optional icon texture</param>
        /// <param name="tooltip">Optional tooltip text that appears on hover</param>
        /// <param name="width">Button width, 0 for auto width</param>
        /// <param name="height">Button height</param>
        /// <param name="iconTextSpacing">Space between icon and text</param>
        /// <param name="tintColor">Optional tint color for the button</param>
        /// <param name="reverse">If true, uses reverse color scheme</param>
        /// <param name="addSpace">If true, adds spacing after the button</param>
        /// <param name="hideNormal">If true, button is transparent when not hovered</param>
        /// <param name="centered">If true, centers the content in the button</param>
        /// <param name="iconSize">Custom icon size, 0 for auto</param>
        /// <param name="textSize">Custom text size, 0 for default</param>
        /// <param name="iconPosition">Position of the icon relative to text</param>
        /// <param name="reverseAlignment">If true, swaps icon and text positions (text-icon instead of icon-text)</param>
        /// <returns>True if the button was clicked</returns>
        public static bool DrawButton(string text, Texture2D icon = null, string tooltip = null, float width = 0, float height = DefaultButtonHeight, float iconTextSpacing = IconTextSpacing, Color? tintColor = null, bool reverse = false, bool addSpace = true, bool hideNormal = false, bool centered = true, float iconSize = 0, float textSize = 0, IconPosition iconPosition = IconPosition.Normal, bool reverseAlignment = false)
        {
            InitializeButtonStyle();

            // Create GUIContent with tooltip
            GUIContent buttonContent = new GUIContent("", tooltip);

            // Get button rect and control ID early
            GUILayoutOption[] layoutOptions = width > 0
                ? new[] { GUILayout.Width(width), GUILayout.Height(height) }
                : new[] { GUILayout.Height(height) };
            Rect buttonRect = GUILayoutUtility.GetRect(buttonContent, _buttonStyle, layoutOptions);
            int controlID = GUIUtility.GetControlID(FocusType.Passive, buttonRect);

            // Cache event type to avoid multiple property accesses
            EventType eventType = Event.current.GetTypeForControl(controlID);
            bool isHover = buttonRect.Contains(Event.current.mousePosition) && GUI.enabled; // Fix: Check GUI.enabled
            bool isActive = GUIUtility.hotControl == controlID;
            bool clicked = false;

            // Handle events
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (isHover && Event.current.button == 0 && GUI.enabled) // Fix: Check GUI.enabled
                    {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isActive)
                    {
                        GUIUtility.hotControl = 0;
                        if (isHover && GUI.enabled) { clicked = true; } // Fix: Check GUI.enabled
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isActive) { Event.current.Use(); }
                    break;

                case EventType.Repaint:
                    // Request repaint for hover effects only if enabled
                    if ((isHover || isActive) && GUI.enabled)
                        RequestRepaint();

                    // Calculate and apply button color
                    Color originalBgColor = GUI.backgroundColor;

                    // Handle disabled state
                    if (!GUI.enabled)
                    {
                        // For disabled buttons, use a muted appearance
                        Color disabledColor = GetBackgroundColor(reverse);
                        disabledColor.a *= 0.5f; // Make it more transparent
                        GUI.backgroundColor = disabledColor;
                    }
                    // Handle hideNormal option
                    else if (hideNormal && !isHover && !isActive)
                    {
                        // Make button transparent in normal state
                        GUI.backgroundColor = Color.clear;
                    }
                    else if (tintColor.HasValue)
                    {
                        // Tinted button - apply state multipliers directly
                        Color targetColor = tintColor.Value;

                        if (isActive && isHover)
                        {
                            // Pressed state - darker
                            GUI.backgroundColor = targetColor * ButtonActiveMultiplier;
                        }
                        else if (isHover)
                        {
                            // Hover state - slightly darker
                            GUI.backgroundColor = targetColor * ButtonHoverMultiplier;
                        }
                        else
                        {
                            // Normal state
                            GUI.backgroundColor = targetColor;
                        }
                    }
                    else
                    {
                        // Non-tinted - use background colors with proper state handling
                        Color baseColor;

                        if (isActive && isHover)
                        {
                            // Pressed state - use active multiplier
                            baseColor = GetDynamicBackgroundColor(reverse) * ButtonActiveMultiplier;
                        }
                        else if (isHover)
                        {
                            // Hover state
                            baseColor = GetDynamicBackgroundColor(reverse);
                        }
                        else
                        {
                            // Normal state
                            baseColor = GetBackgroundColor(reverse);
                        }

                        GUI.backgroundColor = baseColor;
                    }

                    // Draw button background with tooltip
                    _buttonStyle.Draw(buttonRect, buttonContent, controlID);
                    GUI.backgroundColor = originalBgColor;

                    // Draw content with optional centering and reverse alignment
                    // Pass the disabled state to DrawButtonContent
                    DrawButtonContent(buttonRect, text, icon, tooltip, iconTextSpacing, !GUI.enabled, centered, iconSize, textSize, iconPosition, reverseAlignment);
                    break;
            }

            if (addSpace)
                GUILayout.Space(ElementSpace);

            return clicked;
        }

        /// <summary>
        /// Draws a styled button with icon loaded by name.
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="iconName">Name of the icon to load (without Icon- prefix)</param>
        /// <param name="tooltip">Optional tooltip text that appears on hover</param>
        /// <param name="width">Button width, 0 for auto width</param>
        /// <param name="height">Button height</param>
        /// <param name="iconTextSpacing">Space between icon and text</param>
        /// <param name="tintColor">Optional tint color for the button</param>
        /// <param name="reverse">If true, uses reverse color scheme</param>
        /// <param name="addSpace">If true, adds spacing after the button</param>
        /// <param name="reverseAlignment">If true, swaps icon and text positions</param>
        /// <returns>True if the button was clicked</returns>
        public static bool DrawButton(string text, string iconName, string tooltip = null, float width = 0, float height = DefaultButtonHeight, float iconTextSpacing = IconTextSpacing, Color? tintColor = null, bool reverse = false, bool addSpace = true, bool reverseAlignment = false)
        {
            Texture2D icon = string.IsNullOrEmpty(iconName) ? null : GetIcon(iconName);
            // Pass all parameters including the new ones at the end
            return DrawButton(text, icon, tooltip, width, height, iconTextSpacing, tintColor, reverse, addSpace, false, true, 0, 0, IconPosition.Normal, reverseAlignment);
        }

        private static void DrawButtonContent(Rect buttonRect, string text, Texture2D icon, string tooltip, float iconTextSpacing, bool isDisabled, bool centered, float iconSize, float textSize, IconPosition iconPosition, bool reverseAlignment = false)
        {
            if (string.IsNullOrEmpty(text) && icon == null) return;

            // Store original GUI color to restore later
            Color originalGUIColor = GUI.color;
            Color originalContentColor = GUI.contentColor;

            // Apply disabled visual effect
            if (isDisabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f); // Make icons semi-transparent
                GUI.contentColor = new Color(GUI.contentColor.r, GUI.contentColor.g, GUI.contentColor.b, 0.5f); // Make text semi-transparent
            }

            if (centered)
            {
                if (iconPosition == IconPosition.Normal)
                {
                    // Original centered behavior - icon and text together
                    Vector2 calculatedTextSize = Vector2.zero;
                    Vector2 calculatedIconSize = Vector2.zero;

                    if (!string.IsNullOrEmpty(text))
                    {
                        GUIStyle tempTextStyle = new GUIStyle(GUI.skin.label);
                        if (textSize > 0) tempTextStyle.fontSize = Mathf.RoundToInt(textSize);
                        else if (_buttonStyle.fontSize > 0) tempTextStyle.fontSize = _buttonStyle.fontSize;

                        calculatedTextSize = tempTextStyle.CalcSize(new GUIContent(text));
                    }

                    if (icon != null)
                    {
                        if (iconSize > 0)
                        {
                            calculatedIconSize = new Vector2(iconSize, iconSize);
                        }
                        else
                        {
                            calculatedIconSize = new Vector2(icon.width, icon.height);
                            float maxIconHeight = buttonRect.height * 0.8f;
                            if (calculatedIconSize.y > maxIconHeight)
                            {
                                float scale = maxIconHeight / calculatedIconSize.y;
                                calculatedIconSize *= scale;
                            }
                        }
                    }

                    // Calculate total width including spacing
                    float totalWidth = calculatedIconSize.x + calculatedTextSize.x;
                    if (icon != null && !string.IsNullOrEmpty(text))
                    {
                        totalWidth += iconTextSpacing;
                    }

                    // Center the content horizontally
                    float startX = buttonRect.x + (buttonRect.width - totalWidth) * 0.5f;
                    float centerY = buttonRect.y + buttonRect.height * 0.5f;

                    // Draw content based on reverseAlignment
                    if (!reverseAlignment)
                    {
                        // Normal alignment: icon -> text
                        // Draw icon first
                        if (icon != null)
                        {
                            Rect iconRect = new Rect(
                                startX,
                                centerY - calculatedIconSize.y * 0.5f,
                                calculatedIconSize.x,
                                calculatedIconSize.y
                            );

                            GUI.DrawTexture(iconRect, icon);

                            startX += calculatedIconSize.x + iconTextSpacing;
                        }

                        // Draw text
                        if (!string.IsNullOrEmpty(text))
                        {
                            Rect textRect = new Rect(
                                startX,
                                centerY - calculatedTextSize.y * 0.5f,
                                calculatedTextSize.x,
                                calculatedTextSize.y
                            );

                            DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleCenter, textSize);
                        }
                    }
                    else
                    {
                        // Reverse alignment: text -> icon
                        // Draw text first
                        if (!string.IsNullOrEmpty(text))
                        {
                            Rect textRect = new Rect(
                                startX,
                                centerY - calculatedTextSize.y * 0.5f,
                                calculatedTextSize.x,
                                calculatedTextSize.y
                            );

                            DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleCenter, textSize);
                            startX += calculatedTextSize.x;

                            if (icon != null)
                                startX += iconTextSpacing;
                        }

                        // Draw icon
                        if (icon != null)
                        {
                            Rect iconRect = new Rect(
                                startX,
                                centerY - calculatedIconSize.y * 0.5f,
                                calculatedIconSize.x,
                                calculatedIconSize.y
                            );

                            GUI.DrawTexture(iconRect, icon);
                        }
                    }
                }
                else
                {
                    // Icon positioned separately (Left/Right), text centered
                    Vector2 calculatedTextSize = Vector2.zero;
                    Vector2 calculatedIconSize = Vector2.zero;

                    if (!string.IsNullOrEmpty(text))
                    {
                        GUIStyle tempTextStyle = new GUIStyle(GUI.skin.label);
                        if (textSize > 0) tempTextStyle.fontSize = Mathf.RoundToInt(textSize);
                        else if (_buttonStyle.fontSize > 0) tempTextStyle.fontSize = _buttonStyle.fontSize;

                        calculatedTextSize = tempTextStyle.CalcSize(new GUIContent(text));
                    }

                    if (icon != null)
                    {
                        if (iconSize > 0)
                        {
                            calculatedIconSize = new Vector2(iconSize, iconSize);
                        }
                        else
                        {
                            calculatedIconSize = new Vector2(icon.width, icon.height);
                            float maxIconHeight = buttonRect.height * 0.8f;
                            if (calculatedIconSize.y > maxIconHeight)
                            {
                                float scale = maxIconHeight / calculatedIconSize.y;
                                calculatedIconSize *= scale;
                            }
                        }
                    }

                    float centerY = buttonRect.y + buttonRect.height * 0.5f;

                    // Draw text in the center with tooltip
                    if (!string.IsNullOrEmpty(text))
                    {
                        Rect textRect = new Rect(
                            buttonRect.x + (buttonRect.width - calculatedTextSize.x) * 0.5f,
                            centerY - calculatedTextSize.y * 0.5f,
                            calculatedTextSize.x,
                            calculatedTextSize.y
                        );

                        DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleCenter, textSize);
                    }

                    // Draw icon on the specified side
                    if (icon != null)
                    {
                        float iconX;
                        if (iconPosition == IconPosition.Left)
                        {
                            iconX = buttonRect.x + iconTextSpacing;
                        }
                        else // IconPosition.Right
                        {
                            iconX = buttonRect.xMax - calculatedIconSize.x - iconTextSpacing;
                        }

                        Rect iconRect = new Rect(
                            iconX,
                            centerY - calculatedIconSize.y * 0.5f,
                            calculatedIconSize.x,
                            calculatedIconSize.y
                        );

                        GUI.DrawTexture(iconRect, icon);
                    }
                }
            }
            else
            {
                // Left-aligned content
                if (icon != null && !string.IsNullOrEmpty(text))
                {
                    // Draw icon and text side by side (left-aligned)
                    Vector2 calculatedIconSize;
                    if (iconSize > 0)
                    {
                        calculatedIconSize = new Vector2(iconSize, iconSize);
                    }
                    else
                    {
                        calculatedIconSize = new Vector2(icon.width, icon.height);
                        float maxIconHeight = buttonRect.height * 0.8f;
                        if (calculatedIconSize.y > maxIconHeight)
                        {
                            float scale = maxIconHeight / calculatedIconSize.y;
                            calculatedIconSize *= scale;
                        }
                    }

                    if (!reverseAlignment)
                    {
                        // Normal: icon -> text
                        Rect iconRect = new Rect(
                            buttonRect.x + 5,
                            buttonRect.y + (buttonRect.height - calculatedIconSize.y) * 0.5f,
                            calculatedIconSize.x,
                            calculatedIconSize.y
                        );

                        GUI.DrawTexture(iconRect, icon);

                        Rect textRect = new Rect(
                            iconRect.xMax + iconTextSpacing,
                            buttonRect.y,
                            buttonRect.width - calculatedIconSize.x - iconTextSpacing - 10,
                            buttonRect.height
                        );

                        DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleLeft, textSize);
                    }
                    else
                    {
                        // Reverse: text -> icon
                        GUIStyle tempTextStyle = new GUIStyle(GUI.skin.label);
                        if (textSize > 0) tempTextStyle.fontSize = Mathf.RoundToInt(textSize);
                        else if (_buttonStyle.fontSize > 0) tempTextStyle.fontSize = _buttonStyle.fontSize;

                        Vector2 textMeasure = tempTextStyle.CalcSize(new GUIContent(text));

                        Rect textRect = new Rect(
                            buttonRect.x + 5,
                            buttonRect.y,
                            textMeasure.x,
                            buttonRect.height
                        );

                        DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleLeft, textSize);

                        Rect iconRect = new Rect(
                            textRect.xMax + iconTextSpacing,
                            buttonRect.y + (buttonRect.height - calculatedIconSize.y) * 0.5f,
                            calculatedIconSize.x,
                            calculatedIconSize.y
                        );

                        GUI.DrawTexture(iconRect, icon);
                    }
                }
                else if (icon != null)
                {
                    // Just icon
                    Vector2 calculatedIconSize;
                    if (iconSize > 0)
                    {
                        calculatedIconSize = new Vector2(iconSize, iconSize);
                    }
                    else
                    {
                        calculatedIconSize = new Vector2(icon.width, icon.height);
                        float maxIconHeight = buttonRect.height * 0.8f;
                        if (calculatedIconSize.y > maxIconHeight)
                        {
                            float scale = maxIconHeight / calculatedIconSize.y;
                            calculatedIconSize *= scale;
                        }
                    }

                    Rect iconRect = new Rect(
                        buttonRect.x + 5,
                        buttonRect.y + (buttonRect.height - calculatedIconSize.y) * 0.5f,
                        calculatedIconSize.x,
                        calculatedIconSize.y
                    );

                    GUI.DrawTexture(iconRect, icon);
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    // Just text
                    Rect textRect = new Rect(
                        buttonRect.x + 5,
                        buttonRect.y,
                        buttonRect.width - 10,
                        buttonRect.height
                    );

                    DrawTextWithColor(textRect, text, tooltip, GetTextColor(), isDisabled, TextAnchor.MiddleLeft, textSize);
                }
            }

            // Ensure GUI color is restored
            GUI.color = originalGUIColor;
            GUI.contentColor = originalContentColor;
        }

        private static void DrawTextWithColor(Rect textRect, string text, string tooltip, Color baseColor, bool isDisabled, TextAnchor alignment, float fontSize)
        {
            // Apply disabled effect to text color
            Color finalTextColor = isDisabled ? new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.5f) : baseColor;

            // Store GUI state
            Color originalGUIColor = GUI.color;
            Color originalContentColor = GUI.contentColor;

            // Try EditorGUI approach for better color control
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                try
                {
                    GUIStyle editorStyle = new GUIStyle(EditorStyles.label);
                    editorStyle.alignment = alignment;
                    if (fontSize > 0) editorStyle.fontSize = Mathf.RoundToInt(fontSize);
                    editorStyle.normal.textColor = finalTextColor;
                    editorStyle.hover.textColor = finalTextColor;
                    editorStyle.active.textColor = finalTextColor;
                    editorStyle.focused.textColor = finalTextColor;

                    GUI.Label(textRect, new GUIContent(text, tooltip), editorStyle);
                    return;
                }
                catch
                {
                    // Fall back to regular GUI if EditorGUI isn't available
                }
            }

            // Fallback approach
            GUI.color = Color.white;
            GUI.contentColor = finalTextColor;

            GUIStyle directStyle = new GUIStyle(GUI.skin.label);
            directStyle.alignment = alignment;
            if (fontSize > 0) directStyle.fontSize = Mathf.RoundToInt(fontSize);

            GUI.Label(textRect, new GUIContent(text, tooltip), directStyle);

            // Restore GUI state
            GUI.color = originalGUIColor;
            GUI.contentColor = originalContentColor;
        }

        public static void DrawToggle(SerializedProperty property, string title, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, string onLabel = "On", string offLabel = "Off")
        {
            InitializeToggleStyle();

            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor, true); }

            // Create a completely invisible button style for full-width clicking
            GUIStyle invisibleButtonStyle = new GUIStyle()
            {
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };

            // Get the rect for the entire row
            Rect rowRect = GUILayoutUtility.GetRect(new GUIContent(title), GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.Height(ToggleHeight + 4));

            // Draw the invisible full-width button for interaction
            if (GUI.Button(rowRect, "", invisibleButtonStyle))
            {
                property.boolValue = !property.boolValue;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Draw label on the left (visual only)
            Rect labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width - ToggleWidth - 10, rowRect.height);
            GUI.Label(labelRect, new GUIContent(title, tooltip));

            // Draw the visual toggle on the right side (visual only, no interaction)
            Rect toggleRect = new Rect(rowRect.xMax - ToggleWidth - 5, rowRect.y + (rowRect.height - ToggleHeight) * 0.5f, ToggleWidth, ToggleHeight);

            // Create a visual-only toggle style that properly shows the on/off state
            GUIStyle visualToggleStyle = new GUIStyle(_toggleStyle);

            // Manually set the background based on the toggle state for GUI.Label
            if (property.boolValue)
            {
                visualToggleStyle.normal.background = _toggleStyle.onNormal.background;
                visualToggleStyle.hover.background = _toggleStyle.onHover.background;
                visualToggleStyle.active.background = _toggleStyle.onActive.background;
            }
            else
            {
                visualToggleStyle.normal.background = _toggleStyle.normal.background;
                visualToggleStyle.hover.background = _toggleStyle.hover.background;
                visualToggleStyle.active.background = _toggleStyle.active.background;
            }

            // Use GUI.Label with the modified style to show the visual state without interaction
            GUI.Label(toggleRect, new GUIContent(property.boolValue ? onLabel : offLabel), visualToggleStyle);

            // Handle hover effect for the entire row
            if (Event.current.type == EventType.Repaint && rowRect.Contains(Event.current.mousePosition))
            {
                RequestRepaint();
            }

            EndHorizontalBackground(addSpace);
        }

        public static bool DrawToggle(bool value, string title, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false, string onLabel = "On", string offLabel = "Off")
        {
            InitializeToggleStyle();

            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor, true); }

            bool newValue = value;

            // Create a completely invisible button style for full-width clicking
            GUIStyle invisibleButtonStyle = new GUIStyle()
            {
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };

            // Get the rect for the entire row
            Rect rowRect = GUILayoutUtility.GetRect(new GUIContent(title), GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.Height(ToggleHeight));

            // Draw the invisible full-width button for interaction
            if (GUI.Button(rowRect, "", invisibleButtonStyle))
            {
                newValue = !value;
            }

            // Draw label on the left (visual only)
            Rect labelRect = new Rect(rowRect.x - 2, rowRect.y, rowRect.width - ToggleWidth + 4, rowRect.height);
            GUI.Label(labelRect, new GUIContent(title, tooltip));

            // Draw the visual toggle on the right side (visual only, no interaction)
            Rect toggleRect = new Rect(rowRect.xMax - ToggleWidth + 4, rowRect.y + (rowRect.height - ToggleHeight) * 0.5f, ToggleWidth + 4, ToggleHeight);

            // Create a visual-only toggle style that properly shows the on/off state
            GUIStyle visualToggleStyle = new GUIStyle(_toggleStyle);

            // Manually set the background based on the toggle state for GUI.Label
            if (value)
            {
                visualToggleStyle.normal.background = _toggleStyle.onNormal.background;
                visualToggleStyle.hover.background = _toggleStyle.onHover.background;
                visualToggleStyle.active.background = _toggleStyle.onActive.background;
            }
            else
            {
                visualToggleStyle.normal.background = _toggleStyle.normal.background;
                visualToggleStyle.hover.background = _toggleStyle.hover.background;
                visualToggleStyle.active.background = _toggleStyle.active.background;
            }

            // Use GUI.Label with the modified style to show the visual state without interaction
            GUI.Label(toggleRect, new GUIContent(value ? onLabel : offLabel), visualToggleStyle);

            // Handle hover effect for the entire row
            if (Event.current.type == EventType.Repaint && rowRect.Contains(Event.current.mousePosition))
            {
                RequestRepaint();
            }

            EndHorizontalBackground(addSpace);
            return newValue;
        }

        /// <summary>
        /// Draws an information box with optional icon and styling based on type.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="type">The type of info box (affects coloring)</param>
        /// <param name="customIcon">Optional custom icon name</param>
        /// <param name="reverseColor">If true, uses reverse color scheme</param>
        public static void DrawInfoBox(string message, InfoBoxType type = InfoBoxType.Default, string customIcon = null, bool reverseColor = true, RectOffset padding = null)
        {
            // Initialize style
            InitializeInfoBoxStyle();
            if (padding != null) { BeginContainer(padding); }
            // Get background color
            Color backgroundColor = GetInfoBoxBackgroundColor(type, reverseColor);
            // Determine which icon to use
            Texture2D icon;
            if (!string.IsNullOrEmpty(customIcon)) { icon = GetIcon(customIcon); }
            else
            {
                icon = type switch
                {
                    InfoBoxType.Warning => GetIcon("InfoBox-Warning", true),
                    InfoBoxType.Error => GetIcon("InfoBox-Error", true),
                    InfoBoxType.Default => GetIcon("InfoBox-Default", true),
                    _ => GetIcon("InfoBox-Default", true)
                };
            }
            Color cachedGUIColor = GUI.color;
            // Calculate actual text height first
            GUIStyle textStyle = new GUIStyle(_infoBoxTextStyle);
            float textHeight = textStyle.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth - InfoBoxPadding.left - InfoBoxPadding.right - (icon != null ? InfoBoxIconSize + 8 : 0));
            float iconHeight = icon != null ? InfoBoxIconSize : 0;
            float contentHeight = Mathf.Max(textHeight, iconHeight);
            // Only add space if content is smaller than minimum
            float extraSpace = Mathf.Max(0, InfoBoxMinHeight - contentHeight - InfoBoxPadding.top - InfoBoxPadding.bottom);
            GUI.color = backgroundColor;
            using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
            {
                GUI.color = cachedGUIColor;
                // Add top spacing for centering
                if (extraSpace > 0)
                {
                    GUILayout.Space(extraSpace / 2);
                }
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(Mathf.Max(textHeight, iconHeight))))
                {
                    // Draw icon with proper centering
                    if (icon != null)
                    {
                        // Use flexible space above and below icon to center it
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(InfoBoxIconSize)))
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(new GUIContent(icon), GUILayout.Width(InfoBoxIconSize), GUILayout.Height(InfoBoxIconSize));
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.Space(4);
                    }
                    // Draw text with flexible space for centering
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(message, textStyle);
                        GUILayout.FlexibleSpace();
                    }
                }
                // Add bottom spacing for centering
                if (extraSpace > 0)
                {
                    GUILayout.Space(extraSpace / 2);
                }
            }
            GUI.color = cachedGUIColor;
            if (padding != null) { EndContainer(); }
        }

        /// <summary>
        /// Initializes the dropdown style if not already created.
        /// </summary>
        static private void InitializeDropdownStyle()
        {
            if (_dropdownStyle != null && _dropdownButtonStyle != null)
                return;
                
            // Create a custom style based on miniButton for lighter appearance
            _dropdownStyle = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = GetTextColor() },
                hover = { textColor = GetTextColor() },
                active = { textColor = GetTextColor() },
                focused = { textColor = GetTextColor() },
                border = DefaultPaddingRectOffset,
                padding = DropdownPaddingRectOffset,
                alignment = TextAnchor.MiddleLeft,
                fontSize = DropdownFontSize,
                fixedHeight = DropdownHeight + 2,
                stretchWidth = true
            };
            
            // Make the background lighter by using a semi-transparent overlay
            if (EditorGUIUtility.isProSkin)
            {
                // For dark theme, use lighter borders
                _dropdownStyle.normal.textColor = ProSkinDropdownTextColor;
            }
            
            _dropdownButtonStyle = new GUIStyle(_dropdownStyle)
            {
                padding = DropdownPaddingRectOffset
            };
        }
        
        /// <summary>
        /// Draws a dropdown with the specified options.
        /// </summary>
        /// <param name="label">Label text for the dropdown</param>
        /// <param name="selectedIndex">Currently selected option index</param>
        /// <param name="options">Array of dropdown options</param>
        /// <param name="tooltip">Optional tooltip text</param>
        /// <param name="addSpace">If true, adds spacing after the dropdown</param>
        /// <param name="customBackground">If true, uses custom background styling</param>
        /// <param name="revertColor">If true, uses reverse color scheme</param>
        /// <returns>The new selected index</returns>
        public static int DrawDropdown(string label, int selectedIndex, string[] options, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false)
        {
            InitializeDropdownStyle();
            
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor, true); }

            DrawCompactLabel(label, LabelWidth);
            
            // Get dropdown rect with proper height
            GUIContent selectedContent = new GUIContent(selectedIndex >= 0 && selectedIndex < options.Length ? options[selectedIndex] : "");
            Rect dropdownRect = GUILayoutUtility.GetRect(selectedContent, EditorStyles.popup, GUILayout.ExpandWidth(true), GUILayout.Height(DropdownHeight));
            
            // Handle hover
            bool isHover = dropdownRect.Contains(Event.current.mousePosition);
            if (isHover) RequestRepaint();
            
            // Keep the dropdown clean without background tinting
            int newIndex = EditorGUI.Popup(dropdownRect, selectedIndex, options);
            
            EndHorizontalBackground(addSpace);
            
            return newIndex;
        }

        /// <summary>
        /// Draws a dropdown for enum values.
        /// </summary>
        /// <typeparam name="T">The enum type</typeparam>
        /// <param name="label">Label text for the dropdown</param>
        /// <param name="selected">Currently selected enum value</param>
        /// <param name="tooltip">Optional tooltip text</param>
        /// <param name="addSpace">If true, adds spacing after the dropdown</param>
        /// <param name="customBackground">If true, uses custom background styling</param>
        /// <param name="revertColor">If true, uses reverse color scheme</param>
        /// <returns>The new selected enum value</returns>
        public static T DrawEnumDropdown<T>(string label, T selected, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false) where T : Enum
        {
            InitializeDropdownStyle();
            
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor, true); }

            DrawCompactLabel(label, LabelWidth);
            
            // Get dropdown rect with proper height
            GUIContent selectedContent = new GUIContent(selected.ToString());
            Rect dropdownRect = GUILayoutUtility.GetRect(selectedContent, EditorStyles.popup, GUILayout.ExpandWidth(true), GUILayout.Height(DropdownHeight));
            
            // Handle hover
            bool isHover = dropdownRect.Contains(Event.current.mousePosition);
            if (isHover) RequestRepaint();
            
            // Keep the dropdown clean without background tinting
            Enum enumValue = EditorGUI.EnumPopup(dropdownRect, selected);
            T newValue = (T)Enum.ToObject(typeof(T), enumValue);
            
            EndHorizontalBackground(addSpace);
            
            return newValue;
        }

        public static void DrawDropdown(SerializedProperty property, string label, string[] options, string tooltip = null, bool addSpace = true, bool customBackground = true, bool revertColor = false)
        {
            if (!customBackground) { GUILayout.BeginHorizontal(); }
            else { BeginHorizontalBackground(revertColor); }

            DrawCompactLabel(label, LabelWidth);
            
            EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.ExpandWidth(true));
            
            EndHorizontalBackground(addSpace);
        }

        /// <summary>
        /// Draws standard top spacing for sections.
        /// </summary>
        public static void DrawTopSpace()
        {
            GUILayout.Space(TopSpacing);
        }

        /// <summary>
        /// Draws smaller spacing suitable for foldout sections.
        /// </summary>
        public static void DrawFoldoutSpace()
        {
            GUILayout.Space(FoldoutSpacing);
        }

        /// <summary>
        /// Draws a styled header with icon loaded by name.
        /// </summary>

        public static void DrawHeader(string title, string icon, bool addSpaceBefore = false)
        {
            DrawHeader(title, GetIcon(icon), addSpaceBefore);
        }

        /// <summary>
        /// Draws a styled header with optional icon texture.
        /// </summary>
        /// <param name="title">Header text</param>
        /// <param name="icon">Optional icon texture</param>
        /// <param name="addSpaceBefore">If true, adds extra space before the header</param>
        public static void DrawHeader(string title, Texture2D icon = null, bool addSpaceBefore = false)
        {
            if (addSpaceBefore) { GUILayout.Space(HeaderTopSpace); }

            GUILayout.BeginHorizontal();
            GUILayout.Space(HeaderLeftSpace);

            if (icon != null)
            {
                GUILayout.Label(new GUIContent(icon), GUILayout.Width(HeaderIconWidth), GUILayout.Height(FoldoutIconSize));
            }

            Font customFont = GetCustomFont(CustomFontType.Medium);
            GUIStyle headerStyle = new GUIStyle
            {
                font = customFont,
                fontSize = HeaderFontSize,
                normal = { textColor = GetTextColor() },
                hover = { textColor = GetTextColor() },
                alignment = TextAnchor.MiddleLeft,
                margin = HeaderMarginRectOffset
            };

            // Use GUILayoutUtility.GetRect to reserve space and prevent conflicts
            Rect startRect = GUILayoutUtility.GetRect(new GUIContent(title), headerStyle);
            startRect.x += 0; // Adjust for the icon offset

            // Draw text with custom spacing (use InvariantCulture to avoid Turkish İ issue)
            if (!string.IsNullOrEmpty(title))
                DrawTextWithSpacing(title.ToUpperInvariant(), headerStyle, TextSpacing, startRect);

            GUILayout.EndHorizontal();
            GUILayout.Space(HeaderBottomSpace);
        }

        /// <summary>
        /// Draws a styled foldout header that can be expanded/collapsed.
        /// Fixed version with proper text width calculation.
        /// </summary>
        /// <param name="foldout">Current foldout state (expanded/collapsed)</param>
        /// <param name="title">Foldout title text</param>
        /// <param name="icon">Optional icon texture</param>
        /// <param name="customFoldoutIcon">Optional custom foldout arrow icon</param>
        /// <param name="useSpecialText">If true, uses special text formatting with custom font</param>
        /// <returns>New foldout state</returns>
        public static bool DrawFoldout(ref bool foldout, string title, Texture2D icon = null, Texture2D customFoldoutIcon = null, bool useSpecialText = true)
        {
            // Initialize custom style
            InitializeFoldoutStyle();

            if (string.IsNullOrEmpty(title))
                title = "UNTITLED";
            else if (useSpecialText)
                title = title.ToUpperInvariant();

            // Pre-define values
            Color defaultGUIColor = GUI.color;

            // Begin vertical group for the entire header
            GUI.color = GetDynamicBackgroundColor();
            GUILayout.BeginVertical(_foldoutStyle);
            GUI.color = defaultGUIColor;

            // Create background style
            GUIStyle bgStyle = new GUIStyle(GUI.skin.box)
            {
                normal = _foldoutStyle.active,
                border = _foldoutStyle.border,
                padding = FoldoutPaddingRectOffset,
                margin = ZeroMarginRectOffset
            };

            // Get the rect for the background
            Rect backgroundRect = GUILayoutUtility.GetRect(new GUIContent(title), bgStyle, GUILayout.ExpandWidth(true));

            // Only process events and draw during Repaint to prevent glitches
            if (Event.current.type == EventType.Repaint)
            {
                // Draw icon if provided
                if (icon != null)
                {
                    Rect iconRect = new Rect(
                        backgroundRect.x + FoldoutIconLeftOffset,
                        backgroundRect.y + (backgroundRect.height - HeaderIconSize) / 2,
                        HeaderIconSize,
                        HeaderIconSize
                    );
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                // Create header style for text
                GUIStyle headerStyle = new GUIStyle
                {
                    fontSize = FoldoutFontSize,
                    normal = { textColor = GetTextColor() },
                    alignment = TextAnchor.MiddleLeft,
                    margin = ZeroMarginRectOffset,
                    padding = ZeroPaddingRectOffset,
                    wordWrap = false,
                    clipping = TextClipping.Overflow  // Changed from Clip to Overflow
                };

                if (useSpecialText)
                {
                    Font customFont = GetCustomFont(CustomFontType.Medium);
                    if (customFont != null)
                        headerStyle.font = customFont;
                }

                // Calculate text rect - FIXED WIDTH CALCULATION
                float textX = backgroundRect.x + (icon != null ? FoldoutTextLeftOffsetWithIcon : FoldoutTextLeftOffset);
                float textWidth = backgroundRect.width - (textX - backgroundRect.x) - FoldoutTextRightPadding;

                Rect textRect = new Rect(
                    textX,
                    backgroundRect.y,
                    textWidth,  // Use calculated width instead of complex formula
                    backgroundRect.height
                );

                // Draw text based on useSpecialText setting
                if (useSpecialText && FoldoutTextSpacing > 0)
                {
                    // Only use DrawTextWithSpacing when we actually need spacing
                    DrawTextWithSpacingSafe(title, headerStyle, FoldoutTextSpacing, textRect);
                }
                else
                {
                    // Use standard GUI.Label for more stable rendering
                    GUI.Label(textRect, title, headerStyle);
                }

                // Draw foldout arrow
                Rect foldoutRect = new Rect(
                    backgroundRect.xMin + FoldoutIconTopOffset,
                    backgroundRect.y + (backgroundRect.height - FoldoutIconSize) / 2,
                    FoldoutIconSize,
                    FoldoutIconSize
                );

                if (customFoldoutIcon == null)
                {
                    string arrowChar = foldout ? "▼" : "▶";
                    GUI.Label(foldoutRect, arrowChar, new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10
                    });
                }
                else
                {
                    Matrix4x4 matrix = GUI.matrix;
                    GUIUtility.RotateAroundPivot(foldout ? 180 : 0, new Vector2(foldoutRect.center.x, foldoutRect.center.y));
                    GUI.DrawTexture(foldoutRect, customFoldoutIcon);
                    GUI.matrix = matrix;
                }
            }

            // Handle mouse events separately
            if (backgroundRect.Contains(Event.current.mousePosition))
            {
                RequestRepaint();

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    foldout = !foldout;
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            GUILayout.EndVertical();
            return foldout;
        }

        /// <summary>
        /// Safer version of DrawTextWithSpacing that handles width properly
        /// </summary>
        static private void DrawTextWithSpacingSafe(string text, GUIStyle style, float spacing, Rect startRect)
        {
            if (string.IsNullOrEmpty(text) || style == null)
                return;

            // Only draw during repaint to prevent layout issues
            if (Event.current.type != EventType.Repaint)
                return;

            // Ensure rect is valid
            if (startRect.width <= 0 || startRect.height <= 0)
                return;

            Vector2 position = new Vector2(startRect.x, startRect.y);
            float maxX = startRect.x + startRect.width; // Define clear boundary

            foreach (char c in text)
            {
                string charString = c.ToString();
                Vector2 charSize = style.CalcSize(new GUIContent(charString));

                // Check if character would overflow the available width
                if (position.x + charSize.x > maxX)
                    break;

                Rect charRect = new Rect(position.x, position.y, charSize.x, startRect.height);
                GUI.Label(charRect, charString, style);
                position.x += charSize.x + spacing;
            }
        }

        /// <summary>
        /// Draws a horizontal separator line.
        /// </summary>
        /// <param name="height">Height of the separator line</param>
        public static void DrawSeparator(float height = SeparatorHeight)
        {
            // Cache GUI color
            Color cachedGUIColor = GUI.color;

            // Use cached colors for separator
            Color contentColor = EditorGUIUtility.isProSkin ? ProSkinSeparatorColor : PersonalSkinSeparatorColor;

            // Start drawing GUI color
            GUI.color = contentColor;

            if (EditorGUIUtility.whiteTexture != null)
            {
                GUILayout.BeginHorizontal(new GUIStyle()
                {
                    fixedHeight = height,
                    normal = { background = EditorGUIUtility.whiteTexture }
                });
            }
            else
            {
                GUILayout.BeginHorizontal();
            }

            // Revert GUI color for other content
            GUI.color = cachedGUIColor;
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws text with custom character spacing.
        /// </summary>
        /// <param name="text">Text to draw</param>
        /// <param name="style">GUI style to use</param>
        /// <param name="spacing">Additional spacing between characters</param>
        /// <param name="startRect">Rectangle to draw the text in</param>
        static private void DrawTextWithSpacing(string text, GUIStyle style, float spacing, Rect startRect)
        {
            if (string.IsNullOrEmpty(text) || style == null)
                return;

            Vector2 position = new Vector2(startRect.x, startRect.y); // Start position
            foreach (char c in text)
            {
                string charString = c.ToString();
                Vector2 charSize = style.CalcSize(new GUIContent(charString));
                GUI.Label(new Rect(position.x, position.y, charSize.x, startRect.height), charString, style);
                position.x += charSize.x + spacing; // Increment position with spacing
            }
        }

        #region Editor Repaint
        /// <summary>
        /// Register an editor for hover repaints. Call this in OnEnable().
        /// </summary>
        /// <param name="editor">The editor instance to register</param>
        public static void RegisterEditor(UnityEditor.Editor editor)
        {
            if (editor == null)
                return;
                
            _registeredEditors.Add(editor);

            if (!_updateHandlerRegistered)
            {
                EditorApplication.update += HandleRepaints;
                _updateHandlerRegistered = true;
            }
        }

        /// <summary>
        /// Unregister an editor from hover repaints. Call this in OnDisable().
        /// </summary>
        /// <param name="editor">The editor instance to unregister</param>
        public static void UnregisterEditor(UnityEditor.Editor editor)
        {
            if (editor == null)
                return;
                
            _registeredEditors.Remove(editor);

            if (_registeredEditors.Count == 0 && _updateHandlerRegistered)
            {
                EditorApplication.update -= HandleRepaints;
                _updateHandlerRegistered = false;
            }
        }

        /// <summary>
        /// Request a repaint for hover effects. Called internally by hover-sensitive methods.
        /// </summary>
        static private void RequestRepaint()
        {
            _needsRepaint = true;
        }

        /// <summary>
        /// Handle throttled hover repaints for all registered editors.
        /// </summary>
        static private void HandleRepaints()
        {
            if (_needsRepaint && EditorApplication.timeSinceStartup - _lastRepaintTime > EditorRepaintInterval)
            {
                _lastRepaintTime = EditorApplication.timeSinceStartup;
                _needsRepaint = false;

                // Only repaint editors that are likely visible
                if (EditorWindow.focusedWindow != null)
                {
                    foreach (var editor in _registeredEditors)
                    {
                        if (editor != null)
                        {
                            editor.Repaint();
                        }
                    }
                }

                // Clean up null references
                _registeredEditors.RemoveWhere(editor => editor == null);
            }
        }

        /// <summary>
        /// Trigger hover effects. Call this in OnInspectorGUI for hover responsiveness.
        /// </summary>
        public static void HandleInspectorGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                RequestRepaint();
            }
        }
        #endregion

        #region Toolbar/Tabs
        private static GUIStyle toolbarButtonStyle;

        // Opacity settings
        public static float NormalOpacity = 1f;
        public static float HoverOpacity = 1f;
        public static float SelectedOpacity = 1f;
        public static Color NormalColor = new(0.3f, 0.3f, 0.3f);
        public static Color HoverColor = new(0.35f, 0.35f, 0.35f);

        // Manual icon and text settings
        public static float ToolbarIconSize = 15f;
        public static float ToolbarIconTextSpacing = 0f;

        private static void InitStyle()
        {
            if (toolbarButtonStyle == null)
            {
                // Load background textures from your DTPEditorGUI resources
                Texture2D bgSolid = Resources.Load<Texture2D>("EditorBackground-Solid");

                toolbarButtonStyle = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(0, 0, 0, 0),
                    border = new RectOffset(5, 5, 5, 5),
                    normal = { background = bgSolid, textColor = GetTextColor() },
                    hover = { background = bgSolid, textColor = GetTextColor() },
                    active = { background = bgSolid, textColor = GetTextColor() },
                    onNormal = { background = bgSolid, textColor = GetTextColor() },
                    onHover = { background = bgSolid, textColor = GetTextColor() },
                    onActive = { background = bgSolid, textColor = GetTextColor() }
                };
            }
        }

        /// <summary>
        /// Draw toolbar with text only (same as GUILayout.Toolbar)
        /// </summary>
        public static int DrawToolbar(int selected, string[] texts, float spacing = 0, Color? selectedColor = null, params GUILayoutOption[] options)
        {
            InitStyle();
            return DrawCustomToolbar(selected, texts, null, null, spacing, selectedColor, options);
        }

        /// <summary>
        /// Draw toolbar with icons only
        /// </summary>
        public static int DrawToolbar(int selected, Texture2D[] icons, float spacing = 0, Color? selectedColor = null, params GUILayoutOption[] options)
        {
            InitStyle();
            return DrawCustomToolbar(selected, null, icons, null, spacing, selectedColor, options);
        }

        /// <summary>
        /// Draw toolbar with text and icons
        /// </summary>
        public static int DrawToolbar(int selected, string[] texts, string[] iconNames, float spacing = 0, Color? selectedColor = null, params GUILayoutOption[] options)
        {
            InitStyle();
            return DrawCustomToolbar(selected, texts, null, iconNames, spacing, selectedColor, options);
        }

        /// <summary>
        /// Draw toolbar with text, icons, and tooltips
        /// </summary>
        public static int DrawToolbar(int selected, string[] texts, string[] iconNames, string[] tooltips, float spacing = 0, Color? selectedColor = null, params GUILayoutOption[] options)
        {
            InitStyle();
            return DrawCustomToolbar(selected, texts, null, iconNames, spacing, selectedColor, options, tooltips);
        }

        private static int DrawCustomToolbar(int selected, string[] texts, Texture2D[] icons, string[] iconNames, float spacing, Color? selectedColor, GUILayoutOption[] options, string[] tooltips = null)
        {
            int length = texts?.Length ?? icons?.Length ?? iconNames?.Length ?? 0;
            if (length == 0) return selected;

            GUILayout.BeginHorizontal();

            int result = selected;

            for (int i = 0; i < length; i++)
            {
                // Get content
                string text = texts?[i] ?? "";
                Texture2D icon = icons?[i] ?? (iconNames?[i] != null ? GetIcon(iconNames[i]) : null);
                string tooltip = tooltips?[i] ?? "";

                // Use original content for layout (Unity handles this properly)
                GUIContent content = new GUIContent(text, icon, tooltip);

                // Get button rect for hover detection
                Rect buttonRect = GUILayoutUtility.GetRect(content, toolbarButtonStyle, options);
                bool isHover = buttonRect.Contains(Event.current.mousePosition);

                // Apply background color based on selection and hover state
                Color originalBgColor = GUI.backgroundColor;
                if (selected == i)
                {
                    Color bgColor = selectedColor ?? GetDynamicBackgroundColor(true);
                    bgColor.a *= SelectedOpacity;
                    GUI.backgroundColor = bgColor;
                }
                else if (isHover)
                {
                    Color bgColor = HoverColor;
                    bgColor.a *= HoverOpacity;
                    GUI.backgroundColor = bgColor;
                }
                else
                {
                    Color bgColor = NormalColor;
                    bgColor.a *= NormalOpacity;
                    GUI.backgroundColor = bgColor;
                }

                // Draw button normally first
                bool clicked = GUI.Button(buttonRect, "", toolbarButtonStyle); // Empty content

                // Now draw custom content on top
                DrawCustomContent(buttonRect, text, icon);

                if (clicked)
                    result = i;

                // Restore background color
                GUI.backgroundColor = originalBgColor;

                // Add spacing between buttons (except after last button)
                if (spacing > 0 && i < length - 1)
                {
                    GUILayout.Space(spacing);
                }
            }

            GUILayout.EndHorizontal();

            return result;
        }

        private static void DrawCustomContent(Rect buttonRect, string text, Texture2D icon)
        {
            bool hasIcon = icon != null;
            bool hasText = !string.IsNullOrEmpty(text);

            if (!hasIcon && !hasText) return;

            // Calculate content dimensions
            float contentWidth = 0;
            float textWidth = 0;

            if (hasIcon) contentWidth += ToolbarIconSize;
            if (hasText)
            {
                textWidth = toolbarButtonStyle.CalcSize(new GUIContent(text)).x;
                contentWidth += textWidth;
                if (hasIcon) contentWidth += ToolbarIconTextSpacing;
            }

            // Center content in button
            float startX = buttonRect.x + (buttonRect.width - contentWidth) * 0.5f;
            float currentX = startX;

            // Draw icon
            if (hasIcon)
            {
                float iconY = buttonRect.y + (buttonRect.height - ToolbarIconSize) * 0.5f;
                Rect iconRect = new Rect(currentX, iconY, ToolbarIconSize, ToolbarIconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                currentX += ToolbarIconSize;

                if (hasText) currentX += ToolbarIconTextSpacing;
            }

            // Draw text
            if (hasText)
            {
                Color originalContentColor = GUI.contentColor;
                GUI.contentColor = toolbarButtonStyle.normal.textColor;

                Rect textRect = new Rect(currentX, buttonRect.y, textWidth, buttonRect.height);
                GUIStyle textStyle = new GUIStyle(toolbarButtonStyle)
                {
                    normal = { background = null },
                    hover = { background = null },
                    active = { background = null }
                };

                GUI.Label(textRect, text, textStyle);
                GUI.contentColor = originalContentColor;
            }
        }
        #endregion
    }
}