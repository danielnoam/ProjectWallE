using System;
using System.Collections.Generic;
using System.Linq;
using GruelTerraSplines.Managers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Splines;

namespace GruelTerraSplines
{
    // Interface for polymorphic list items
    public interface IListItemData
    {
        bool isFoldoutExpanded { get; set; }
    }

    // Data structure for ListView
    [Serializable]
    public class SplineItemData : IListItemData
    {
        public SplineContainer container;
        public Texture2D previewTexture;
        public SplineStrokeSettings settings;
        public bool isOverrideBrushTabActive = false; // Track which tab is active
        public bool isOverridePaintTabActive = false; // Track paint tab state
        public bool isOverrideDetailTabActive = false; // Track detail tab state
        public bool isOverrideTreeTabActive = false; // Track tree tab state
        public bool isDetailNoiseFoldoutExpanded = false; // Track detail noise foldout state
        public bool isTreeNoiseFoldoutExpanded = false; // Track tree noise foldout state
        public bool isPaintNoiseFoldoutExpanded = false; // Track paint noise foldout state
        public bool isBrushNoiseFoldoutExpanded = false; // Track brush noise foldout state
        public bool isFoldoutExpanded { get; set; } = true; // Track foldout state (default expanded)
    }

    // Data structure for group containers
    [Serializable]
    public class GroupItemData : IListItemData
    {
        public Transform groupTransform;
        public string hierarchyPath;
        public List<SplineItemData> splines = new List<SplineItemData>();
        [NonSerialized] public List<VisualElement> cachedSplineElements = new List<VisualElement>();
        [NonSerialized] public bool splineElementsBuilt;
        public bool isFoldoutExpanded { get; set; } = true; // Track foldout state (default expanded)
    }

    // Helper class to track hierarchy position for proper ordering
    public class SplineHierarchyInfo
    {
        public SplineContainer container;
        public Transform parent;
        public int siblingIndex;
    }

    public partial class TerraSplinesWindow : EditorWindow
    {
        const string MenuPath = "Tools/Gruel Terra Splines";
        const string WindowTitle = "Gruel Terra Splines";
        const string ResourcePathBase = "UI/TerraSplinesWindow";
        const string EditorPrefsPrefix = "GruelTerraSplines";
        const string LegacyEditorPrefsPrefix = "DKSplineTerrain";
        const string LogPrefix = "[Gruel Terra Splines]";
        const string PluginAssetRoot = "Assets/Plugins/GruelTerraSplines";
        const string LegacyPluginAssetRoot = "Assets/Plugins/DKSplineTerrain";
        public const string DocumentationUrl = "https://gruelscum.gitbook.io/gruelterrasplines-docs/";
        const string DiscordInviteUrl = "https://discord.com/invite/cTtfczGrb3";
        const string ResumeIconName = "resume";
        const string PauseIconName = "pause";

        static double overlayInteractionCooldownUntil;

        static string BuildEditorPrefsKey(string suffix) => $"{EditorPrefsPrefix}.{suffix}";
        static string BuildLegacyEditorPrefsKey(string suffix) => $"{LegacyEditorPrefsPrefix}.{suffix}";

        static bool LoadBoolPref(string suffix, bool defaultValue)
        {
            string key = BuildEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(key))
                return EditorPrefs.GetBool(key, defaultValue);

            string legacyKey = BuildLegacyEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(legacyKey))
                return EditorPrefs.GetBool(legacyKey, defaultValue);

            return defaultValue;
        }

        public static TerraSplinesWindow GetOpenWindow()
        {
            return Resources.FindObjectsOfTypeAll<TerraSplinesWindow>().FirstOrDefault();
        }

        public float CurrentGlobalBrushSize => globalBrushSize;

        public static void DeferInteractiveRefresh(double seconds)
        {
            overlayInteractionCooldownUntil = Math.Max(overlayInteractionCooldownUntil, EditorApplication.timeSinceStartup + seconds);
        }

        static bool IsInteractiveRefreshDeferred()
        {
            return EditorApplication.timeSinceStartup < overlayInteractionCooldownUntil;
        }

        static float LoadFloatPref(string suffix, float defaultValue)
        {
            string key = BuildEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(key))
                return EditorPrefs.GetFloat(key, defaultValue);

            string legacyKey = BuildLegacyEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(legacyKey))
                return EditorPrefs.GetFloat(legacyKey, defaultValue);

            return defaultValue;
        }

        static int LoadIntPref(string suffix, int defaultValue)
        {
            string key = BuildEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(key))
                return EditorPrefs.GetInt(key, defaultValue);

            string legacyKey = BuildLegacyEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(legacyKey))
                return EditorPrefs.GetInt(legacyKey, defaultValue);

            return defaultValue;
        }

        // Serialized fields
        [SerializeField] GameObject targetTerrainGroup; // GameObject containing Terrain components in children
        [SerializeField] Terrain targetTerrain; // Legacy field for backward compatibility (will be migrated)
        [SerializeField] Transform splineGroup;
        [SerializeField] float globalBrushSize = 50f;
        [SerializeField] float globalStrength = 1f;
        [SerializeField] float globalSampleStep = 2f;
        [SerializeField] SplineApplyMode globalMode = SplineApplyMode.Path;
        [SerializeField] float updateInterval = 0.1f;
        [SerializeField] float levelingValue = 0f;
        [SerializeField] bool updatesPaused = false;
        [SerializeField] bool supportOutsideChanges = true; // Default: enabled
        [SerializeField] bool featureHoleEnabled = true;
        [SerializeField] bool featurePaintEnabled = true;
        [SerializeField] bool featureDetailEnabled = true;
        [SerializeField] bool featureTreeEnabled = true;
        [SerializeField] bool featureGizmosEnabled = true;
        [SerializeField] bool heightmapPreviewEnabled = true; // Enable/disable heightmap preview generation

        // Detail feature flag
        bool FeatureDetailEnabled => featureDetailEnabled;
        bool FeatureTreeEnabled => featureTreeEnabled;

        // New brush falloff parameters
        [SerializeField] float globalBrushHardness = 0.5f; // 0-1, controls falloff curve exponent
        [SerializeField] Texture2D globalBrush; // For brush selection
        [SerializeField] BrushNoiseSettings globalBrushNoise = new BrushNoiseSettings();

        // Toggle-based operation flags (preferred over legacy enum)
        [SerializeField] bool globalOpHeight = true;
        [SerializeField] bool globalOpPaint = false;
        [SerializeField] bool globalOpHole = false;
        [SerializeField] bool globalOpFill = false;
        [SerializeField] bool globalOpAddDetail = false;
        [SerializeField] bool globalOpRemoveDetail = false;
        [SerializeField] bool globalOpAddTree = false;
        [SerializeField] bool globalOpRemoveTree = false;

        // Global paint settings
        [SerializeField] int globalSelectedLayerIndex = 0;
        [SerializeField] float globalPaintStrength = 1f;
        [SerializeField] float globalPaintBlend = 0.5f;
        [SerializeField] float globalPaintThreshold = 0.5f;
        [SerializeField] List<PaintNoiseLayerSettings> globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();

        // Global grass settings
        [SerializeField] int globalSelectedDetailLayerIndex = 0;
        [SerializeField] List<int> globalSelectedDetailLayerIndices = new List<int>();
        [SerializeField] float globalDetailStrength = 1f;
        [SerializeField] DetailOperationMode globalDetailMode = DetailOperationMode.Add;
        [SerializeField] float globalDetailSlopeLimitDegrees = 90f;
        [SerializeField] float globalDetailFalloffPower = 0.89f;
        [SerializeField] int globalDetailSpreadRadius = 0;
        [SerializeField] float globalDetailRemoveThreshold = 0.5f;
        [SerializeField] List<DetailNoiseLayerSettings> globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();

        // Global tree settings
        [SerializeField] int globalSelectedTreePrototypeIndex = 0;
        [SerializeField] List<int> globalSelectedTreePrototypeIndices = new List<int>();
        [SerializeField] float globalTreeStrength = 1f;
        [SerializeField] int globalTreeTargetDensity = 128;
        [SerializeField] float globalTreeSlopeLimitDegrees = 90f;
        [SerializeField] float globalTreeFalloffPower = 0.89f;
        [SerializeField] AnimationCurve globalTreeHeightMultiplier = SplineStrokeSettings.CreateBuiltInTreeHeightMultiplierCurve();
        [SerializeField] float globalTreeRemoveThreshold = 0.5f;
        [SerializeField] List<TreeNoisePrototypeSettings> globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();

        // Global tab state
        [SerializeField] int globalActiveTab = 0; // 0=Mode, 1=Brush, 2=Paint, 3=Detail

        // Internal state
        float[,] originalHeights; // Original terrain at tool start (never auto-updated)
        float[,] baselineHeights; // Original + manual edits
        float[,] workingHeights; // Baseline + splines (applied to terrain)
        
        bool[,] originalHoles; // Original terrain holes at tool start
        bool[,] baselineHoles; // Original + manual hole edits
        bool[,] workingHoles; // Baseline + spline holes (applied to terrain)
        bool[,] holeSplineMask; // Mask for hole/fill operations
        
        float[,,] originalAlphamaps; // Original terrain alphamaps at tool start
        float[,,] baselineAlphamaps; // Original + manual paint edits
        float[,,] workingAlphamaps; // Baseline + spline paint modifications
        float[,] paintSplineAlphaMask; // Mask for paint operations
        
        Texture2D baselineTex;
        Texture2D previewTex;
        
        readonly Stack<TerrainSnapshot> undoStack = new Stack<TerrainSnapshot>();
        readonly Stack<TerrainSnapshot> redoStack = new Stack<TerrainSnapshot>();

        sealed class TerrainSnapshot
        {
            public float[,] heights;
            public bool[,] holes;
            public float[,,] alphamaps;
            public TreeInstance[] treeInstances;
        }

        // Baseline update strategy
        IBaselineUpdateStrategy baselineUpdateStrategy;

        bool CanUndo => undoStack.Count > 0;

        bool CanRedo => redoStack.Count > 0;

        DateTime lastUpdate = DateTime.MinValue;
        bool lastHadActiveSplines = false;
        bool previewTexturesNeedUpdate = false;
        bool needsFreshBaseline = true;
        bool isPreviewAppliedToTerrain = false;

        // Event-driven update pipeline
        bool baselineDirty = true;
        bool previewDirty = true;
        bool applyDirty = true;
        bool heightRangeDirty = true;
        bool updateScheduled = false;
        bool updatePendingWhilePaused = false;
        int internalTerrainChangeSuppressionCount = 0;
        int internalSplineChangeSuppressionCount = 0;
        int suppressRefreshPreviewRequestCount = 0;
        double internalTerrainChangeIgnoreUntil = 0;
        bool autoPipelineSuspendedForReferenceRefresh = false;
        double lastPipelineRunTime = 0;
        double lastObservedTerrainScanTime = 0;
        readonly Dictionary<EntityId, int> observedTerrainContentHashes = new Dictionary<EntityId, int>();
        const double PIPELINE_MIN_INTERVAL_SECONDS = 1.0 / 30.0;
        const double INTERNAL_TERRAIN_CHANGE_IGNORE_SECONDS = 0.35;

        // Foldout state persistence
        [SerializeField] bool foldoutTargets = false;
        [SerializeField] bool foldoutTargetGroups = true;
        [SerializeField] bool foldoutDebug = false;

        // UI Elements
        VisualElement root;
        ListView splinesListView;
        List<IListItemData> splineItems = new List<IListItemData>();

        // Icon cache
        Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

        // UI Element References
        ObjectField terrainField;
        ObjectField splineGroupField;
        Slider levelingValueSlider;
        FloatField levelingValueField;
        Button offsetButton;
        Button levelButton;
        Button fillHolesButton;
        Button clearPaintButton;
        Button clearDetailButton;
        Button clearTreesButton;
        Button pathModeButton;
        Button shapeModeButton;
        Image globalBrushPreviewImage;
        Slider globalBrushHardnessSlider;
        FloatField globalBrushHardnessField;
        Slider globalBrushSizeSlider;
        FloatField globalBrushSizeField;
        Slider globalStrengthSlider;
        FloatField globalStrengthField;
        Slider globalSampleStepSlider;
        FloatField globalSampleStepField;
        Button undoButton;
        Button applyButton;
        Button redoButton;
        Slider updateIntervalSlider;
        FloatField updateIntervalField;
        Button pauseResumeButton;
        Button updatePreviewOnceButton;
        Image baselineTexture;
        Image previewTexture;
        Button rebuildPreviewButton;
        Button clearCacheButton;
        Label cacheInfoLabel;
        Label backendStatusLabel;
        Button cpuFallbackButton;
        Button gpuPerformantButton;
        Toggle supportOutsideChangesToggle;
        Toggle heightmapPreviewToggle;
        Label performanceTimingLabel;
        
        // Performance timing
        double terrainOperationTimeMs = 0;
        double previewGenerationTimeMs = 0;
        double updateBaselineTimeMs = 0;
        double applyPreviewTimeMs = 0;
        double updatePreviewTexturesTimeMs = 0;
        double updateHeightRangeTimeMs = 0;
        Button benchmarkCacheButton;
        Label heightRangeLabel;
        Button setupGroupButton;
        Button findRefsButton;
        Foldout terrainSplinesGroupsFoldout;
        VisualElement terrainSplinesGroupsListContainer;
        Button collapseAllButton;
        Button expandAllButton;
        Label splineCountLabel;
        VisualElement creditsContainer;
        Label creditsLinkLabel;
        bool listRefreshScheduled;
        bool terrainSplinesGroupsListRefreshScheduled;
        Foldout targetsFoldout;
        Label targetsWarningIcon; // Yellow warning icon for the Targets foldout
        Toggle featureHoleToggle;
        Toggle featurePaintToggle;
        Toggle featureDetailToggle;
        Toggle featureTreeToggle;
        Toggle featureGizmosToggle;
        Label globalOperationWarningLabel;
        Button debugDocsButton;
        Image debugDocsIcon;
        Image debugStatusIcon;

        // Global icon elements (injected into foldout header)
        VisualElement globalIconsContainer;
        Image globalModeIcon;
        VisualElement globalOperationIconsContainer;

        // Paint UI Elements
        VisualElement globalPaintLayerPalette;
        Slider globalPaintStrengthSlider;
        FloatField globalPaintStrengthField;
        Slider globalPaintBlendSlider;
        FloatField globalPaintBlendField;
        Slider globalPaintThresholdSlider;
        FloatField globalPaintThresholdField;
        Button paintAllButton;

        // Global Tab UI Elements
        Button globalModeTab;
        Button globalBrushTab;
        Button globalPaintTab;
        Button globalDetailTab;
        Button globalTreeTab;
        VisualElement globalModeContent;
        VisualElement globalBrushContent;
        VisualElement globalPaintContent;
        VisualElement globalDetailContent;
        VisualElement globalTreeContent;

        // Height range caching to prevent unnecessary calculations
        (float min, float max) cachedBaselineRange = (0, 0);
        (float min, float max) cachedPreviewRange = (0, 0);
        bool heightRangeNeedsUpdate = true;

        // Layer palette caching
        TerrainLayer[] cachedTerrainLayers;
        int lastTerrainLayersHash;
        int lastDetailPrototypeHash;
        double lastTerrainPaletteCheckTime = 0;
        const double TERRAIN_PALETTE_CHECK_INTERVAL_SECONDS = 1.0;

        // Flag to suppress automatic baseline updates during special operations
        bool suppressBaselineUpdate = false;
        bool manualUpdateRequested = false;

        // Foldout state persistence across RefreshChildren() calls
        Dictionary<EntityId, bool> splineFoldoutStates = new Dictionary<EntityId, bool>();
        Dictionary<EntityId, bool> groupFoldoutStates = new Dictionary<EntityId, bool>();
        // Tab state persistence across RefreshChildren() calls (per spline)
        Dictionary<SplineContainer, (bool brush, bool paint, bool detail, bool tree)> splineTabStates = new Dictionary<SplineContainer, (bool, bool, bool, bool)>();
        readonly HashSet<VisualElement> boundSplineRowElements = new HashSet<VisualElement>();
        readonly Dictionary<VisualElement, Action> boundSplineRowFeatureRefreshers = new Dictionary<VisualElement, Action>();

        // Track spline versions to detect changes (e.g., Closed state)
        Dictionary<SplineContainer, int> lastSplineVersions = new Dictionary<SplineContainer, int>();
        
        // Track preview texture cache keys (version + mode) for preview caching
        Dictionary<SplineContainer, int> previewTextureCacheKeys = new Dictionary<SplineContainer, int>();
        
        // Track last effective preview settings for early-exit optimization in UpdatePreviewTextures.
        int lastGlobalPreviewSettingsHash;

        // Track per-spline effective preview settings to detect thumbnail-invalidating changes.
        Dictionary<SplineContainer, int> lastSplinePreviewSettingsHashes = new Dictionary<SplineContainer, int>();

        // Throttle property checks to improve performance
        DateTime lastPropertyCheckTime = DateTime.MinValue;
        const float PROPERTY_CHECK_INTERVAL = 0.25f; // Check every 250ms instead of every frame

        // Hierarchy refresh throttling (EditorApplication.hierarchyChanged can fire very frequently)
        double lastHierarchyChangedTime = 0;
        bool hierarchyRefreshScheduled = false;
        bool hierarchyRefreshPendingWhilePaused = false;
        int lastSplineGroupStructureHash = 0;
        const double HIERARCHY_REFRESH_DEBOUNCE_SECONDS = 0.1;
        const double HIERARCHY_REFRESH_DEBOUNCE_GIZMOS_OFF_SECONDS = 0.75;

        // Fast live-updates while actively editing splines (independent of UpdateInterval)
        int lastSelectedSplineEditHash = 0;
        double lastSelectedSplineEditUpdateTime = 0;
        const double SPLINE_EDIT_MIN_UPDATE_INTERVAL_SECONDS = 1.0 / 30.0;
        const double SPLINE_EDIT_SETTLE_DELAY_SECONDS = 0.4;

        // Manager instances (using fully qualified names for Unity compilation)
        TerrainStateManager terrainStateManager;
        SplineDataManager splineDataManager;
        EditorUpdateManager editorUpdateManager;
        PreviewManager previewManager;

        /// <summary>
        /// Get all Terrain components from the target terrain group GameObject
        /// </summary>
        List<Terrain> GetAllTerrains()
        {
            var terrains = new List<Terrain>();
            
            // Migrate legacy targetTerrain if present
            if (targetTerrain != null && targetTerrainGroup == null)
            {
                // Auto-migrate: create or find GameObject parent
                if (targetTerrain.transform.parent != null)
                {
                    targetTerrainGroup = targetTerrain.transform.parent.gameObject;
                }
                else
                {
                    // Create a parent GameObject for the terrain
                    var groupGO = new GameObject("Terrain Group");
                    targetTerrain.transform.SetParent(groupGO.transform);
                    targetTerrainGroup = groupGO;
                }
                targetTerrain = null; // Clear legacy field after migration
            }
            
            if (targetTerrainGroup != null)
            {
                terrains.AddRange(targetTerrainGroup.GetComponentsInChildren<Terrain>());
            }
            
            return terrains;
        }

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<TerraSplinesWindow>(false, WindowTitle, true);
            window.AutoDetectTerrainAndSplineGroup();
        }

        /// <summary>
        /// Public method to trigger preview update from external sources (e.g., shortcuts).
        /// This method can be called when the window is open to force a preview rebuild.
        /// </summary>
        public void TriggerPreviewUpdate()
        {
            ResumeAutoPipeline();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("No target terrains assigned. Cannot update preview.");
                return;
            }

            // Ensure we have a baseline before rebuilding
            EnsureBaseline();

            // Rebuild the preview without applying to terrain (just for visualization)
            RebuildPreview(applyToTerrain: false);

            // Apply the preview to all terrains for visualization
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null && state.WorkingHeights != null)
                {
                    TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.WorkingHeights, state.WorkingHoles);
                    isPreviewAppliedToTerrain = true;
                }
            }

            // Update preview textures
            UpdatePreviewTextures();

            // Update preview texture UI elements
            UpdatePreviewTexturesUI();

            // Update height range display
            heightRangeNeedsUpdate = true;
        }

        /// <summary>
        /// Public method to refresh baseline data from current terrain state.
        /// This is called when terrain has been modified externally (e.g., from overlay).
        /// </summary>
        public void RefreshBaselineFromTerrain()
        {
            // Delegate to terrain state manager
            if (terrainStateManager != null)
            {
                terrainStateManager.RefreshBaselineFromTerrain();

                // Sync back to main window fields for backward compatibility
                baselineHeights = terrainStateManager.BaselineHeights;
                baselineTex = terrainStateManager.BaselineTexture;
                baselineHoles = terrainStateManager.BaselineHoles;
                baselineAlphamaps = terrainStateManager.BaselineAlphamaps;
                workingHeights = terrainStateManager.WorkingHeights;
                workingHoles = terrainStateManager.WorkingHoles;
                workingAlphamaps = terrainStateManager.WorkingAlphamaps;
                heightRangeNeedsUpdate = true;

                // Rebuild preview to reflect the new baseline
                RebuildPreview(applyToTerrain: false);
            }
        }

        /// <summary>
        /// Attempt to apply a specific spline using existing window state.
        /// Returns true if the spline was found and applied.
        /// </summary>
        public bool TryApplySpline(SplineContainer container)
        {
            if (container == null)
                return false;

            EnsureBaseline();

            var splineData = FindSplineItemForContainer(container);
            if (splineData == null)
            {
                RefreshChildren();
                splineData = FindSplineItemForContainer(container);
            }

            if (splineData == null)
            {
                Debug.LogWarning($"{LogPrefix} Could not find spline '{container.name}' in the Terra Splines window.");
                return false;
            }

            OnApplySplineClicked(splineData);
            return true;
        }

        public void RefreshSplineList()
        {
            RefreshChildren();
        }

        /// <summary>
        /// Public method to toggle pause/resume state from external sources (e.g., shortcuts).
        /// This method can be called when the window is open to toggle the pause/resume state.
        /// </summary>
        public void TogglePauseResume()
        {
            // Toggle the pause state
            updatesPaused = !updatesPaused;

            if (!updatesPaused)
            {
                // Clear any pending manual request when resuming auto updates
                manualUpdateRequested = false;
            }

            // Update editor update manager
            if (editorUpdateManager != null)
            {
                editorUpdateManager.UpdatesPaused = updatesPaused;
            }

            SaveEditorSettings();

            // Update UI elements to reflect the new state
            if (pauseResumeButton != null)
                pauseResumeButton.text = updatesPaused ? "Resume Updates" : "Pause Updates";
            if (updatePreviewOnceButton != null)
                updatePreviewOnceButton.SetEnabled(updatesPaused);
            UpdateDebugStatusIcon();
        }

        void ClearAutomaticPipelineDirtyFlags()
        {
            baselineDirty = false;
            previewDirty = false;
            applyDirty = false;
            previewTexturesNeedUpdate = false;
            heightRangeDirty = false;
        }

        void SuspendAutoPipelineForReferenceRefresh()
        {
            autoPipelineSuspendedForReferenceRefresh = true;
            ClearAutomaticPipelineDirtyFlags();
        }

        void ResumeAutoPipeline()
        {
            autoPipelineSuspendedForReferenceRefresh = false;
        }

        void RunWithSuppressedRefreshPreviewRequests(Action action)
        {
            if (action == null)
                return;

            suppressRefreshPreviewRequestCount++;
            try
            {
                action();
            }
            finally
            {
                suppressRefreshPreviewRequestCount--;
            }
        }

        void ExtendSuppressedRefreshPreviewRequestsToNextUiTick()
        {
            suppressRefreshPreviewRequestCount++;

            if (splinesListView != null)
            {
                splinesListView.schedule.Execute(() =>
                {
                    suppressRefreshPreviewRequestCount = Math.Max(0, suppressRefreshPreviewRequestCount - 1);
                }).ExecuteLater(0);
                return;
            }

            EditorApplication.delayCall += () =>
            {
                suppressRefreshPreviewRequestCount = Math.Max(0, suppressRefreshPreviewRequestCount - 1);
            };
        }

        public void CreateGUI()
        {
            try
            {
                // Load UXML and USS
                var visualTree = Resources.Load<VisualTreeAsset>(ResourcePathBase);
                var styleSheet = Resources.Load<StyleSheet>(ResourcePathBase);

                if (visualTree == null)
                {
                    Debug.LogError($"Failed to load {ResourcePathBase} UXML from Resources/");
                    return;
                }

                if (styleSheet == null)
                {
                    Debug.LogError($"Failed to load {ResourcePathBase} USS from Resources/");
                    return;
                }

                root = visualTree.CloneTree();
                root.styleSheets.Add(styleSheet);

                var listItemStyleSheet = Resources.Load<StyleSheet>("UI/TerraSplinesListItem");
                if (listItemStyleSheet != null)
                {
                    root.styleSheets.Add(listItemStyleSheet);
                }
                else
                {
                    Debug.LogWarning("Failed to load UI/TerraSplinesListItem USS from Resources/");
                }

                rootVisualElement.Add(root);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating GUI: {e.Message}\n{e.StackTrace}");
                return;
            }

            // Load editor settings before initializing UI to ensure saved preferences are used
            LoadEditorSettings();

            // Query UI elements
            QueryUIElements();

            // Setup ListView
            SetupSplinesListView();

            // Initialize UI values
            InitializeUIValues();

            // Bind events
            BindEvents();

            // Initialize state
            InitializeState();
        }

        void InitializeState()
        {
            // Initialize managers
            terrainStateManager = new TerrainStateManager();
            terrainStateManager.Initialize(this);

            splineDataManager = new SplineDataManager();
            splineDataManager.Initialize(this);

            editorUpdateManager = new EditorUpdateManager(splineDataManager, terrainStateManager);
            editorUpdateManager.Initialize(this);

            previewManager = new PreviewManager(terrainStateManager, splineDataManager);
            previewManager.Initialize(this);

            // Initialize baseline update strategy based on supportOutsideChanges setting
            baselineUpdateStrategy = supportOutsideChanges
                ? new DynamicBaselineUpdateStrategy()
                : new StaticBaselineUpdateStrategy();

            terrainStateManager.SetBaselineUpdateStrategy(baselineUpdateStrategy);

            AutoDetectTerrainAndSplineGroup();

            // Update the UI fields to show the found references
            if (root != null)
            {
                if (terrainField != null)
                    terrainField.value = targetTerrainGroup;
                if (splineGroupField != null)
                    splineGroupField.value = splineGroup;
                
                // Update warning icon after auto-detection
                UpdateTargetsWarning();
            }

            // Initialize terrain state manager with all terrains
            var terrains = GetAllTerrains();
            if (terrains.Count > 0)
            {
                terrainStateManager.SetTerrains(terrains);
                
                // Sync to window fields for backward compatibility (use first terrain)
                var firstTerrain = terrains[0];
                if (baselineHeights == null && terrainStateManager.BaselineHeights != null)
                {
                    baselineHeights = terrainStateManager.BaselineHeights;
                    workingHeights = terrainStateManager.WorkingHeights;
                    baselineTex = terrainStateManager.BaselineTexture;
                    needsFreshBaseline = false;
                }
            }

            // Initialize UI state if elements are available
            if (root != null)
            {
                UpdateUIState();
            }

            // Ensure the spline list populates at least once on open.
            // InitializeUIValues() assigns splineGroupField.value before events are bound, so the value-changed
            // callback may not fire during window creation. With hierarchyChanged debounced, refresh explicitly.
            if (splineGroup != null && splineGroup.gameObject != null)
            {
                RefreshChildren();
                lastSplineGroupStructureHash = ComputeSplineGroupStructureHash();
            }

            RefreshTerrainSplinesGroupsList();
        }

        void MarkPipelineDirty(bool baseline = false, bool preview = false, bool apply = false, bool previewTextures = false, bool heightRange = false, bool allowWhileAutoPipelineSuspended = false)
        {
            if (autoPipelineSuspendedForReferenceRefresh && !allowWhileAutoPipelineSuspended)
            {
                heightRangeNeedsUpdate |= heightRange;
                return;
            }

            baselineDirty |= baseline;
            previewDirty |= preview;
            applyDirty |= apply;
            previewTexturesNeedUpdate |= previewTextures;
            heightRangeDirty |= heightRange;
            heightRangeNeedsUpdate |= heightRange;

            RequestPipelineUpdate();
        }

        void RequestPipelineUpdate()
        {
            if (updatesPaused)
            {
                updatePendingWhilePaused = true;
                return;
            }

            if (updateScheduled) return;
            updateScheduled = true;
            EditorApplication.delayCall += ProcessPipelineUpdate;
        }

        void ProcessPipelineUpdate()
        {
            if (this == null)
            {
                updateScheduled = false;
                return;
            }

            if (IsInteractiveRefreshDeferred())
            {
                updateScheduled = false;
                RequestPipelineUpdate();
                return;
            }

            if (ShouldDeferExpensiveSplineOperations())
            {
                updateScheduled = false;
                return;
            }

            if (updatesPaused)
            {
                updatePendingWhilePaused = true;
                updateScheduled = false;
                return;
            }

            if (autoPipelineSuspendedForReferenceRefresh)
            {
                updateScheduled = false;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now - lastPipelineRunTime < PIPELINE_MIN_INTERVAL_SECONDS)
            {
                updateScheduled = false;
                RequestPipelineUpdate();
                return;
            }

            updateScheduled = false;
            lastPipelineRunTime = now;

            RunPipelineIfDirty();
            SyncObservedTerrainContentHashes();

            // If something dirtied the pipeline while we were running, schedule another pass.
            if (baselineDirty || previewDirty || applyDirty || previewTexturesNeedUpdate || heightRangeDirty)
            {
                RequestPipelineUpdate();
            }
        }

        void RunPipelineIfDirty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (terrainStateManager == null) return;

            ValidateAndResetReferences();
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;

            bool hasActiveSplines = HasActiveSplines();
            if (!hasActiveSplines)
            {
                if (isPreviewAppliedToTerrain)
                {
                    RunWithSuppressedTerrainChangeNotifications(() =>
                    {
                        foreach (var terrain in terrains)
                        {
                            var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                            if (state != null && state.BaselineHeights != null)
                            {
                                TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles, state.BaselineAlphamaps);
                                if (state.BaselineDetailLayers != null)
                                {
                                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.BaselineDetailLayers);
                                }

                                if (state.BaselineTreeInstances != null)
                                {
                                    TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.BaselineTreeInstances);
                                }
                            }
                        }
                    });
                    isPreviewAppliedToTerrain = false;
                }

                baselineDirty = false;
                previewDirty = false;
                applyDirty = false;
                previewTexturesNeedUpdate = false;
                heightRangeDirty = true;
                heightRangeNeedsUpdate = true;
                UpdateHeightRangeDisplay(forceRecalculate: true);
                return;
            }

            EnsureBaseline();

            if (needsFreshBaseline)
            {
                baselineDirty = true;
            }

            if (baselineDirty)
            {
                var baselineSw = System.Diagnostics.Stopwatch.StartNew();
                terrainStateManager?.UpdateBaseline(suppressBaselineUpdate);
                baselineSw.Stop();
                updateBaselineTimeMs = baselineSw.Elapsed.TotalMilliseconds;

                var firstTerrain = terrains[0];
                var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
                if (firstState != null)
                {
                    baselineHeights = firstState.BaselineHeights;
                    workingHeights = firstState.WorkingHeights;
                    baselineHoles = firstState.BaselineHoles;
                    workingHoles = firstState.WorkingHoles;
                    baselineAlphamaps = firstState.BaselineAlphamaps;
                    workingAlphamaps = firstState.WorkingAlphamaps;
                    paintSplineAlphaMask = firstState.PaintSplineAlphaMask;
                    holeSplineMask = firstState.HoleSplineMask;
                    UpdateCombinedBaselineTexture();
                    heightRangeNeedsUpdate = true;
                }
            }

            if (baselineDirty || previewDirty)
            {
                RebuildPreview(applyToTerrain: false);
                previewDirty = false;
            }

            if (baselineDirty || applyDirty)
            {
                var applySw = System.Diagnostics.Stopwatch.StartNew();
                RunWithSuppressedTerrainChangeNotifications(() =>
                {
                    foreach (var terrain in terrains)
                    {
                        var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                        if (state != null && state.WorkingHeights != null)
                        {
                            TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.WorkingHeights, state.WorkingHoles, state.WorkingAlphamaps);
                            isPreviewAppliedToTerrain = true;

                            if (state.WorkingDetailLayers != null)
                            {
                                TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.WorkingDetailLayers);
                            }

                            if (state.WorkingTreeInstances != null)
                            {
                                TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.WorkingTreeInstances);
                            }
                        }
                    }
                });
                applySw.Stop();
                applyPreviewTimeMs = applySw.Elapsed.TotalMilliseconds;
                applyDirty = false;
            }

            if (previewTexturesNeedUpdate)
            {
                var previewTexturesSw = System.Diagnostics.Stopwatch.StartNew();
                UpdatePreviewTextures();
                previewTexturesNeedUpdate = false;
                previewTexturesSw.Stop();
                updatePreviewTexturesTimeMs = previewTexturesSw.Elapsed.TotalMilliseconds;

                UpdatePreviewTexturesUI();
            }

            if (heightRangeDirty || heightRangeNeedsUpdate)
            {
                var heightRangeSw = System.Diagnostics.Stopwatch.StartNew();
                UpdateHeightRangeDisplay(forceRecalculate: true);
                heightRangeSw.Stop();
                updateHeightRangeTimeMs = heightRangeSw.Elapsed.TotalMilliseconds;
                heightRangeDirty = false;
            }

            baselineDirty = false;
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            try
            {
                if (modifications == null || modifications.Length == 0) return modifications;

                bool splineChanged = false;
                bool terrainChanged = false;
                bool inSplineEditMode = TerraSplinesTool.IsSplineEditModeActive();

                for (int i = 0; i < modifications.Length; i++)
                {
                    var current = modifications[i];
                    var target = current.currentValue?.target;
                    if (target == null) continue;

                    if (target is SplineContainer container)
                    {
                        if (splineGroup != null && container.transform != null && container.transform.IsChildOf(splineGroup))
                        {
                            splineChanged = true;
                        }
                        continue;
                    }

                    if (target is Transform tr)
                    {
                        var sc = tr.GetComponent<SplineContainer>();
                        if (sc != null && splineGroup != null && sc.transform != null && sc.transform.IsChildOf(splineGroup))
                        {
                            splineChanged = true;
                            continue;
                        }
                    }

                    if (target is TerrainData || target is TerrainLayer || target is Terrain)
                    {
                        terrainChanged = true;
                        continue;
                    }

                    if (target is Component comp)
                    {
                        var sc = comp.GetComponent<SplineContainer>();
                        if (sc != null && splineGroup != null && sc.transform != null && sc.transform.IsChildOf(splineGroup))
                        {
                            splineChanged = true;
                        }
                    }
                }

                if (terrainChanged)
                {
                    if (internalTerrainChangeSuppressionCount > 0 || EditorApplication.timeSinceStartup < internalTerrainChangeIgnoreUntil)
                        return modifications;

                    ResumeAutoPipeline();
                    MarkPipelineDirty(baseline: true, preview: true, apply: true, previewTextures: !inSplineEditMode, heightRange: true);
                }
                else if (splineChanged)
                {
                    if (internalSplineChangeSuppressionCount > 0)
                        return modifications;

                    ResumeAutoPipeline();
                    // Avoid thrashing spline thumbnail previews during continuous knot dragging; the terrain preview is the priority.
                    MarkPipelineDirty(preview: true, apply: true, previewTextures: !inSplineEditMode, heightRange: true);
                }
            }
            catch
            {
                // Ignore
            }

            return modifications;
        }

        void OnUndoRedoPerformed()
        {
            ResumeAutoPipeline();
            MarkPipelineDirty(baseline: true, preview: true, apply: true, previewTextures: true, heightRange: true);
        }

        void OnEnable()
        {
            EnsureGlobalDefaultCurves();

            // Avoid duplicate subscriptions across domain reloads / window re-enable.
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnSceneChanged;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            TerrainSplineSettingsOverlay.SettingsChanged -= OnOverlaySettingsChanged;
            TerrainSplineSettingsOverlay.SettingsChanged += OnOverlaySettingsChanged;
            OperationApplierBase.NoiseTextureCacheInvalidated -= OnNoiseTextureCacheInvalidated;
            OperationApplierBase.NoiseTextureCacheInvalidated += OnNoiseTextureCacheInvalidated;
            AssetThumbnailCache.PrefabThumbnailsChanged -= OnPrefabThumbnailsChanged;
            AssetThumbnailCache.PrefabThumbnailsChanged += OnPrefabThumbnailsChanged;
            TerrainSplinesGroup.GroupsChanged -= OnTerrainSplinesGroupsChanged;
            TerrainSplinesGroup.GroupsChanged += OnTerrainSplinesGroupsChanged;
        }

        void EnsureGlobalDefaultCurves()
        {
            globalBrushNoise ??= new BrushNoiseSettings();

            if (globalBrushNoise.noiseResponse == null || SplineStrokeSettings.IsBuiltInBrushNoiseResponseCurve(globalBrushNoise.noiseResponse))
            {
                globalBrushNoise.noiseResponse = SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve();
            }

            if (globalTreeHeightMultiplier == null || SplineStrokeSettings.IsBuiltInTreeHeightMultiplierCurve(globalTreeHeightMultiplier))
            {
                globalTreeHeightMultiplier = SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve();
            }
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            TerrainSplineSettingsOverlay.SettingsChanged -= OnOverlaySettingsChanged;
            OperationApplierBase.NoiseTextureCacheInvalidated -= OnNoiseTextureCacheInvalidated;
            AssetThumbnailCache.PrefabThumbnailsChanged -= OnPrefabThumbnailsChanged;
            TerrainSplinesGroup.GroupsChanged -= OnTerrainSplinesGroupsChanged;

            isPreviewAppliedToTerrain = false;
            TerraSplinesTool.ClearShapeHeightmapCache();
            lastHadActiveSplines = false;
            ClearUndoRedoStacks(refreshUI: false);

            var allSplines = GetAllSplineItems();
            foreach (var item in allSplines)
            {
                if (item.previewTexture != null)
                {
                    DestroyImmediate(item.previewTexture);
                    item.previewTexture = null;
                }
            }
            
            // Clear preview texture cache on disable
            previewTextureCacheKeys.Clear();

            previewTexturesNeedUpdate = false;

            // Clean up heightmap states
            originalHeights = null;
            baselineHeights = null;
            workingHeights = null;

            // Cleanup managers
            terrainStateManager?.OnDestroy();
            splineDataManager?.OnDestroy();
            editorUpdateManager?.OnDestroy();
            previewManager?.OnDestroy();
        }

        void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            OperationApplierBase.NoiseTextureCacheInvalidated -= OnNoiseTextureCacheInvalidated;
            AssetThumbnailCache.PrefabThumbnailsChanged -= OnPrefabThumbnailsChanged;
            TerrainSplinesGroup.GroupsChanged -= OnTerrainSplinesGroupsChanged;
        }

        void OnNoiseTextureCacheInvalidated()
        {
            RefreshPreview();
        }

        void OnPrefabThumbnailsChanged()
        {
            PopulateTreePrototypePalette();
            splinesListView?.RefreshItems();
            Repaint();
        }

        /// <summary>
        /// Load editor settings from EditorPrefs to restore session state
        /// </summary>
        void LoadEditorSettings()
        {
            // Load SupportOutsideChanges
            supportOutsideChanges = LoadBoolPref("SupportOutsideChanges", true);

            // Load BackendType
            BackendType savedBackendType = (BackendType)LoadIntPref("BackendType", (int)BackendType.GPU);
            TerraSplinesTool.CurrentBackendType = savedBackendType;

            // Load UpdatesPaused
            updatesPaused = LoadBoolPref("UpdatesPaused", false);

            // Load UpdateInterval
            updateInterval = LoadFloatPref("UpdateInterval", 0.1f);

            // Load feature toggle states
            featureHoleEnabled = LoadBoolPref("FeatureHoleEnabled", true);
            featurePaintEnabled = LoadBoolPref("FeaturePaintEnabled", true);
            featureDetailEnabled = LoadBoolPref("FeatureDetailEnabled", true);
            featureTreeEnabled = LoadBoolPref("FeatureTreeEnabled", true);
            featureGizmosEnabled = LoadBoolPref("FeatureGizmosEnabled", true);
            heightmapPreviewEnabled = LoadBoolPref("HeightmapPreviewEnabled", true);
        }

        /// <summary>
        /// Save current editor settings to EditorPrefs for session persistence
        /// </summary>
        void SaveEditorSettings()
        {
            EditorPrefs.SetBool(BuildEditorPrefsKey("SupportOutsideChanges"), supportOutsideChanges);
            EditorPrefs.SetInt(BuildEditorPrefsKey("BackendType"), (int)TerraSplinesTool.CurrentBackendType);
            EditorPrefs.SetBool(BuildEditorPrefsKey("UpdatesPaused"), updatesPaused);
            EditorPrefs.SetFloat(BuildEditorPrefsKey("UpdateInterval"), updateInterval);
            EditorPrefs.SetBool(BuildEditorPrefsKey("FeatureHoleEnabled"), featureHoleEnabled);
            EditorPrefs.SetBool(BuildEditorPrefsKey("FeaturePaintEnabled"), featurePaintEnabled);
            EditorPrefs.SetBool(BuildEditorPrefsKey("FeatureDetailEnabled"), featureDetailEnabled);
            EditorPrefs.SetBool(BuildEditorPrefsKey("FeatureTreeEnabled"), featureTreeEnabled);
            EditorPrefs.SetBool(BuildEditorPrefsKey("FeatureGizmosEnabled"), featureGizmosEnabled);
            EditorPrefs.SetBool(BuildEditorPrefsKey("HeightmapPreviewEnabled"), heightmapPreviewEnabled);
        }

        TerrainSnapshot CaptureSnapshot()
        {
            var terrains = GetAllTerrains();
            var firstTerrainState = terrains.Count > 0 ? terrainStateManager?.TerrainStates?.GetValueOrDefault(terrains[0]) : null;

            if (baselineHeights == null && baselineHoles == null && baselineAlphamaps == null && firstTerrainState?.BaselineTreeInstances == null)
            {
                return null;
            }

            return new TerrainSnapshot
            {
                heights = baselineHeights != null ? TerraSplinesTool.CopyHeights(baselineHeights) : null,
                holes = baselineHoles != null ? TerraSplinesTool.CopyHoles(baselineHoles) : null,
                alphamaps = baselineAlphamaps != null ? TerraSplinesTool.CopyAlphamaps(baselineAlphamaps) : null,
                treeInstances = TerraSplinesTool.CopyTreeInstances(firstTerrainState?.BaselineTreeInstances)
            };
        }

        void PushUndoSnapshot()
        {
            var snapshot = CaptureSnapshot();
            if (snapshot == null)
            {
                return;
            }

            undoStack.Push(snapshot);
            redoStack.Clear();
            UpdateUIState();
        }

        void RestoreSnapshot(TerrainSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            baselineHeights = snapshot.heights != null ? TerraSplinesTool.CopyHeights(snapshot.heights) : null;
            workingHeights = baselineHeights != null ? TerraSplinesTool.CopyHeights(baselineHeights) : null;

            baselineHoles = snapshot.holes != null ? TerraSplinesTool.CopyHoles(snapshot.holes) : null;
            workingHoles = baselineHoles != null ? TerraSplinesTool.CopyHoles(baselineHoles) : null;

            baselineAlphamaps = snapshot.alphamaps != null ? TerraSplinesTool.CopyAlphamaps(snapshot.alphamaps) : null;
            workingAlphamaps = baselineAlphamaps != null ? TerraSplinesTool.CopyAlphamaps(baselineAlphamaps) : null;

            var terrains = GetAllTerrains();
            if (terrains.Count > 0)
            {
                var firstTerrainState = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrains[0]);
                if (firstTerrainState != null)
                {
                    firstTerrainState.BaselineTreeInstances = TerraSplinesTool.CopyTreeInstances(snapshot.treeInstances);
                    firstTerrainState.WorkingTreeInstances = TerraSplinesTool.CopyTreeInstances(snapshot.treeInstances);
                }
            }

            // Update combined baseline texture from all terrains
            UpdateCombinedBaselineTexture();
            previewTexturesNeedUpdate = true;
            heightRangeNeedsUpdate = true;
        }

        void ApplyBaselineToTerrain()
        {
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;

            // Apply baseline to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null) continue;

                var td = terrain.terrainData;

                if (state.BaselineHeights != null)
                {
                    td.SetHeights(0, 0, state.BaselineHeights);
                }

                if (state.BaselineHoles != null)
                {
                    var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, state.BaselineHoles);
                    td.SetHoles(0, 0, terrainHoles);
                }

                if (state.BaselineAlphamaps != null)
                {
                    td.SetAlphamaps(0, 0, state.BaselineAlphamaps);
                }

                if (state.BaselineDetailLayers != null && featureDetailEnabled)
                {
                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.BaselineDetailLayers);
                }

                if (state.BaselineTreeInstances != null && featureTreeEnabled)
                {
                    TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.BaselineTreeInstances);
                }

                if (state.BaselineHeights != null)
                {
                    if (state.BaselineHoles != null && state.BaselineAlphamaps != null)
                    {
                        TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles, state.BaselineAlphamaps);
                    }
                    else if (state.BaselineHoles != null)
                    {
                        TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles);
                    }
                    else
                    {
                        TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights);
                    }
                }
            }
        }

        void ClearUndoRedoStacks(bool refreshUI = true)
        {
            undoStack.Clear();
            redoStack.Clear();
            if (refreshUI)
            {
                UpdateUIState();
            }
        }

        public void SetTargets(GameObject target, Transform splines)
        {
            if (root == null)
            {
                targetTerrainGroup = target;
                splineGroup = splines;
                return;
            }

            ApplyTargetReferences(target, splines);
        }
    }
}
