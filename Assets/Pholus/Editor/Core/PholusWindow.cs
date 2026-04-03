using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pholus.Editor.Analysis;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Consensus;
using Pholus.Editor.Fixes;
using Pholus.Editor.Fixes.Models;
using Pholus.Editor.Providers;
using Pholus.Editor.Providers.Interfaces;
using Pholus.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace Pholus.Editor.Core
{
    /// <summary>
    /// Main Pholus Editor Window.
    /// Entry point for the tool's UI.
    /// </summary>
    public class PholusWindow : EditorWindow
    {
        // Analysis mode
        private enum AnalysisMode { Scripts, Folder, Project }
        private AnalysisMode _analysisMode = AnalysisMode.Scripts;
        private const int MaxParallelScans = 3; // Limit concurrent API calls to avoid rate limits

        // Services
        private AnalysisService _analysisService;
        private FixService _fixService;
        private ICLIDetector _detector;

        // UI Views
        private IssueListView _issueListView;
        private DiffPreviewView _diffPreviewView;

        // State
        [SerializeField] private List<MonoScript> _selectedScripts = new List<MonoScript>();
        private int _objectPickerControlId;
        [SerializeField] private AnalysisResult _currentResult;
        [SerializeField] private ConsensusResult _currentConsensusResult;
        private FixPreview _currentFixPreview;
        private CancellationTokenSource _cts;

        // Folder scan state
        [SerializeField] private string _selectedFolderPath;
        [SerializeField] private ProjectScanResult _scanResult;
        private ScanProgress _currentScanProgress;
        private Dictionary<string, bool> _resultFoldouts = new Dictionary<string, bool>();
        private bool _isScanning;
        private bool _cleanScriptsFoldout;

        // Result cache (persisted to disk)
        private Dictionary<string, AnalysisResult> _scriptResultCache = new Dictionary<string, AnalysisResult>();
        private Dictionary<string, ProjectScanResult> _folderScanCache = new Dictionary<string, ProjectScanResult>();
        private ProjectScanResult _projectScanCache;

        // Consensus results cache (for showing provider opinions in scan results)
        [SerializeField] private List<string> _consensusCacheKeys = new List<string>();
        [SerializeField] private List<ConsensusResult> _consensusCacheValues = new List<ConsensusResult>();
        [NonSerialized] private Dictionary<string, ConsensusResult> _consensusResultCache;
        private static readonly string CachePath = Path.Combine(Application.dataPath, "..", "Library", "Pholus", "ResultCache.json");

        private bool _isAnalyzing;
        private bool _isGeneratingFix;
        private bool _showDiffPreview;
        private bool _showSettings;
        private string _statusMessage;
        private MessageType _statusType;
        private float _statusTime;
        private float _analysisStartTime;

        // Settings foldouts
        private Vector2 _settingsScrollPosition;
        private bool _providerOptionsFoldout = true;
        private bool _analysisOptionsFoldout = true;
        private bool _consensusModeFoldout = false;
        private bool _fixOptionsFoldout = true;
        private bool _preferencesFoldout = true;
        private bool _loggingFoldout = false;
        private bool _proFeaturesFoldout = false;
        private bool _contactUsFoldout = false;

        // Main UI foldouts
        private bool _scriptSelectionFoldout = true;
        private bool _resultsFoldout = true;
        private bool _detailViewFoldout = true;

        // Model Selection state
        private bool _isDiscoveringModels;
        private ProviderType? _discoveringProvider;
        private bool _showAddModelPopup;
        private string _newModelInput = "";
        private ProviderType _addModelForProvider;

        // Cached detector status (to avoid blocking main thread every frame)
        private bool _cachedIsInstalled;
        private bool _cachedIsAuthenticated;
        private string _cachedStatusMessage = "Checking...";
        private bool _isRefreshingDetectorStatus;

        private Vector2 _mainScrollPosition;

        [MenuItem("Tools/Pholus/Pholus Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<PholusWindow>();
            window.titleContent = new GUIContent("Pholus", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeServices();
            InitializeViews();
            LoadCache();

            // Clear orphaned results in Scripts mode if scripts list is empty
            if (_analysisMode == AnalysisMode.Scripts && _selectedScripts.Count == 0)
            {
                _currentResult = null;
                _scanResult = null;
            }

            // Clear non-serialized state that may be inconsistent after recompile
            // (e.g., user was viewing diff preview when recompile happened)
            _showDiffPreview = false;
            _currentFixPreview = null;
            _isAnalyzing = false;
            _isGeneratingFix = false;
            _isScanning = false;

            // Subscribe to settings changes
            PholusSettings.OnSettingsChanged += OnSettingsChanged;

            // Subscribe to Cursor background refresh (repaint when WSL check completes)
            CursorProvider.OnRefreshComplete += OnCursorRefreshComplete;
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            PholusSettings.OnSettingsChanged -= OnSettingsChanged;
            CursorProvider.OnRefreshComplete -= OnCursorRefreshComplete;
        }

        private void OnCursorRefreshComplete()
        {
            Repaint();
        }

        private void InitializeServices()
        {
            try
            {
                PholusServices.Initialize();

                _analysisService = AnalysisService.Create();
                _fixService = FixService.Create();

                // Always get fresh detector (may have changed if provider switched)
                _detector = PholusServices.Get<ICLIDetector>();

                // Refresh detector status in background
                RefreshDetectorStatusAsync();
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to initialize: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes detector status (IsInstalled, IsAuthenticated, StatusMessage) in background.
        /// Prevents blocking main thread when checking CLI status.
        /// </summary>
        private void RefreshDetectorStatusAsync()
        {
            if (_isRefreshingDetectorStatus || _detector == null) return;

            _isRefreshingDetectorStatus = true;
            _cachedStatusMessage = "Checking...";

            // OpenRouter uses EditorPrefs which isn't thread-safe - check synchronously
            var settings = PholusSettings.Instance;
            if (settings.ActiveProvider == ProviderType.OpenRouter)
            {
                try
                {
                    _detector.ClearCache();
                    _cachedIsInstalled = _detector.IsInstalled();
                    _cachedIsAuthenticated = _detector.IsAuthenticated();
                    _cachedStatusMessage = _detector.GetStatusMessage();
                    _isRefreshingDetectorStatus = false;
                    Repaint();
                }
                catch (Exception ex)
                {
                    _cachedStatusMessage = $"Error: {ex.Message}";
                    _isRefreshingDetectorStatus = false;
                    Repaint();
                }
                return;
            }

            // Other providers - check in background
            Task.Run(() =>
            {
                try
                {
                    _detector.ClearCache();
                    var isInstalled = _detector.IsInstalled();
                    var isAuthenticated = _detector.IsAuthenticated();
                    var statusMessage = _detector.GetStatusMessage();

                    EditorApplication.delayCall += () =>
                    {
                        _cachedIsInstalled = isInstalled;
                        _cachedIsAuthenticated = isAuthenticated;
                        _cachedStatusMessage = statusMessage;
                        _isRefreshingDetectorStatus = false;
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    EditorApplication.delayCall += () =>
                    {
                        _cachedStatusMessage = $"Error: {ex.Message}";
                        _isRefreshingDetectorStatus = false;
                        Repaint();
                    };
                }
            });
        }

        private void InitializeViews()
        {
            _issueListView = new IssueListView();
            _issueListView.OnFixRequested += OnFixRequested;
            _issueListView.OnSkipRequested += OnSkipRequested;

            // Restore consensus result after recompile
            if (_currentConsensusResult != null)
            {
                _issueListView.SetConsensusResult(_currentConsensusResult, PholusSettings.Instance.ShowProviderBreakdown);
            }

            _diffPreviewView = new DiffPreviewView();
            _diffPreviewView.OnApply += OnApplyFix;
            _diffPreviewView.OnApplyAndDontAsk += OnApplyFixAndDontAsk;
            _diffPreviewView.OnCancel += OnCancelFix;
        }

        private void OnGUI()
        {
            Styles.EnsureInitialized();

            if (_showSettings)
            {
                DrawSettings();
                return;
            }

            if (_showDiffPreview && _currentFixPreview != null)
            {
                DrawDiffPreview();
                return;
            }

            DrawHeader();
            DrawScriptSelector();

            // Show script list and drag-drop zone for multi-script mode
            if (_analysisMode == AnalysisMode.Scripts)
            {
                DrawMultiScriptList();
            }

            DrawActions();

            // Show prominent progress bar during analysis/fix/scan
            if (_isAnalyzing || _isGeneratingFix || _isScanning)
            {
                DrawProgressBar();
            }

            DrawMainContent();
            DrawFooter();
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            PholusEditorGUI.BeginHorizontalBackground();

            PholusEditorGUI.DrawHeader("Pholus", PholusEditorGUI.GetIcon("Timeline"));

            GUILayout.FlexibleSpace();

            // Token/Cache stats
            DrawTokenStats();

            GUILayout.Space(15);

            // Provider status (use cached values to avoid blocking main thread)
            var status = _cachedStatusMessage ?? "Not initialized";
            var isConnected = _cachedIsAuthenticated;

            var statusColor = _isRefreshingDetectorStatus ? Color.gray : (isConnected ? Styles.SuccessColor : Styles.MediumColor);
            var prevColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label($"\u25cf {status}", Styles.LabelMuted);
            GUI.color = prevColor;

            GUILayout.Space(10);

            if (PholusEditorGUI.DrawButton("Settings", PholusEditorGUI.GetIcon("Settings"), reverse: true, width: 90, height: 26, iconSize: 14, textSize: 12))
            {
                _showSettings = true;
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private void DrawTokenStats()
        {
            var settings = PholusSettings.Instance;

            // Only show token stats for Claude-only mode (accurate tracking + cache TTL)
            var isClaudeOnly = !settings.EnableConsensusMode &&
                               settings.ActiveProvider == ProviderType.Claude;

            if (!isClaudeOnly)
                return;

            var total = SessionUsageTracker.SessionTotal;
            var requestCount = SessionUsageTracker.RequestCount;

            if (requestCount == 0)
            {
                GUILayout.Label("No requests", Styles.LabelMuted);
                return;
            }

            // Used tokens
            var usedTokens = total.InputTokens + total.OutputTokens;
            GUILayout.Label($"{usedTokens:N0} used", Styles.LabelMuted);

            // Cache info
            if (SessionUsageTracker.IsCacheValid)
            {
                GUILayout.Space(8);
                var seconds = SessionUsageTracker.CacheSecondsRemaining;
                var minutes = seconds / 60;
                var secs = seconds % 60;
                var timeStr = minutes > 0 ? $"{minutes}:{secs:D2}" : $"{secs}s";

                var cacheColor = seconds < 60 ? Styles.MediumColor : Styles.SuccessColor;
                var prevColor = GUI.color;
                GUI.color = cacheColor;
                GUILayout.Label($"\u2713 {SessionUsageTracker.CachedTokens:N0} cached ({timeStr})", Styles.LabelMuted);
                GUI.color = prevColor;

                // Keep refreshing while cache is valid
                Repaint();
            }
            else if (SessionUsageTracker.CachedTokens > 0)
            {
                GUILayout.Space(8);
                var prevColor = GUI.color;
                GUI.color = Styles.LowColor;
                GUILayout.Label("cache expired", Styles.LabelMuted);
                GUI.color = prevColor;
            }
        }

        private void DrawScriptSelector()
        {
            PholusEditorGUI.BeginHorizontalBackground(isDynamic: true);

            // Mode dropdown
            GUILayout.Label("Mode:", GUILayout.Width(40));
            var newMode = (AnalysisMode)EditorGUILayout.EnumPopup(_analysisMode, GUILayout.Width(110));
            if (newMode != _analysisMode)
            {
                PholusLogger.Log($"Mode changed: {_analysisMode} -> {newMode}");

                // Store current results in cache before switching
                StoreCurrentResultsInCache();

                _analysisMode = newMode;

                // Restore from cache for new mode
                RestoreResultsFromCache();

                Repaint();
            }

            GUILayout.Space(10);

            if (_analysisMode == AnalysisMode.Scripts)
            {
                // Just show the Analyze button inline with mode selector
                GUILayout.FlexibleSpace();

                GUI.enabled = _selectedScripts.Count > 0 && !_isAnalyzing && !_isGeneratingFix && !_isScanning;
                if (PholusEditorGUI.DrawButton("Analyze", PholusEditorGUI.GetIcon("Play"),
                    reverse: true, width: 100, height: 22, iconSize: 12, textSize: 12,
                    tintColor: new Color(0.2f, 1.0f, 0.2f, 0.25f)))
                {
                    AnalyzeScripts();
                }
                GUI.enabled = true;

                // Handle object picker result (picker opened from foldout)
                if (Event.current.commandName == "ObjectSelectorUpdated" &&
                    EditorGUIUtility.GetObjectPickerControlID() == _objectPickerControlId)
                {
                    var pickedScript = EditorGUIUtility.GetObjectPickerObject() as MonoScript;
                    if (pickedScript != null && !_selectedScripts.Contains(pickedScript))
                    {
                        _selectedScripts.Add(pickedScript);
                        _scanResult = null;
                    }
                }
            }
            else if (_analysisMode == AnalysisMode.Folder)
            {
                GUILayout.Label("Folder:", GUILayout.Width(45));

                // Display folder path or placeholder
                var displayPath = string.IsNullOrEmpty(_selectedFolderPath) ? "Select a folder..." : _selectedFolderPath;
                EditorGUILayout.TextField(displayPath);

                if (PholusEditorGUI.DrawButton("Browse", PholusEditorGUI.GetIcon("Folder"),
                    reverse: true, width: 75, height: 22, iconSize: 12, textSize: 11,
                    tintColor: new Color(0.5f, 0.7f, 1.0f, 0.25f)))
                {
                    var startPath = string.IsNullOrEmpty(_selectedFolderPath) ? "Assets" : _selectedFolderPath;
                    var fullPath = EditorUtility.OpenFolderPanel("Select Folder to Scan", startPath, "");

                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        // Convert to Assets-relative path if inside project
                        var dataPath = Application.dataPath;
                        if (fullPath.StartsWith(dataPath))
                        {
                            _selectedFolderPath = "Assets" + fullPath.Substring(dataPath.Length);
                        }
                        else
                        {
                            _selectedFolderPath = fullPath;
                        }
                        _scanResult = null;
                        _resultFoldouts.Clear();
                    }
                }
            }
            else // Project mode
            {
                GUILayout.Label("Target:", GUILayout.Width(45));
                var prevColor = GUI.color;
                GUI.color = new Color(0.5f, 0.8f, 1f);
                GUILayout.Label("Entire Project (Assets/)", Styles.LabelBold);
                GUI.color = prevColor;

                GUILayout.FlexibleSpace();

                // Show excluded folders info
                GUILayout.Label("Excludes: Plugins, Editor", Styles.LabelMuted);
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private void DrawMultiScriptList()
        {
            PholusEditorGUI.BeginVerticalBackground();

            // Use same DrawFoldout as Analysis Results
            var title = _selectedScripts.Count > 0
                ? $"Selected Scripts ({_selectedScripts.Count})"
                : "Selected Scripts";

            if (PholusEditorGUI.DrawFoldout(ref _scriptSelectionFoldout, title, PholusEditorGUI.GetIcon("Copy")))
            {
                // Action buttons row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                // Add button
                if (PholusEditorGUI.DrawButton("+ Add", PholusEditorGUI.GetIcon("Plus"),
                    reverse: true, width: 65, height: 22, iconSize: 12, textSize: 11,
                    tintColor: new Color(0.3f, 0.8f, 0.4f, 0.25f)))
                {
                    _objectPickerControlId = GUIUtility.GetControlID(FocusType.Passive);
                    EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "t:MonoScript", _objectPickerControlId);
                }

                // Clear button (only if scripts selected)
                if (_selectedScripts.Count > 0)
                {
                    GUILayout.Space(5);
                    if (PholusEditorGUI.DrawButton("Clear All", PholusEditorGUI.GetIcon("Delete"),
                        reverse: true, width: 75, height: 22, iconSize: 12, textSize: 11,
                        tintColor: new Color(0.8f, 0.5f, 0.3f, 0.25f)))
                    {
                        _selectedScripts.Clear();
                        _scanResult = null;
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                // Script list with remove buttons
                for (int i = _selectedScripts.Count - 1; i >= 0; i--)
                {
                    var script = _selectedScripts[i];
                    if (script == null)
                    {
                        _selectedScripts.RemoveAt(i);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    // Script icon
                    var scriptIcon = EditorGUIUtility.ObjectContent(script, typeof(MonoScript)).image;
                    GUILayout.Label(scriptIcon, GUILayout.Width(16), GUILayout.Height(16));

                    // Script name (clickable to ping in project)
                    var nameStyle = new GUIStyle(Styles.LabelBold)
                    {
                        hover = { textColor = new Color(0.5f, 0.8f, 1f) }
                    };
                    if (GUILayout.Button(script.name, nameStyle))
                    {
                        EditorGUIUtility.PingObject(script);
                    }

                    GUILayout.FlexibleSpace();

                    // Remove button
                    if (PholusEditorGUI.DrawButton("×", width: 22, height: 20, textSize: 14,
                        tintColor: new Color(1.0f, 0.4f, 0.4f, 0.25f)))
                    {
                        _selectedScripts.RemoveAt(i);
                        _scanResult = null; // Clear results when selection changes
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // Drag-drop zone
                DrawDragDropZone();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawDragDropZone()
        {
            EditorGUILayout.Space(5);

            var dropRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));

            // Check for drag operation
            var isDragging = DragAndDrop.objectReferences.Length > 0 &&
                             DragAndDrop.objectReferences.Any(o => o is MonoScript);

            var isHovering = dropRect.Contains(Event.current.mousePosition);

            // Draw dark background with subtle border
            var bgColor = isHovering && isDragging
                ? new Color(0.2f, 0.4f, 0.6f, 0.4f)
                : new Color(0.15f, 0.15f, 0.15f, 0.5f);
            EditorGUI.DrawRect(dropRect, bgColor);

            // Draw border
            var borderColor = isHovering && isDragging
                ? new Color(0.3f, 0.6f, 1f, 0.6f)
                : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            // Top
            EditorGUI.DrawRect(new Rect(dropRect.x, dropRect.y, dropRect.width, 1), borderColor);
            // Bottom
            EditorGUI.DrawRect(new Rect(dropRect.x, dropRect.yMax - 1, dropRect.width, 1), borderColor);
            // Left
            EditorGUI.DrawRect(new Rect(dropRect.x, dropRect.y, 1, dropRect.height), borderColor);
            // Right
            EditorGUI.DrawRect(new Rect(dropRect.xMax - 1, dropRect.y, 1, dropRect.height), borderColor);

            // Draw text
            var textStyle = new GUIStyle(Styles.LabelMuted)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            var labelText = isHovering && isDragging ? "Drop to add" : "Drag scripts here...";
            GUI.Label(dropRect, labelText, textStyle);

            // Handle drag events
            if (isHovering)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    if (DragAndDrop.objectReferences.Any(o => o is MonoScript))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is MonoScript script && !_selectedScripts.Contains(script))
                        {
                            _selectedScripts.Add(script);
                        }
                    }

                    _scanResult = null;
                    Event.current.Use();
                }
            }
        }

        private void DrawActions()
        {
            // Scripts mode: no action bar needed (All Results button is inside detail view)
            if (_analysisMode == AnalysisMode.Scripts)
            {
                return;
            }

            PholusEditorGUI.BeginHorizontalBackground();

            if (_analysisMode == AnalysisMode.Folder)
            {
                GUI.enabled = !string.IsNullOrEmpty(_selectedFolderPath) && !_isScanning && !_isAnalyzing;

                if (PholusEditorGUI.DrawButton("Scan Folder", PholusEditorGUI.GetIcon("Refresh"), reverse: true, width: 120, height: 28, iconSize: 14, textSize: 13, tintColor: new Color(0.4f, 0.7f, 1.0f, 0.25f)))
                {
                    ScanFolder();
                }

                GUI.enabled = true;
            }
            else // Project mode
            {
                GUI.enabled = !_isScanning && !_isAnalyzing;

                if (PholusEditorGUI.DrawButton("Scan Project", PholusEditorGUI.GetIcon("Refresh"), reverse: true, width: 120, height: 28, iconSize: 14, textSize: 13, tintColor: new Color(1.0f, 0.6f, 0.2f, 0.25f)))
                {
                    StartProjectScan();
                }

                GUI.enabled = true;
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private void DrawProgressBar()
        {
            PholusEditorGUI.BeginHorizontalBackground(isDynamic: true);

            var elapsed = Time.realtimeSinceStartup - _analysisStartTime;
            string title;
            string detail;

            if (_isScanning && _currentScanProgress != null)
            {
                // Folder scan progress
                title = "Scanning";
                detail = $"{_currentScanProgress.CurrentFile} ({_currentScanProgress.CurrentIndex}/{_currentScanProgress.TotalFiles})";

                // Animated indeterminate progress bar (bounces left-right)
                var rect = GUILayoutUtility.GetRect(60, 12);
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
                var progress = (Mathf.Sin((float)EditorApplication.timeSinceStartup * 3f) + 1f) / 2f;
                var fillWidth = rect.width * 0.4f;
                var fillX = rect.x + (rect.width - fillWidth) * progress;
                EditorGUI.DrawRect(new Rect(fillX, rect.y + 1, fillWidth, rect.height - 2), new Color(0.3f, 0.8f, 0.4f, 1f));
            }
            else
            {
                // Single script analysis or fix generation
                title = _isAnalyzing ? "Analyzing" : "Generating fix";
                detail = GetProgressDetail(elapsed);

                // Animated indeterminate progress bar
                var rect = GUILayoutUtility.GetRect(60, 12);
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
                var progress = (Mathf.Sin((float)EditorApplication.timeSinceStartup * 3f) + 1f) / 2f;
                var fillWidth = rect.width * 0.4f;
                var fillX = rect.x + (rect.width - fillWidth) * progress;
                EditorGUI.DrawRect(new Rect(fillX, rect.y + 1, fillWidth, rect.height - 2), new Color(0.3f, 0.6f, 1f, 1f));
            }

            GUILayout.Label($"{title}: {detail} ({elapsed:F0}s)", Styles.LabelBold);

            GUILayout.FlexibleSpace();

            if (PholusEditorGUI.DrawButton("Cancel", PholusEditorGUI.GetIcon("Stop"), reverse: true, width: 80, height: 24, iconSize: 12, textSize: 12, tintColor: new Color(1.0f, 0.4f, 0.4f, 0.25f)))
            {
                CancelCurrentOperation();
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private string GetProgressDetail(float elapsed)
        {
            if (elapsed < 2f)
                return "Preparing request...";
            if (elapsed < 5f)
                return "Sending to AI...";
            if (elapsed < 15f)
                return "Waiting for response...";
            if (elapsed < 30f)
                return "AI is thinking...";
            if (elapsed < 60f)
                return "Processing complex code...";
            if (elapsed < 90f)
                return "Still working...";
            return "Taking longer than usual...";
        }

        private void CancelCurrentOperation()
        {
            _cts?.Cancel();
            EditorApplication.update -= UpdateAnalysisProgress;
            EditorApplication.update -= UpdateFixProgress;
            _isAnalyzing = false;
            _isGeneratingFix = false;
            _isScanning = false;
            _currentScanProgress = null;
            ShowStatus("Operation cancelled", MessageType.Warning);
            Repaint();
        }

        private void DrawMainContent()
        {
            _mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition);

            // Check for scan results (Scripts, Folder, or Project mode)
            if (_scanResult != null && !_isScanning)
            {
                // If viewing a single script detail from scan results
                if (_currentResult != null && IsValidResult(_currentResult))
                {
                    DrawDetailView();
                }
                else
                {
                    // Show scan results list
                    DrawScanResults();
                }
            }
            // Otherwise show welcome
            else if (!_isScanning)
            {
                DrawWelcome();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailView()
        {
            var fileName = Path.GetFileName(_currentResult.ScriptPath);
            var scriptName = Path.GetFileNameWithoutExtension(fileName);

            PholusEditorGUI.BeginVerticalBackground();

            if (PholusEditorGUI.DrawFoldout(ref _detailViewFoldout, "Analysis Result", PholusEditorGUI.GetIcon("Zoom")))
            {
                // Script name row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label(fileName, Styles.LabelBold);
                GUILayout.Label($"({_currentResult.TotalIssueCount} issues)", Styles.LabelMuted);
                GUILayout.FlexibleSpace();

                // All Results button
                if (PholusEditorGUI.DrawButton("← All Results",
                    reverse: true, width: 100, height: 24, iconSize: 12, textSize: 12,
                    tintColor: new Color(0.4f, 0.6f, 0.9f, 0.3f)))
                {
                    _currentResult = null;
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(5);

                // Clear cache button
                if (PholusEditorGUI.DrawButton("Clear", PholusEditorGUI.GetIcon("Delete"),
                    reverse: true, width: 70, height: 24, iconSize: 12, textSize: 12,
                    tintColor: new Color(0.8f, 0.5f, 0.3f, 0.25f)))
                {
                    ClearScriptCache(_currentResult.ScriptPath);
                    _currentResult = null;
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(5);

                // Export button
                if (PholusEditorGUI.DrawButton("Export", PholusEditorGUI.GetIcon("Save"),
                    reverse: true, width: 75, height: 24, iconSize: 12, textSize: 12,
                    tintColor: new Color(0.3f, 0.6f, 0.8f, 0.25f)))
                {
                    ExportAnalysisAsMarkdown(_currentResult);
                }
                EditorGUILayout.EndHorizontal();

                // Stats row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                // Score with color
                var scoreColor = Styles.GetScoreColor(_currentResult.Score);
                var prevColor = GUI.color;
                GUI.color = scoreColor;
                GUILayout.Label($"Score: {_currentResult.Score}/100", Styles.LabelMuted);
                GUI.color = prevColor;

                // Consensus mode badge
                if (_currentConsensusResult != null)
                {
                    GUILayout.Space(10);
                    var badgeStyle = new GUIStyle(Styles.LabelMuted)
                    {
                        normal = { textColor = new Color(0.5f, 0.7f, 0.9f) }
                    };
                    var analyzerCount = _currentConsensusResult.AnalyzersUsed?.Count ?? 0;
                    GUILayout.Label($"Consensus: {analyzerCount} providers", badgeStyle);
                }

                // Dismissed count
                if (_currentConsensusResult?.Verdict != null && _currentConsensusResult.Verdict.DismissedCount > 0)
                {
                    GUILayout.Space(10);
                    var dismissedStyle = new GUIStyle(Styles.LabelMuted)
                    {
                        normal = { textColor = new Color(0.6f, 0.5f, 0.4f) }
                    };
                    GUILayout.Label($"{_currentConsensusResult.Verdict.DismissedCount} dismissed", dismissedStyle);
                }

                // Timestamp
                if (_currentResult.AnalyzedAt != default)
                {
                    GUILayout.Space(10);
                    var timeStyle = new GUIStyle(Styles.LabelMuted) { fontStyle = FontStyle.Italic };
                    GUILayout.Label(GetRelativeTime(_currentResult.AnalyzedAt), timeStyle);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Issue list
                _issueListView.Draw(_currentResult);
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private bool IsValidResult(AnalysisResult result)
        {
            // Result is valid if it has a script path (was actually analyzed)
            return result != null && !string.IsNullOrEmpty(result.ScriptPath);
        }

        private void DrawWelcome()
        {
            PholusEditorGUI.BeginVerticalBackground();

            PholusEditorGUI.DrawHeader("Welcome to Pholus", PholusEditorGUI.GetIcon("Zoom"));

            EditorGUILayout.Space(5);

            // Subtitle with color
            var prevColor = GUI.color;
            GUI.color = new Color(0.6f, 0.8f, 1f);
            GUILayout.Label("A trusted code review companion, focused on guidance.", Styles.LabelBold);
            GUI.color = prevColor;

            EditorGUILayout.Space(15);

            // Getting started steps with colored numbers
            GUILayout.Label("To get started:", Styles.LabelBold);
            EditorGUILayout.Space(5);

            DrawStepItem(1, "Select scripts using '+ Add' or drag & drop", Styles.LowColor);
            DrawStepItem(2, "Click 'Analyze' to check for performance issues", Styles.MediumColor);
            DrawStepItem(3, "Review issues and click 'Fix' for one-click fixes", Styles.SuccessColor);

            EditorGUILayout.Space(15);

            // Status warnings (use cached values to avoid blocking main thread)
            if (_isRefreshingDetectorStatus)
            {
                PholusEditorGUI.DrawInfoBox("Checking CLI status...", PholusEditorGUI.InfoBoxType.Default);
            }
            else if (!_cachedIsInstalled)
            {
                PholusEditorGUI.DrawInfoBox("CLI not detected. Open Settings to install.", PholusEditorGUI.InfoBoxType.Warning);
            }
            else if (!_cachedIsAuthenticated)
            {
                PholusEditorGUI.DrawInfoBox("Not authenticated. Open Settings to login.", PholusEditorGUI.InfoBoxType.Warning);
            }
            else
            {
                PholusEditorGUI.DrawInfoBox("Ready to analyze! Select scripts and click Analyze.");
            }

            // Support section
            EditorGUILayout.Space(15);
            DrawSupportSection();

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawSupportSection()
        {
            var supportStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            var emailStyle = new GUIStyle(supportStyle)
            {
                normal = { textColor = new Color(0.4f, 0.65f, 0.9f) }
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Questions or issues? Reach out:", supportStyle);
            if (GUILayout.Button("codeturion@gmail.com", emailStyle, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("mailto:codeturion@gmail.com?subject=Pholus%20Support");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStepItem(int number, string text, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            // Colored number badge
            var prevColor = GUI.color;
            GUI.color = color;
            GUILayout.Label($"  {number}.", Styles.LabelBold, GUILayout.Width(25));
            GUI.color = prevColor;

            GUILayout.Label(text, Styles.RichTextLabel);

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawScanResults()
        {
            if (_scanResult == null || _scanResult.Results == null) return;

            PholusEditorGUI.BeginVerticalBackground();

            // Build foldout title with stats
            var headerTitle = _analysisMode switch
            {
                AnalysisMode.Project => $"Project Scan Results ({_scanResult.TotalIssues} issues)",
                AnalysisMode.Scripts => $"Analysis Results ({_scanResult.TotalIssues} issues)",
                _ => $"Folder Scan Results ({_scanResult.TotalIssues} issues)"
            };

            if (PholusEditorGUI.DrawFoldout(ref _resultsFoldout, headerTitle, PholusEditorGUI.GetIcon("Zoom")))
            {
                // Summary stats row
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                // Timestamp
                if (_scanResult.ScannedAt != default)
                {
                    var timeStyle = new GUIStyle(Styles.LabelMuted)
                    {
                        fontStyle = FontStyle.Italic
                    };
                    GUILayout.Label(GetRelativeTime(_scanResult.ScannedAt), timeStyle);
                    GUILayout.Space(10);
                }

                // Average score with color
                var scoreColor = Styles.GetScoreColor(_scanResult.AverageScore);
                var prevColor = GUI.color;
                GUI.color = scoreColor;
                GUILayout.Label($"Avg: {_scanResult.AverageScore}/100", Styles.LabelBold);
                GUI.color = prevColor;

                GUILayout.Space(10);
                GUILayout.Label($"{_scanResult.ScriptsWithIssues} files with issues", Styles.LabelMuted);

                GUILayout.FlexibleSpace();

                // Clear button
                if (PholusEditorGUI.DrawButton("Clear", PholusEditorGUI.GetIcon("Delete"),
                    reverse: true, width: 55, height: 20, iconSize: 10, textSize: 10,
                    tintColor: new Color(0.8f, 0.5f, 0.3f, 0.25f)))
                {
                    if (_analysisMode == AnalysisMode.Project)
                    {
                        ClearProjectCache();
                    }
                    else if (_analysisMode == AnalysisMode.Folder && !string.IsNullOrEmpty(_selectedFolderPath))
                    {
                        ClearFolderCache(_selectedFolderPath);
                    }
                    _scanResult = null;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Scripts with issues - expandable list
                var scriptsWithIssues = _scanResult.Results
                    .Where(r => r.HasIssues)
                    .OrderBy(r => r.Score)
                    .ToList();

                if (scriptsWithIssues.Count > 0)
                {
                    foreach (var result in scriptsWithIssues)
                    {
                        DrawScanResultItem(result);
                    }
                }

                // Clean scripts section
                var cleanScripts = _scanResult.Results.Where(r => !r.HasIssues).ToList();
                if (cleanScripts.Count > 0)
                {
                    EditorGUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    var cleanArrow = _cleanScriptsFoldout ? "\u25BC" : "\u25B6";
                    if (GUILayout.Button(cleanArrow, EditorStyles.miniLabel, GUILayout.Width(15)))
                    {
                        _cleanScriptsFoldout = !_cleanScriptsFoldout;
                    }
                    var checkIcon = GUI.color;
                    GUI.color = Styles.SuccessColor;
                    GUILayout.Label("\u2713", GUILayout.Width(15));
                    GUI.color = checkIcon;
                    if (GUILayout.Button($"Clean Scripts ({cleanScripts.Count})", Styles.LabelMuted))
                    {
                        _cleanScriptsFoldout = !_cleanScriptsFoldout;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    if (_cleanScriptsFoldout)
                    {
                        foreach (var result in cleanScripts)
                        {
                            var fileName = Path.GetFileName(result.ScriptPath);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            var prevClr = GUI.color;
                            GUI.color = Styles.SuccessColor;
                            GUILayout.Label("\u2713", GUILayout.Width(15));
                            GUI.color = prevClr;
                            GUILayout.Label(fileName, Styles.LabelMuted);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }

            // Support section
            EditorGUILayout.Space(15);
            DrawSupportSection();

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawScanResultItem(AnalysisResult result)
        {
            var fileName = Path.GetFileName(result.ScriptPath);
            var key = result.ScriptPath;

            if (!_resultFoldouts.ContainsKey(key))
                _resultFoldouts[key] = false;

            PholusEditorGUI.BeginVerticalBackground();

            EditorGUILayout.BeginHorizontal();

            // Foldout arrow button
            var arrow = _resultFoldouts[key] ? "\u25BC" : "\u25B6";
            if (GUILayout.Button(arrow, EditorStyles.miniLabel, GUILayout.Width(15)))
            {
                _resultFoldouts[key] = !_resultFoldouts[key];
            }

            // File name (clickable to toggle)
            if (GUILayout.Button(fileName, Styles.LabelBold))
            {
                _resultFoldouts[key] = !_resultFoldouts[key];
            }

            GUILayout.FlexibleSpace();

            // Score with color
            var scoreColor = Styles.GetScoreColor(result.Score);
            var prevColor = GUI.color;
            GUI.color = scoreColor;
            GUILayout.Label($"{result.Score}/100", Styles.LabelBold, GUILayout.Width(50));
            GUI.color = prevColor;

            // Issue count
            var issueText = result.TotalIssueCount == 1 ? "1 issue" : $"{result.TotalIssueCount} issues";
            GUILayout.Label(issueText, Styles.LabelMuted, GUILayout.Width(60));

            // Open button - shows detail view for this script
            if (PholusEditorGUI.DrawButton("Open", PholusEditorGUI.GetIcon("Forward"),
                reverse: true, width: 60, height: 20, iconSize: 10, textSize: 11,
                tintColor: new Color(0.3f, 0.7f, 1.0f, 0.25f)))
            {
                _currentResult = result;
                // Keep _scanResult so user can go back!

                // Load consensus result if available (for showing provider opinions)
                var consensusCache = GetConsensusCache();
                if (result.ScriptPath != null && consensusCache.TryGetValue(result.ScriptPath, out var consensusResult))
                {
                    _currentConsensusResult = consensusResult;
                    _issueListView.SetConsensusResult(consensusResult, PholusSettings.Instance.ShowProviderBreakdown);
                }
                else
                {
                    _currentConsensusResult = null;
                    _issueListView.ClearConsensusResult();
                }

                // Exit GUI to prevent layout errors from state change
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            // Expanded content - show issue titles
            if (_resultFoldouts[key])
            {
                EditorGUILayout.Space(3);

                var issuesToShow = result.AllIssues.Take(5).ToList();
                foreach (var issue in issuesToShow)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    // Severity indicator based on confidence
                    var severityColor = issue.Confidence switch
                    {
                        IssueConfidence.High => Styles.HighColor,
                        IssueConfidence.Medium => Styles.MediumColor,
                        _ => Styles.LowColor
                    };
                    var prevSevColor = GUI.color;
                    GUI.color = severityColor;
                    GUILayout.Label("\u2022", GUILayout.Width(10));
                    GUI.color = prevSevColor;

                    GUILayout.Label(issue.Title, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                if (result.TotalIssueCount > 5)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    GUILayout.Label($"... and {result.TotalIssueCount - 5} more", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }

            PholusEditorGUI.EndVerticalBackground();
            EditorGUILayout.Space(2);
        }

        private void DrawFooter()
        {
            // Only show footer if there's something to undo
            if (!(_fixService?.CanUndo ?? false))
                return;

            PholusEditorGUI.BeginHorizontalBackground();

            GUILayout.FlexibleSpace();

            // Undo button
            if (PholusEditorGUI.DrawButton("Undo Last Fix", PholusEditorGUI.GetIcon("Refresh"), reverse: true, width: 120, height: 28, iconSize: 14, textSize: 13, tintColor: new Color(1.0f, 0.7f, 0.3f, 0.25f)))
            {
                if (_fixService.UndoLastFix())
                {
                    ShowStatus("Fix undone", MessageType.Info);
                    Repaint();
                }
            }

            PholusEditorGUI.EndHorizontalBackground();
        }

        private void DrawStatusBar()
        {
            // Don't show status bar during analysis/fix - we have the progress bar
            if (_isAnalyzing || _isGeneratingFix)
            {
                return;
            }

            if (string.IsNullOrEmpty(_statusMessage))
            {
                return;
            }

            // Auto-hide after 5 seconds
            if (Time.realtimeSinceStartup - _statusTime > 5f)
            {
                _statusMessage = null;
                Repaint();
                return;
            }

            var infoType = _statusType switch
            {
                MessageType.Error => PholusEditorGUI.InfoBoxType.Error,
                MessageType.Warning => PholusEditorGUI.InfoBoxType.Warning,
                _ => PholusEditorGUI.InfoBoxType.Default
            };
            PholusEditorGUI.DrawInfoBox(_statusMessage, infoType);
        }

        private void DrawDiffPreview()
        {
            _diffPreviewView.Draw();
        }

        private void DrawSettings()
        {
            // Header with back button
            PholusEditorGUI.BeginHorizontalBackground();
            PholusEditorGUI.DrawHeader("Settings", PholusEditorGUI.GetIcon("Settings"));
            GUILayout.FlexibleSpace();
            if (PholusEditorGUI.DrawButton("Back", PholusEditorGUI.GetIcon("ArrowMinimize"), reverse: true, width: 80, height: 26, iconSize: 14, textSize: 12))
            {
                _showSettings = false;
            }
            PholusEditorGUI.EndHorizontalBackground();

            // Begin scroll view for settings content
            _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);

            var settings = PholusSettings.Instance;

            // Provider Options
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();
            if (PholusEditorGUI.DrawFoldout(ref _providerOptionsFoldout, "AI Provider", PholusEditorGUI.GetIcon("Template")))
            {
                PholusEditorGUI.BeginContainer();

                var oldProvider = settings.ActiveProvider;
                settings.ActiveProvider = PholusEditorGUI.DrawEnumDropdown("Provider", settings.ActiveProvider);

                // Provider status badge
                EditorGUILayout.Space(2);
                var (badgeText, badgeColor) = GetProviderBadge(settings.ActiveProvider);
                if (!string.IsNullOrEmpty(badgeText))
                {
                    var noteStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = badgeColor },
                        fontStyle = FontStyle.Italic
                    };
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Label(badgeText, noteStyle);
                    EditorGUILayout.EndHorizontal();
                }

                // Reinitialize services if provider changed
                if (oldProvider != settings.ActiveProvider)
                {
                    // Reset refresh state to allow new refresh for new provider
                    _isRefreshingDetectorStatus = false;
                    _cachedStatusMessage = "Checking...";
                    _cachedIsInstalled = false;
                    _cachedIsAuthenticated = false;
                    InitializeServices();
                }

                // Model selection inline
                EditorGUILayout.Space(5);
                DrawCompactModelSelector(settings, settings.ActiveProvider);

                // Show status (use cached values to avoid blocking main thread)
                EditorGUILayout.Space(5);
                var statusColor = _cachedIsAuthenticated ? Styles.SuccessColor : Styles.MediumColor;
                var statusText = _cachedStatusMessage ?? "Not initialized";

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Status:", GUILayout.Width(100));
                var prevColor = GUI.color;
                GUI.color = _isRefreshingDetectorStatus ? Color.gray : statusColor;
                GUILayout.Label(statusText);
                GUI.color = prevColor;

                // Refresh status button
                GUILayout.FlexibleSpace();
                GUI.enabled = !_isRefreshingDetectorStatus;
                if (GUILayout.Button(PholusEditorGUI.GetIcon("Refresh"), GUILayout.Width(22), GUILayout.Height(18)))
                {
                    _isRefreshingDetectorStatus = false;
                    _cachedStatusMessage = "Checking...";
                    RefreshDetectorStatusAsync();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                // Show Login/Relogin button (use cached IsInstalled)
                if (_cachedIsInstalled)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(100); // Align with status label

                    if (_cachedIsAuthenticated)
                    {
                        // Relogin - subtle styling since already logged in
                        if (PholusEditorGUI.DrawButton("Relogin", PholusEditorGUI.GetIcon("Refresh"),
                            reverse: true, width: 85, height: 24, iconSize: 12, textSize: 11,
                            tintColor: new Color(0.6f, 0.5f, 0.8f, 0.25f)))
                        {
                            var installer = PholusServices.Get<IProviderFactory>().CreateInstaller(settings.ActiveProvider);
                            installer.OpenLoginTerminal();
                        }
                    }
                    else
                    {
                        // Login - prominent styling to encourage action
                        if (PholusEditorGUI.DrawButton("Open Terminal & Login", PholusEditorGUI.GetIcon("Forward"),
                            reverse: true, width: 165, height: 26, iconSize: 14, textSize: 12,
                            tintColor: new Color(0.3f, 0.8f, 0.5f, 0.3f)))
                        {
                            var installer = PholusServices.Get<IProviderFactory>().CreateInstaller(settings.ActiveProvider);
                            installer.OpenLoginTerminal();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (!_cachedIsAuthenticated)
                    {
                        EditorGUILayout.Space(3);
                        var hint = settings.ActiveProvider == ProviderType.Claude
                            ? "Run 'claude /login' in the terminal to authenticate."
                            : "Run 'codex' in the terminal to authenticate.";
                        EditorGUILayout.HelpBox(hint, MessageType.Info);
                    }
                }

                PholusEditorGUI.EndContainer();
            }
            PholusEditorGUI.EndVerticalBackground();

            // Analysis Options (foldout pattern)
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();
            if (PholusEditorGUI.DrawFoldout(ref _analysisOptionsFoldout, "Analysis Options", PholusEditorGUI.GetIcon("Zoom")))
            {
                PholusEditorGUI.BeginContainer();
                settings.TargetPlatform = PholusEditorGUI.DrawEnumDropdown("Target Platform", settings.TargetPlatform);
                settings.ShowWhyMightBeWrong = PholusEditorGUI.DrawToggle(settings.ShowWhyMightBeWrong, "Show 'Why I Might Be Wrong'");
                settings.GroupByCertainty = PholusEditorGUI.DrawToggle(settings.GroupByCertainty, "Group by Certainty");

                PholusEditorGUI.EndContainer();
            }
            PholusEditorGUI.EndVerticalBackground();

            // Consensus Mode (foldout pattern)
            DrawConsensusModeSection(settings);

            // Fix Options (foldout pattern)
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();
            if (PholusEditorGUI.DrawFoldout(ref _fixOptionsFoldout, "Fix Options", PholusEditorGUI.GetIcon("Edit")))
            {
                PholusEditorGUI.BeginContainer();
                settings.CreateBackupBeforeFix = PholusEditorGUI.DrawToggle(settings.CreateBackupBeforeFix, "Create Backup Before Fix");
                settings.MaxBackupsToKeep = (int)PholusEditorGUI.DrawSlider("Max Backups", settings.MaxBackupsToKeep, 1, 50);
                PholusEditorGUI.EndContainer();
            }
            PholusEditorGUI.EndVerticalBackground();

            // Preferences (foldout pattern)
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();
            if (PholusEditorGUI.DrawFoldout(ref _preferencesFoldout, "Preferences", PholusEditorGUI.GetIcon("Paste")))
            {
                PholusEditorGUI.BeginContainer();
                settings.AutoApplyFixes = PholusEditorGUI.DrawToggle(settings.AutoApplyFixes, "Auto-Apply Fixes (Skip Diff Preview)");

                if (settings.AutoApplyFixes)
                {
                    EditorGUILayout.Space(3);
                    var noteStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(1f, 0.7f, 0.3f) },
                        fontStyle = FontStyle.Italic,
                        wordWrap = true
                    };
                    EditorGUILayout.LabelField("Fixes will be applied automatically. Use Undo if needed.", noteStyle);
                }

                PholusEditorGUI.EndContainer();
            }
            PholusEditorGUI.EndVerticalBackground();

            // Logging (foldout pattern)
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();
            if (PholusEditorGUI.DrawFoldout(ref _loggingFoldout, "Logging", PholusEditorGUI.GetIcon("CodeGen")))
            {
                PholusEditorGUI.BeginContainer();
                settings.LogDebugMessages = PholusEditorGUI.DrawToggle(settings.LogDebugMessages, "Log Debug Messages", tooltip: "Show informational log messages in the Unity console");
                settings.LogWarningMessages = PholusEditorGUI.DrawToggle(settings.LogWarningMessages, "Log Warnings", tooltip: "Show warning messages in the Unity console");
                settings.LogErrorMessages = PholusEditorGUI.DrawToggle(settings.LogErrorMessages, "Log Errors", tooltip: "Show error messages in the Unity console");
                PholusEditorGUI.EndContainer();
            }
            PholusEditorGUI.EndVerticalBackground();

            // Pro Features (teaser section)
            DrawProFeaturesSection();

            // Contact Us section (foldout)
            DrawContactUsSection();

            // Reset and Clear Cache buttons
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginHorizontalBackground();
            GUILayout.FlexibleSpace();

            // Clear cache button
            var cacheCount = _scriptResultCache.Count + _folderScanCache.Count + (_projectScanCache != null ? 1 : 0);
            var cacheLabel = cacheCount > 0 ? $"Clear Cache ({cacheCount})" : "Clear Cache";
            if (PholusEditorGUI.DrawButton(cacheLabel, PholusEditorGUI.GetIcon("Delete"), reverse: true, width: 140, height: 30, iconSize: 16, textSize: 14, tintColor: new Color(0.8f, 0.6f, 0.2f, 0.25f)))
            {
                ClearAllCache();
            }

            GUILayout.Space(10);

            // Clear model cache button
            if (PholusEditorGUI.DrawButton("Clear Models", PholusEditorGUI.GetIcon("Delete"), reverse: true, width: 130, height: 30, iconSize: 16, textSize: 14, tintColor: new Color(0.6f, 0.4f, 0.8f, 0.25f)))
            {
                settings.ClearModelCache();
            }

            GUILayout.Space(10);

            if (PholusEditorGUI.DrawButton("Reset to Defaults", PholusEditorGUI.GetIcon("Refresh"), reverse: true, width: 160, height: 30, iconSize: 16, textSize: 14, tintColor: new Color(1.0f, 0.4f, 0.4f, 0.25f)))
            {
                settings.ResetToDefaults();
            }
            PholusEditorGUI.EndHorizontalBackground();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the Contact Us section.
        /// </summary>
        private void DrawContactUsSection()
        {
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            if (PholusEditorGUI.DrawFoldout(ref _contactUsFoldout, "Contact Us", PholusEditorGUI.GetIcon("Unlink")))
            {
                PholusEditorGUI.BeginContainer();

                var contactStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };

                var linkStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.5f, 0.7f, 1f) }
                };

                // Email
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Email:", contactStyle, GUILayout.Width(55));
                if (GUILayout.Button("codeturion@gmail.com", linkStyle, GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL("mailto:codeturion@gmail.com");
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Discord
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Discord:", contactStyle, GUILayout.Width(55));
                GUILayout.Label("codeturion", linkStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        private void DrawConsensusModeSection(PholusSettings settings)
        {
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            if (PholusEditorGUI.DrawFoldout(ref _consensusModeFoldout, "Consensus Mode", PholusEditorGUI.GetIcon("Sequence")))
            {
                PholusEditorGUI.BeginContainer();

                // Enable toggle
                settings.EnableConsensusMode = PholusEditorGUI.DrawToggle(settings.EnableConsensusMode, "Enable Multi-Provider Consensus");

                if (settings.EnableConsensusMode)
                {
                    EditorGUILayout.Space(5);

                    // Get provider factory to check status
                    var factory = PholusServices.Get<IProviderFactory>();

                    // Analyzer Providers section
                    EditorGUILayout.LabelField("Analyzer Providers:", EditorStyles.boldLabel);

                    var analyzers = settings.ConsensusAnalyzers;
                    var modifiedAnalyzers = new List<ProviderType>(analyzers);

                    foreach (ProviderType providerType in Enum.GetValues(typeof(ProviderType)))
                    {
                        EditorGUILayout.BeginHorizontal();

                        var isSelected = analyzers.Contains(providerType);
                        var detector = factory?.CreateDetector(providerType);
                        var isInstalled = detector?.IsInstalled() ?? false;
                        var isAuthenticated = detector?.IsAuthenticated() ?? false;

                        // Provider checkbox
                        var newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                        if (newSelected != isSelected)
                        {
                            if (newSelected)
                                modifiedAnalyzers.Add(providerType);
                            else
                                modifiedAnalyzers.Remove(providerType);
                        }

                        // Provider name with color (wider to fit "Google Gemini")
                        var providerColor = GetProviderColor(providerType);
                        var prevColor = GUI.color;
                        GUI.color = providerColor;
                        EditorGUILayout.LabelField(GetProviderDisplayName(providerType), GUILayout.Width(95));
                        GUI.color = prevColor;

                        // Inline model dropdown
                        GUI.enabled = isAuthenticated;
                        DrawInlineModelDropdown(settings, providerType, 200f);
                        GUI.enabled = true;

                        // Status indicator
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel);
                        if (isAuthenticated)
                        {
                            statusStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
                            EditorGUILayout.LabelField("✓", statusStyle, GUILayout.Width(15));
                        }
                        else if (isInstalled)
                        {
                            statusStyle.normal.textColor = new Color(0.9f, 0.7f, 0.2f);
                            EditorGUILayout.LabelField("⚠", statusStyle, GUILayout.Width(15));
                        }
                        else
                        {
                            statusStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                            EditorGUILayout.LabelField("✗", statusStyle, GUILayout.Width(15));
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    // Update analyzers if changed
                    if (!analyzers.SequenceEqual(modifiedAnalyzers))
                    {
                        settings.ConsensusAnalyzers = modifiedAnalyzers;
                    }

                    EditorGUILayout.Space(8);

                    // Director dropdown
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Director:", GUILayout.Width(60));
                    settings.ConsensusDirector = (ProviderType)EditorGUILayout.EnumPopup(settings.ConsensusDirector, GUILayout.Width(80));

                    // Director model dropdown
                    DrawInlineModelDropdown(settings, settings.ConsensusDirector, 100);

                    // Recommended label for Claude
                    if (settings.ConsensusDirector == ProviderType.Claude)
                    {
                        var recStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.4f, 0.8f, 1f) }
                        };
                        EditorGUILayout.LabelField("★ Recommended", recStyle, GUILayout.Width(90));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // Show provider breakdown toggle
                    settings.ShowProviderBreakdown = PholusEditorGUI.DrawToggle(settings.ShowProviderBreakdown, "Show Provider Breakdown in Results");

                    // Validation message
                    EditorGUILayout.Space(5);
                    var validAnalyzerCount = modifiedAnalyzers.Count(a =>
                    {
                        var det = factory?.CreateDetector(a);
                        return det?.IsAuthenticated() ?? false;
                    });

                    if (validAnalyzerCount < 2)
                    {
                        EditorGUILayout.HelpBox(
                            "Requires 2+ authenticated providers. Select more providers or authenticate existing ones.",
                            MessageType.Warning);
                    }
                    else
                    {
                        var infoStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.5f, 0.7f, 0.9f) },
                            wordWrap = true
                        };
                        EditorGUILayout.LabelField(
                            $"✓ {validAnalyzerCount} analyzers ready. Director will review combined results.",
                            infoStyle);
                    }
                }
                else
                {
                    var disabledStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                        fontStyle = FontStyle.Italic
                    };
                    EditorGUILayout.LabelField("Run analysis with multiple providers for higher accuracy", disabledStyle);
                }

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        /// <summary>
        /// Draws the Pro Features teaser section with locked features.
        /// </summary>
        private void DrawProFeaturesSection()
        {
            PholusEditorGUI.DrawFoldoutSpace();
            PholusEditorGUI.BeginVerticalBackground();

            if (PholusEditorGUI.DrawFoldout(ref _proFeaturesFoldout, "Pro Features", PholusEditorGUI.GetIcon("Height")))
            {
                PholusEditorGUI.BeginContainer();

                // Pro badge style
                var proBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold,
                    fontSize = 9
                };

                var lockedStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };

                var descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.45f, 0.45f, 0.45f) },
                    wordWrap = true,
                    fontStyle = FontStyle.Italic
                };

                // Feature Mode
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(false, GUILayout.Width(15));
                EditorGUI.EndDisabledGroup();
                GUILayout.Label("Feature Mode", lockedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("PRO", proBadgeStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Feature development and code generation for Unity projects", descStyle);

                EditorGUILayout.Space(8);

                // TONL Token Savings Mode
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(false, GUILayout.Width(15));
                EditorGUI.EndDisabledGroup();
                GUILayout.Label("TONL Token Savings Mode", lockedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("PRO", proBadgeStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Intelligent caching and prompt optimization for reduced API costs", descStyle);

                EditorGUILayout.Space(8);

                // Architect Mode
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(false, GUILayout.Width(15));
                EditorGUI.EndDisabledGroup();
                GUILayout.Label("Architect Mode", lockedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("PRO", proBadgeStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Deep architectural analysis with dependency graphs and refactoring suggestions", descStyle);

                EditorGUILayout.Space(8);

                // Roslyn Syntax Validation
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(false, GUILayout.Width(15));
                EditorGUI.EndDisabledGroup();
                GUILayout.Label("Roslyn Syntax Validation", lockedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("PRO", proBadgeStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Full C# syntax validation before applying fixes using the Roslyn compiler", descStyle);

                EditorGUILayout.Space(10);

                // Coming soon note
                var comingSoonStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
                GUILayout.Label("Coming soon in Pholus Pro", comingSoonStyle);

                PholusEditorGUI.EndContainer();
            }

            PholusEditorGUI.EndVerticalBackground();
        }

        /// <summary>
        /// Draws a compact model selector with dropdown, Refresh, Add, and Remove buttons.
        /// Used in the main AI Provider section.
        /// </summary>
        private void DrawCompactModelSelector(PholusSettings settings, ProviderType provider)
        {
            var models = settings.GetAvailableModels(provider);
            var selectedModel = settings.GetSelectedModel(provider);
            var hasDiscovered = settings.HasDiscoveredModels(provider);

            // Convert to display names (empty = "(CLI Default)")
            var displayNames = models.Select(m => string.IsNullOrEmpty(m) ? "(CLI Default)" : m).ToArray();
            var selectedIndex = models.IndexOf(selectedModel);
            if (selectedIndex < 0) selectedIndex = 0;

            // Model dropdown row - use same styling as DrawEnumDropdown
            PholusEditorGUI.BeginHorizontalBackground(isDynamic: true);

            // Label aligned with other settings (100px width)
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };
            GUILayout.Label("Model", labelStyle, GUILayout.Width(100));

            var newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.MinWidth(120));
            if (newIndex != selectedIndex && newIndex >= 0 && newIndex < models.Count)
            {
                settings.SetSelectedModel(provider, models[newIndex]);
            }

            GUILayout.Space(5);

            // Action buttons inline
            GUI.enabled = !_isDiscoveringModels;

            // Refresh button
            var isThisProviderLoading = _isDiscoveringModels && _discoveringProvider == provider;
            var refreshLabel = isThisProviderLoading ? "..." : "Refresh";
            if (PholusEditorGUI.DrawButton(refreshLabel, PholusEditorGUI.GetIcon("Refresh"),
                reverse: true, width: 70, height: 22, iconSize: 12, textSize: 11,
                tintColor: new Color(0.3f, 0.7f, 1.0f, 0.25f)))
            {
                if (!_isDiscoveringModels)
                {
                    DiscoverModelsForProvider(provider);
                }
            }

            // Add custom model button
            if (PholusEditorGUI.DrawButton("Add", PholusEditorGUI.GetIcon("Plus"),
                reverse: true, width: 55, height: 22, iconSize: 12, textSize: 11,
                tintColor: new Color(0.3f, 0.8f, 0.4f, 0.25f)))
            {
                _addModelForProvider = provider;
                _showAddModelPopup = true;
                _newModelInput = "";
            }

            // Remove button (only for custom models)
            var canRemove = !string.IsNullOrEmpty(selectedModel) && settings.IsCustomModel(provider, selectedModel);
            GUI.enabled = canRemove && !_isDiscoveringModels;
            if (PholusEditorGUI.DrawButton("Remove", PholusEditorGUI.GetIcon("Delete"),
                reverse: true, width: 70, height: 22, iconSize: 12, textSize: 11,
                tintColor: new Color(1.0f, 0.4f, 0.4f, 0.25f)))
            {
                settings.RemoveCustomModel(provider, selectedModel);
            }
            GUI.enabled = true;

            // Show hint if no models discovered yet
            if (!hasDiscovered)
            {
                GUILayout.Space(5);
                var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                GUILayout.Label("(click Refresh)", hintStyle);
            }

            GUILayout.FlexibleSpace();
            PholusEditorGUI.EndHorizontalBackground();

            // Show add model popup if active
            if (_showAddModelPopup && _addModelForProvider == provider)
            {
                DrawAddModelPopup(settings, provider);
            }
        }

        /// <summary>
        /// Draws a compact inline model dropdown (no buttons).
        /// Used in consensus mode provider list.
        /// </summary>
        private void DrawInlineModelDropdown(PholusSettings settings, ProviderType provider, float width)
        {
            var models = settings.GetAvailableModels(provider);
            var selectedModel = settings.GetSelectedModel(provider);

            // Convert to display names (empty = "(Default)")
            var displayNames = models.Select(m => string.IsNullOrEmpty(m) ? "(Default)" : m).ToArray();
            var selectedIndex = models.IndexOf(selectedModel);
            if (selectedIndex < 0) selectedIndex = 0;

            var newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.Width(width));
            if (newIndex != selectedIndex && newIndex >= 0 && newIndex < models.Count)
            {
                settings.SetSelectedModel(provider, models[newIndex]);
            }
        }

        private void DrawAddModelPopup(PholusSettings settings, ProviderType provider)
        {
            EditorGUILayout.Space(5);
            PholusEditorGUI.BeginVerticalBackground(isContainerItem: true);

            EditorGUILayout.LabelField("Add Custom Model", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model Name:", GUILayout.Width(80));
            _newModelInput = EditorGUILayout.TextField(_newModelInput);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (PholusEditorGUI.DrawButton("Cancel", PholusEditorGUI.GetIcon("Stop"),
                reverse: true, width: 70, height: 24, iconSize: 12, textSize: 11,
                tintColor: new Color(0.6f, 0.6f, 0.6f, 0.25f)))
            {
                _showAddModelPopup = false;
                _newModelInput = "";
            }

            GUILayout.Space(5);

            GUI.enabled = !string.IsNullOrWhiteSpace(_newModelInput);
            if (PholusEditorGUI.DrawButton("Add Model", PholusEditorGUI.GetIcon("Plus"),
                reverse: true, width: 85, height: 24, iconSize: 12, textSize: 11,
                tintColor: new Color(0.3f, 0.8f, 0.4f, 0.25f)))
            {
                var trimmedModel = _newModelInput.Trim();
                settings.AddCustomModel(provider, trimmedModel);
                settings.SetSelectedModel(provider, trimmedModel);
                _showAddModelPopup = false;
                _newModelInput = "";
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            PholusEditorGUI.EndVerticalBackground();
        }

        private async void DiscoverModelsForProvider(ProviderType provider)
        {
            if (_isDiscoveringModels) return;

            _isDiscoveringModels = true;
            _discoveringProvider = provider;
            ShowStatus($"Discovering {GetProviderDisplayName(provider)} models...", MessageType.Info);
            Repaint();

            try
            {
                var models = await ModelDiscoveryService.DiscoverModelsAsync(provider);

                if (models.Count > 0)
                {
                    PholusSettings.Instance.SetDiscoveredModels(provider, models);
                    ShowStatus($"Found {models.Count} models for {GetProviderDisplayName(provider)}", MessageType.Info);
                }
                else
                {
                    ShowStatus($"Could not discover models. Using CLI default.", MessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Model discovery failed: {ex.Message}");
                ShowStatus($"Model discovery failed: {ex.Message}", MessageType.Error);
            }
            finally
            {
                _isDiscoveringModels = false;
                _discoveringProvider = null;
                Repaint();
            }
        }

        private static string GetProviderDisplayName(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => "Claude Code",
                ProviderType.Codex => "OpenAI Codex",
                ProviderType.Gemini => "Google Gemini",
                ProviderType.Cursor => "Cursor",
                ProviderType.OpenRouter => "OpenRouter",
                _ => type.ToString()
            };
        }

        private static Color GetProviderColor(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => new Color(0.4f, 0.6f, 1f),             // Blue
                ProviderType.Codex => new Color(0.4f, 0.8f, 0.4f),            // Green
                ProviderType.Gemini => new Color(1f, 0.7f, 0.3f),             // Orange
                ProviderType.Cursor => new Color(0.9f, 0.4f, 0.9f),           // Purple
                ProviderType.OpenRouter => new Color(1.0f, 0.1f, 0.1f, 0.65f), // Red,
                _ => Color.white
            };
        }

        /// <summary>
        /// Gets the badge text and color for a provider (Recommended, Beta, Alpha).
        /// Reusable across Settings and Setup Wizard.
        /// </summary>
        private static (string text, Color color) GetProviderBadge(ProviderType type)
        {
            return type switch
            {
                ProviderType.Claude => ("★ Recommended - Best precision", new Color(0.4f, 0.8f, 1f)),
                ProviderType.Cursor => ("β Beta - Experimental", new Color(1f, 0.8f, 0.3f)),
                ProviderType.OpenRouter => ("α Alpha - Early testing", new Color(1f, 0.5f, 0.3f)),
                _ => ("", Color.gray)
            };
        }

        private void AnalyzeScripts()
        {
            if (_selectedScripts.Count == 0) return;

            // Get full paths for all selected scripts
            var scriptPaths = _selectedScripts
                .Where(s => s != null)
                .Select(s => Path.GetFullPath(AssetDatabase.GetAssetPath(s)))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            if (scriptPaths.Count == 0) return;

            // Clear detail view - we're starting fresh
            _currentResult = null;

            // Reuse the parallel scan pattern
            ScanProjectParallel(scriptPaths);
        }

        private void UpdateAnalysisProgress()
        {
            if (!_isAnalyzing && !_isScanning) return;
            Repaint(); // Refresh the animated progress bar and elapsed time
        }

        private async void ScanFolder()
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                ShowStatus("No folder selected", MessageType.Warning);
                return;
            }

            // Convert Assets-relative path to full path
            string fullPath;
            if (_selectedFolderPath.StartsWith("Assets"))
            {
                fullPath = Path.Combine(Application.dataPath, _selectedFolderPath.Substring(7)); // Remove "Assets/"
            }
            else
            {
                fullPath = _selectedFolderPath;
            }

            if (!Directory.Exists(fullPath))
            {
                ShowStatus("Selected folder does not exist", MessageType.Error);
                return;
            }

            // Find all .cs files recursively
            var scripts = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                .ToList();

            if (scripts.Count == 0)
            {
                ShowStatus("No C# scripts found in folder", MessageType.Warning);
                return;
            }

            // Clear detail view - we're starting fresh
            _currentResult = null;

            var settings = PholusSettings.Instance;
            var useConsensus = settings.EnableConsensusMode && settings.ConsensusAnalyzers.Count >= 2;

            if (useConsensus)
            {
                // Use parallel scan with consensus (same as project scan)
                ScanProjectParallel(scripts);
                return;
            }

            // Standard single-provider scan
            _isScanning = true;
            _analysisStartTime = Time.realtimeSinceStartup;
            _scanResult = null;
            _resultFoldouts.Clear();
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            ShowStatus($"Scanning {scripts.Count} scripts...", MessageType.Info);

            // Subscribe to update for live elapsed time
            EditorApplication.update += UpdateAnalysisProgress;

            // Create progress reporter
            var progress = new Progress<ScanProgress>(p =>
            {
                _currentScanProgress = p;
                Repaint();
            });

            try
            {
                _scanResult = await _analysisService.ScanProjectAsync(scripts, progress, _cts.Token);

                var issueScripts = _scanResult.ScriptsWithIssues;
                var totalIssues = _scanResult.TotalIssues;
                ShowStatus($"Scan complete: {issueScripts}/{_scanResult.ScriptsAnalyzed} scripts with issues ({totalIssues} total)", MessageType.Info);

                // Save to folder cache
                if (!string.IsNullOrEmpty(_selectedFolderPath))
                {
                    _folderScanCache[_selectedFolderPath] = _scanResult;
                    SaveCache();
                }
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Scan cancelled", MessageType.Warning);
            }
            catch (Exception ex)
            {
                ShowStatus($"Scan failed: {ex.Message}", MessageType.Error);
                PholusLogger.LogError($"{ex}");
            }
            finally
            {
                EditorApplication.update -= UpdateAnalysisProgress;
                _isScanning = false;
                _currentScanProgress = null;
                Repaint();
            }
        }

        private void StartProjectScan()
        {
            // Clear folder path to indicate this is a project scan
            _selectedFolderPath = null;

            // Find all scripts, excluding certain folders
            var allScripts = GetProjectScripts();

            if (allScripts.Count == 0)
            {
                ShowStatus("No C# scripts found in project", MessageType.Warning);
                return;
            }

            // Show confirmation dialog
            var settings = PholusSettings.Instance;
            var useConsensus = settings.EnableConsensusMode && settings.ConsensusAnalyzers.Count >= 2;
            var analyzerCount = useConsensus ? settings.ConsensusAnalyzers.Count : 1;

            // Estimate time: ~20s per script in single mode, ~20s * analyzers in consensus
            var estimatedSeconds = allScripts.Count * 20 * analyzerCount / MaxParallelScans;
            var estimatedMinutes = estimatedSeconds / 60;

            var modeText = useConsensus ? $"Consensus ({analyzerCount} providers)" : "Single provider";
            var timeText = estimatedMinutes > 1 ? $"~{estimatedMinutes} minutes" : $"~{estimatedSeconds} seconds";

            var message = $"Scan Entire Project?\n\n" +
                         $"Found {allScripts.Count} C# scripts.\n\n" +
                         $"Mode: {modeText}\n" +
                         $"Parallel: {MaxParallelScans} concurrent\n" +
                         $"Estimated time: {timeText}\n\n" +
                         $"This will use significant API tokens.\n" +
                         $"Partial results are kept if cancelled.";

            if (!EditorUtility.DisplayDialog("Project Scan", message, "Start Scan", "Cancel"))
            {
                return;
            }

            // Clear detail view - we're starting fresh
            _currentResult = null;

            // Start the parallel scan
            ScanProjectParallel(allScripts);
        }

        private List<string> GetProjectScripts()
        {
            var assetsPath = Application.dataPath;
            var allScripts = new List<string>();

            try
            {
                var files = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Normalize path separators
                    var normalizedPath = file.Replace("\\", "/");

                    // Skip excluded folders
                    if (normalizedPath.Contains("/Plugins/")) continue;
                    if (normalizedPath.Contains("/Editor/")) continue;
                    if (normalizedPath.Contains("/.")) continue; // Hidden folders

                    allScripts.Add(file);
                }
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to enumerate scripts: {ex.Message}");
            }

            return allScripts;
        }

        private async void ScanProjectParallel(List<string> scripts)
        {
            _isScanning = true;
            _analysisStartTime = Time.realtimeSinceStartup;
            _scanResult = new ProjectScanResult
            {
                Results = new List<AnalysisResult>(),
                ScannedAt = DateTime.Now
            };
            _resultFoldouts.Clear();
            ClearConsensusCache(); // Clear previous scan's consensus data
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var settings = PholusSettings.Instance;
            var useConsensus = settings.EnableConsensusMode && settings.ConsensusAnalyzers.Count >= 2;

            ShowStatus($"Scanning {scripts.Count} scripts ({(useConsensus ? "consensus" : "single")} mode, {MaxParallelScans} parallel)...", MessageType.Info);

            EditorApplication.update += UpdateAnalysisProgress;

            var completedCount = 0;
            var semaphore = new SemaphoreSlim(MaxParallelScans);
            var lockObj = new object();

            try
            {
                var tasks = scripts.Select(async scriptPath =>
                {
                    await semaphore.WaitAsync(_cts.Token);

                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();

                        var fileName = Path.GetFileName(scriptPath);

                        // Update progress (shows which file is being analyzed)
                        lock (lockObj)
                        {
                            _currentScanProgress = new ScanProgress
                            {
                                CurrentFile = fileName,
                                CurrentIndex = completedCount + 1,
                                TotalFiles = scripts.Count
                            };
                        }
                        // Note: Repaint is called via EditorApplication.update -> UpdateAnalysisProgress

                        AnalysisResult result;

                        if (useConsensus)
                        {
                            var consensusService = ConsensusService.Create();
                            var consensusSettings = ConsensusSettings.FromSettings(settings);
                            var consensusResult = await consensusService.AnalyzeWithConsensusAsync(
                                scriptPath,
                                consensusSettings,
                                _cts.Token);
                            result = consensusResult.FinalResult;

                            // Store consensus result for showing provider opinions later
                            lock (lockObj)
                            {
                                SetConsensusCache(scriptPath, consensusResult);
                            }
                        }
                        else
                        {
                            result = await _analysisService.AnalyzeScriptAsync(scriptPath, _cts.Token);
                        }

                        // Add to results (thread-safe)
                        lock (lockObj)
                        {
                            _scanResult.Results.Add(result);
                            completedCount++;
                        }

                        Repaint();
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancelled - partial results are kept
                    }
                    catch (Exception ex)
                    {
                        PholusLogger.LogWarning($"Failed to analyze {Path.GetFileName(scriptPath)}: {ex.Message}");

                        lock (lockObj)
                        {
                            _scanResult.Results.Add(AnalysisResult.Error(scriptPath, ex.Message));
                            completedCount++;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks);

                _scanResult.Duration = DateTime.Now - _scanResult.ScannedAt;

                var issueScripts = _scanResult.ScriptsWithIssues;
                var totalIssues = _scanResult.TotalIssues;
                ShowStatus($"Project scan complete: {issueScripts}/{_scanResult.ScriptsAnalyzed} scripts with issues ({totalIssues} total)", MessageType.Info);

                // Save to project cache
                _projectScanCache = _scanResult;
                SaveCache();
            }
            catch (OperationCanceledException)
            {
                _scanResult.Duration = DateTime.Now - _scanResult.ScannedAt;
                ShowStatus($"Scan cancelled. Partial results: {_scanResult.ScriptsAnalyzed} scripts analyzed", MessageType.Warning);

                // Still save partial results to project cache
                _projectScanCache = _scanResult;
                SaveCache();
            }
            catch (Exception ex)
            {
                ShowStatus($"Scan failed: {ex.Message}", MessageType.Error);
                PholusLogger.LogError($"{ex}");
            }
            finally
            {
                EditorApplication.update -= UpdateAnalysisProgress;
                _isScanning = false;
                _currentScanProgress = null;
                semaphore.Dispose();
                Repaint();
            }
        }

        private async void OnFixRequested(PerformanceIssue issue)
        {
            if (_currentResult == null) return;

            _isGeneratingFix = true;
            _issueListView.SetFixInProgress(true);
            _analysisStartTime = Time.realtimeSinceStartup;
            ShowStatus($"Generating fix for: {issue.Title}", MessageType.Info);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // Subscribe to update for live elapsed time
            EditorApplication.update += UpdateFixProgress;

            try
            {
                _currentFixPreview = await _fixService.GenerateFixPreviewAsync(
                    _currentResult.ScriptPath,
                    issue,
                    _cts.Token);

                if (_currentFixPreview.Success)
                {
                    // Auto-apply if setting is enabled
                    if (PholusSettings.Instance.AutoApplyFixes)
                    {
                        await AutoApplyFixAsync();
                    }
                    else
                    {
                        _diffPreviewView.SetPreview(_currentFixPreview);
                        _showDiffPreview = true;
                    }
                }
                else
                {
                    ShowStatus($"Fix failed: {_currentFixPreview.Error}", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Fix generation failed: {ex.Message}", MessageType.Error);
            }
            finally
            {
                EditorApplication.update -= UpdateFixProgress;
                _isGeneratingFix = false;
                _issueListView.SetFixInProgress(false);
                Repaint();
            }
        }

        private void UpdateFixProgress()
        {
            if (!_isGeneratingFix) return;
            Repaint();
        }

        private void OnSkipRequested(PerformanceIssue issue)
        {
            ShowStatus($"Skipped: {issue.Title}", MessageType.Info);
            Repaint();
        }

        private async void OnApplyFix()
        {
            if (_currentFixPreview == null || !_currentFixPreview.Success) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                var result = await _fixService.ApplyFixAsync(_currentFixPreview, _cts.Token);

                if (result.Success)
                {
                    ShowStatus($"Fix applied! {_currentFixPreview.Diff.Summary}", MessageType.Info);

                    // Mark issue as fixed so it's removed from active list
                    if (_currentFixPreview.Issue != null)
                    {
                        _currentFixPreview.Issue.IsFixed = true;
                    }

                    _showDiffPreview = false;
                    _currentFixPreview = null;
                }
                else
                {
                    ShowStatus($"Failed to apply fix: {result.Error}", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to apply fix: {ex.Message}", MessageType.Error);
            }

            Repaint();
        }

        private async Task AutoApplyFixAsync()
        {
            if (_currentFixPreview == null || !_currentFixPreview.Success) return;

            try
            {
                var result = await _fixService.ApplyFixAsync(_currentFixPreview, _cts.Token);

                if (result.Success)
                {
                    ShowStatus($"Auto-applied: {_currentFixPreview.Diff.Summary}", MessageType.Info);

                    // Mark issue as fixed
                    if (_currentFixPreview.Issue != null)
                    {
                        _currentFixPreview.Issue.IsFixed = true;
                    }

                    _currentFixPreview = null;
                }
                else
                {
                    ShowStatus($"Auto-apply failed: {result.Error}", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Auto-apply failed: {ex.Message}", MessageType.Error);
            }
        }

        private void OnApplyFixAndDontAsk()
        {
            // Enable auto-apply setting
            PholusSettings.Instance.AutoApplyFixes = true;
            ShowStatus("Auto-apply enabled. Future fixes will be applied automatically.", MessageType.Info);

            // Apply the current fix
            OnApplyFix();
        }

        private void OnCancelFix()
        {
            _showDiffPreview = false;
            _currentFixPreview = null;
            _diffPreviewView.Clear();
            Repaint();
        }

        private void OnSettingsChanged(PholusSettings settings)
        {
            Repaint();
        }

        private void ShowStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            _statusTime = Time.realtimeSinceStartup;
        }

        #region Export

        private void ExportAnalysisAsMarkdown(AnalysisResult result)
        {
            if (result == null) return;

            var fileName = Path.GetFileNameWithoutExtension(result.ScriptPath);
            var defaultName = $"{fileName}_analysis.md";
            var path = EditorUtility.SaveFilePanel("Export Analysis", "", defaultName, "md");

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var markdown = GenerateMarkdownReport(result);
                File.WriteAllText(path, markdown);
                PholusLogger.Log($"Analysis exported to: {path}");
                ShowStatus($"Exported to {Path.GetFileName(path)}", MessageType.Info);
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Export failed: {ex.Message}");
                ShowStatus($"Export failed: {ex.Message}", MessageType.Error);
            }
        }

        private string GenerateMarkdownReport(AnalysisResult result)
        {
            var sb = new System.Text.StringBuilder();
            var fileName = Path.GetFileName(result.ScriptPath);

            // Header
            sb.AppendLine($"# Analysis Report: {fileName}");
            sb.AppendLine();
            sb.AppendLine($"**Score:** {result.Score}/100");

            // Use current time if AnalyzedAt wasn't set
            var analyzedAt = result.AnalyzedAt > DateTime.MinValue ? result.AnalyzedAt : DateTime.Now;
            sb.AppendLine($"**Analyzed:** {analyzedAt:yyyy-MM-dd HH:mm}");

            // Check if this was a consensus analysis
            if (_currentConsensusResult != null && _currentConsensusResult.AnalyzersUsed?.Count > 1)
            {
                sb.AppendLine($"**Mode:** Consensus ({_currentConsensusResult.AnalyzersUsed.Count} providers)");
                sb.AppendLine($"**Providers:** {string.Join(", ", _currentConsensusResult.AnalyzersUsed)}");
            }
            else
            {
                sb.AppendLine($"**Provider:** {PholusSettings.Instance.ActiveProvider}");
            }
            sb.AppendLine();

            // Summary
            if (!string.IsNullOrEmpty(result.Summary))
            {
                sb.AppendLine("## Summary");
                sb.AppendLine(result.Summary);
                sb.AppendLine();
            }

            // Definite Issues
            if (result.DefiniteIssues?.Count > 0)
            {
                sb.AppendLine("## Definite Issues");
                sb.AppendLine();
                foreach (var issue in result.DefiniteIssues)
                {
                    AppendIssue(sb, issue);
                }
            }

            // Contextual Issues
            if (result.ContextualIssues?.Count > 0)
            {
                sb.AppendLine("## Contextual Issues");
                sb.AppendLine();
                foreach (var issue in result.ContextualIssues)
                {
                    AppendIssue(sb, issue);
                }
            }

            // Suggestions
            if (result.Suggestions?.Count > 0)
            {
                sb.AppendLine("## Suggestions");
                sb.AppendLine();
                foreach (var issue in result.Suggestions)
                {
                    AppendIssue(sb, issue);
                }
            }

            // Positive Notes
            if (result.PositiveNotes?.Count > 0)
            {
                sb.AppendLine("## Positive Notes");
                sb.AppendLine();
                foreach (var note in result.PositiveNotes)
                {
                    sb.AppendLine($"- {note}");
                }
                sb.AppendLine();
            }

            // Provider Breakdown (for consensus mode)
            if (_currentConsensusResult != null && _currentConsensusResult.ProviderResults?.Count > 0)
            {
                sb.AppendLine("## Provider Breakdown");
                sb.AppendLine();

                // Director info
                sb.AppendLine($"**Director:** {_currentConsensusResult.DirectorUsed}");
                sb.AppendLine();

                foreach (var kvp in _currentConsensusResult.ProviderResults)
                {
                    var provider = kvp.Key;
                    var providerResult = kvp.Value;

                    sb.AppendLine($"### {provider}");
                    sb.AppendLine($"**Score:** {providerResult.Score}/100");
                    sb.AppendLine();

                    // Provider's summary
                    if (!string.IsNullOrEmpty(providerResult.Summary))
                    {
                        sb.AppendLine($"**Summary:** {providerResult.Summary}");
                        sb.AppendLine();
                    }

                    // Provider's issues
                    if (providerResult.DefiniteIssues?.Count > 0)
                    {
                        sb.AppendLine("**Definite Issues:**");
                        foreach (var issue in providerResult.DefiniteIssues)
                        {
                            sb.AppendLine($"- {issue.Title} (Line {issue.Line}, {issue.Severity})");
                        }
                        sb.AppendLine();
                    }

                    if (providerResult.ContextualIssues?.Count > 0)
                    {
                        sb.AppendLine("**Contextual Issues:**");
                        foreach (var issue in providerResult.ContextualIssues)
                        {
                            sb.AppendLine($"- {issue.Title} (Line {issue.Line}, {issue.Severity})");
                        }
                        sb.AppendLine();
                    }

                    if (providerResult.Suggestions?.Count > 0)
                    {
                        sb.AppendLine("**Suggestions:**");
                        foreach (var issue in providerResult.Suggestions)
                        {
                            sb.AppendLine($"- {issue.Title}");
                        }
                        sb.AppendLine();
                    }
                }
            }

            // Footer
            sb.AppendLine("---");
            sb.AppendLine("*Generated by Pholus*");

            return sb.ToString();
        }

        private void AppendIssue(System.Text.StringBuilder sb, PerformanceIssue issue)
        {
            sb.AppendLine($"### {issue.Title}");
            sb.AppendLine();
            sb.AppendLine($"- **Severity:** {issue.Severity}");
            if (issue.Line > 0)
                sb.AppendLine($"- **Line:** {issue.Line}");
            if (issue.ConfidencePercent > 0)
                sb.AppendLine($"- **Confidence:** {issue.ConfidencePercent}%");

            if (!string.IsNullOrEmpty(issue.Explanation))
            {
                sb.AppendLine();
                sb.AppendLine($"**Explanation:** {issue.Explanation}");
            }

            if (!string.IsNullOrEmpty(issue.Impact))
            {
                sb.AppendLine();
                sb.AppendLine($"**Impact:** {issue.Impact}");
            }

            if (!string.IsNullOrEmpty(issue.CodeSnippet))
            {
                sb.AppendLine();
                sb.AppendLine("**Code:**");
                sb.AppendLine("```csharp");
                sb.AppendLine(issue.CodeSnippet);
                sb.AppendLine("```");
            }

            if (!string.IsNullOrEmpty(issue.WhyIMightBeWrong))
            {
                sb.AppendLine();
                sb.AppendLine($"**Why I Might Be Wrong:** {issue.WhyIMightBeWrong}");
            }

            sb.AppendLine();
        }

        #endregion

        #region Result Cache Persistence

        [Serializable]
        private class CacheEntry<T>
        {
            public string Key;
            public T Value;
        }

        [Serializable]
        private class ResultCacheData
        {
            public List<CacheEntry<AnalysisResult>> ScriptResults = new List<CacheEntry<AnalysisResult>>();
            public List<CacheEntry<ProjectScanResult>> FolderResults = new List<CacheEntry<ProjectScanResult>>();
            public ProjectScanResult ProjectResult;
        }

        private void SaveCache()
        {
            try
            {
                var cacheDir = Path.GetDirectoryName(CachePath);
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                var data = new ResultCacheData
                {
                    ProjectResult = _projectScanCache
                };

                foreach (var kvp in _scriptResultCache)
                {
                    data.ScriptResults.Add(new CacheEntry<AnalysisResult> { Key = kvp.Key, Value = kvp.Value });
                }

                foreach (var kvp in _folderScanCache)
                {
                    data.FolderResults.Add(new CacheEntry<ProjectScanResult> { Key = kvp.Key, Value = kvp.Value });
                }

                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(CachePath, json);
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Failed to save cache: {ex.Message}");
            }
        }

        private void LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath))
                {
                    return;
                }

                var json = File.ReadAllText(CachePath);
                var data = JsonUtility.FromJson<ResultCacheData>(json);

                if (data == null) return;

                _projectScanCache = data.ProjectResult;

                _scriptResultCache.Clear();
                if (data.ScriptResults != null)
                {
                    foreach (var entry in data.ScriptResults)
                    {
                        if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
                        {
                            _scriptResultCache[entry.Key] = entry.Value;
                        }
                    }
                }

                _folderScanCache.Clear();
                if (data.FolderResults != null)
                {
                    foreach (var entry in data.FolderResults)
                    {
                        if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
                        {
                            _folderScanCache[entry.Key] = entry.Value;
                        }
                    }
                }

                PholusLogger.Log($"Loaded cache: {_scriptResultCache.Count} scripts, {_folderScanCache.Count} folders, project: {(_projectScanCache != null ? "yes" : "no")}");
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Failed to load cache: {ex.Message}");
            }
        }

        private void ClearAllCache()
        {
            _scriptResultCache.Clear();
            _folderScanCache.Clear();
            _projectScanCache = null;
            _currentResult = null;
            _currentConsensusResult = null;
            _scanResult = null;
            ClearConsensusCache();
            SaveCache();
            ShowStatus("Cache cleared", MessageType.Info);
        }

        private Dictionary<string, ConsensusResult> GetConsensusCache()
        {
            if (_consensusResultCache == null)
            {
                _consensusResultCache = new Dictionary<string, ConsensusResult>();
                for (int i = 0; i < _consensusCacheKeys.Count && i < _consensusCacheValues.Count; i++)
                {
                    _consensusResultCache[_consensusCacheKeys[i]] = _consensusCacheValues[i];
                }
            }
            return _consensusResultCache;
        }

        private void SetConsensusCache(string scriptPath, ConsensusResult result)
        {
            var cache = GetConsensusCache();
            cache[scriptPath] = result;
            // Sync to serializable lists
            _consensusCacheKeys = cache.Keys.ToList();
            _consensusCacheValues = cache.Values.ToList();
        }

        private void ClearConsensusCache()
        {
            _consensusResultCache?.Clear();
            _consensusCacheKeys.Clear();
            _consensusCacheValues.Clear();
        }

        private void ClearScriptCache(string scriptPath)
        {
            if (_scriptResultCache.ContainsKey(scriptPath))
            {
                _scriptResultCache.Remove(scriptPath);
                SaveCache();
            }
        }

        private void ClearFolderCache(string folderPath)
        {
            if (_folderScanCache.ContainsKey(folderPath))
            {
                _folderScanCache.Remove(folderPath);
                SaveCache();
            }
        }

        private void ClearProjectCache()
        {
            _projectScanCache = null;
            SaveCache();
        }

        private static string GetRelativeTime(DateTime analyzedAt)
        {
            var elapsed = DateTime.Now - analyzedAt;
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return analyzedAt.ToString("MMM d");
        }

        private void StoreCurrentResultsInCache()
        {
            // Store current single script result
            if (_currentResult != null && !string.IsNullOrEmpty(_currentResult.ScriptPath))
            {
                _scriptResultCache[_currentResult.ScriptPath] = _currentResult;
            }

            // Store current scan result based on mode
            if (_scanResult != null)
            {
                if (_analysisMode == AnalysisMode.Project)
                {
                    _projectScanCache = _scanResult;
                }
                else if (_analysisMode == AnalysisMode.Folder && !string.IsNullOrEmpty(_selectedFolderPath))
                {
                    _folderScanCache[_selectedFolderPath] = _scanResult;
                }
            }

            SaveCache();
        }

        private void RestoreResultsFromCache()
        {
            // Clear current results first
            _currentResult = null;
            _scanResult = null;

            switch (_analysisMode)
            {
                case AnalysisMode.Scripts:
                    // Scripts mode doesn't persist scan results
                    // Results are regenerated when user clicks Analyze
                    break;

                case AnalysisMode.Folder:
                    // Try to restore from folder cache
                    if (!string.IsNullOrEmpty(_selectedFolderPath) &&
                        _folderScanCache.TryGetValue(_selectedFolderPath, out var folderResult))
                    {
                        _scanResult = folderResult;
                    }
                    break;

                case AnalysisMode.Project:
                    // Restore from project cache
                    if (_projectScanCache != null)
                    {
                        _scanResult = _projectScanCache;
                    }
                    break;
            }
        }

        #endregion
    }
}
