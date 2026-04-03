using System;
using System.Threading;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Providers;
using Pholus.Editor.Providers.Interfaces;
using Pholus.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Setup wizard for first-run experience.
    /// Guides users through CLI installation and authentication.
    /// </summary>
    public class PholusSetupWizard : EditorWindow
    {
        private const string WizardShownKey = "Pholus_WizardShown";

        private enum WizardStep
        {
            Welcome,
            Prerequisites,
            Installation,
            Authentication,
            Complete
        }

        private WizardStep _currentStep = WizardStep.Welcome;
        private ProviderFactory _providerFactory;
        private ICLIDetector _detector;
        private ICLIInstaller _installer;
        private ProviderType _selectedProvider;

        // Status tracking
        private bool _prerequisitesChecked;
        private bool _isCheckingPrereqs;
        private bool _prerequisitesMet;
        private string _prerequisiteStatus;

        private bool _cliChecked;
        private bool _isCheckingCLI;
        private bool _cliInstalled;
        private string _cliVersion;
        private string _cliStatus;

        private bool _authChecked;
        private bool _isCheckingAuth;
        private bool _isAuthenticated;

        // Installation state
        private bool _isInstalling;
        private float _installProgress;
        private string _installStatus;
        private CancellationTokenSource _installCts;

        // Verification state
        private bool _isVerifying;
        private bool _verificationComplete;
        private bool _verificationSuccess;
        private string _verificationMessage;

        // OpenRouter API key input
        private string _openRouterApiKeyInput = "";

        private Vector2 _scrollPosition;

        [MenuItem("Tools/Pholus/Setup Wizard", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<PholusSetupWizard>("Pholus Setup");
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(600, 500);
            window.Show();
        }

        /// <summary>
        /// Shows wizard automatically on first run.
        /// </summary>
        public static void ShowIfFirstRun()
        {
            // Check if wizard was already shown (or completed)
            if (EditorPrefs.GetBool(WizardShownKey, false) || PholusSettings.Instance.FirstRunComplete)
            {
                return;
            }

            // Mark as shown so it won't pop up again
            EditorPrefs.SetBool(WizardShownKey, true);

            ShowWindow();
        }

        private void OnEnable()
        {
            _providerFactory = new ProviderFactory();
            _selectedProvider = PholusSettings.Instance.ActiveProvider;
            UpdateProviderServices();
        }

        private void UpdateProviderServices()
        {
            _detector = _providerFactory.CreateDetector(_selectedProvider);
            _installer = _providerFactory.CreateInstaller(_selectedProvider);

            // Save selection immediately so it persists across recompiles
            PholusSettings.Instance.ActiveProvider = _selectedProvider;

            // Reset status checks when provider changes
            _cliChecked = false;
            _authChecked = false;
            _verificationComplete = false;
        }

        private string ProviderDisplayName => _selectedProvider switch
        {
            ProviderType.Claude => "Claude Code",
            ProviderType.Codex => "OpenAI Codex",
            ProviderType.Gemini => "Google Gemini",
            ProviderType.Cursor => "Cursor",
            ProviderType.OpenRouter => "OpenRouter",
            _ => "AI Provider"
        };

        private string ProviderAccountName => _selectedProvider switch
        {
            ProviderType.Claude => "Anthropic",
            ProviderType.Codex => "OpenAI",
            ProviderType.Gemini => "Google",
            ProviderType.Cursor => "Cursor",
            ProviderType.OpenRouter => "OpenRouter",
            _ => "Provider"
        };

        private void OnDisable()
        {
            _installCts?.Cancel();
            _installCts?.Dispose();
        }

        private void OnGUI()
        {
            Styles.EnsureInitialized();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_currentStep)
            {
                case WizardStep.Welcome:
                    DrawWelcome();
                    break;
                case WizardStep.Prerequisites:
                    DrawPrerequisites();
                    break;
                case WizardStep.Installation:
                    DrawInstallation();
                    break;
                case WizardStep.Authentication:
                    DrawAuthentication();
                    break;
                case WizardStep.Complete:
                    DrawComplete();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawWelcome()
        {
            PholusEditorGUI.BeginVerticalBackground();
            PholusEditorGUI.DrawHeader("Welcome to Pholus", "Animation");
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            GUILayout.Label("A trusted code review companion, focused on guidance.", Styles.RichTextLabel);
            EditorGUILayout.Space(10);

            // Provider selection
            GUILayout.Label("Choose your AI provider:", Styles.LabelBold);
            EditorGUILayout.Space(8);

            // Three-option selector
            var toggleRect = EditorGUILayout.GetControlRect(GUILayout.Height(44));
            toggleRect.x += 10;
            toggleRect.width -= 20;

            // Draw background
            var bgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            EditorGUI.DrawRect(toggleRect, bgColor);
            GUI.Box(toggleRect, GUIContent.none, EditorStyles.helpBox);

            // Calculate button rects (5 columns)
            var fifthWidth = toggleRect.width / 5f;
            var claudeRect = new Rect(toggleRect.x, toggleRect.y, fifthWidth, toggleRect.height);
            var codexRect = new Rect(toggleRect.x + fifthWidth, toggleRect.y, fifthWidth, toggleRect.height);
            var geminiRect = new Rect(toggleRect.x + fifthWidth * 2, toggleRect.y, fifthWidth, toggleRect.height);
            var cursorRect = new Rect(toggleRect.x + fifthWidth * 3, toggleRect.y, fifthWidth, toggleRect.height);
            var openRouterRect = new Rect(toggleRect.x + fifthWidth * 4, toggleRect.y, fifthWidth, toggleRect.height);

            // Determine selected rect and color
            Rect selectedRect;
            Color highlightColor;
            switch (_selectedProvider)
            {
                case ProviderType.Claude:
                    selectedRect = claudeRect;
                    highlightColor = new Color(0.2f, 0.5f, 1.0f, 0.6f);  // Blue
                    break;
                case ProviderType.Codex:
                    selectedRect = codexRect;
                    highlightColor = new Color(0.2f, 0.8f, 0.4f, 0.6f);  // Green
                    break;
                case ProviderType.Gemini:
                    selectedRect = geminiRect;
                    highlightColor = new Color(1.0f, 0.6f, 0.2f, 0.6f);  // Orange
                    break;
                case ProviderType.Cursor:
                    selectedRect = cursorRect;
                    highlightColor = new Color(0.9f, 0.4f, 0.9f, 0.6f);  // Purple
                    break;
                case ProviderType.OpenRouter:
                    selectedRect = openRouterRect;
                    highlightColor = new Color(1.0f, 0.1f, 0.1f, 0.65f); // Red
                    break;
                default:
                    selectedRect = claudeRect;
                    highlightColor = new Color(0.2f, 0.5f, 1.0f, 0.6f);
                    break;
            }

            // Draw selection highlight
            var highlightRect = new Rect(selectedRect.x + 2, selectedRect.y + 2, selectedRect.width - 4, selectedRect.height - 4);
            EditorGUI.DrawRect(highlightRect, highlightColor);

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };

            var iconSize = 16f;
            var spacing = 4f;
            var prevColor = GUI.color;

            // Helper to draw provider option
            void DrawProviderOption(Rect rect, string iconName, string label, bool isSelected)
            {
                var icon = PholusEditorGUI.GetIcon(iconName);
                var labelColor = isSelected ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                GUI.color = labelColor;

                if (icon != null)
                {
                    var labelWidth = label.Length * 7f;
                    var totalWidth = iconSize + spacing + labelWidth;
                    var iconRect = new Rect(
                        rect.x + (rect.width - totalWidth) / 2f,
                        rect.y + (rect.height - iconSize) / 2f,
                        iconSize, iconSize);
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                    var textRect = new Rect(iconRect.xMax + spacing, rect.y, labelWidth, rect.height);
                    GUI.Label(textRect, label, labelStyle);
                }
                else
                {
                    GUI.Label(rect, label, labelStyle);
                }
                GUI.color = prevColor;
            }

            // Draw all five options (Claude is recommended)
            DrawProviderOption(claudeRect, "Animation", "Claude", _selectedProvider == ProviderType.Claude);
            DrawProviderOption(codexRect, "CodeGen", "Codex", _selectedProvider == ProviderType.Codex);
            DrawProviderOption(geminiRect, "Link", "Gemini", _selectedProvider == ProviderType.Gemini);
            DrawProviderOption(cursorRect, "Edit", "Cursor", _selectedProvider == ProviderType.Cursor);
            DrawProviderOption(openRouterRect, "Group", "OpenRouter", _selectedProvider == ProviderType.OpenRouter);

            // Provider badges
            DrawProviderBadge(claudeRect, "★ Recommended", new Color(0.4f, 0.8f, 1f));
            DrawProviderBadge(cursorRect, "β Beta", new Color(1f, 0.8f, 0.3f));
            DrawProviderBadge(openRouterRect, "α Alpha", new Color(1f, 0.5f, 0.3f));

            // Handle clicks
            if (Event.current.type == EventType.MouseDown)
            {
                if (claudeRect.Contains(Event.current.mousePosition) && _selectedProvider != ProviderType.Claude)
                {
                    _selectedProvider = ProviderType.Claude;
                    UpdateProviderServices();
                    Event.current.Use();
                    Repaint();
                }
                else if (codexRect.Contains(Event.current.mousePosition) && _selectedProvider != ProviderType.Codex)
                {
                    _selectedProvider = ProviderType.Codex;
                    UpdateProviderServices();
                    Event.current.Use();
                    Repaint();
                }
                else if (geminiRect.Contains(Event.current.mousePosition) && _selectedProvider != ProviderType.Gemini)
                {
                    _selectedProvider = ProviderType.Gemini;
                    UpdateProviderServices();
                    Event.current.Use();
                    Repaint();
                }
                else if (cursorRect.Contains(Event.current.mousePosition) && _selectedProvider != ProviderType.Cursor)
                {
                    _selectedProvider = ProviderType.Cursor;
                    UpdateProviderServices();
                    Event.current.Use();
                    Repaint();
                }
                else if (openRouterRect.Contains(Event.current.mousePosition) && _selectedProvider != ProviderType.OpenRouter)
                {
                    _selectedProvider = ProviderType.OpenRouter;
                    UpdateProviderServices();
                    Event.current.Use();
                    Repaint();
                }
            }

            // Cursor hints
            EditorGUIUtility.AddCursorRect(claudeRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(codexRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(geminiRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Link);
            EditorGUIUtility.AddCursorRect(openRouterRect, MouseCursor.Link);

            EditorGUILayout.Space(12);
            GUILayout.Label("★ Recommended: Best precision  •  β Beta: Experimental  •  α Alpha: Early testing", Styles.LabelMuted);
            GUILayout.Label("You can change this later in Settings.", Styles.LabelMuted);

            EditorGUILayout.Space(10);
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            GUILayout.Label("This wizard will help you set up:", Styles.LabelBold);
            EditorGUILayout.Space(5);
            GUILayout.Label("  1. Check prerequisites (Node.js/npm)", Styles.RichTextLabel);
            GUILayout.Label($"  2. Install the {ProviderDisplayName} CLI", Styles.RichTextLabel);
            GUILayout.Label($"  3. Authenticate with your {ProviderAccountName} account", Styles.RichTextLabel);
            GUILayout.Label("  4. Verify the connection", Styles.RichTextLabel);
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            var accountHint = _selectedProvider switch
            {
                ProviderType.Claude => "You'll need an Anthropic account with Claude Pro/Max subscription.",
                ProviderType.Codex => "You'll need an OpenAI account with ChatGPT Plus subscription.",
                ProviderType.Gemini => "Free with Google account (1000 requests/day) or paid API key.",
                ProviderType.Cursor => "Free with Cursor account (limited usage). Supports multiple AI models",
                ProviderType.OpenRouter => "Pay-per-use API. Get your API key from openrouter.ai/keys",
                _ => "You'll need an account with the selected provider."
            };
            PholusEditorGUI.DrawInfoBox(accountHint);
            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawPrerequisites()
        {
            PholusEditorGUI.BeginVerticalBackground();
            PholusEditorGUI.DrawHeader("Step 1: Prerequisites", "Zoom");
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            GUILayout.Label("Pholus requires Node.js and npm to be installed.", Styles.RichTextLabel);
            EditorGUILayout.Space(10);

            if (_isCheckingPrereqs)
            {
                DrawStatusRow("Node.js/npm", false, "Checking...");
            }
            else if (!_prerequisitesChecked)
            {
                if (PholusEditorGUI.DrawButton("Check Prerequisites", PholusEditorGUI.GetIcon("Zoom"), width: 180, height: 30))
                {
                    CheckPrerequisites();
                }
            }
            else
            {
                DrawStatusRow("Node.js/npm", _prerequisitesMet, _prerequisiteStatus);
            }

            PholusEditorGUI.EndVerticalBackground();

            if (_prerequisitesChecked && !_prerequisitesMet)
            {
                EditorGUILayout.Space(10);
                PholusEditorGUI.BeginVerticalBackground();

                // Check if it's a version issue (nvm use case) vs not installed
                var isVersionIssue = _prerequisiteStatus != null && _prerequisiteStatus.Contains("Requires v20");

                if (isVersionIssue)
                {
                    PholusEditorGUI.DrawInfoBox("Node.js version is incompatible. Requires v20+ (v18 has ESM issues).", PholusEditorGUI.InfoBoxType.Warning);

                    EditorGUILayout.Space(5);
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    // Check if nvm is installed
                    var nvmPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".nvm");

                    if (System.IO.Directory.Exists(nvmPath))
                    {
                        if (PholusEditorGUI.DrawButton("Set Node 22 as Default", PholusEditorGUI.GetIcon("Template"), width: 200, height: 28))
                        {
                            // Open terminal with nvm commands to install (if needed) and set default
                            ProcessRunner.OpenTerminal("source ~/.nvm/nvm.sh && nvm install 22 && nvm alias default 22 && echo '' && echo '✓ Node 22 is now your default version.' && echo 'You can close this terminal and click Re-check in Unity.'", keepOpen: true);
                        }
                        EditorGUILayout.Space(5);
                        GUILayout.Label("Installs Node 22 (if needed) and sets it as default.", Styles.LabelMuted);
                    }
                    else
                    {
                        GUILayout.Label("nvm not detected. Install Node.js v20 or v22+ from nodejs.org", Styles.LabelMuted);
                        EditorGUILayout.Space(5);
                        if (PholusEditorGUI.DrawButton("Open Node.js Website", PholusEditorGUI.GetIcon("Link"), width: 180, height: 28))
                        {
                            Application.OpenURL("https://nodejs.org");
                        }
                    }
#else
                    // Windows - suggest using nvm-windows or reinstalling Node
                    GUILayout.Label("Use nvm-windows to switch versions, or install Node.js v20/v22+ from nodejs.org", Styles.LabelMuted);
                    EditorGUILayout.Space(5);
                    if (PholusEditorGUI.DrawButton("Open Node.js Website", PholusEditorGUI.GetIcon("Link"), width: 180, height: 28))
                    {
                        Application.OpenURL("https://nodejs.org");
                    }
#endif
                }
                else
                {
                    PholusEditorGUI.DrawInfoBox("Please install Node.js from https://nodejs.org and restart Unity.", PholusEditorGUI.InfoBoxType.Warning);

                    EditorGUILayout.Space(5);
                    if (PholusEditorGUI.DrawButton("Open Node.js Website", PholusEditorGUI.GetIcon("Link"), width: 180, height: 28))
                    {
                        Application.OpenURL("https://nodejs.org");
                    }
                }

                EditorGUILayout.Space(5);
                if (PholusEditorGUI.DrawButton("Re-check Prerequisites", PholusEditorGUI.GetIcon("Refresh"), width: 180, height: 28))
                {
                    _prerequisitesChecked = false;
                }
                PholusEditorGUI.EndVerticalBackground();
            }
        }

        private void DrawInstallation()
        {
            PholusEditorGUI.BeginVerticalBackground();
            PholusEditorGUI.DrawHeader($"Step 2: Install {ProviderDisplayName} CLI", "CodeGen");
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            // Check current status
            if (!_cliChecked)
            {
                CheckCLIStatus();
            }

            PholusEditorGUI.BeginVerticalBackground();
            if (_isCheckingCLI)
            {
                DrawStatusRow($"{ProviderDisplayName} CLI", false, "Checking...");
            }
            else
            {
                DrawStatusRow($"{ProviderDisplayName} CLI", _cliInstalled, _cliStatus);
            }

            if (_cliInstalled && !_isCheckingCLI)
            {
                EditorGUILayout.Space(5);
                GUILayout.Label($"Version: {_cliVersion}", Styles.LabelMuted);
            }
            PholusEditorGUI.EndVerticalBackground();

            if (!_cliInstalled)
            {
                EditorGUILayout.Space(10);
                PholusEditorGUI.BeginVerticalBackground();

                if (_isInstalling)
                {
                    GUILayout.Label($"Installing {ProviderDisplayName} CLI...", Styles.RichTextLabel);
                    EditorGUILayout.Space(5);

                    var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.ProgressBar(progressRect, _installProgress, _installStatus ?? "Installing...");

                    EditorGUILayout.Space(5);
                    if (PholusEditorGUI.DrawButton("Cancel", PholusEditorGUI.GetIcon("ArrowMinimize"), width: 100, height: 28))
                    {
                        _installCts?.Cancel();
                    }
                }
                else
                {
                    GUILayout.Label($"Click to install {ProviderDisplayName} CLI via npm:", Styles.RichTextLabel);
                    EditorGUILayout.Space(5);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (PholusEditorGUI.DrawButton($"Install {ProviderDisplayName}", PholusEditorGUI.GetIcon("CodeGen"), width: 180, height: 32, tintColor: new Color(0.2f, 1.0f, 0.2f, 0.25f)))
                    {
                        StartInstallation();
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);
                    GUILayout.Label($"Command: {_installer.InstallCommand}", Styles.LabelMuted);

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    // Mac/Linux hint about terminal window
                    EditorGUILayout.Space(8);
                    EditorGUILayout.HelpBox(
                        "A Terminal window will open. Enter your password if prompted, wait for installation to complete, then click 'Re-check Installation' below.",
                        MessageType.Info);
#endif

                    // Re-check button in case installation completed but wasn't detected
                    EditorGUILayout.Space(10);
                    if (PholusEditorGUI.DrawButton("Re-check Installation", PholusEditorGUI.GetIcon("Refresh"), width: 160, height: 28))
                    {
                        _cliChecked = false;
                        CheckCLIStatus();
                        Repaint();
                    }
                }

                PholusEditorGUI.EndVerticalBackground();
            }
            else
            {
                // CLI is installed - show re-check option
                EditorGUILayout.Space(5);
                if (PholusEditorGUI.DrawButton("Re-check", PholusEditorGUI.GetIcon("Refresh"), width: 100, height: 24))
                {
                    _cliChecked = false;
                    CheckCLIStatus();
                    Repaint();
                }
            }

            // Credit for OpenRouter CLI author - show whether installed or not
            if (_selectedProvider == ProviderType.OpenRouter)
            {
                EditorGUILayout.Space(10);
                DrawOpenRouterCredit();
            }
        }

        private void DrawAuthentication()
        {
            PholusEditorGUI.BeginVerticalBackground();
            PholusEditorGUI.DrawHeader("Step 3: Authentication", "Settings");
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            // Re-check auth status
            if (!_authChecked)
            {
                CheckAuthStatus();
            }

            PholusEditorGUI.BeginVerticalBackground();
            if (_isCheckingAuth)
            {
                DrawStatusRow($"{ProviderDisplayName}", false, "Checking...");
            }
            else
            {
                DrawStatusRow($"{ProviderDisplayName}", _isAuthenticated, _isAuthenticated ? "Logged in" : "Not logged in");
            }
            PholusEditorGUI.EndVerticalBackground();

            if (!_isAuthenticated && !_isCheckingAuth)
            {
                EditorGUILayout.Space(10);
                PholusEditorGUI.BeginVerticalBackground();

                // Special flow for OpenRouter - enter API key directly
                if (_selectedProvider == ProviderType.OpenRouter)
                {
                    DrawOpenRouterApiKeyEntry();
                }
                else
                {
                    // Standard flow for other providers - open terminal
                    GUILayout.Label($"You need to log in to {ProviderDisplayName} to use Pholus.", Styles.RichTextLabel);
                    EditorGUILayout.Space(5);
                    GUILayout.Label("Click the button below to open a terminal and run the login command.", Styles.LabelMuted);
                    EditorGUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (PholusEditorGUI.DrawButton("Open Login Terminal", PholusEditorGUI.GetIcon("Template"), width: 180, height: 32, tintColor: new Color(0.2f, 0.6f, 1.0f, 0.25f)))
                    {
                        _installer.OpenLoginTerminal();
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(10);
                    GUILayout.Label($"Command: {_installer.LoginCommand}", Styles.LabelMuted);

                    EditorGUILayout.Space(10);
                    if (PholusEditorGUI.DrawButton("Re-check Authentication", PholusEditorGUI.GetIcon("Refresh"), width: 180, height: 28))
                    {
                        _authChecked = false;
                    }
                }

                PholusEditorGUI.EndVerticalBackground();
            }
            else if (_isAuthenticated && !_isCheckingAuth)
            {
                EditorGUILayout.Space(10);
                PholusEditorGUI.BeginVerticalBackground();

                if (!_verificationComplete)
                {
                    GUILayout.Label("Let's verify the connection works:", Styles.RichTextLabel);
                    EditorGUILayout.Space(10);

                    if (_isVerifying)
                    {
                        GUILayout.Label("Verifying connection...", Styles.LabelMuted);
                        EditorGUILayout.Space(5);
                        var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                        EditorGUI.ProgressBar(progressRect, -1f, "Testing...");
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (PholusEditorGUI.DrawButton("Verify Connection", PholusEditorGUI.GetIcon("Animation"), width: 160, height: 32, tintColor: new Color(0.2f, 1.0f, 0.2f, 0.25f)))
                        {
                            VerifyConnection();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    if (_verificationSuccess)
                    {
                        var prevColor = GUI.color;
                        GUI.color = Styles.SuccessColor;
                        GUILayout.Label("\u2713 Connection verified successfully!", Styles.LabelBold);
                        GUI.color = prevColor;
                    }
                    else
                    {
                        PholusEditorGUI.DrawInfoBox($"Verification failed: {_verificationMessage}", PholusEditorGUI.InfoBoxType.Error);
                        EditorGUILayout.Space(5);
                        if (PholusEditorGUI.DrawButton("Retry Verification", PholusEditorGUI.GetIcon("Refresh"), width: 150, height: 28))
                        {
                            _verificationComplete = false;
                        }
                    }
                }

                PholusEditorGUI.EndVerticalBackground();
            }
        }

        private void DrawComplete()
        {
            PholusEditorGUI.BeginVerticalBackground();

            var prevColor = GUI.color;
            GUI.color = Styles.SuccessColor;
            PholusEditorGUI.DrawHeader("\u2713 Setup Complete!", "Animation");
            GUI.color = prevColor;

            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            GUILayout.Label("Pholus is ready to use!", Styles.LabelBold);
            EditorGUILayout.Space(5);
            GUILayout.Label($"Configured with: {ProviderDisplayName}", Styles.LabelMuted);
            EditorGUILayout.Space(10);
            GUILayout.Label("You can now:", Styles.RichTextLabel);
            EditorGUILayout.Space(5);
            GUILayout.Label("  \u2022 Open scripts in the Pholus window", Styles.RichTextLabel);
            GUILayout.Label("  \u2022 Analyze scripts for performance issues", Styles.RichTextLabel);
            GUILayout.Label("  \u2022 Apply AI-generated fixes with preview", Styles.RichTextLabel);
            GUILayout.Label("  \u2022 Scan entire projects for issues", Styles.RichTextLabel);
            PholusEditorGUI.EndVerticalBackground();

            EditorGUILayout.Space(10);

            PholusEditorGUI.BeginVerticalBackground();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (PholusEditorGUI.DrawButton("Open Pholus", PholusEditorGUI.GetIcon("Animation"), width: 160, height: 36, tintColor: new Color(0.2f, 1.0f, 0.2f, 0.25f)))
            {
                // Save selected provider to settings
                PholusSettings.Instance.ActiveProvider = _selectedProvider;
                PholusSettings.Instance.FirstRunComplete = true;

                // Reinitialize services with selected provider
                PholusServices.Reinitialize();

                PholusWindow.ShowWindow();
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawFooter()
        {
            // Support section
            EditorGUILayout.Space(8);
            DrawSupportSection();

            EditorGUILayout.Space(10);
            PholusEditorGUI.BeginHorizontalBackground();

            // Back button (except on welcome and complete)
            if (_currentStep > WizardStep.Welcome && _currentStep < WizardStep.Complete)
            {
                if (PholusEditorGUI.DrawButton("Back", PholusEditorGUI.GetIcon("ArrowMinimize"), width: 80, height: 28))
                {
                    _currentStep--;
                    ResetStepState();
                }
            }
            else
            {
                GUILayout.Space(80);
            }

            GUILayout.FlexibleSpace();

            // Step indicator
            GUILayout.Label($"Step {(int)_currentStep + 1} of 5", Styles.LabelMuted);

            GUILayout.FlexibleSpace();

            // Next/Skip button
            if (_currentStep < WizardStep.Complete)
            {
                var canProceed = CanProceedToNextStep();

                EditorGUI.BeginDisabledGroup(!canProceed && !CanSkipStep());
                if (PholusEditorGUI.DrawButton(canProceed ? "Next" : "Skip", PholusEditorGUI.GetIcon("ArrowExpand"), reverse: true, width: 80, height: 28, tintColor: canProceed ? new Color(0.2f, 1.0f, 0.2f, 0.25f) : Color.clear))
                {
                    _currentStep++;
                    ResetStepState();
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUILayout.Space(80);
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private void DrawSupportSection()
        {
            var supportStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                wordWrap = true
            };

            var emailStyle = new GUIStyle(supportStyle)
            {
                normal = { textColor = new Color(0.4f, 0.7f, 1.0f) },
                hover = { textColor = new Color(0.5f, 0.8f, 1.0f) }
            };

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Having trouble with setup? We're here to help and improve.", supportStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Reach out to us at", supportStyle);
            if (GUILayout.Button("codeturion@gmail.com", emailStyle))
            {
                Application.OpenURL("mailto:codeturion@gmail.com?subject=Pholus%20Setup%20Support");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(string label, bool isOk, string status)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, Styles.LabelBold, GUILayout.Width(150));

            var prevColor = GUI.color;
            GUI.color = isOk ? Styles.SuccessColor : Styles.CriticalColor;
            GUILayout.Label(isOk ? "\u2713" : "\u2717", GUILayout.Width(20));
            GUI.color = prevColor;

            GUILayout.Label(status, Styles.LabelMuted);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws credit for the OpenRouter CLI open-source project.
        /// </summary>
        private void DrawOpenRouterCredit()
        {
            var creditStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };

            EditorGUILayout.BeginVertical();

            GUILayout.Label("CLI by <color=#88AAFF>Jason Williams</color> (jwill9999)", creditStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var linkStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.7f, 1f) },
                hover = { textColor = new Color(0.7f, 0.85f, 1f) },
                fontSize = 11
            };

            if (GUILayout.Button("GitHub", linkStyle, GUILayout.Width(50)))
            {
                Application.OpenURL("https://github.com/jwill9999/openrouter-cli");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.Label("•", creditStyle, GUILayout.Width(12));

            if (GUILayout.Button("npm", linkStyle, GUILayout.Width(35)))
            {
                Application.OpenURL("https://www.npmjs.com/package/@letuscode/openrouter-cli");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a badge label below a provider option (e.g., "★ Recommended", "β Beta", "α Alpha").
        /// Reusable for different badge types with custom colors.
        /// </summary>
        private void DrawProviderBadge(Rect providerRect, string badgeText, Color badgeColor)
        {
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = badgeColor },
                fontSize = 9
            };
            var badgeRect = new Rect(providerRect.x, providerRect.yMax + 2, providerRect.width, 14);
            GUI.Label(badgeRect, badgeText, badgeStyle);
        }

        private void DrawOpenRouterApiKeyEntry()
        {
            // Header
            var headerStyle = new GUIStyle(Styles.LabelBold)
            {
                fontSize = 13,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };
            GUILayout.Label("Enter your OpenRouter API Key", headerStyle);
            EditorGUILayout.Space(8);

            // Instructions
            GUILayout.Label("OpenRouter provides access to 100+ AI models with pay-per-use pricing.", Styles.RichTextLabel);
            EditorGUILayout.Space(5);

            // Link to get API key - prominent button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("1. Get your API key:", Styles.LabelBold, GUILayout.Width(120));
            if (PholusEditorGUI.DrawButton("Open openrouter.ai/keys", PholusEditorGUI.GetIcon("Link"), width: 200, height: 28, tintColor: new Color(0.8f, 0.6f, 0.2f, 0.3f)))
            {
                Application.OpenURL("https://openrouter.ai/keys");
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // API Key input field
            GUILayout.Label("2. Paste your API key below:", Styles.LabelBold);
            EditorGUILayout.Space(5);

            // Text field with placeholder styling
            var inputStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 6, 6)
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            _openRouterApiKeyInput = EditorGUILayout.TextField(_openRouterApiKeyInput, inputStyle, GUILayout.Height(28));
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            // Hint about key format
            if (string.IsNullOrEmpty(_openRouterApiKeyInput))
            {
                var hintStyle = new GUIStyle(Styles.LabelMuted) { fontStyle = FontStyle.Italic };
                GUILayout.Label("  Key starts with 'sk-or-...'", hintStyle);
            }

            EditorGUILayout.Space(15);

            // Save button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_openRouterApiKeyInput) || !_openRouterApiKeyInput.StartsWith("sk-"));
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (PholusEditorGUI.DrawButton("Save API Key", PholusEditorGUI.GetIcon("Animation"), width: 160, height: 36, tintColor: new Color(0.2f, 1.0f, 0.2f, 0.3f)))
            {
                // Save to settings
                PholusSettings.Instance.OpenRouterApiKey = _openRouterApiKeyInput.Trim();

                // For OpenRouter, we know the API key is valid if it's saved
                // Don't use background check since EditorPrefs isn't thread-safe
                _isAuthenticated = true;
                _authChecked = true;
                _isCheckingAuth = false;
                PholusLogger.Log("OpenRouter API key saved successfully");
                Repaint();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // Security note
            EditorGUILayout.Space(10);
            PholusEditorGUI.DrawInfoBox("Your API key is stored locally in Unity EditorPrefs and never sent anywhere except OpenRouter's API.", PholusEditorGUI.InfoBoxType.Default);
        }

        private void CheckPrerequisites()
        {
            // Prevent multiple concurrent checks
            if (_isCheckingPrereqs) return;

            _isCheckingPrereqs = true;
            _prerequisiteStatus = "Checking...";

            // Run check in background to avoid freezing UI
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var met = _installer.CheckPrerequisites();
                    var status = _installer.GetPrerequisiteStatus();

                    // Update UI on main thread
                    EditorApplication.delayCall += () =>
                    {
                        _prerequisitesMet = met;
                        _prerequisiteStatus = status;
                        _prerequisitesChecked = true;
                        _isCheckingPrereqs = false;
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    EditorApplication.delayCall += () =>
                    {
                        _prerequisiteStatus = $"Error: {ex.Message}";
                        _prerequisitesChecked = true;
                        _isCheckingPrereqs = false;
                        Repaint();
                    };
                }
            });
        }

        private void CheckCLIStatus()
        {
            // Prevent multiple concurrent checks
            if (_isCheckingCLI) return;

            _isCheckingCLI = true;
            _cliStatus = "Checking...";

            // Run check in background to avoid freezing UI
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Clear provider cache to ensure fresh detection
                    _detector.ClearCache();

                    var installed = _detector.IsInstalled();
                    var version = _detector.GetVersion();
                    var status = _detector.GetStatusMessage();

                    // Update UI on main thread
                    EditorApplication.delayCall += () =>
                    {
                        _cliInstalled = installed;
                        _cliVersion = version;
                        _cliStatus = status;
                        _cliChecked = true;
                        _isCheckingCLI = false;
                        PholusLogger.Log($"CLI Status Check: Installed={_cliInstalled}, Version={_cliVersion}");
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    EditorApplication.delayCall += () =>
                    {
                        _cliStatus = $"Error: {ex.Message}";
                        _cliChecked = true;
                        _isCheckingCLI = false;
                        Repaint();
                    };
                }
            });
        }

        private void CheckAuthStatus()
        {
            // Prevent multiple concurrent checks
            if (_isCheckingAuth) return;

            _isCheckingAuth = true;

            // OpenRouter uses EditorPrefs for API key storage, which isn't thread-safe
            // Check it synchronously on main thread instead
            if (_selectedProvider == ProviderType.OpenRouter)
            {
                try
                {
                    _detector.ClearCache();
                    _isAuthenticated = _detector.IsAuthenticated();
                    _authChecked = true;
                    _isCheckingAuth = false;
                    PholusLogger.Log($"Auth Status Check (OpenRouter): Authenticated={_isAuthenticated}");
                    Repaint();
                }
                catch (Exception ex)
                {
                    PholusLogger.LogWarning($"Auth check error: {ex.Message}");
                    _authChecked = true;
                    _isCheckingAuth = false;
                    Repaint();
                }
                return;
            }

            // Run check in background to avoid freezing UI (for other providers)
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Clear cache for fresh auth check
                    _detector.ClearCache();

                    var authenticated = _detector.IsAuthenticated();

                    // Update UI on main thread
                    EditorApplication.delayCall += () =>
                    {
                        _isAuthenticated = authenticated;
                        _authChecked = true;
                        _isCheckingAuth = false;
                        PholusLogger.Log($"Auth Status Check: Authenticated={_isAuthenticated}");
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    EditorApplication.delayCall += () =>
                    {
                        PholusLogger.LogWarning($"Auth check error: {ex.Message}");
                        _authChecked = true;
                        _isCheckingAuth = false;
                        Repaint();
                    };
                }
            });
        }

        private async void StartInstallation()
        {
            _isInstalling = true;
            _installProgress = 0f;
            _installStatus = "Starting installation...";
            _installCts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<float>(p =>
                {
                    _installProgress = p;
                    _installStatus = p < 0.3f ? "Downloading..." : p < 0.8f ? "Installing..." : "Finalizing...";
                    Repaint();
                });

                var result = await _installer.InstallAsync(progress, _installCts.Token);

                if (result.Success)
                {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                    // On Mac/Linux, terminal opens for user to complete install manually
                    // Don't auto-check - user will click "Re-check Installation" when done
                    PholusLogger.Log("Terminal opened for installation. User should click 'Re-check Installation' when complete.");
#else
                    // On Windows, install runs in background and completes here
                    _cliChecked = false; // Force re-check
                    CheckCLIStatus();
#endif
                }
                else
                {
                    PholusLogger.LogError($"Installation failed: {result.Error}");
                    EditorUtility.DisplayDialog("Installation Failed", result.Error, "OK");
                }
            }
            catch (OperationCanceledException)
            {
                PholusLogger.Log("Installation cancelled");
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Installation error: {ex.Message}");
                EditorUtility.DisplayDialog("Installation Error", ex.Message, "OK");
            }
            finally
            {
                _isInstalling = false;
                _installCts?.Dispose();
                _installCts = null;
                Repaint();
            }
        }

        private async void VerifyConnection()
        {
            _isVerifying = true;
            Repaint();

            try
            {
                // Simple test - just check if the CLI responds
                // Use _selectedProvider (not ActiveProvider) since settings aren't saved until wizard completes
                var provider = _providerFactory.CreateProvider(_selectedProvider);
                var response = await provider.SendPromptAsync("Reply with just the word 'OK' - nothing else.");

                _verificationSuccess = response.Success && !string.IsNullOrEmpty(response.Output);
                _verificationMessage = _verificationSuccess ? "OK" : (response.Error ?? "No response received");
            }
            catch (Exception ex)
            {
                _verificationSuccess = false;
                _verificationMessage = ex.Message;
            }
            finally
            {
                _isVerifying = false;
                _verificationComplete = true;
                Repaint();
            }
        }

        private bool CanProceedToNextStep()
        {
            return _currentStep switch
            {
                WizardStep.Welcome => true,
                WizardStep.Prerequisites => _prerequisitesChecked && _prerequisitesMet,
                WizardStep.Installation => _cliInstalled,
                WizardStep.Authentication => _isAuthenticated && _verificationComplete && _verificationSuccess,
                WizardStep.Complete => true,
                _ => false
            };
        }

        private bool CanSkipStep()
        {
            // Allow skipping only if already set up
            return _currentStep switch
            {
                WizardStep.Prerequisites => _prerequisitesMet,
                WizardStep.Installation => _cliInstalled,
                WizardStep.Authentication => _isAuthenticated,
                _ => false
            };
        }

        private void ResetStepState()
        {
            // Reset step-specific state when navigating
            if (_currentStep == WizardStep.Prerequisites)
            {
                // Keep the check result, but reset checking flag
                _isCheckingPrereqs = false;
            }
            else if (_currentStep == WizardStep.Installation)
            {
                _cliChecked = false;
                _isCheckingCLI = false;
            }
            else if (_currentStep == WizardStep.Authentication)
            {
                _authChecked = false;
                _isCheckingAuth = false;
                _verificationComplete = false;
            }
        }
    }
}
