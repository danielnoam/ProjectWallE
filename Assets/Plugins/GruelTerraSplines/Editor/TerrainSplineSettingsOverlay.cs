using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace GruelTerraSplines
{
    /// <summary>
    /// Unity Overlay that displays terrain spline settings in Scene View when a GameObject 
    /// with TerrainSplineSettings component is selected.
    /// </summary>
    [Overlay(typeof(SceneView), "Terra Splines Settings", true)]
    [Icon("Assets/Plugins/GruelTerraSplines/Resources/Icons/SplineSettings.png")]
    public class TerrainSplineSettingsOverlay : Overlay
    {
        const string ResourcePathBase = "UI/TerrainSplineSettingsOverlay";
        const string OverlayBottomDockedClass = "overlay-bottom-docked";
        const string TabContentStackName = "tab-content-stack";
        const string BottomDockOverrideHostName = "bottom-dock-override-host";
        const string OverrideToggleRowClass = "override-toggle-row";

        public static event Action<TerrainSplineSettings> SettingsChanged;
        public static event Action FeatureFlagsChanged;

        public static void NotifySettingsChanged(TerrainSplineSettings settings)
        {
            if (settings == null) return;
            SettingsChanged?.Invoke(settings);
        }

        public static void NotifyFeatureFlagsChanged()
        {
            FeatureFlagsChanged?.Invoke();
        }
        const string EditorPrefsPrefix = "GruelTerraSplines";
        const string LegacyEditorPrefsPrefix = "DKSplineTerrain";
        const string SelectOnCreatePrefKey = "OverlaySelectOnCreate";

        private TerrainSplineSettings targetSettings;
        private VisualElement root;
        private VisualElement overlayRootElement;

        private VisualElement multiSelectionMessage;
        private VisualElement settingsContainer;
        private VisualElement noSelectionContainer;
        private VisualElement overlayHeaderElement;
        private VisualElement splineTabsContainer;
        private VisualElement tabContentStack;
        private VisualElement bottomDockOverrideHost;

        // UI Elements
        private Label overlayTitle;
        private Button applyButton;
        private Button duplicateButton;
        private Button deleteButton;
        private Button openWindowButton;
        private Button openDocsButton;
        private Button createPathButton;
        private Button createShapeButton;
        private Button drawCustomButton;
        private Image applyIcon;
        private Image duplicateIcon;
        private Image deleteIcon;
        private Image openWindowIcon;
        private Image openDocsIcon;
        private Image createPathIcon;
        private Image createShapeIcon;
        private Image drawCustomIcon;
        private Toggle selectOnCreateToggle;
        private Toggle overrideModeToggle;
        private Button pathModeButton;
        private Button shapeModeButton;
        private Toggle opHeightToggle;
        private Toggle opPaintToggle;
        private Toggle opHoleToggle;
        private Toggle opFillToggle;
        private Toggle opAddDetailToggle;
        private Toggle opRemoveDetailToggle;
        private Toggle opAddTreeToggle;
        private Toggle opRemoveTreeToggle;
        private Label operationWarningLabel;
        private Image globalModeIcon;

        private bool selectOnCreate = true;

        private const string WarningIconGlyph = "⚠";
        private const string FeatureDisabledTooltip = "Feature disabled";

        private readonly Color opSelectedColor = Color.white;
        // Non-selected/available icon tint
        private readonly Color opAvailableColor = new Color(0.47f, 0.47f, 0.47f, 1f);
        // Blocked/disabled icon tint (matches window)
        private readonly Color opBlockedColor = new Color(1f, 0.32f, 0.29f, 1f);
        private readonly Color noiseInvertOnColor = Color.white;
        private readonly Color noiseInvertOffColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color noiseEmptyColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color noiseActiveColor = Color.white;

        private void ConfigureWarningIconLabel(Label label, string tooltip = FeatureDisabledTooltip)
        {
            if (label == null) return;

            label.text = WarningIconGlyph;
            label.tooltip = tooltip;
            label.style.color = opBlockedColor;
            label.style.fontSize = 14f;
            label.style.marginLeft = 6f;
            label.style.marginTop = 0f;
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private static string GetDisabledFeatureWarningTooltip(bool requiresPaint, bool requiresHole, bool requiresFill, bool requiresDetail, bool requiresTree,
            bool holeEnabled, bool paintEnabled, bool detailEnabled, bool treeEnabled)
        {
            var disabledFeatures = new List<string>();

            if (requiresPaint && !paintEnabled)
                disabledFeatures.Add("Paint");

            if ((requiresHole || requiresFill) && !holeEnabled)
                disabledFeatures.Add("Holes");

            if (requiresDetail && !detailEnabled)
                disabledFeatures.Add("Detail");

            if (requiresTree && !treeEnabled)
                disabledFeatures.Add("Trees");

            if (disabledFeatures.Count == 0)
                return FeatureDisabledTooltip;

            return $"{FeatureDisabledTooltip}: {string.Join(", ", disabledFeatures)}";
        }

        private void SetWarningIconState(Label label, bool shouldShow, string tooltip)
        {
            if (label == null) return;

            ConfigureWarningIconLabel(label, tooltip);
            label.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Toggle overrideBrushToggle;
        private Image brushPreviewImage;
        private Slider brushSizeSlider;
        private FloatField brushSizeField;
        private Slider brushHardnessSlider;
        private FloatField brushHardnessField;
        private Slider strengthSlider;
        private FloatField strengthField;
        private Slider sampleStepSlider;
        private FloatField sampleStepField;
        private Toggle overrideSizeMultiplierToggle;
        private CurveField brushSizeMultiplierCurve;
        private VisualElement brushSizeMultiplierSection;
        private ObjectField brushNoiseTextureField;
        private Slider brushNoiseStrengthSlider;
        private FloatField brushNoiseStrengthField;
        private Slider brushNoiseEdgeSlider;
        private FloatField brushNoiseEdgeField;
        private Slider brushNoiseSizeSlider;
        private FloatField brushNoiseSizeField;
        private Vector2Field brushNoiseOffsetField;
        private CurveField brushNoiseResponseCurve;
        private Toggle brushNoiseInvertToggle;
        private Foldout brushNoiseFoldout;

        private Toggle overridePaintToggle;
        private VisualElement paintLayerPalette;
        private Slider paintStrengthSlider;
        private FloatField paintStrengthField;
        private Slider paintBlendSlider;
        private FloatField paintBlendField;
        private Slider paintThresholdSlider;
        private FloatField paintThresholdField;
        private ObjectField paintNoiseTextureField;
        private Slider paintNoiseStrengthSlider;
        private FloatField paintNoiseStrengthField;
        private Slider paintNoiseEdgeSlider;
        private FloatField paintNoiseEdgeField;
        private Slider paintNoiseSizeSlider;
        private FloatField paintNoiseSizeField;
        private Vector2Field paintNoiseOffsetField;
        private CurveField paintNoiseResponseCurve;
        private Toggle paintNoiseInvertToggle;
        private Foldout paintNoiseFoldout;

        private Toggle overrideDetailToggle;
        private VisualElement detailLayerPalette;
        private Slider detailStrengthSlider;
        private FloatField detailStrengthField;
        private Slider detailFalloffSlider;
        private FloatField detailFalloffField;
        private SliderInt detailSpreadSlider;
        private IntegerField detailSpreadField;
        private Slider detailSlopeLimitSlider;

        private FloatField detailSlopeLimitField;
        private Slider detailRemoveThresholdSlider;
        private FloatField detailRemoveThresholdField;
        private ObjectField detailNoiseTextureField;
        private Slider detailNoiseSizeSlider;
        private FloatField detailNoiseSizeField;
        private Vector2Field detailNoiseOffsetField;
        private Slider detailNoiseThresholdSlider;
        private FloatField detailNoiseThresholdField;
        private Toggle detailNoiseInvertToggle;
        private Foldout detailNoiseFoldout;

        private Toggle overrideTreeToggle;
        private VisualElement treePrototypePalette;
        private Slider treeStrengthSlider;
        private FloatField treeStrengthField;
        private Slider treeFalloffSlider;
        private FloatField treeFalloffField;
        private CurveField treeHeightMultiplierCurve;
        private Slider treeSlopeLimitSlider;
        private FloatField treeSlopeLimitField;
        private Slider treeRemoveThresholdSlider;
        private FloatField treeRemoveThresholdField;
        private ObjectField treeNoiseTextureField;
        private Slider treeNoiseSizeSlider;
        private FloatField treeNoiseSizeField;
        private Vector2Field treeNoiseOffsetField;
        private Slider treeNoiseThresholdSlider;
        private FloatField treeNoiseThresholdField;
        private Toggle treeNoiseInvertToggle;
        private Foldout treeNoiseFoldout;

        // Tab elements
        private Button modeTab;
        private Button brushTab;
        private Button paintTab;
        private Button detailTab;
        private Button treeTab;
        private VisualElement modeContent;
        private VisualElement brushContent;
        private VisualElement paintContent;
        private VisualElement detailContent;
        private VisualElement treeContent;

        // State
        private int activeTab = 0; // 0=Mode, 1=Brush, 2=Paint, 3=Detail, 4=Trees
        private Terrain targetTerrain;
        private bool? lastWindowOpenState;
        private bool dockStateRefreshQueued;
        private bool? lastOverlayFloating;
        private bool? lastOverlayInToolbar;
        private OverlayDockEdge currentDockEdge = OverlayDockEdge.Unknown;

        private enum OverlayDockEdge
        {
            Unknown,
            Top,
            Bottom,
            Left,
            Right
        }

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

        public override VisualElement CreatePanelContent()
        {
            ResetPanelStateForRebuild();

            // Load UXML and USS
            var visualTree = Resources.Load<VisualTreeAsset>(ResourcePathBase);
            var styleSheet = Resources.Load<StyleSheet>(ResourcePathBase);

            if (visualTree == null)
            {
                Debug.LogError($"Failed to load {ResourcePathBase} UXML from Resources/");
                return new Label("Failed to load overlay template");
            }

            if (styleSheet == null)
            {
                Debug.LogError($"Failed to load {ResourcePathBase} USS from Resources/");
                return new Label("Failed to load overlay styles");
            }

            root = visualTree.CloneTree();
            root.styleSheets.Add(styleSheet);

            // Query UI elements
            QueryUIElements();

            // Setup event handlers
            SetupEventHandlers();
            InitializeDockResponsiveLayout();

            // Subscribe to selection changes
            Selection.selectionChanged += OnSelectionChanged;
            SettingsChanged += OnExternalSettingsChanged;
            FeatureFlagsChanged += OnFeatureFlagsChanged;
            AssetThumbnailCache.PrefabThumbnailsChanged += OnPrefabThumbnailsChanged;
            EditorApplication.update += OnEditorUpdate;

            // Initial update
            OnSelectionChanged();

            return root;
        }

        private void ResetPanelStateForRebuild()
        {
            if (root != null)
            {
                root.UnregisterCallback<AttachToPanelEvent>(OnOverlayAttachToPanel);
                root.UnregisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);
            }

            Selection.selectionChanged -= OnSelectionChanged;
            SettingsChanged -= OnExternalSettingsChanged;
            FeatureFlagsChanged -= OnFeatureFlagsChanged;
            AssetThumbnailCache.PrefabThumbnailsChanged -= OnPrefabThumbnailsChanged;
            EditorApplication.update -= OnEditorUpdate;

            root = null;
            overlayRootElement = null;
            multiSelectionMessage = null;
            settingsContainer = null;
            noSelectionContainer = null;
            overlayHeaderElement = null;
            splineTabsContainer = null;
            tabContentStack = null;
            bottomDockOverrideHost = null;

            lastOverlayFloating = null;
            lastOverlayInToolbar = null;
            currentDockEdge = OverlayDockEdge.Unknown;
            dockStateRefreshQueued = false;
        }

        private void QueryUIElements()
        {
            overlayRootElement = root.Q<VisualElement>(className: "spline-settings-overlay") ?? root;

            // Multi-selection message
            multiSelectionMessage = root.Q<VisualElement>("multi-selection-message");
            settingsContainer = root.Q<VisualElement>("settings-container");
            noSelectionContainer = root.Q<VisualElement>("no-selection-container");
            EnsureNoSelectionUI();

            // Header elements
            overlayHeaderElement = settingsContainer?.Q<VisualElement>(className: "overlay-header");
            overlayTitle = root.Q<Label>("overlay-title");
            applyButton = root.Q<Button>("apply-button");
            duplicateButton = root.Q<Button>("duplicate-button");
            deleteButton = root.Q<Button>("delete-button");
            applyIcon = root.Q<Image>("apply-icon");
            duplicateIcon = root.Q<Image>("duplicate-icon");
            deleteIcon = root.Q<Image>("delete-icon");
            openWindowButton = root.Q<Button>("open-window-button");
            openDocsButton = root.Q<Button>("open-docs-button");
            createPathButton = root.Q<Button>("create-path-button");
            createShapeButton = root.Q<Button>("create-shape-button");
            drawCustomButton = root.Q<Button>("draw-custom-button");
            openWindowIcon = root.Q<Image>("open-window-icon");
            openDocsIcon = root.Q<Image>("open-docs-icon");
            createPathIcon = root.Q<Image>("create-path-icon");
            createShapeIcon = root.Q<Image>("create-shape-icon");
            drawCustomIcon = root.Q<Image>("draw-custom-icon");
            selectOnCreateToggle = root.Q<Toggle>("select-on-create-toggle");

            // Mode tab elements
            overrideModeToggle = root.Q<Toggle>("override-mode-toggle");
            pathModeButton = root.Q<Button>("path-mode-button");
            shapeModeButton = root.Q<Button>("shape-mode-button");
            EnsureModeButtonContent(pathModeButton, "path", "Path");
            EnsureModeButtonContent(shapeModeButton, "shape", "Shape");
            var operationContainer = root.Q<VisualElement>("operation-dropdown");
            if (operationContainer != null)
            {
                operationContainer.Clear();
                operationContainer.RemoveFromClassList("operation-dropdown");
                operationContainer.style.flexDirection = FlexDirection.Row;
                operationContainer.style.flexWrap = Wrap.NoWrap;
                operationContainer.style.alignItems = Align.Center;
                operationContainer.style.justifyContent = Justify.FlexStart;
                operationContainer.style.marginTop = 0;
                operationContainer.style.marginBottom = 0;
                operationContainer.style.minHeight = 22;
                operationContainer.style.height = 22;
                operationContainer.style.flexShrink = 0;

                globalModeIcon = new Image();
                globalModeIcon.AddToClassList("operation-icon");
                globalModeIcon.AddToClassList("global-operation-icon");
                globalModeIcon.style.display = DisplayStyle.None;
                operationContainer.Add(globalModeIcon);

                opHeightToggle = CreateOperationIconToggle("height", "Height");
                opPaintToggle = CreateOperationIconToggle("paint", "Paint");
                opHoleToggle = CreateOperationIconToggle("hole", "Hole");
                opFillToggle = CreateOperationIconToggle("fill", "Fill");
                opAddDetailToggle = CreateOperationIconToggle("grass", "Add Detail");
                opRemoveDetailToggle = CreateOperationIconToggle("cut", "Remove Detail");
                opAddTreeToggle = CreateOperationIconToggle("tree", "Add Tree");
                opRemoveTreeToggle = CreateOperationIconToggle("axe", "Remove Tree");

                var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
                foreach (var toggle in toggles)
                {
                    toggle.style.display = DisplayStyle.Flex;
                    operationContainer.Add(toggle);
                }

                UpdateLastOperationToggleClass(operationContainer);

                opHeightToggle?.SetValueWithoutNotify(true);
                opPaintToggle?.SetValueWithoutNotify(false);
                opHoleToggle?.SetValueWithoutNotify(false);
                opFillToggle?.SetValueWithoutNotify(false);
                opAddDetailToggle?.SetValueWithoutNotify(false);
                opRemoveDetailToggle?.SetValueWithoutNotify(false);
                opAddTreeToggle?.SetValueWithoutNotify(false);
                opRemoveTreeToggle?.SetValueWithoutNotify(false);

                foreach (var toggle in toggles)
                {
                    RefreshOperationToggleVisual(toggle);
                }
            }

            operationWarningLabel = root.Q<Label>("operation-warning");
            ConfigureWarningIconLabel(operationWarningLabel);

            // Brush tab elements
            overrideBrushToggle = root.Q<Toggle>("override-brush-toggle");
            brushPreviewImage = root.Q<Image>("brush-preview-image");
            brushSizeSlider = root.Q<Slider>("brush-size-slider");
            brushSizeField = root.Q<FloatField>("brush-size-field");
            brushHardnessSlider = root.Q<Slider>("brush-hardness-slider");
            brushHardnessField = root.Q<FloatField>("brush-hardness-field");
            strengthSlider = root.Q<Slider>("strength-slider");
            strengthField = root.Q<FloatField>("strength-field");
            sampleStepSlider = root.Q<Slider>("sample-step-slider");
            sampleStepField = root.Q<FloatField>("sample-step-field");
            overrideSizeMultiplierToggle = root.Q<Toggle>("override-size-multiplier-toggle");
            brushSizeMultiplierCurve = root.Q<CurveField>("brush-size-multiplier-curve");
            brushSizeMultiplierSection = root.Q<VisualElement>("brush-size-multiplier-section");
            brushNoiseTextureField = root.Q<ObjectField>("brush-noise-texture");
            brushNoiseStrengthSlider = root.Q<Slider>("brush-noise-strength-slider");
            brushNoiseStrengthField = root.Q<FloatField>("brush-noise-strength-field");
            brushNoiseEdgeSlider = root.Q<Slider>("brush-noise-edge-slider");
            brushNoiseEdgeField = root.Q<FloatField>("brush-noise-edge-field");
            brushNoiseSizeSlider = root.Q<Slider>("brush-noise-size-slider");
            brushNoiseSizeField = root.Q<FloatField>("brush-noise-size-field");
            brushNoiseOffsetField = root.Q<Vector2Field>("brush-noise-offset-field");
            brushNoiseResponseCurve = root.Q<CurveField>("brush-noise-response-curve");
            brushNoiseInvertToggle = root.Q<Toggle>("brush-noise-invert-toggle");
            brushNoiseFoldout = root.Q<Foldout>("brush-noise-foldout");

            // Paint tab elements
            overridePaintToggle = root.Q<Toggle>("override-paint-toggle");
            paintLayerPalette = root.Q<VisualElement>("paint-layer-palette");
            paintStrengthSlider = root.Q<Slider>("paint-strength-slider");
            paintStrengthField = root.Q<FloatField>("paint-strength-field");
            paintBlendSlider = root.Q<Slider>("paint-blend-slider");
            paintBlendField = root.Q<FloatField>("paint-blend-field");
            paintThresholdSlider = root.Q<Slider>("paint-threshold-slider");
            paintThresholdField = root.Q<FloatField>("paint-threshold-field");
            paintNoiseTextureField = root.Q<ObjectField>("paint-noise-texture");
            paintNoiseStrengthSlider = root.Q<Slider>("paint-noise-strength-slider");
            paintNoiseStrengthField = root.Q<FloatField>("paint-noise-strength-field");
            paintNoiseEdgeSlider = root.Q<Slider>("paint-noise-edge-slider");
            paintNoiseEdgeField = root.Q<FloatField>("paint-noise-edge-field");
            paintNoiseSizeSlider = root.Q<Slider>("paint-noise-size-slider");
            paintNoiseSizeField = root.Q<FloatField>("paint-noise-size-field");
            paintNoiseOffsetField = root.Q<Vector2Field>("paint-noise-offset-field");
            paintNoiseResponseCurve = root.Q<CurveField>("paint-noise-response-curve");
            paintNoiseInvertToggle = root.Q<Toggle>("paint-noise-invert-toggle");
            paintNoiseFoldout = root.Q<Foldout>("paint-noise-foldout");

            // Detail tab elements
            overrideDetailToggle = root.Q<Toggle>("override-detail-toggle");
            detailLayerPalette = root.Q<VisualElement>("detail-layer-palette");
            var detailModeContainer = root.Q<VisualElement>("detail-mode-dropdown");
            detailModeContainer?.RemoveFromHierarchy();
            detailStrengthSlider = root.Q<Slider>("detail-strength-slider");
            detailStrengthField = root.Q<FloatField>("detail-strength-field");
            detailFalloffSlider = root.Q<Slider>("detail-falloff-slider");
            detailFalloffField = root.Q<FloatField>("detail-falloff-field");
            detailSpreadSlider = root.Q<SliderInt>("detail-spread-slider");
            detailSpreadField = root.Q<IntegerField>("detail-spread-field");
            detailSlopeLimitSlider = root.Q<Slider>("detail-slope-slider");
            detailSlopeLimitField = root.Q<FloatField>("detail-slope-field");
            detailRemoveThresholdSlider = root.Q<Slider>("detail-remove-threshold-slider");
            detailRemoveThresholdField = root.Q<FloatField>("detail-remove-threshold-field");
            detailNoiseTextureField = root.Q<ObjectField>("detail-noise-texture");
            detailNoiseSizeSlider = root.Q<Slider>("detail-noise-size-slider");
            detailNoiseSizeField = root.Q<FloatField>("detail-noise-size-field");
            detailNoiseOffsetField = root.Q<Vector2Field>("detail-noise-offset-field");
            detailNoiseThresholdSlider = root.Q<Slider>("detail-noise-threshold-slider");
            detailNoiseThresholdField = root.Q<FloatField>("detail-noise-threshold-field");
            detailNoiseInvertToggle = root.Q<Toggle>("detail-noise-invert-toggle");
            detailNoiseFoldout = root.Q<Foldout>("detail-noise-foldout");

            // Tree tab elements
            overrideTreeToggle = root.Q<Toggle>("override-tree-toggle");
            treePrototypePalette = root.Q<VisualElement>("tree-prototype-palette");
            treeStrengthSlider = root.Q<Slider>("tree-strength-slider");
            treeStrengthField = root.Q<FloatField>("tree-strength-field");
            treeFalloffSlider = root.Q<Slider>("tree-falloff-slider");
            treeFalloffField = root.Q<FloatField>("tree-falloff-field");
            treeHeightMultiplierCurve = root.Q<CurveField>("tree-height-multiplier-curve");
            treeSlopeLimitSlider = root.Q<Slider>("tree-slope-slider");
            treeSlopeLimitField = root.Q<FloatField>("tree-slope-field");
            treeRemoveThresholdSlider = root.Q<Slider>("tree-remove-threshold-slider");
            treeRemoveThresholdField = root.Q<FloatField>("tree-remove-threshold-field");
            treeNoiseTextureField = root.Q<ObjectField>("tree-noise-texture");
            treeNoiseSizeSlider = root.Q<Slider>("tree-noise-size-slider");
            treeNoiseSizeField = root.Q<FloatField>("tree-noise-size-field");
            treeNoiseOffsetField = root.Q<Vector2Field>("tree-noise-offset-field");
            treeNoiseThresholdSlider = root.Q<Slider>("tree-noise-threshold-slider");
            treeNoiseThresholdField = root.Q<FloatField>("tree-noise-threshold-field");
            treeNoiseInvertToggle = root.Q<Toggle>("tree-noise-invert-toggle");
            treeNoiseFoldout = root.Q<Foldout>("tree-noise-foldout");
            InsertNoiseFoldoutIcon(brushNoiseFoldout);
            InsertNoiseFoldoutIcon(paintNoiseFoldout);
            InsertNoiseFoldoutIcon(detailNoiseFoldout);

            CurvePresetDropdownUtility.AttachDropdown(brushSizeMultiplierCurve, CurvePresetListType.BrushSizeMultiplier);
            CurvePresetDropdownUtility.AttachDropdown(brushNoiseResponseCurve, CurvePresetListType.BrushNoiseContrast);
            CurvePresetDropdownUtility.AttachDropdown(paintNoiseResponseCurve, CurvePresetListType.PaintNoiseContrast);
            CurvePresetDropdownUtility.AttachDropdown(treeHeightMultiplierCurve, CurvePresetListType.TreeHeightDistribution);
            InsertNoiseFoldoutIcon(treeNoiseFoldout);

            // Tab elements
            modeTab = root.Q<Button>("mode-tab");
            brushTab = root.Q<Button>("brush-tab");
            paintTab = root.Q<Button>("paint-tab");
            detailTab = root.Q<Button>("detail-tab");
            treeTab = root.Q<Button>("tree-tab");
            splineTabsContainer = settingsContainer?.Q<VisualElement>(className: "spline-tabs");
            EnsureTabButtonContent(modeTab, "Mode");
            EnsureTabButtonContent(brushTab, "Brush");
            EnsureTabButtonContent(paintTab, "Paint");
            EnsureTabButtonContent(detailTab, "Detail");
            EnsureTabButtonContent(treeTab, "Trees");
            modeContent = root.Q<VisualElement>("mode-content");
            brushContent = root.Q<VisualElement>("brush-content");
            paintContent = root.Q<VisualElement>("paint-content");
            detailContent = root.Q<VisualElement>("detail-content");
            treeContent = root.Q<VisualElement>("tree-content");

            AddOverrideToggleClass(overrideModeToggle);
            AddOverrideToggleClass(overrideBrushToggle);
            AddOverrideToggleClass(overridePaintToggle);
            AddOverrideToggleClass(overrideDetailToggle);
            AddOverrideToggleClass(overrideTreeToggle);
        }

        private static void AddOverrideToggleClass(Toggle toggle)
        {
            if (toggle == null)
                return;

            if (!toggle.ClassListContains(OverrideToggleRowClass))
                toggle.AddToClassList(OverrideToggleRowClass);
        }

        private void InitializeDockResponsiveLayout()
        {
            EnsureTabLayoutContainers();

            if (root == null)
                return;

            root.RegisterCallback<AttachToPanelEvent>(OnOverlayAttachToPanel);
            root.RegisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);
            ScheduleDockStateRefresh();
        }

        private void OnOverlayAttachToPanel(AttachToPanelEvent evt)
        {
            ScheduleDockStateRefresh();
        }

        private void OnOverlayGeometryChanged(GeometryChangedEvent evt)
        {
            ScheduleDockStateRefresh();
        }

        private void ScheduleDockStateRefresh()
        {
            if (dockStateRefreshQueued || root == null)
                return;

            dockStateRefreshQueued = true;
            root.schedule.Execute(() =>
            {
                dockStateRefreshQueued = false;
                RefreshDockResponsiveLayout();
            });
        }

        private void RefreshDockResponsiveLayout()
        {
            EnsureTabLayoutContainers();

            if (settingsContainer == null || splineTabsContainer == null || tabContentStack == null || overlayRootElement == null)
                return;

            bool overlayFloating = floating;
            bool overlayInToolbar = isInToolbar;
            var dockEdge = overlayFloating || overlayInToolbar ? OverlayDockEdge.Unknown : InferDockEdge();

            if (lastOverlayFloating == overlayFloating &&
                lastOverlayInToolbar == overlayInToolbar &&
                currentDockEdge == dockEdge)
            {
                return;
            }

            lastOverlayFloating = overlayFloating;
            lastOverlayInToolbar = overlayInToolbar;
            currentDockEdge = dockEdge;

            ApplyDockResponsiveTabLayout();
        }

        private OverlayDockEdge InferDockEdge()
        {
            if (overlayRootElement == null)
                return OverlayDockEdge.Unknown;

            var windowRoot = containerWindow?.rootVisualElement;
            if (windowRoot == null)
                return OverlayDockEdge.Unknown;

            var overlayBounds = overlayRootElement.worldBound;
            var windowBounds = windowRoot.worldBound;

            if (overlayBounds.width <= 0f || overlayBounds.height <= 0f ||
                windowBounds.width <= 0f || windowBounds.height <= 0f)
            {
                return OverlayDockEdge.Unknown;
            }

            float distanceToTop = Mathf.Abs(overlayBounds.yMin - windowBounds.yMin);
            float distanceToBottom = Mathf.Abs(windowBounds.yMax - overlayBounds.yMax);
            float distanceToLeft = Mathf.Abs(overlayBounds.xMin - windowBounds.xMin);
            float distanceToRight = Mathf.Abs(windowBounds.xMax - overlayBounds.xMax);

            float minDistance = Mathf.Min(distanceToTop, distanceToBottom, distanceToLeft, distanceToRight);

            if (Mathf.Approximately(minDistance, distanceToBottom))
                return OverlayDockEdge.Bottom;

            if (Mathf.Approximately(minDistance, distanceToTop))
                return OverlayDockEdge.Top;

            if (Mathf.Approximately(minDistance, distanceToLeft))
                return OverlayDockEdge.Left;

            return OverlayDockEdge.Right;
        }

        private void EnsureTabLayoutContainers()
        {
            if (settingsContainer == null)
                return;

            overlayHeaderElement ??= settingsContainer.Q<VisualElement>(className: "overlay-header");
            splineTabsContainer ??= settingsContainer.Q<VisualElement>(className: "spline-tabs");

            if (splineTabsContainer == null)
                return;

            tabContentStack ??= settingsContainer.Q<VisualElement>(TabContentStackName);
            if (tabContentStack == null)
            {
                tabContentStack = new VisualElement { name = TabContentStackName };
                tabContentStack.AddToClassList(TabContentStackName);
            }

            bottomDockOverrideHost ??= settingsContainer.Q<VisualElement>(BottomDockOverrideHostName);
            if (bottomDockOverrideHost == null)
            {
                bottomDockOverrideHost = new VisualElement { name = BottomDockOverrideHostName };
                bottomDockOverrideHost.AddToClassList(BottomDockOverrideHostName);
            }

            MoveTabContentToStack(modeContent);
            MoveTabContentToStack(brushContent);
            MoveTabContentToStack(paintContent);
            MoveTabContentToStack(detailContent);
            MoveTabContentToStack(treeContent);

            if (tabContentStack.parent != settingsContainer)
                settingsContainer.Add(tabContentStack);

            if (bottomDockOverrideHost.parent != settingsContainer)
                settingsContainer.Add(bottomDockOverrideHost);
        }

        private void MoveTabContentToStack(VisualElement content)
        {
            if (content == null || tabContentStack == null)
                return;

            if (content.parent == tabContentStack)
                return;

            content.RemoveFromHierarchy();
            tabContentStack.Add(content);
        }

        private void ApplyDockResponsiveTabLayout()
        {
            if (settingsContainer == null || splineTabsContainer == null || tabContentStack == null || bottomDockOverrideHost == null || overlayRootElement == null)
                return;

            bool bottomDocked = !floating && !isInToolbar && currentDockEdge == OverlayDockEdge.Bottom;

            PlaceActiveOverrideToggleInBottomHost();

            if (bottomDocked)
            {
                overlayRootElement.AddToClassList(OverlayBottomDockedClass);

                int overrideHostIndex = Mathf.Max(0, settingsContainer.childCount - 1);
                if (bottomDockOverrideHost.parent != settingsContainer || settingsContainer.IndexOf(bottomDockOverrideHost) != overrideHostIndex)
                {
                    bottomDockOverrideHost.RemoveFromHierarchy();
                    settingsContainer.Insert(Mathf.Clamp(overrideHostIndex, 0, settingsContainer.childCount), bottomDockOverrideHost);
                }

                if (splineTabsContainer.parent != settingsContainer || settingsContainer.IndexOf(splineTabsContainer) != settingsContainer.childCount - 1)
                {
                    splineTabsContainer.RemoveFromHierarchy();
                    settingsContainer.Add(splineTabsContainer);
                }

                return;
            }

            overlayRootElement.RemoveFromClassList(OverlayBottomDockedClass);

            int targetIndex = 0;
            if (overlayHeaderElement != null && overlayHeaderElement.parent == settingsContainer)
                targetIndex = settingsContainer.IndexOf(overlayHeaderElement) + 1;

            if (splineTabsContainer.parent != settingsContainer || settingsContainer.IndexOf(splineTabsContainer) != targetIndex)
            {
                splineTabsContainer.RemoveFromHierarchy();
                settingsContainer.Insert(Mathf.Clamp(targetIndex, 0, settingsContainer.childCount), splineTabsContainer);
            }

            int bottomHostTargetIndex = settingsContainer.IndexOf(splineTabsContainer) + 1;
            if (bottomDockOverrideHost.parent != settingsContainer || settingsContainer.IndexOf(bottomDockOverrideHost) != bottomHostTargetIndex)
            {
                bottomDockOverrideHost.RemoveFromHierarchy();
                settingsContainer.Insert(Mathf.Clamp(bottomHostTargetIndex, 0, settingsContainer.childCount), bottomDockOverrideHost);
            }
        }

        private void PlaceActiveOverrideToggleInBottomHost()
        {
            if (bottomDockOverrideHost == null)
                return;

            var activeToggle = GetOverrideToggleForActiveTab();
            RestoreInactiveOverrideTogglesToTabContent(activeToggle);

            if (activeToggle == null)
            {
                bottomDockOverrideHost.style.display = DisplayStyle.None;
                return;
            }

            if (activeToggle.parent != bottomDockOverrideHost)
            {
                activeToggle.RemoveFromHierarchy();
                bottomDockOverrideHost.Add(activeToggle);
            }

            bottomDockOverrideHost.style.display = DisplayStyle.Flex;
        }

        private void RestoreInactiveOverrideTogglesToTabContent(Toggle activeToggle)
        {
            RestoreOverrideToggleToContent(activeToggle == overrideModeToggle ? null : overrideModeToggle, modeContent);
            RestoreOverrideToggleToContent(activeToggle == overrideBrushToggle ? null : overrideBrushToggle, brushContent);
            RestoreOverrideToggleToContent(activeToggle == overridePaintToggle ? null : overridePaintToggle, paintContent);
            RestoreOverrideToggleToContent(activeToggle == overrideDetailToggle ? null : overrideDetailToggle, detailContent);
            RestoreOverrideToggleToContent(activeToggle == overrideTreeToggle ? null : overrideTreeToggle, treeContent);
        }

        private void RestoreOverrideTogglesToTabContent()
        {
            RestoreOverrideToggleToContent(overrideModeToggle, modeContent);
            RestoreOverrideToggleToContent(overrideBrushToggle, brushContent);
            RestoreOverrideToggleToContent(overridePaintToggle, paintContent);
            RestoreOverrideToggleToContent(overrideDetailToggle, detailContent);
            RestoreOverrideToggleToContent(overrideTreeToggle, treeContent);

            if (bottomDockOverrideHost != null)
                bottomDockOverrideHost.style.display = DisplayStyle.None;
        }

        private void RestoreOverrideToggleToContent(Toggle toggle, VisualElement content)
        {
            if (toggle == null || content == null)
                return;

            if (toggle.parent == content && content.IndexOf(toggle) == 0)
                return;

            toggle.RemoveFromHierarchy();
            content.Insert(0, toggle);
        }

        private Toggle GetOverrideToggleForActiveTab()
        {
            return activeTab switch
            {
                0 => overrideModeToggle,
                1 => overrideBrushToggle,
                2 => overridePaintToggle,
                3 => overrideDetailToggle,
                4 => overrideTreeToggle,
                _ => null
            };
        }

        private void EnsureNoSelectionUI()
        {
            if (noSelectionContainer != null)
            {
                if (noSelectionContainer.Q<Button>("draw-custom-button") == null)
                {
                    var existingCreateRow = noSelectionContainer.Q<VisualElement>(className: "no-selection-create-row");
                    if (existingCreateRow != null)
                    {
                        drawCustomButton = new Button { name = "draw-custom-button" };
                        drawCustomButton.AddToClassList("spline-tab-button");
                        drawCustomButton.AddToClassList("no-selection-create-button");

                        var existingDrawCustomContent = new VisualElement();
                        existingDrawCustomContent.AddToClassList("no-selection-create-button-content");
                        drawCustomIcon = new Image { name = "draw-custom-icon" };
                        drawCustomIcon.AddToClassList("no-selection-create-icon");
                        existingDrawCustomContent.Add(drawCustomIcon);
                        var existingDrawCustomLabel = new Label("Custom");
                        existingDrawCustomLabel.AddToClassList("no-selection-create-button-label");
                        existingDrawCustomContent.Add(existingDrawCustomLabel);
                        drawCustomButton.Add(existingDrawCustomContent);

                        existingCreateRow.Add(drawCustomButton);
                    }
                }

                return;
            }

            var overlayRoot = root.Q<VisualElement>(className: "spline-settings-overlay") ?? root;

            noSelectionContainer = new VisualElement { name = "no-selection-container" };
            noSelectionContainer.AddToClassList("no-selection-container");
            noSelectionContainer.style.display = DisplayStyle.None;

            var header = new VisualElement();
            header.AddToClassList("no-selection-header");

            var spacer = new VisualElement();
            spacer.AddToClassList("no-selection-header-spacer");
            header.Add(spacer);

            openWindowButton = new Button { name = "open-window-button", tooltip = "Open Gruel Terra Splines window" };
            openWindowButton.AddToClassList("overlay-action-button");
            openWindowIcon = new Image { name = "open-window-icon" };
            openWindowIcon.AddToClassList("overlay-action-icon");
            openWindowButton.Add(openWindowIcon);
            header.Add(openWindowButton);

            openDocsButton = new Button { name = "open-docs-button", tooltip = "Open Documentation" };
            openDocsButton.AddToClassList("overlay-action-button");
            openDocsIcon = new Image { name = "open-docs-icon" };
            openDocsIcon.AddToClassList("overlay-action-icon");
            openDocsButton.Add(openDocsIcon);
            header.Add(openDocsButton);

            noSelectionContainer.Add(header);

            var createLabel = new Label("Create");
            createLabel.AddToClassList("no-selection-create-label");
            noSelectionContainer.Add(createLabel);

            var createRow = new VisualElement();
            createRow.AddToClassList("no-selection-create-row");

            createPathButton = new Button { name = "create-path-button" };
            createPathButton.AddToClassList("spline-tab-button");
            createPathButton.AddToClassList("no-selection-create-button");
            var createPathContent = new VisualElement();
            createPathContent.AddToClassList("no-selection-create-button-content");
            createPathIcon = new Image { name = "create-path-icon" };
            createPathIcon.AddToClassList("no-selection-create-icon");
            createPathContent.Add(createPathIcon);
            var createPathLabel = new Label("Path");
            createPathLabel.AddToClassList("no-selection-create-button-label");
            createPathContent.Add(createPathLabel);
            createPathButton.Add(createPathContent);
            createRow.Add(createPathButton);

            createShapeButton = new Button { name = "create-shape-button" };
            createShapeButton.AddToClassList("spline-tab-button");
            createShapeButton.AddToClassList("no-selection-create-button");
            var createShapeContent = new VisualElement();
            createShapeContent.AddToClassList("no-selection-create-button-content");
            createShapeIcon = new Image { name = "create-shape-icon" };
            createShapeIcon.AddToClassList("no-selection-create-icon");
            createShapeContent.Add(createShapeIcon);
            var createShapeLabel = new Label("Shape");
            createShapeLabel.AddToClassList("no-selection-create-button-label");
            createShapeContent.Add(createShapeLabel);
            createShapeButton.Add(createShapeContent);
            createRow.Add(createShapeButton);

            drawCustomButton = new Button { name = "draw-custom-button" };
            drawCustomButton.AddToClassList("spline-tab-button");
            drawCustomButton.AddToClassList("no-selection-create-button");
            var drawCustomContent = new VisualElement();
            drawCustomContent.AddToClassList("no-selection-create-button-content");
            drawCustomIcon = new Image { name = "draw-custom-icon" };
            drawCustomIcon.AddToClassList("no-selection-create-icon");
            drawCustomContent.Add(drawCustomIcon);
            var drawCustomLabel = new Label("Custom");
            drawCustomLabel.AddToClassList("no-selection-create-button-label");
            drawCustomContent.Add(drawCustomLabel);
            drawCustomButton.Add(drawCustomContent);
            createRow.Add(drawCustomButton);

            noSelectionContainer.Add(createRow);

            var toggleRow = new VisualElement();
            toggleRow.AddToClassList("no-selection-toggle-row");

            var toggleLabel = new Label("Select spline on creation");
            toggleLabel.AddToClassList("no-selection-toggle-label");
            toggleRow.Add(toggleLabel);

            var toggleSpacer = new VisualElement();
            toggleSpacer.AddToClassList("no-selection-toggle-spacer");
            toggleRow.Add(toggleSpacer);

            selectOnCreateToggle = new Toggle { name = "select-on-create-toggle" };
            selectOnCreateToggle.AddToClassList("no-selection-toggle");
            selectOnCreateToggle.SetValueWithoutNotify(true);
            toggleRow.Add(selectOnCreateToggle);
            noSelectionContainer.Add(toggleRow);

            if (settingsContainer != null && settingsContainer.parent == overlayRoot)
                overlayRoot.Insert(1, noSelectionContainer);
            else
                overlayRoot.Add(noSelectionContainer);
        }

        private void SetupEventHandlers()
        {
            // Action buttons
            if (applyButton != null)
                applyButton.clicked += OnApplyClicked;
            if (duplicateButton != null)
                duplicateButton.clicked += OnDuplicateClicked;
            if (deleteButton != null)
                deleteButton.clicked += OnDeleteClicked;
            if (openWindowButton != null)
                openWindowButton.clicked += OnOpenWindowClicked;
            if (openDocsButton != null)
                openDocsButton.clicked += OnOpenDocsClicked;
            if (createPathButton != null)
                createPathButton.clicked += () => CreateSpline(SplineApplyMode.Path);
            if (createShapeButton != null)
                createShapeButton.clicked += () => CreateSpline(SplineApplyMode.Shape);
            if (drawCustomButton != null)
                drawCustomButton.clicked += OnDrawCustomClicked;

            // Tab switching
            if (modeTab != null)
                modeTab.clicked += () => SetActiveTab(0);
            if (brushTab != null)
                brushTab.clicked += () => SetActiveTab(1);
            if (paintTab != null)
                paintTab.clicked += () => SetActiveTab(2);
            if (detailTab != null)
                detailTab.clicked += () => SetActiveTab(3);
            if (treeTab != null)
                treeTab.clicked += () => SetActiveTab(4);

            // Mode controls
            if (pathModeButton != null)
                pathModeButton.clicked += () => SetMode(SplineApplyMode.Path);
            if (shapeModeButton != null)
                shapeModeButton.clicked += () => SetMode(SplineApplyMode.Shape);

            // Bind change events for auto-save
            BindChangeEvents();

            if (selectOnCreateToggle != null)
            {
                selectOnCreateToggle.RegisterValueChangedCallback(evt =>
                {
                    selectOnCreate = evt.newValue;
                    EditorPrefs.SetBool(BuildEditorPrefsKey(SelectOnCreatePrefKey), selectOnCreate);
                });
            }
        }

        private void EnsureModeButtonContent(Button button, string iconName, string labelText)
        {
            if (button == null)
                return;

            if (button.Q<VisualElement>("mode-button-content") != null)
                return;

            button.text = string.Empty;

            var content = new VisualElement { name = "mode-button-content" };
            content.style.flexDirection = FlexDirection.Row;
            content.style.alignItems = Align.Center;
            content.style.justifyContent = Justify.Center;
            content.style.height = Length.Percent(100);
            content.style.width = Length.Percent(100);

            var icon = new Image();
            icon.style.width = 14;
            icon.style.height = 14;
            icon.style.minWidth = 14;
            icon.style.minHeight = 14;
            icon.style.marginRight = 6;
            icon.scaleMode = ScaleMode.ScaleToFit;
            content.Add(icon);

            button.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (icon.image == null)
                    icon.image = Resources.Load<Texture2D>($"Icons/{iconName}");
            });

            var label = new Label(labelText);
            label.style.fontSize = 11;
            label.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            content.Add(label);

            button.Add(content);
        }

        private void BindChangeEvents()
        {
            if (overrideModeToggle != null)
                overrideModeToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overrideMode = evt.newValue;
                        UpdateTabText(modeTab, "Mode", evt.newValue);
                        SetModeControlsEnabled(evt.newValue);
                        UpdateOperationInterlocks(targetSettings.settings);
                        UpdateOperationWarning();
                        ApplyChanges();
                    }
                });

            void OnOperationToggleChanged()
            {
                if (targetSettings == null) return;

                RecordChange();

                var s = targetSettings.settings;
                if (!s.overrideMode)
                {
                    // If using globals, ignore edits and just reflect current state
                    SyncOperationTogglesFromSettings(s);
                    UpdateOperationInterlocks(s);
                    UpdateOperationWarning();
                    return;
                }

                s.operationHeight = opHeightToggle?.value ?? false;
                s.operationPaint = opPaintToggle?.value ?? false;
                s.operationHole = opHoleToggle?.value ?? false;
                s.operationFill = opFillToggle?.value ?? false;
                s.operationAddDetail = opAddDetailToggle?.value ?? false;
                s.operationRemoveDetail = opRemoveDetailToggle?.value ?? false;
                s.operationAddTree = opAddTreeToggle?.value ?? false;
                s.operationRemoveTree = opRemoveTreeToggle?.value ?? false;

                EnforceOperationRules(s);
                s.detailMode = s.operationRemoveDetail ? DetailOperationMode.Remove : DetailOperationMode.Add;
                SyncOperationTogglesFromSettings(s);
                UpdateOperationWarning();
                ApplyChanges();
            }

            opHeightToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opPaintToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opHoleToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opFillToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opAddDetailToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opRemoveDetailToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opAddTreeToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opRemoveTreeToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());

            if (overrideBrushToggle != null)
                overrideBrushToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overrideBrush = evt.newValue;
                        UpdateTabText(brushTab, "Brush", evt.newValue);
                        SetBrushControlsEnabled(evt.newValue);
                        UpdateSizeMultiplierVisibility();
                        ApplyChanges();
                    }
                });

            if (brushSizeSlider != null)
                brushSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                        targetSettings.settings.sizeMeters = clampedValue;
                        if (brushSizeField != null) brushSizeField.SetValueWithoutNotify(clampedValue);
                        ApplyChanges();
                    }
                });

            if (brushSizeField != null)
                brushSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                        targetSettings.settings.sizeMeters = clampedValue;
                        if (brushSizeSlider != null) brushSizeSlider.SetValueWithoutNotify(clampedValue);
                        if (brushSizeField != null) brushSizeField.SetValueWithoutNotify(clampedValue);
                        ApplyChanges();
                    }
                });

            if (brushHardnessSlider != null)
                brushHardnessSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.hardness = evt.newValue;
                        if (brushHardnessField != null) brushHardnessField.SetValueWithoutNotify(evt.newValue);
                        UpdateBrushPreview();
                        ApplyChanges();
                    }
                });

            if (brushHardnessField != null)
                brushHardnessField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.hardness = evt.newValue;
                        if (brushHardnessSlider != null) brushHardnessSlider.SetValueWithoutNotify(evt.newValue);
                        UpdateBrushPreview();
                        ApplyChanges();
                    }
                });

            if (strengthSlider != null)
                strengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.strength = evt.newValue;
                        if (strengthField != null) strengthField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (strengthField != null)
                strengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.strength = evt.newValue;
                        if (strengthSlider != null) strengthSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (sampleStepSlider != null)
                sampleStepSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.sampleStep = evt.newValue;
                        if (sampleStepField != null) sampleStepField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (sampleStepField != null)
                sampleStepField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.sampleStep = evt.newValue;
                        if (sampleStepSlider != null) sampleStepSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (brushNoiseTextureField != null)
                brushNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseTexture = evt.newValue as Texture2D;
                        SetNoiseHeaderIconTint(brushNoiseFoldout, targetSettings.settings.brushNoise.noiseTexture != null);
                        ApplyChanges();
                    }
                });

            if (brushNoiseStrengthSlider != null)
                brushNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                        if (brushNoiseStrengthField != null) brushNoiseStrengthField.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseStrength);
                        ApplyChanges();
                    }
                });

            if (brushNoiseStrengthField != null)
                brushNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                        if (brushNoiseStrengthSlider != null) brushNoiseStrengthSlider.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseStrength);
                        ApplyChanges();
                    }
                });

            if (brushNoiseEdgeSlider != null)
                brushNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                        if (brushNoiseEdgeField != null) brushNoiseEdgeField.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseEdge);
                        ApplyChanges();
                    }
                });

            if (brushNoiseEdgeField != null)
                brushNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                        if (brushNoiseEdgeSlider != null) brushNoiseEdgeSlider.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseEdge);
                        ApplyChanges();
                    }
                });

            if (brushNoiseSizeSlider != null)
                brushNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                        if (brushNoiseSizeField != null) brushNoiseSizeField.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseWorldSizeMeters);
                        ApplyChanges();
                    }
                });

            if (brushNoiseSizeField != null)
                brushNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                        if (brushNoiseSizeSlider != null) brushNoiseSizeSlider.SetValueWithoutNotify(targetSettings.settings.brushNoise.noiseWorldSizeMeters);
                        ApplyChanges();
                    }
                });

            if (brushNoiseOffsetField != null)
                brushNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseOffset = evt.newValue;
                        ApplyChanges();
                    }
                });

            if (brushNoiseResponseCurve != null)
                brushNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve();
                        ApplyChanges();
                    }
                });

            if (brushNoiseInvertToggle != null)
                brushNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.brushNoise == null)
                            targetSettings.settings.brushNoise = new BrushNoiseSettings();
                        targetSettings.settings.brushNoise.noiseInvert = evt.newValue;
                        UpdateNoiseInvertToggleVisual(brushNoiseInvertToggle);
                        ApplyChanges();
                    }
                });

            if (overrideSizeMultiplierToggle != null)
                overrideSizeMultiplierToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overrideSizeMultiplier = evt.newValue;
                        UpdateSizeMultiplierVisibility();
                        UpdateSizeMultiplierVisualState();
                        ApplyChanges();
                    }
                });

            if (brushSizeMultiplierCurve != null)
                brushSizeMultiplierCurve.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.sizeMultiplier = evt.newValue.CloneCurve();
                        ApplyChanges();
                    }
                });

            if (overridePaintToggle != null)
                overridePaintToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overridePaint = evt.newValue;
                        UpdateTabText(paintTab, "Paint", evt.newValue);
                        SetPaintControlsEnabled(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintStrengthSlider != null)
                paintStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintStrength = evt.newValue;
                        if (paintStrengthField != null) paintStrengthField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintStrengthField != null)
                paintStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintStrength = evt.newValue;
                        if (paintStrengthSlider != null) paintStrengthSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintBlendSlider != null)
                paintBlendSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintBlend = evt.newValue;
                        if (paintBlendField != null) paintBlendField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintBlendField != null)
                paintBlendField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintBlend = evt.newValue;
                        if (paintBlendSlider != null) paintBlendSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintThresholdSlider != null)
                paintThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintThreshold = evt.newValue;
                        if (paintThresholdField != null) paintThresholdField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintThresholdField != null)
                paintThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.paintThreshold = evt.newValue;
                        if (paintThresholdSlider != null) paintThresholdSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (paintNoiseTextureField != null)
                paintNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseTexture = evt.newValue as Texture2D;
                        SetNoiseHeaderIconTint(paintNoiseFoldout, entry.noiseTexture != null);
                        PopulateLayerPalette();
                        ApplyChanges();
                    }
                });

            if (paintNoiseStrengthSlider != null)
                paintNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                        if (paintNoiseStrengthField != null) paintNoiseStrengthField.SetValueWithoutNotify(entry.noiseStrength);
                        ApplyChanges();
                    }
                });

            if (paintNoiseStrengthField != null)
                paintNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                        if (paintNoiseStrengthSlider != null) paintNoiseStrengthSlider.SetValueWithoutNotify(entry.noiseStrength);
                        ApplyChanges();
                    }
                });

            if (paintNoiseEdgeSlider != null)
                paintNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                        if (paintNoiseEdgeField != null) paintNoiseEdgeField.SetValueWithoutNotify(entry.noiseEdge);
                        ApplyChanges();
                    }
                });

            if (paintNoiseEdgeField != null)
                paintNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                        if (paintNoiseEdgeSlider != null) paintNoiseEdgeSlider.SetValueWithoutNotify(entry.noiseEdge);
                        ApplyChanges();
                    }
                });

            if (paintNoiseSizeSlider != null)
                paintNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                        if (paintNoiseSizeField != null) paintNoiseSizeField.SetValueWithoutNotify(entry.noiseWorldSizeMeters);
                        ApplyChanges();
                    }
                });

            if (paintNoiseSizeField != null)
                paintNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                        if (paintNoiseSizeSlider != null) paintNoiseSizeSlider.SetValueWithoutNotify(entry.noiseWorldSizeMeters);
                        ApplyChanges();
                    }
                });

            if (paintNoiseOffsetField != null)
                paintNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseOffset = evt.newValue;
                        ApplyChanges();
                    }
                });

            if (paintNoiseResponseCurve != null)
                paintNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve();
                        ApplyChanges();
                    }
                });

            if (paintNoiseInvertToggle != null)
                paintNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.paintNoiseLayers == null)
                            targetSettings.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                        var entry = GetOrCreatePaintNoiseLayer(targetSettings.settings.paintNoiseLayers, targetSettings.settings.selectedLayerIndex);
                        entry.noiseInvert = evt.newValue;
                        UpdateNoiseInvertToggleVisual(paintNoiseInvertToggle);
                        ApplyChanges();
                    }
                });

            if (overrideDetailToggle != null)
                overrideDetailToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overrideDetail = evt.newValue;
                        UpdateTabText(detailTab, "Detail", evt.newValue);
                        if (detailLayerPalette != null) detailLayerPalette.SetEnabled(evt.newValue);
                        if (detailStrengthSlider != null) detailStrengthSlider.SetEnabled(evt.newValue);
                        if (detailStrengthField != null) detailStrengthField.SetEnabled(evt.newValue);
                        if (detailFalloffSlider != null) detailFalloffSlider.SetEnabled(evt.newValue);
                        if (detailFalloffField != null) detailFalloffField.SetEnabled(evt.newValue);
                        if (detailSpreadSlider != null) detailSpreadSlider.SetEnabled(evt.newValue);
                        if (detailSpreadField != null) detailSpreadField.SetEnabled(evt.newValue);
                        if (detailSlopeLimitSlider != null) detailSlopeLimitSlider.SetEnabled(evt.newValue);
                        if (detailSlopeLimitField != null) detailSlopeLimitField.SetEnabled(evt.newValue);
                        if (detailRemoveThresholdSlider != null) detailRemoveThresholdSlider.SetEnabled(evt.newValue);
                        if (detailRemoveThresholdField != null) detailRemoveThresholdField.SetEnabled(evt.newValue);
                        if (detailNoiseTextureField != null) detailNoiseTextureField.SetEnabled(evt.newValue);
                        if (detailNoiseSizeSlider != null) detailNoiseSizeSlider.SetEnabled(evt.newValue);
                        if (detailNoiseSizeField != null) detailNoiseSizeField.SetEnabled(evt.newValue);
                        if (detailNoiseOffsetField != null) detailNoiseOffsetField.SetEnabled(evt.newValue);
                        if (detailNoiseThresholdSlider != null) detailNoiseThresholdSlider.SetEnabled(evt.newValue);
                        if (detailNoiseThresholdField != null) detailNoiseThresholdField.SetEnabled(evt.newValue);
                        if (detailNoiseInvertToggle != null) detailNoiseInvertToggle.SetEnabled(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailStrengthSlider != null)
                detailStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailStrength = evt.newValue;
                        if (detailStrengthField != null) detailStrengthField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailStrengthField != null)
                detailStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailStrength = evt.newValue;
                        if (detailStrengthSlider != null) detailStrengthSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailFalloffSlider != null)
                detailFalloffSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float normalized = Mathf.Clamp01(evt.newValue);
                        targetSettings.settings.detailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                        if (detailFalloffField != null) detailFalloffField.SetValueWithoutNotify(normalized);
                        ApplyChanges();
                    }
                });

            if (detailFalloffField != null)
                detailFalloffField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float normalized = Mathf.Clamp01(evt.newValue);
                        targetSettings.settings.detailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                        if (detailFalloffSlider != null) detailFalloffSlider.SetValueWithoutNotify(normalized);
                        ApplyChanges();
                    }
                });

            if (detailSpreadSlider != null)
                detailSpreadSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailSpreadRadius = evt.newValue;
                        if (detailSpreadField != null) detailSpreadField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailSpreadField != null)
                detailSpreadField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailSpreadRadius = evt.newValue;
                        if (detailSpreadSlider != null) detailSpreadSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailSlopeLimitSlider != null)
                detailSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailSlopeLimitDegrees = evt.newValue;
                        if (detailSlopeLimitField != null) detailSlopeLimitField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailSlopeLimitField != null)
                detailSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailSlopeLimitDegrees = evt.newValue;
                        if (detailSlopeLimitSlider != null) detailSlopeLimitSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailRemoveThresholdSlider != null)
                detailRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailRemoveThreshold = evt.newValue;
                        if (detailRemoveThresholdField != null) detailRemoveThresholdField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailRemoveThresholdField != null)
                detailRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.detailRemoveThreshold = evt.newValue;
                        if (detailRemoveThresholdSlider != null) detailRemoveThresholdSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (detailNoiseTextureField != null)
                detailNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        bool hasNoiseTexture = false;
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseTexture = evt.newValue as Texture2D;
                            if (entry.noiseTexture != null)
                                hasNoiseTexture = true;
                        }
                        SetNoiseHeaderIconTint(detailNoiseFoldout, hasNoiseTexture);
                        ApplyChanges();
                        PopulateDetailPalette();
                    }
                });

            if (detailNoiseSizeSlider != null)
                detailNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        float noiseSize = Mathf.Max(0.001f, evt.newValue);
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseWorldSizeMeters = noiseSize;
                        }
                        if (detailNoiseSizeField != null) detailNoiseSizeField.SetValueWithoutNotify(noiseSize);
                        ApplyChanges();
                    }
                });

            if (detailNoiseSizeField != null)
                detailNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        float noiseSize = Mathf.Max(0.001f, evt.newValue);
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseWorldSizeMeters = noiseSize;
                        }
                        if (detailNoiseSizeSlider != null) detailNoiseSizeSlider.SetValueWithoutNotify(noiseSize);
                        ApplyChanges();
                    }
                });

            if (detailNoiseOffsetField != null)
                detailNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseOffset = evt.newValue;
                        }
                        ApplyChanges();
                    }
                });

            if (detailNoiseThresholdSlider != null)
                detailNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        float noiseThreshold = Mathf.Clamp01(evt.newValue);
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseThreshold = noiseThreshold;
                        }
                        if (detailNoiseThresholdField != null) detailNoiseThresholdField.SetValueWithoutNotify(noiseThreshold);
                        ApplyChanges();
                    }
                });

            if (detailNoiseThresholdField != null)
                detailNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        float noiseThreshold = Mathf.Clamp01(evt.newValue);
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseThreshold = noiseThreshold;
                        }
                        if (detailNoiseThresholdSlider != null) detailNoiseThresholdSlider.SetValueWithoutNotify(noiseThreshold);
                        ApplyChanges();
                    }
                });

            if (detailNoiseInvertToggle != null)
                detailNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.detailNoiseLayers == null)
                            targetSettings.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                        var selectedLayerIndices = GetSelectedDetailLayerIndices();
                        foreach (var layerIndex in selectedLayerIndices)
                        {
                            var entry = GetOrCreateDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                            entry.noiseInvert = evt.newValue;
                        }
                        UpdateNoiseInvertToggleVisual(detailNoiseInvertToggle);
                        ApplyChanges();
                    }
                });

            if (overrideTreeToggle != null)
                overrideTreeToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.overrideTrees = evt.newValue;
                        UpdateTabText(treeTab, "Trees", evt.newValue);
                        SetTreeControlsEnabled(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeStrengthSlider != null)
                treeStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeStrength = evt.newValue;
                        if (treeStrengthField != null) treeStrengthField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeStrengthField != null)
                treeStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeStrength = evt.newValue;
                        if (treeStrengthSlider != null) treeStrengthSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeFalloffSlider != null)
                treeFalloffSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float normalized = Mathf.Clamp01(evt.newValue);
                        targetSettings.settings.treeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                        if (treeFalloffField != null) treeFalloffField.SetValueWithoutNotify(normalized);
                        ApplyChanges();
                    }
                });

            if (treeFalloffField != null)
                treeFalloffField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        float normalized = Mathf.Clamp01(evt.newValue);
                        targetSettings.settings.treeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                        if (treeFalloffSlider != null) treeFalloffSlider.SetValueWithoutNotify(normalized);
                        ApplyChanges();
                    }
                });

            if (treeHeightMultiplierCurve != null)
                treeHeightMultiplierCurve.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeHeightMultiplier = evt.newValue?.CloneCurve() ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve();
                        ApplyChanges();
                    }
                });

            if (treeSlopeLimitSlider != null)
                treeSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeSlopeLimitDegrees = evt.newValue;
                        if (treeSlopeLimitField != null) treeSlopeLimitField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeSlopeLimitField != null)
                treeSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeSlopeLimitDegrees = evt.newValue;
                        if (treeSlopeLimitSlider != null) treeSlopeLimitSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeRemoveThresholdSlider != null)
                treeRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeRemoveThreshold = evt.newValue;
                        if (treeRemoveThresholdField != null) treeRemoveThresholdField.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeRemoveThresholdField != null)
                treeRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.treeRemoveThreshold = evt.newValue;
                        if (treeRemoveThresholdSlider != null) treeRemoveThresholdSlider.SetValueWithoutNotify(evt.newValue);
                        ApplyChanges();
                    }
                });

            if (treeNoiseTextureField != null)
                treeNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        bool hasNoiseTexture = false;
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseTexture = evt.newValue as Texture2D;
                            if (entry.noiseTexture != null)
                                hasNoiseTexture = true;
                        }
                        SetNoiseHeaderIconTint(treeNoiseFoldout, hasNoiseTexture);
                        ApplyChanges();
                        PopulateTreePalette();
                    }
                });

            if (treeNoiseSizeSlider != null)
                treeNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        float noiseSize = Mathf.Max(0.001f, evt.newValue);
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseWorldSizeMeters = noiseSize;
                        }
                        if (treeNoiseSizeField != null) treeNoiseSizeField.SetValueWithoutNotify(noiseSize);
                        ApplyChanges();
                    }
                });

            if (treeNoiseSizeField != null)
                treeNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        float noiseSize = Mathf.Max(0.001f, evt.newValue);
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseWorldSizeMeters = noiseSize;
                        }
                        if (treeNoiseSizeSlider != null) treeNoiseSizeSlider.SetValueWithoutNotify(noiseSize);
                        ApplyChanges();
                    }
                });

            if (treeNoiseOffsetField != null)
                treeNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseOffset = evt.newValue;
                        }
                        ApplyChanges();
                    }
                });

            if (treeNoiseThresholdSlider != null)
                treeNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        float noiseThreshold = Mathf.Clamp01(evt.newValue);
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseThreshold = noiseThreshold;
                        }
                        if (treeNoiseThresholdField != null) treeNoiseThresholdField.SetValueWithoutNotify(noiseThreshold);
                        ApplyChanges();
                    }
                });

            if (treeNoiseThresholdField != null)
                treeNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        float noiseThreshold = Mathf.Clamp01(evt.newValue);
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseThreshold = noiseThreshold;
                        }
                        if (treeNoiseThresholdSlider != null) treeNoiseThresholdSlider.SetValueWithoutNotify(noiseThreshold);
                        ApplyChanges();
                    }
                });

            if (treeNoiseInvertToggle != null)
                treeNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        if (targetSettings.settings.treeNoiseLayers == null)
                            targetSettings.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                        var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                        foreach (var prototypeIndex in selectedPrototypeIndices)
                        {
                            var entry = GetOrCreateTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                            entry.noiseInvert = evt.newValue;
                        }
                        UpdateNoiseInvertToggleVisual(treeNoiseInvertToggle);
                        ApplyChanges();
                    }
                });

        }

        private void OnSelectionChanged()
        {
            var selectedObjects = Selection.gameObjects;
            targetSettings = null;
            targetTerrain = null;

            if (selectedObjects.Length == 0)
            {
                ShowNoSelectionUI();
                return;
            }

            if (selectedObjects.Length > 1)
            {
                ShowMultiSelectionMessage("Multi-object editing not supported");
                return;
            }

            var selectedObject = selectedObjects[0];
            targetSettings = selectedObject.GetComponent<TerrainSplineSettings>();

            if (targetSettings == null)
            {
                ShowNoSelectionUI();
                return;
            }

            // Find target terrain for layer palette
            targetTerrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();

            ShowSettings();
            UpdateUI();
            UpdateHeader();
        }

        private void OnExternalSettingsChanged(TerrainSplineSettings settings)
        {
            if (targetSettings == null || settings != targetSettings) return;
            UpdateUI();
            UpdateOperationWarning();
            UpdateHeader();
        }

        private void OnFeatureFlagsChanged()
        {
            if (targetSettings == null) return;
            UpdateOperationInterlocks(targetSettings.settings);
            UpdateOperationWarning();
        }

        private void OnPrefabThumbnailsChanged()
        {
            if (treePrototypePalette == null)
                return;

            PopulateTreePalette();
            treePrototypePalette.MarkDirtyRepaint();
            root?.MarkDirtyRepaint();
        }

        private void ShowMultiSelectionMessage(string message)
        {
            if (multiSelectionMessage != null)
            {
                multiSelectionMessage.style.display = DisplayStyle.Flex;
                var label = multiSelectionMessage.Q<Label>();
                if (label != null)
                    label.text = message;
            }

            if (settingsContainer != null)
                settingsContainer.style.display = DisplayStyle.None;

            if (noSelectionContainer != null)
                noSelectionContainer.style.display = DisplayStyle.None;
        }

        private void ShowSettings()
        {
            if (multiSelectionMessage != null)
                multiSelectionMessage.style.display = DisplayStyle.None;

            if (settingsContainer != null)
                settingsContainer.style.display = DisplayStyle.Flex;

            if (noSelectionContainer != null)
                noSelectionContainer.style.display = DisplayStyle.None;
        }

        private void ShowNoSelectionUI()
        {
            if (multiSelectionMessage != null)
                multiSelectionMessage.style.display = DisplayStyle.None;

            if (settingsContainer != null)
                settingsContainer.style.display = DisplayStyle.None;

            if (noSelectionContainer != null)
                noSelectionContainer.style.display = DisplayStyle.Flex;

            UpdateNoSelectionUIState();
        }

        private void UpdateNoSelectionUIState()
        {
            lastWindowOpenState = IsWindowOpen();

            if (selectOnCreateToggle != null)
            {
                selectOnCreate = LoadBoolPref(SelectOnCreatePrefKey, true);
                selectOnCreateToggle.SetValueWithoutNotify(selectOnCreate);
            }

            if (openWindowButton != null)
                openWindowButton.SetEnabled(!lastWindowOpenState.Value);

            if (openWindowIcon != null && openWindowIcon.image == null)
            {
                var icon = Resources.Load<Texture2D>("Icons/SplineSettings");
                if (icon != null)
                    openWindowIcon.image = icon;
            }

            if (openDocsIcon != null && openDocsIcon.image == null)
            {
                var icon = Resources.Load<Texture2D>("Icons/doc");
                if (icon != null)
                    openDocsIcon.image = icon;
            }

            if (createPathIcon != null && createPathIcon.image == null)
            {
                var icon = Resources.Load<Texture2D>("Icons/path");
                if (icon != null)
                    createPathIcon.image = icon;
            }

            if (createShapeIcon != null && createShapeIcon.image == null)
            {
                var icon = Resources.Load<Texture2D>("Icons/shape");
                if (icon != null)
                    createShapeIcon.image = icon;
            }

            if (drawCustomIcon != null && drawCustomIcon.image == null)
            {
                var icon = Resources.Load<Texture2D>("Icons/custom");
                if (icon != null)
                    drawCustomIcon.image = icon;
            }
        }

        private bool IsWindowOpen()
        {
            return TerraSplinesWindow.GetOpenWindow() != null;
        }

        private void OnEditorUpdate()
        {
            if (openWindowButton == null)
                return;

            bool isWindowOpen = IsWindowOpen();
            if (lastWindowOpenState.HasValue && lastWindowOpenState.Value == isWindowOpen)
                return;

            UpdateNoSelectionUIState();
        }

        private void OnOpenWindowClicked()
        {
            TerraSplinesWindow.Open();
            UpdateNoSelectionUIState();
        }

        private void OnOpenDocsClicked()
        {
            Application.OpenURL(TerraSplinesWindow.DocumentationUrl);
        }

        private void OnDrawCustomClicked()
        {
            TerraSplinesWindow.DeferInteractiveRefresh(1.0d);

            var splineObject = CreateSplineObject("Custom Spline", SplineApplyMode.Path, false, true);
            if (splineObject == null)
                return;

            var splineContainer = splineObject.GetComponent<SplineContainer>();
            if (splineContainer == null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (splineObject == null || splineContainer == null)
                    return;

                Selection.activeObject = splineContainer;
                TryActivateSplineDrawTool(splineContainer);
                SceneView.lastActiveSceneView?.Focus();
            };
        }

        private void CreateSpline(SplineApplyMode mode)
        {
            TerraSplinesWindow.DeferInteractiveRefresh(1.5d);
            CreateSplineObject(mode == SplineApplyMode.Path ? "Path Spline" : "Shape Spline", mode, true, false);
        }

        private GameObject CreateSplineObject(string objectName, SplineApplyMode mode, bool initializeDefaultSpline, bool forceSelect)
        {
            var window = TerraSplinesWindow.GetOpenWindow();
            Transform group = ResolveOrCreateSplineGroup(window);

            Vector3 spawnPosition = Vector3.zero;
            if (TryGetTerrainHitPoint(out var hitPoint))
                spawnPosition = hitPoint;

            var splineObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(splineObject, "Create Spline");
            splineObject.transform.SetParent(group, true);
            splineObject.transform.position = spawnPosition;

            var splineContainer = splineObject.AddComponent<SplineContainer>();
            if (initializeDefaultSpline)
                InitializeDefaultSpline(splineContainer, mode);
            else
                splineContainer.Spline.Clear();

            var settings = splineObject.AddComponent<TerrainSplineSettings>();
            settings.settings.overrideMode = true;
            settings.settings.mode = mode;
            EditorUtility.SetDirty(splineContainer);
            EditorUtility.SetDirty(settings);

            if (forceSelect || (selectOnCreateToggle != null ? selectOnCreateToggle.value : selectOnCreate))
                Selection.activeGameObject = splineObject;

            window?.Repaint();

            return splineObject;
        }

        private static Transform ResolveOrCreateSplineGroup(TerraSplinesWindow window)
        {
            var group = window?.GetSplineGroup();
            if (group != null)
                return group;

            group = FindExistingSplineGroup();
            if (group == null)
            {
                var groupObject = new GameObject("Spline Group");
                Undo.RegisterCreatedObjectUndo(groupObject, "Create Spline Group");
                group = groupObject.transform;
            }

            window?.SetSplineGroupExternal(group, refreshList: false);
            return group;
        }

        private static Transform FindExistingSplineGroup()
        {
            var namedGroup = GameObject.Find("Spline Group");
            if (namedGroup != null)
                return namedGroup.transform;

            var containers = UnityEngine.Object.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);
            if (containers == null || containers.Length == 0)
                return null;

            return containers[0] != null ? containers[0].transform.root : null;
        }

        private static bool TryActivateSplineDrawTool(SplineContainer targetContainer)
        {
            if (targetContainer == null)
                return false;

            var splineToolContextType = Type.GetType("UnityEditor.Splines.SplineToolContext, Unity.Splines.Editor");
            var createSplineToolType = Type.GetType("UnityEditor.Splines.CreateSplineTool, Unity.Splines.Editor");

            if (splineToolContextType == null || createSplineToolType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    splineToolContextType ??= assembly.GetType("UnityEditor.Splines.SplineToolContext");
                    createSplineToolType ??= assembly.GetType("UnityEditor.Splines.CreateSplineTool");
                    if (splineToolContextType != null && createSplineToolType != null)
                        break;
                }
            }

            var toolManagerType = Type.GetType("UnityEditor.EditorTools.ToolManager, UnityEditor")
                ?? typeof(Editor).Assembly.GetType("UnityEditor.EditorTools.ToolManager");

            if (toolManagerType != null && splineToolContextType != null && createSplineToolType != null)
            {
                var setActiveContextMethod = toolManagerType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                        method.Name == "SetActiveContext"
                        && method.IsGenericMethodDefinition
                        && method.GetParameters().Length == 0);

                var setActiveToolMethod = toolManagerType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                        method.Name == "SetActiveTool"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(Type));

                if (setActiveContextMethod != null && setActiveToolMethod != null)
                {
                    Selection.activeObject = targetContainer;
                    setActiveContextMethod.MakeGenericMethod(splineToolContextType).Invoke(null, null);

                    EditorApplication.delayCall += () =>
                    {
                        if (targetContainer == null)
                            return;

                        Selection.activeObject = targetContainer;
                        setActiveToolMethod.Invoke(null, new object[] { createSplineToolType });
                        SceneView.RepaintAll();
                    };

                    return true;
                }
            }

            return EditorApplication.ExecuteMenuItem("GameObject/Spline/Draw Splines Tool...");
        }

        private void InitializeDefaultSpline(SplineContainer container, SplineApplyMode mode)
        {
            if (container == null)
                return;

            var spline = container.Spline;
            if (mode == SplineApplyMode.Shape)
                ApplyDefaultShapeSpline(spline);
            else
                ApplyDefaultPathSpline(spline);

            EditorUtility.SetDirty(container);
        }

        private static void ApplyDefaultPathSpline(Spline spline)
        {
            if (spline == null)
                return;

            spline.Clear();
            spline.Add(
                new BezierKnot(
                    new float3(-40.6347f, 0.0076904f, 7.893433f),
                    new float3(0.0919284f, 0.0f, -22.34731f),
                    new float3(17.55054f, 2.100002f, 11.22614f),
                    new quaternion(0.0f, 0.5847102f, 0.0f, 0.8112422f)),
                TangentMode.Broken);

            spline.Add(
                new BezierKnot(
                    new float3(13.4653f, 8.107691f, 13.39343f),
                    new float3(0.0f, 0.0f, -24.04193f),
                    new float3(0.0f, 0.0f, 19.61352f),
                    new quaternion(0.0028727f, 0.4631151f, -0.0055116f, 0.8862764f)),
                TangentMode.Continuous);

            spline.Add(
                new BezierKnot(
                    new float3(68.46527f, 0.0076904f, 6.093445f),
                    new float3(0.0f, 0.0f, -23.93013f),
                    new float3(0.0f, 0.0f, 6.28921f),
                    new quaternion(0.0419887f, 0.9168789f, -0.015093f, 0.3966638f)),
                TangentMode.Continuous);

            spline.Closed = false;
        }

        private static void ApplyDefaultShapeSpline(Spline spline)
        {
            if (spline == null)
                return;

            spline.Clear();
            spline.Add(
                new BezierKnot(
                    new float3(-38.59845f, 7.687691f, -32.18164f),
                    new float3(0.0f, 0.0f, -15.56584f),
                    new float3(0.0f, 0.0f, 17.77811f),
                    new quaternion(0.0464014f, 0.9321283f, -0.1283027f, 0.3354434f)),
                TangentMode.Continuous);

            spline.Add(
                new BezierKnot(
                    new float3(34.70154f, 0.0076904f, -46.08167f),
                    new float3(0.0f, 0.0f, -14.86876f),
                    new float3(0.0f, 0.0f, 14.85918f),
                    new quaternion(-0.0016432f, 0.4027079f, 0.000443f, 0.9153269f)),
                TangentMode.Continuous);

            spline.Add(
                new BezierKnot(
                    new float3(27.20154f, 7.97769f, 28.01837f),
                    new float3(0.0f, 0.0f, -17.27702f),
                    new float3(0.0f, 0.0f, 16.06373f),
                    new quaternion(-0.0172051f, -0.4267769f, -0.0078468f, 0.9041592f)),
                TangentMode.Continuous);

            spline.Add(
                new BezierKnot(
                    new float3(-38.29843f, 15.30769f, 21.91833f),
                    new float3(0.0f, 0.0f, -33.87344f),
                    new float3(0.0f, 0.0f, 21.66568f),
                    new quaternion(0.0145489f, 0.8981608f, 0.0274491f, -0.4385681f)),
                TangentMode.Continuous);

            spline.Closed = true;
        }

        private bool TryGetTerrainHitPoint(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                return false;

            var camera = sceneView.camera;
            var center = new Vector2(camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f);
            var ray = camera.ScreenPointToRay(center);
            var hits = Physics.RaycastAll(ray, 100000f);

            if (hits == null || hits.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            bool found = false;

            foreach (var hit in hits)
            {
                var terrain = hit.collider != null
                    ? hit.collider.GetComponent<Terrain>() ?? hit.collider.GetComponentInParent<Terrain>()
                    : null;
                if (terrain == null)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    hitPoint = hit.point;
                    found = true;
                }
            }

            return found;
        }

        private void UpdateUI()
        {
            if (targetSettings == null) return;

            var settings = targetSettings.settings;

            // Mode tab
            if (overrideModeToggle != null)
                overrideModeToggle.SetValueWithoutNotify(settings.overrideMode);
            SetModeControlsEnabled(settings.overrideMode);
            if (pathModeButton != null)
                SetModeButtonSelected(pathModeButton, settings.mode == SplineApplyMode.Path);
            if (shapeModeButton != null)
                SetModeButtonSelected(shapeModeButton, settings.mode == SplineApplyMode.Shape);
            SyncOperationTogglesFromSettings(settings);

            // Brush tab
            if (overrideBrushToggle != null)
                overrideBrushToggle.SetValueWithoutNotify(settings.overrideBrush);
            SetBrushControlsEnabled(settings.overrideBrush);
            UpdateSizeMultiplierVisualState();
            if (brushSizeSlider != null)
                brushSizeSlider.SetValueWithoutNotify(settings.sizeMeters);
            if (brushSizeField != null)
                brushSizeField.SetValueWithoutNotify(settings.sizeMeters);
            if (brushHardnessSlider != null)
                brushHardnessSlider.SetValueWithoutNotify(settings.hardness);
            if (brushHardnessField != null)
                brushHardnessField.SetValueWithoutNotify(settings.hardness);
            if (strengthSlider != null)
                strengthSlider.SetValueWithoutNotify(settings.strength);
            if (strengthField != null)
                strengthField.SetValueWithoutNotify(settings.strength);
            if (sampleStepSlider != null)
                sampleStepSlider.SetValueWithoutNotify(settings.sampleStep);
            if (sampleStepField != null)
                sampleStepField.SetValueWithoutNotify(settings.sampleStep);
            if (overrideSizeMultiplierToggle != null)
                overrideSizeMultiplierToggle.SetValueWithoutNotify(settings.overrideSizeMultiplier);
            if (brushSizeMultiplierCurve != null)
                brushSizeMultiplierCurve.SetValueWithoutNotify(settings.sizeMultiplier.CloneCurve());
            var brushNoiseEntry = settings.brushNoise ?? new BrushNoiseSettings();
            if (brushNoiseTextureField != null)
                brushNoiseTextureField.SetValueWithoutNotify(brushNoiseEntry.noiseTexture);
            if (brushNoiseStrengthSlider != null)
                brushNoiseStrengthSlider.SetValueWithoutNotify(brushNoiseEntry.noiseStrength);
            if (brushNoiseStrengthField != null)
                brushNoiseStrengthField.SetValueWithoutNotify(brushNoiseEntry.noiseStrength);
            if (brushNoiseEdgeSlider != null)
                brushNoiseEdgeSlider.SetValueWithoutNotify(brushNoiseEntry.noiseEdge);
            if (brushNoiseEdgeField != null)
                brushNoiseEdgeField.SetValueWithoutNotify(brushNoiseEntry.noiseEdge);
            if (brushNoiseSizeSlider != null)
                brushNoiseSizeSlider.SetValueWithoutNotify(brushNoiseEntry.noiseWorldSizeMeters);
            if (brushNoiseSizeField != null)
                brushNoiseSizeField.SetValueWithoutNotify(brushNoiseEntry.noiseWorldSizeMeters);
            if (brushNoiseOffsetField != null)
                brushNoiseOffsetField.SetValueWithoutNotify(brushNoiseEntry.noiseOffset);
            if (brushNoiseResponseCurve != null)
                brushNoiseResponseCurve.SetValueWithoutNotify((brushNoiseEntry.noiseResponse ?? SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve()).CloneCurve());
            if (brushNoiseInvertToggle != null)
            {
                brushNoiseInvertToggle.SetValueWithoutNotify(brushNoiseEntry.noiseInvert);
                UpdateNoiseInvertToggleVisual(brushNoiseInvertToggle);
            }
            if (brushNoiseFoldout != null)
            {
                SetNoiseHeaderIconTint(brushNoiseFoldout, brushNoiseEntry.noiseTexture != null);
            }

            // Paint tab
            if (overridePaintToggle != null)
                overridePaintToggle.SetValueWithoutNotify(settings.overridePaint);
            SetPaintControlsEnabled(settings.overridePaint);
            if (paintStrengthSlider != null)
                paintStrengthSlider.SetValueWithoutNotify(settings.paintStrength);
            if (paintStrengthField != null)
                paintStrengthField.SetValueWithoutNotify(settings.paintStrength);
            if (paintBlendSlider != null)
                paintBlendSlider.SetValueWithoutNotify(settings.paintBlend);
            if (paintBlendField != null)
                paintBlendField.SetValueWithoutNotify(settings.paintBlend);
            if (paintThresholdSlider != null)
                paintThresholdSlider.SetValueWithoutNotify(settings.paintThreshold);
            if (paintThresholdField != null)
                paintThresholdField.SetValueWithoutNotify(settings.paintThreshold);

            // Detail tab
            if (overrideDetailToggle != null)
                overrideDetailToggle.SetValueWithoutNotify(settings.overrideDetail);
            if (detailStrengthSlider != null)
                detailStrengthSlider.SetValueWithoutNotify(settings.detailStrength);
            if (detailStrengthField != null)
                detailStrengthField.SetValueWithoutNotify(settings.detailStrength);
            if (detailFalloffSlider != null)
                detailFalloffSlider.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(settings.detailFalloffPower));
            if (detailFalloffField != null)
                detailFalloffField.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(settings.detailFalloffPower));
            if (detailSpreadSlider != null)
                detailSpreadSlider.SetValueWithoutNotify(settings.detailSpreadRadius);
            if (detailSpreadField != null)
                detailSpreadField.SetValueWithoutNotify(settings.detailSpreadRadius);
            if (detailSlopeLimitSlider != null)
                detailSlopeLimitSlider.SetValueWithoutNotify(settings.detailSlopeLimitDegrees);
            if (detailSlopeLimitField != null)
                detailSlopeLimitField.SetValueWithoutNotify(settings.detailSlopeLimitDegrees);
            if (detailRemoveThresholdSlider != null)
                detailRemoveThresholdSlider.SetValueWithoutNotify(settings.detailRemoveThreshold);
            if (detailRemoveThresholdField != null)
                detailRemoveThresholdField.SetValueWithoutNotify(settings.detailRemoveThreshold);
            UpdatePaintNoiseUI();
            UpdateDetailNoiseUI();
            if (overrideDetailToggle != null)
            {
                bool enableDetailControls = overrideDetailToggle.value;
                if (detailLayerPalette != null) detailLayerPalette.SetEnabled(enableDetailControls);
                if (detailStrengthSlider != null) detailStrengthSlider.SetEnabled(enableDetailControls);
                if (detailStrengthField != null) detailStrengthField.SetEnabled(enableDetailControls);
                if (detailFalloffSlider != null) detailFalloffSlider.SetEnabled(enableDetailControls);
                if (detailFalloffField != null) detailFalloffField.SetEnabled(enableDetailControls);
                if (detailSpreadSlider != null) detailSpreadSlider.SetEnabled(enableDetailControls);
                if (detailSpreadField != null) detailSpreadField.SetEnabled(enableDetailControls);
                if (detailSlopeLimitSlider != null) detailSlopeLimitSlider.SetEnabled(enableDetailControls);
                if (detailSlopeLimitField != null) detailSlopeLimitField.SetEnabled(enableDetailControls);
                if (detailRemoveThresholdSlider != null) detailRemoveThresholdSlider.SetEnabled(enableDetailControls);
                if (detailRemoveThresholdField != null) detailRemoveThresholdField.SetEnabled(enableDetailControls);
                if (detailNoiseTextureField != null) detailNoiseTextureField.SetEnabled(enableDetailControls);
                if (detailNoiseSizeSlider != null) detailNoiseSizeSlider.SetEnabled(enableDetailControls);
                if (detailNoiseSizeField != null) detailNoiseSizeField.SetEnabled(enableDetailControls);
                if (detailNoiseOffsetField != null) detailNoiseOffsetField.SetEnabled(enableDetailControls);
                if (detailNoiseThresholdSlider != null) detailNoiseThresholdSlider.SetEnabled(enableDetailControls);
                if (detailNoiseThresholdField != null) detailNoiseThresholdField.SetEnabled(enableDetailControls);
                if (detailNoiseInvertToggle != null) detailNoiseInvertToggle.SetEnabled(enableDetailControls);
            }

            // Tree tab
            if (overrideTreeToggle != null)
                overrideTreeToggle.SetValueWithoutNotify(settings.overrideTrees);
            if (treeStrengthSlider != null)
                treeStrengthSlider.SetValueWithoutNotify(settings.treeStrength);
            if (treeStrengthField != null)
                treeStrengthField.SetValueWithoutNotify(settings.treeStrength);
            if (treeFalloffSlider != null)
                treeFalloffSlider.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(settings.treeFalloffPower));
            if (treeFalloffField != null)
                treeFalloffField.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(settings.treeFalloffPower));
            if (treeHeightMultiplierCurve != null)
                treeHeightMultiplierCurve.SetValueWithoutNotify((settings.treeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve());
            if (treeSlopeLimitSlider != null)
                treeSlopeLimitSlider.SetValueWithoutNotify(settings.treeSlopeLimitDegrees);
            if (treeSlopeLimitField != null)
                treeSlopeLimitField.SetValueWithoutNotify(settings.treeSlopeLimitDegrees);
            if (treeRemoveThresholdSlider != null)
                treeRemoveThresholdSlider.SetValueWithoutNotify(settings.treeRemoveThreshold);
            if (treeRemoveThresholdField != null)
                treeRemoveThresholdField.SetValueWithoutNotify(settings.treeRemoveThreshold);
            UpdateTreeNoiseUI();
            if (overrideTreeToggle != null)
                SetTreeControlsEnabled(overrideTreeToggle.value);

            // Update tab text with override indicators
            UpdateTabText(modeTab, "Mode", settings.overrideMode);
            UpdateTabText(brushTab, "Brush", settings.overrideBrush);
            UpdateTabText(paintTab, "Paint", settings.overridePaint);
            UpdateTabText(detailTab, "Detail", settings.overrideDetail);
            UpdateTabText(treeTab, "Trees", settings.overrideTrees);

            // Update UI state
            UpdateSizeMultiplierVisibility();
            UpdateBrushPreview();
            UpdateTabStates();
            PopulateLayerPalette();
            UpdatePaintNoiseUI();
            PopulateDetailPalette();
            PopulateTreePalette();
            UpdateOperationWarning();
        }

        private void UpdateHeader()
        {
            if (targetSettings == null) return;

            // Update title with GameObject name
            if (overlayTitle != null)
            {
                overlayTitle.text = targetSettings.gameObject.name;
            }

            // Load and assign button icons
            if (applyIcon != null)
            {
                var applyTexture = Resources.Load<Texture2D>("Icons/apply");
                if (applyTexture != null)
                    applyIcon.image = applyTexture;
            }

            if (duplicateIcon != null)
            {
                var duplicateTexture = Resources.Load<Texture2D>("Icons/duplicate");
                if (duplicateTexture != null)
                    duplicateIcon.image = duplicateTexture;
            }

            if (deleteIcon != null)
            {
                var deleteTexture = Resources.Load<Texture2D>("Icons/delete");
                if (deleteTexture != null)
                    deleteIcon.image = deleteTexture;
            }
        }

        private void SetActiveTab(int tabIndex)
        {
            activeTab = tabIndex;
            UpdateTabStates();
            PlaceActiveOverrideToggleInBottomHost();
        }

        private void UpdateTabStates()
        {
            if (modeTab != null)
                SetTabButtonActive(modeTab, activeTab == 0);
            if (brushTab != null)
                SetTabButtonActive(brushTab, activeTab == 1);
            if (paintTab != null)
                SetTabButtonActive(paintTab, activeTab == 2);
            if (detailTab != null)
                SetTabButtonActive(detailTab, activeTab == 3);
            if (treeTab != null)
                SetTabButtonActive(treeTab, activeTab == 4);

            if (modeContent != null)
                modeContent.style.display = activeTab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (brushContent != null)
                brushContent.style.display = activeTab == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (paintContent != null)
                paintContent.style.display = activeTab == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            if (detailContent != null)
                detailContent.style.display = activeTab == 3 ? DisplayStyle.Flex : DisplayStyle.None;
            if (treeContent != null)
                treeContent.style.display = activeTab == 4 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetMode(SplineApplyMode mode)
        {
            if (targetSettings == null) return;

            RecordChange();
            targetSettings.settings.mode = mode;
            ApplyChanges();

            // Update UI
            if (pathModeButton != null)
                SetModeButtonSelected(pathModeButton, mode == SplineApplyMode.Path);
            if (shapeModeButton != null)
                SetModeButtonSelected(shapeModeButton, mode == SplineApplyMode.Shape);
        }

        private void SetModeButtonSelected(Button button, bool selected)
        {
            if (button == null) return;

            if (selected)
            {
                button.AddToClassList("mode-button-selected");
            }
            else
            {
                button.RemoveFromClassList("mode-button-selected");
            }
        }

        private void SetTabButtonActive(Button button, bool active)
        {
            SplineTabButtonUtility.SetActive(button, active);
        }

        private void UpdateSizeMultiplierVisibility()
        {
            if (brushSizeMultiplierCurve != null && targetSettings != null)
            {
                bool showCurve = targetSettings.settings.overrideBrush && targetSettings.settings.overrideSizeMultiplier;
                CurvePresetDropdownUtility.SetDisplay(brushSizeMultiplierCurve, showCurve);
            }
        }

        private void UpdateSizeMultiplierVisualState()
        {
            bool enabled = targetSettings != null && targetSettings.settings.overrideBrush;
            brushSizeMultiplierSection?.SetEnabled(enabled);
            if (brushSizeMultiplierSection != null)
            {
                brushSizeMultiplierSection.style.opacity = enabled ? 1f : 0.4f;
            }
        }

        private void SetBrushControlsEnabled(bool enabled)
        {
            brushPreviewImage?.SetEnabled(enabled);
            brushSizeSlider?.SetEnabled(enabled);
            brushSizeField?.SetEnabled(enabled);
            brushHardnessSlider?.SetEnabled(enabled);
            brushHardnessField?.SetEnabled(enabled);
            strengthSlider?.SetEnabled(enabled);
            strengthField?.SetEnabled(enabled);
            sampleStepSlider?.SetEnabled(enabled);
            sampleStepField?.SetEnabled(enabled);
            overrideSizeMultiplierToggle?.SetEnabled(enabled);
            CurvePresetDropdownUtility.SetEnabled(brushSizeMultiplierCurve, enabled);
            brushNoiseTextureField?.SetEnabled(enabled);
            brushNoiseStrengthSlider?.SetEnabled(enabled);
            brushNoiseStrengthField?.SetEnabled(enabled);
            brushNoiseEdgeSlider?.SetEnabled(enabled);
            brushNoiseEdgeField?.SetEnabled(enabled);
            brushNoiseSizeSlider?.SetEnabled(enabled);
            brushNoiseSizeField?.SetEnabled(enabled);
            brushNoiseOffsetField?.SetEnabled(enabled);
            CurvePresetDropdownUtility.SetEnabled(brushNoiseResponseCurve, enabled);
            brushNoiseInvertToggle?.SetEnabled(enabled);
            UpdateSizeMultiplierVisualState();
        }

        private void SetPaintControlsEnabled(bool enabled)
        {
            paintLayerPalette?.SetEnabled(enabled);
            paintStrengthSlider?.SetEnabled(enabled);
            paintStrengthField?.SetEnabled(enabled);
            paintBlendSlider?.SetEnabled(enabled);
            paintBlendField?.SetEnabled(enabled);
            paintThresholdSlider?.SetEnabled(enabled);
            paintThresholdField?.SetEnabled(enabled);
            paintNoiseTextureField?.SetEnabled(enabled);
            paintNoiseStrengthSlider?.SetEnabled(enabled);
            paintNoiseStrengthField?.SetEnabled(enabled);
            paintNoiseEdgeSlider?.SetEnabled(enabled);
            paintNoiseEdgeField?.SetEnabled(enabled);
            paintNoiseSizeSlider?.SetEnabled(enabled);
            paintNoiseSizeField?.SetEnabled(enabled);
            paintNoiseOffsetField?.SetEnabled(enabled);
            CurvePresetDropdownUtility.SetEnabled(paintNoiseResponseCurve, enabled);
            paintNoiseInvertToggle?.SetEnabled(enabled);
        }

        private void SetTreeControlsEnabled(bool enabled)
        {
            treePrototypePalette?.SetEnabled(enabled);
            treeStrengthSlider?.SetEnabled(enabled);
            treeStrengthField?.SetEnabled(enabled);
            treeFalloffSlider?.SetEnabled(enabled);
            treeFalloffField?.SetEnabled(enabled);
            CurvePresetDropdownUtility.SetEnabled(treeHeightMultiplierCurve, enabled);
            treeSlopeLimitSlider?.SetEnabled(enabled);
            treeSlopeLimitField?.SetEnabled(enabled);
            treeRemoveThresholdSlider?.SetEnabled(enabled);
            treeRemoveThresholdField?.SetEnabled(enabled);
            treeNoiseTextureField?.SetEnabled(enabled);
            treeNoiseSizeSlider?.SetEnabled(enabled);
            treeNoiseSizeField?.SetEnabled(enabled);
            treeNoiseOffsetField?.SetEnabled(enabled);
            treeNoiseThresholdSlider?.SetEnabled(enabled);
            treeNoiseThresholdField?.SetEnabled(enabled);
            treeNoiseInvertToggle?.SetEnabled(enabled);
        }

        private void SetModeControlsEnabled(bool enabled)
        {
            pathModeButton?.SetEnabled(enabled);
            shapeModeButton?.SetEnabled(enabled);
            opHeightToggle?.SetEnabled(enabled);
            opPaintToggle?.SetEnabled(enabled);
            opHoleToggle?.SetEnabled(enabled);
            opFillToggle?.SetEnabled(enabled);
            opAddDetailToggle?.SetEnabled(enabled);
            opRemoveDetailToggle?.SetEnabled(enabled);
            opAddTreeToggle?.SetEnabled(enabled);
            opRemoveTreeToggle?.SetEnabled(enabled);

            var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
            foreach (var toggle in toggles)
            {
                RefreshOperationToggleVisual(toggle);
            }
        }

        private void UpdateBrushPreview()
        {
            if (brushPreviewImage != null && targetSettings != null)
            {
                brushPreviewImage.image = BrushFalloffUtils.GenerateBrushPreviewTexture(64, targetSettings.settings.hardness);
            }
        }

        private void PopulateLayerPalette()
        {
            if (paintLayerPalette == null) return;

            paintLayerPalette.Clear();

            if (targetTerrain == null || targetTerrain.terrainData == null)
            {
                var noLayersLabel = new Label("No terrain selected");
                noLayersLabel.AddToClassList("no-layers-label");
                paintLayerPalette.Add(noLayersLabel);
                return;
            }

            var terrainLayers = targetTerrain.terrainData.terrainLayers;
            if (terrainLayers == null || terrainLayers.Length == 0)
            {
                var noLayersLabel = new Label("No layers configured");
                noLayersLabel.AddToClassList("no-layers-label");
                paintLayerPalette.Add(noLayersLabel);
                return;
            }

            // Create layer buttons
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                var layer = terrainLayers[i];
                if (layer == null) continue;

                int layerIndex = i; // Capture for closure
                var layerButton = CreateLayerButton(layer, i, targetSettings.settings.selectedLayerIndex);
                layerButton.clicked += () =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();
                        targetSettings.settings.selectedLayerIndex = layerIndex;
                        ApplyChanges();
                        PopulateLayerPalette(); // Refresh to update selection
                        UpdatePaintNoiseUI();
                    }
                };
                paintLayerPalette.Add(layerButton);
            }
        }

        private void PopulateDetailPalette()
        {
            if (detailLayerPalette == null) return;

            detailLayerPalette.Clear();

            if (targetSettings != null && targetSettings.settings.selectedDetailLayerIndices == null)
                targetSettings.settings.selectedDetailLayerIndices = new List<int>();
            var selectedDetailSet = (targetSettings != null && targetSettings.settings.selectedDetailLayerIndices != null && targetSettings.settings.selectedDetailLayerIndices.Count > 0)
                ? new HashSet<int>(targetSettings.settings.selectedDetailLayerIndices)
                : (targetSettings != null ? new HashSet<int> { targetSettings.settings.selectedDetailLayerIndex } : new HashSet<int>());

            if (targetTerrain == null || targetTerrain.terrainData == null)
            {
                var noLayersLabel = new Label("No terrain selected");
                noLayersLabel.AddToClassList("no-layers-label");
                detailLayerPalette.Add(noLayersLabel);
                return;
            }

            var detailPrototypes = targetTerrain.terrainData.detailPrototypes;
            if (detailPrototypes == null || detailPrototypes.Length == 0)
            {
                var noLayersLabel = new Label("No detail layers");
                noLayersLabel.AddToClassList("no-layers-label");
                detailLayerPalette.Add(noLayersLabel);
                return;
            }

            for (int i = 0; i < detailPrototypes.Length; i++)
            {
                var prototype = detailPrototypes[i];
                if (prototype == null) continue;

                int layerIndex = i;
                var layerButton = new Button();
                layerButton.AddToClassList("layer-button");
                layerButton.AddToClassList(selectedDetailSet.Contains(i) ? "layer-button-selected" : "layer-button-unselected");
                layerButton.tooltip = "Click to select detail layer. Shift+Click to toggle multiple layers.";

                var thumbnail = new Image();
                thumbnail.AddToClassList("layer-thumbnail");
                thumbnail.image = prototype.prototypeTexture != null ? prototype.prototypeTexture : Texture2D.grayTexture;
                layerButton.Add(thumbnail);

                var noiseEntryForLayer = GetDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);
                if (noiseEntryForLayer != null && noiseEntryForLayer.noiseTexture != null)
                {
                    var noiseBadge = new Image();
                    noiseBadge.AddToClassList("layer-noise-badge");
                    noiseBadge.image = Resources.Load<Texture2D>("Icons/noise");
                    noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                    layerButton.Add(noiseBadge);
                }

                string layerNameText = prototype.prototype != null ? prototype.prototype.name : $"Layer {i}";
                var layerName = new Label(layerNameText);
                layerName.AddToClassList("layer-name");
                layerButton.Add(layerName);

                layerButton.RegisterCallback<ClickEvent>(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();

                        if (targetSettings.settings.selectedDetailLayerIndices == null)
                            targetSettings.settings.selectedDetailLayerIndices = new List<int>();

                        if (!evt.shiftKey)
                        {
                            targetSettings.settings.selectedDetailLayerIndices.Clear();
                            targetSettings.settings.selectedDetailLayerIndex = layerIndex;
                        }
                        else
                        {
                            if (targetSettings.settings.selectedDetailLayerIndices.Count == 0)
                                targetSettings.settings.selectedDetailLayerIndices.Add(targetSettings.settings.selectedDetailLayerIndex);

                            if (targetSettings.settings.selectedDetailLayerIndices.Contains(layerIndex))
                                targetSettings.settings.selectedDetailLayerIndices.Remove(layerIndex);
                            else
                                targetSettings.settings.selectedDetailLayerIndices.Add(layerIndex);

                            if (targetSettings.settings.selectedDetailLayerIndices.Count == 0)
                                targetSettings.settings.selectedDetailLayerIndices.Add(layerIndex);

                            targetSettings.settings.selectedDetailLayerIndex = layerIndex;
                            targetSettings.settings.selectedDetailLayerIndices.Sort();
                        }

                        ApplyChanges();
                        PopulateDetailPalette();
                        UpdateDetailNoiseUI();
                    }
                });

                detailLayerPalette.Add(layerButton);
            }
        }

        private void PopulateTreePalette()
        {
            if (treePrototypePalette == null) return;

            treePrototypePalette.Clear();

            if (targetSettings != null && targetSettings.settings.selectedTreePrototypeIndices == null)
                targetSettings.settings.selectedTreePrototypeIndices = new List<int>();
            var selectedTreeSet = (targetSettings != null && targetSettings.settings.selectedTreePrototypeIndices != null && targetSettings.settings.selectedTreePrototypeIndices.Count > 0)
                ? new HashSet<int>(targetSettings.settings.selectedTreePrototypeIndices)
                : (targetSettings != null ? new HashSet<int> { targetSettings.settings.selectedTreePrototypeIndex } : new HashSet<int>());

            if (targetTerrain == null || targetTerrain.terrainData == null)
            {
                var noTreesLabel = new Label("No terrain selected");
                noTreesLabel.AddToClassList("no-layers-label");
                treePrototypePalette.Add(noTreesLabel);
                return;
            }

            var treePrototypes = targetTerrain.terrainData.treePrototypes;
            if (treePrototypes == null || treePrototypes.Length == 0)
            {
                var noTreesLabel = new Label("No tree prototypes");
                noTreesLabel.AddToClassList("no-layers-label");
                treePrototypePalette.Add(noTreesLabel);
                return;
            }

            for (int i = 0; i < treePrototypes.Length; i++)
            {
                var prototype = treePrototypes[i];
                int prototypeIndex = i;
                var prototypeButton = new Button();
                prototypeButton.AddToClassList("layer-button");
                prototypeButton.AddToClassList(selectedTreeSet.Contains(i) ? "layer-button-selected" : "layer-button-unselected");
                prototypeButton.tooltip = "Click to select tree. Shift+Click to toggle multiple trees.";

                var thumbnail = new Image();
                thumbnail.AddToClassList("layer-thumbnail");
                var prefab = prototype.prefab;
                thumbnail.image = AssetThumbnailCache.GetPrefabThumbnail(prefab);
                prototypeButton.Add(thumbnail);

                var noiseEntryForPrototype = GetTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);
                if (noiseEntryForPrototype != null && noiseEntryForPrototype.noiseTexture != null)
                {
                    var noiseBadge = new Image();
                    noiseBadge.AddToClassList("layer-noise-badge");
                    noiseBadge.image = Resources.Load<Texture2D>("Icons/noise");
                    noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                    prototypeButton.Add(noiseBadge);
                }

                string prototypeNameText = GetTreePrototypeDisplayName(prototypeIndex);
                var prototypeName = new Label(prototypeNameText);
                prototypeName.AddToClassList("layer-name");
                prototypeButton.Add(prototypeName);

                prototypeButton.RegisterCallback<ClickEvent>(evt =>
                {
                    if (targetSettings != null)
                    {
                        RecordChange();

                        if (targetSettings.settings.selectedTreePrototypeIndices == null)
                            targetSettings.settings.selectedTreePrototypeIndices = new List<int>();

                        if (!evt.shiftKey)
                        {
                            targetSettings.settings.selectedTreePrototypeIndices.Clear();
                            targetSettings.settings.selectedTreePrototypeIndex = prototypeIndex;
                        }
                        else
                        {
                            if (targetSettings.settings.selectedTreePrototypeIndices.Count == 0)
                                targetSettings.settings.selectedTreePrototypeIndices.Add(targetSettings.settings.selectedTreePrototypeIndex);

                            if (targetSettings.settings.selectedTreePrototypeIndices.Contains(prototypeIndex))
                                targetSettings.settings.selectedTreePrototypeIndices.Remove(prototypeIndex);
                            else
                                targetSettings.settings.selectedTreePrototypeIndices.Add(prototypeIndex);

                            if (targetSettings.settings.selectedTreePrototypeIndices.Count == 0)
                                targetSettings.settings.selectedTreePrototypeIndices.Add(prototypeIndex);

                            targetSettings.settings.selectedTreePrototypeIndex = prototypeIndex;
                            targetSettings.settings.selectedTreePrototypeIndices.Sort();
                        }

                        ApplyChanges();
                        PopulateTreePalette();
                        UpdateTreeNoiseUI();
                    }
                });

                treePrototypePalette.Add(prototypeButton);
            }
        }

        private Button CreateLayerButton(TerrainLayer layer, int index, int selectedIndex)
        {
            var layerButton = new Button();
            layerButton.AddToClassList("layer-button");
            layerButton.AddToClassList(index == selectedIndex ? "layer-button-selected" : "layer-button-unselected");
            layerButton.tooltip = $"Click to select terrain layer '{layer.name}' for painting this spline";

            // Create thumbnail
            var thumbnail = new Image();
            thumbnail.AddToClassList("layer-thumbnail");

            if (layer.diffuseTexture != null)
            {
                thumbnail.image = layer.diffuseTexture;
            }
            else
            {
                // Create a placeholder texture
                var placeholder = new Texture2D(32, 32);
                var colors = new Color32[32 * 32];
                for (int j = 0; j < colors.Length; j++)
                {
                    colors[j] = new Color32(128, 128, 128, 255);
                }

                placeholder.SetPixels32(colors);
                placeholder.Apply();
                thumbnail.image = placeholder;
            }

            layerButton.Add(thumbnail);

            var noiseEntry = targetSettings != null
                ? GetPaintNoiseLayer(targetSettings.settings.paintNoiseLayers, index)
                : null;
            if (noiseEntry != null && noiseEntry.noiseTexture != null)
            {
                var noiseBadge = new Image();
                noiseBadge.AddToClassList("layer-noise-badge");
                noiseBadge.image = Resources.Load<Texture2D>("Icons/noise");
                noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                layerButton.Add(noiseBadge);
            }

            // Add layer name
            var layerName = new Label(layer.name);
            layerName.AddToClassList("layer-name");
            layerButton.Add(layerName);

            return layerButton;
        }

        private void UpdatePaintNoiseUI()
        {
            if (targetSettings == null) return;

            if (paintNoiseTextureField == null && paintNoiseStrengthSlider == null && paintNoiseEdgeSlider == null
                && paintNoiseOffsetField == null && paintNoiseResponseCurve == null
                && paintNoiseSizeSlider == null && paintNoiseInvertToggle == null && paintNoiseFoldout == null)
                return;

            int layerIndex = targetSettings.settings.selectedLayerIndex;
            var entry = GetPaintNoiseLayer(targetSettings.settings.paintNoiseLayers, layerIndex);

            var noiseTexture = entry != null ? entry.noiseTexture : null;
            float noiseStrength = entry != null ? entry.noiseStrength : 1f;
            float noiseEdge = entry != null ? entry.noiseEdge : 0f;
            float noiseSize = entry != null ? entry.noiseWorldSizeMeters : 10f;
            Vector2 noiseOffset = entry != null ? entry.noiseOffset : Vector2.zero;
            AnimationCurve noiseResponse = entry != null && entry.noiseResponse != null ? entry.noiseResponse : SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve();
            bool noiseInvert = entry != null && entry.noiseInvert;

            if (paintNoiseFoldout != null)
            {
                paintNoiseFoldout.text = $"Noise {GetPaintLayerDisplayName(layerIndex)}";
                SetNoiseHeaderIconTint(paintNoiseFoldout, noiseTexture != null);
            }

            if (paintNoiseTextureField != null)
                paintNoiseTextureField.SetValueWithoutNotify(noiseTexture);
            if (paintNoiseStrengthSlider != null)
                paintNoiseStrengthSlider.SetValueWithoutNotify(noiseStrength);
            if (paintNoiseStrengthField != null)
                paintNoiseStrengthField.SetValueWithoutNotify(noiseStrength);
            if (paintNoiseEdgeSlider != null)
                paintNoiseEdgeSlider.SetValueWithoutNotify(noiseEdge);
            if (paintNoiseEdgeField != null)
                paintNoiseEdgeField.SetValueWithoutNotify(noiseEdge);
            if (paintNoiseSizeSlider != null)
                paintNoiseSizeSlider.SetValueWithoutNotify(noiseSize);
            if (paintNoiseSizeField != null)
                paintNoiseSizeField.SetValueWithoutNotify(noiseSize);
            if (paintNoiseOffsetField != null)
                paintNoiseOffsetField.SetValueWithoutNotify(noiseOffset);
            if (paintNoiseResponseCurve != null)
                paintNoiseResponseCurve.SetValueWithoutNotify(noiseResponse.CloneCurve());
            if (paintNoiseInvertToggle != null)
            {
                paintNoiseInvertToggle.SetValueWithoutNotify(noiseInvert);
                UpdateNoiseInvertToggleVisual(paintNoiseInvertToggle);
            }
        }

        private void UpdateDetailNoiseUI()
        {
            if (targetSettings == null) return;

            if (detailNoiseTextureField == null && detailNoiseSizeSlider == null && detailNoiseOffsetField == null
                && detailNoiseThresholdSlider == null && detailNoiseInvertToggle == null && detailNoiseFoldout == null)
                return;

            var selectedLayerIndices = GetSelectedDetailLayerIndices();
            if (selectedLayerIndices.Count == 0)
                return;

            int layerIndex = selectedLayerIndices[0];
            var entry = GetDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, layerIndex);

            var noiseTexture = entry != null ? entry.noiseTexture : null;
            float noiseSize = entry != null ? entry.noiseWorldSizeMeters : 10f;
            Vector2 noiseOffset = entry != null ? entry.noiseOffset : Vector2.zero;
            float noiseThreshold = entry != null ? entry.noiseThreshold : 0f;
            bool noiseInvert = entry != null && entry.noiseInvert;

            bool mixedTexture = false;
            bool mixedSize = false;
            bool mixedOffset = false;
            bool mixedThreshold = false;
            bool mixedInvert = false;
            bool hasNoiseTexture = noiseTexture != null;

            for (int i = 1; i < selectedLayerIndices.Count; i++)
            {
                int index = selectedLayerIndices[i];
                var otherEntry = GetDetailNoiseLayer(targetSettings.settings.detailNoiseLayers, index);
                var otherTexture = otherEntry != null ? otherEntry.noiseTexture : null;
                float otherSize = otherEntry != null ? otherEntry.noiseWorldSizeMeters : 10f;
                Vector2 otherOffset = otherEntry != null ? otherEntry.noiseOffset : Vector2.zero;
                float otherThreshold = otherEntry != null ? otherEntry.noiseThreshold : 0f;
                bool otherInvert = otherEntry != null && otherEntry.noiseInvert;

                if (otherTexture != noiseTexture)
                    mixedTexture = true;
                if (!Mathf.Approximately(otherSize, noiseSize))
                    mixedSize = true;
                if (otherOffset != noiseOffset)
                    mixedOffset = true;
                if (!Mathf.Approximately(otherThreshold, noiseThreshold))
                    mixedThreshold = true;
                if (otherInvert != noiseInvert)
                    mixedInvert = true;
                if (otherTexture != null)
                    hasNoiseTexture = true;
            }

            if (detailNoiseFoldout != null)
            {
                detailNoiseFoldout.text = selectedLayerIndices.Count > 1
                    ? "Noise Multiple Layers"
                    : $"Noise {GetDetailLayerDisplayName(layerIndex)}";
                SetNoiseHeaderIconTint(detailNoiseFoldout, hasNoiseTexture);
            }

            if (detailNoiseTextureField != null)
            {
                detailNoiseTextureField.SetValueWithoutNotify(noiseTexture);
                detailNoiseTextureField.showMixedValue = mixedTexture;
            }
            if (detailNoiseSizeSlider != null)
            {
                detailNoiseSizeSlider.SetValueWithoutNotify(noiseSize);
                detailNoiseSizeSlider.showMixedValue = mixedSize;
            }
            if (detailNoiseSizeField != null)
            {
                detailNoiseSizeField.SetValueWithoutNotify(noiseSize);
                detailNoiseSizeField.showMixedValue = mixedSize;
            }
            if (detailNoiseOffsetField != null)
            {
                detailNoiseOffsetField.SetValueWithoutNotify(noiseOffset);
                detailNoiseOffsetField.showMixedValue = mixedOffset;
            }
            if (detailNoiseThresholdSlider != null)
            {
                detailNoiseThresholdSlider.SetValueWithoutNotify(noiseThreshold);
                detailNoiseThresholdSlider.showMixedValue = mixedThreshold;
            }
            if (detailNoiseThresholdField != null)
            {
                detailNoiseThresholdField.SetValueWithoutNotify(noiseThreshold);
                detailNoiseThresholdField.showMixedValue = mixedThreshold;
            }
            if (detailNoiseInvertToggle != null)
            {
                detailNoiseInvertToggle.SetValueWithoutNotify(noiseInvert);
                detailNoiseInvertToggle.showMixedValue = mixedInvert;
                UpdateNoiseInvertToggleVisual(detailNoiseInvertToggle);
            }
        }

        private void UpdateTreeNoiseUI()
        {
            if (targetSettings == null) return;

            if (treeNoiseTextureField == null && treeNoiseSizeSlider == null && treeNoiseOffsetField == null
                && treeNoiseThresholdSlider == null && treeNoiseInvertToggle == null && treeNoiseFoldout == null)
                return;

            var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
            if (selectedPrototypeIndices.Count == 0)
                return;

            int prototypeIndex = selectedPrototypeIndices[0];
            var entry = GetTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, prototypeIndex);

            var noiseTexture = entry != null ? entry.noiseTexture : null;
            float noiseSize = entry != null ? entry.noiseWorldSizeMeters : 10f;
            Vector2 noiseOffset = entry != null ? entry.noiseOffset : Vector2.zero;
            float noiseThreshold = entry != null ? entry.noiseThreshold : 0f;
            bool noiseInvert = entry != null && entry.noiseInvert;

            bool mixedTexture = false;
            bool mixedSize = false;
            bool mixedOffset = false;
            bool mixedThreshold = false;
            bool mixedInvert = false;
            bool hasNoiseTexture = noiseTexture != null;

            for (int i = 1; i < selectedPrototypeIndices.Count; i++)
            {
                int index = selectedPrototypeIndices[i];
                var otherEntry = GetTreeNoiseLayer(targetSettings.settings.treeNoiseLayers, index);
                var otherTexture = otherEntry != null ? otherEntry.noiseTexture : null;
                float otherSize = otherEntry != null ? otherEntry.noiseWorldSizeMeters : 10f;
                Vector2 otherOffset = otherEntry != null ? otherEntry.noiseOffset : Vector2.zero;
                float otherThreshold = otherEntry != null ? otherEntry.noiseThreshold : 0f;
                bool otherInvert = otherEntry != null && otherEntry.noiseInvert;

                if (otherTexture != noiseTexture)
                    mixedTexture = true;
                if (!Mathf.Approximately(otherSize, noiseSize))
                    mixedSize = true;
                if (otherOffset != noiseOffset)
                    mixedOffset = true;
                if (!Mathf.Approximately(otherThreshold, noiseThreshold))
                    mixedThreshold = true;
                if (otherInvert != noiseInvert)
                    mixedInvert = true;
                if (otherTexture != null)
                    hasNoiseTexture = true;
            }

            if (treeNoiseFoldout != null)
            {
                treeNoiseFoldout.text = selectedPrototypeIndices.Count > 1
                    ? "Noise Multiple Trees"
                    : $"Noise {GetTreePrototypeDisplayName(prototypeIndex)}";
                SetNoiseHeaderIconTint(treeNoiseFoldout, hasNoiseTexture);
            }

            if (treeNoiseTextureField != null)
            {
                treeNoiseTextureField.SetValueWithoutNotify(noiseTexture);
                treeNoiseTextureField.showMixedValue = mixedTexture;
            }
            if (treeNoiseSizeSlider != null)
            {
                treeNoiseSizeSlider.SetValueWithoutNotify(noiseSize);
                treeNoiseSizeSlider.showMixedValue = mixedSize;
            }
            if (treeNoiseSizeField != null)
            {
                treeNoiseSizeField.SetValueWithoutNotify(noiseSize);
                treeNoiseSizeField.showMixedValue = mixedSize;
            }
            if (treeNoiseOffsetField != null)
            {
                treeNoiseOffsetField.SetValueWithoutNotify(noiseOffset);
                treeNoiseOffsetField.showMixedValue = mixedOffset;
            }
            if (treeNoiseThresholdSlider != null)
            {
                treeNoiseThresholdSlider.SetValueWithoutNotify(noiseThreshold);
                treeNoiseThresholdSlider.showMixedValue = mixedThreshold;
            }
            if (treeNoiseThresholdField != null)
            {
                treeNoiseThresholdField.SetValueWithoutNotify(noiseThreshold);
                treeNoiseThresholdField.showMixedValue = mixedThreshold;
            }
            if (treeNoiseInvertToggle != null)
            {
                treeNoiseInvertToggle.SetValueWithoutNotify(noiseInvert);
                treeNoiseInvertToggle.showMixedValue = mixedInvert;
                UpdateNoiseInvertToggleVisual(treeNoiseInvertToggle);
            }
        }

        private List<int> GetSelectedDetailLayerIndices()
        {
            var indices = new List<int>();
            if (targetSettings == null)
                return indices;

            var settings = targetSettings.settings;
            if (settings.selectedDetailLayerIndices != null && settings.selectedDetailLayerIndices.Count > 0)
            {
                var used = new HashSet<int>();
                for (int i = 0; i < settings.selectedDetailLayerIndices.Count; i++)
                {
                    int index = settings.selectedDetailLayerIndices[i];
                    if (used.Add(index))
                        indices.Add(index);
                }
            }
            else
            {
                indices.Add(settings.selectedDetailLayerIndex);
            }

            return indices;
        }

        private List<int> GetSelectedTreePrototypeIndices()
        {
            var indices = new List<int>();
            if (targetSettings == null)
                return indices;

            var settings = targetSettings.settings;
            if (settings.selectedTreePrototypeIndices != null && settings.selectedTreePrototypeIndices.Count > 0)
            {
                var used = new HashSet<int>();
                for (int i = 0; i < settings.selectedTreePrototypeIndices.Count; i++)
                {
                    int index = settings.selectedTreePrototypeIndices[i];
                    if (used.Add(index))
                        indices.Add(index);
                }
            }
            else
            {
                indices.Add(settings.selectedTreePrototypeIndex);
            }

            return indices;
        }

        private DetailNoiseLayerSettings GetDetailNoiseLayer(List<DetailNoiseLayerSettings> list, int layerIndex)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry != null && entry.detailLayerIndex == layerIndex)
                    return entry;
            }
            return null;
        }

        private TreeNoisePrototypeSettings GetTreeNoiseLayer(List<TreeNoisePrototypeSettings> list, int prototypeIndex)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry != null && entry.treePrototypeIndex == prototypeIndex)
                    return entry;
            }
            return null;
        }

        private PaintNoiseLayerSettings GetPaintNoiseLayer(List<PaintNoiseLayerSettings> list, int layerIndex)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry != null && entry.paintLayerIndex == layerIndex)
                    return entry;
            }
            return null;
        }

        private DetailNoiseLayerSettings GetOrCreateDetailNoiseLayer(List<DetailNoiseLayerSettings> list, int layerIndex)
        {
            if (list == null)
                list = new List<DetailNoiseLayerSettings>();

            var entry = GetDetailNoiseLayer(list, layerIndex);
            if (entry != null) return entry;

            entry = new DetailNoiseLayerSettings { detailLayerIndex = layerIndex };
            list.Add(entry);
            return entry;
        }

        private TreeNoisePrototypeSettings GetOrCreateTreeNoiseLayer(List<TreeNoisePrototypeSettings> list, int prototypeIndex)
        {
            if (list == null)
                list = new List<TreeNoisePrototypeSettings>();

            var entry = GetTreeNoiseLayer(list, prototypeIndex);
            if (entry != null) return entry;

            entry = new TreeNoisePrototypeSettings { treePrototypeIndex = prototypeIndex };
            list.Add(entry);
            return entry;
        }

        private PaintNoiseLayerSettings GetOrCreatePaintNoiseLayer(List<PaintNoiseLayerSettings> list, int layerIndex)
        {
            if (list == null)
                list = new List<PaintNoiseLayerSettings>();

            var entry = GetPaintNoiseLayer(list, layerIndex);
            if (entry != null) return entry;

            entry = new PaintNoiseLayerSettings { paintLayerIndex = layerIndex };
            list.Add(entry);
            return entry;
        }

        private string GetDetailLayerDisplayName(int layerIndex)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null || targetTerrain.terrainData.detailPrototypes == null)
                return $"Layer {layerIndex}";

            var detailPrototypes = targetTerrain.terrainData.detailPrototypes;
            if (layerIndex < 0 || layerIndex >= detailPrototypes.Length)
                return $"Layer {layerIndex}";

            var prototype = detailPrototypes[layerIndex];
            return prototype != null && prototype.prototype != null ? prototype.prototype.name : $"Layer {layerIndex}";
        }

        private string GetTreePrototypeDisplayName(int prototypeIndex)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null || targetTerrain.terrainData.treePrototypes == null)
                return $"Tree {prototypeIndex}";

            var treePrototypes = targetTerrain.terrainData.treePrototypes;
            if (prototypeIndex < 0 || prototypeIndex >= treePrototypes.Length)
                return $"Tree {prototypeIndex}";

            var prototype = treePrototypes[prototypeIndex];
            return prototype != null && prototype.prefab != null ? prototype.prefab.name : $"Tree {prototypeIndex}";
        }

        private string GetPaintLayerDisplayName(int layerIndex)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null || targetTerrain.terrainData.terrainLayers == null)
                return $"Layer {layerIndex}";

            var terrainLayers = targetTerrain.terrainData.terrainLayers;
            if (layerIndex < 0 || layerIndex >= terrainLayers.Length)
                return $"Layer {layerIndex}";

            var layer = terrainLayers[layerIndex];
            return layer != null ? layer.name : $"Layer {layerIndex}";
        }

        private void InsertNoiseFoldoutIcon(Foldout foldout)
        {
            if (foldout == null) return;

            var toggle = foldout.Q<Toggle>();
            if (toggle == null) return;

            var existing = toggle.Q<Image>(className: "noise-header-icon");
            if (existing != null) return;

            var icon = new Image
            {
                image = Resources.Load<Texture2D>("Icons/noise"),
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.AddToClassList("noise-header-icon");

            toggle.Add(icon);
        }

        private void UpdateNoiseInvertToggleVisual(Toggle toggle)
        {
            if (toggle == null) return;
            var icon = toggle.Q<Image>(className: "noise-invert-icon");
            if (icon == null) return;

            bool isOn = toggle.value;
            icon.image = Resources.Load<Texture2D>(isOn ? "Icons/invert" : "Icons/uninvert");
            icon.tintColor = isOn ? noiseInvertOnColor : noiseInvertOffColor;
        }

        private void SetNoiseHeaderIconTint(Foldout foldout, bool hasTexture)
        {
            if (foldout == null) return;

            var toggle = foldout.Q<Toggle>();
            if (toggle == null) return;

            var icon = toggle.Q<Image>(className: "noise-header-icon");
            if (icon == null) return;

            icon.tintColor = hasTexture ? noiseActiveColor : noiseEmptyColor;
        }

        private void UpdateTabText(Button tabButton, string baseText, bool isOverrideActive)
        {
            SplineTabButtonUtility.SetOverrideState(tabButton, baseText, isOverrideActive);
        }

        private void EnsureTabButtonContent(Button button, string labelText)
        {
            SplineTabButtonUtility.Ensure(button, labelText);
        }

        private void RecordChange()
        {
            if (targetSettings != null)
            {
                Undo.RecordObject(targetSettings, "Modify Spline Terrain Settings");
            }
        }

        private void ApplyChanges()
        {
            if (targetSettings != null)
            {
                EditorUtility.SetDirty(targetSettings);
                NotifySettingsChanged(targetSettings);
            }
        }

        private void OnApplyClicked()
        {
            if (targetSettings == null || targetSettings.gameObject == null)
            {
                Debug.LogWarning("Cannot apply spline: No target settings or GameObject found");
                return;
            }

            var splineContainer = targetSettings.GetComponent<SplineContainer>();
            if (splineContainer == null)
            {
                Debug.LogWarning("Cannot apply spline: No SplineContainer found on target GameObject");
                return;
            }

            if (TryApplyViaMainWindow(splineContainer))
            {
                Debug.Log($"Applied spline '{splineContainer.name}' via Terra Splines window");
                return;
            }

            // Find target terrain
            var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("Cannot apply spline: No terrain found in scene");
                return;
            }

            // Get terrain data
            var terrainData = terrain.terrainData;
            if (terrainData == null)
            {
                Debug.LogWarning("Cannot apply spline: No terrain data found");
                return;
            }

            // Create global settings for the operation
            var globals = new TerraSplinesTool.GlobalSettings
            {
                mode = targetSettings.settings.mode,
                brushSizeMeters = targetSettings.settings.sizeMeters,
                strength = targetSettings.settings.strength,
                sampleStepMeters = targetSettings.settings.sampleStep,
                brushHardness = targetSettings.settings.hardness,
                operationHeight = targetSettings.settings.operationHeight,
                operationPaint = targetSettings.settings.operationPaint,
                operationHole = targetSettings.settings.operationHole,
                operationFill = targetSettings.settings.operationFill,
                operationAddDetail = targetSettings.settings.operationAddDetail,
                operationRemoveDetail = targetSettings.settings.operationRemoveDetail,
                operationAddTree = targetSettings.settings.operationAddTree,
                operationRemoveTree = targetSettings.settings.operationRemoveTree,
                globalSelectedLayerIndex = targetSettings.settings.selectedLayerIndex,
                globalPaintStrength = targetSettings.settings.paintStrength,
                globalPaintBlend = targetSettings.settings.paintBlend,
                globalPaintThreshold = targetSettings.settings.paintThreshold,
                globalSelectedDetailLayerIndex = targetSettings.settings.selectedDetailLayerIndex,
                globalSelectedDetailLayerIndices = (targetSettings.settings.selectedDetailLayerIndices != null && targetSettings.settings.selectedDetailLayerIndices.Count > 0)
                    ? targetSettings.settings.selectedDetailLayerIndices.ToArray()
                    : null,
                globalDetailStrength = targetSettings.settings.detailStrength,
                globalDetailMode = targetSettings.settings.detailMode,
                globalDetailSlopeLimitDegrees = targetSettings.settings.detailSlopeLimitDegrees,
                globalDetailFalloffPower = targetSettings.settings.detailFalloffPower,
                globalDetailSpreadRadius = targetSettings.settings.detailSpreadRadius,
                globalDetailRemoveThreshold = targetSettings.settings.detailRemoveThreshold,
                globalDetailNoiseLayers = (targetSettings.settings.detailNoiseLayers != null && targetSettings.settings.detailNoiseLayers.Count > 0)
                    ? new List<DetailNoiseLayerSettings>(targetSettings.settings.detailNoiseLayers)
                    : null,
                globalSelectedTreePrototypeIndex = targetSettings.settings.selectedTreePrototypeIndex,
                globalSelectedTreePrototypeIndices = (targetSettings.settings.selectedTreePrototypeIndices != null && targetSettings.settings.selectedTreePrototypeIndices.Count > 0)
                    ? targetSettings.settings.selectedTreePrototypeIndices.ToArray()
                    : null,
                globalTreeStrength = targetSettings.settings.treeStrength,
                globalTreeTargetDensity = targetSettings.settings.treeTargetDensity,
                globalTreeSlopeLimitDegrees = targetSettings.settings.treeSlopeLimitDegrees,
                globalTreeFalloffPower = targetSettings.settings.treeFalloffPower,
                globalTreeHeightMultiplier = (targetSettings.settings.treeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve(),
                globalTreeRemoveThreshold = targetSettings.settings.treeRemoveThreshold,
                globalTreeNoiseLayers = (targetSettings.settings.treeNoiseLayers != null && targetSettings.settings.treeNoiseLayers.Count > 0)
                    ? new List<TreeNoisePrototypeSettings>(targetSettings.settings.treeNoiseLayers)
                    : null,
            };

            // Get current terrain heights as baseline
            int resolution = terrainData.heightmapResolution;
            var currentHeights = terrainData.GetHeights(0, 0, resolution, resolution);
            var tempHeights = new float[resolution, resolution];

            // Create temporary holes array
            var currentHoles = TerraSplinesTool.GetTerrainHoles(terrain);
            var heightmapHoles = TerraSplinesTool.ConvertTerrainHolesToHeightmapHoles(terrain, currentHoles);
            var tempHoles = new bool[resolution, resolution];

            // Get current alphamaps as baseline
            float[,,] currentAlphamaps = null;
            if (terrainData.alphamapLayers > 0)
            {
                currentAlphamaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            }

            // Create temporary alphamaps if needed
            float[,,] tempAlphamaps = null;
            if (terrainData.alphamapLayers > 0 && currentAlphamaps != null)
            {
                int alphamapHeight = terrainData.alphamapHeight;
                int alphamapWidth = terrainData.alphamapWidth;
                int layers = terrainData.alphamapLayers;
                tempAlphamaps = new float[alphamapHeight, alphamapWidth, layers];
            }

            // Get current detail layers as baseline (for detail operations)
            int[][,] currentDetailLayers = null;
            int[][,] tempDetailLayers = null;
            if (terrainData.detailPrototypes != null && terrainData.detailPrototypes.Length > 0 && terrainData.detailWidth > 0 && terrainData.detailHeight > 0)
            {
                int detailLayerCount = terrainData.detailPrototypes.Length;
                currentDetailLayers = new int[detailLayerCount][,];
                tempDetailLayers = new int[detailLayerCount][,];
                for (int layer = 0; layer < detailLayerCount; layer++)
                {
                    currentDetailLayers[layer] = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer);
                    tempDetailLayers[layer] = new int[terrainData.detailHeight, terrainData.detailWidth];
                }
            }

            TreeInstance[] currentTreeInstances = terrainData.treeInstances;
            TreeInstance[] tempTreeInstances = null;

            // Apply the spline to the terrain
            TerraSplinesTool.ApplySingleSplineToBaseline(terrain, splineContainer, targetSettings.settings, globals,
                currentHeights, tempHeights, heightmapHoles, tempHoles, currentAlphamaps, tempAlphamaps, currentDetailLayers, tempDetailLayers,
                currentTreeInstances, trees => tempTreeInstances = trees);

            // Apply heights with undo support
            TerraSplinesTool.ApplyFinalToTerrainWithUndo(terrain, tempHeights, $"Apply Spline '{splineContainer.name}'");

            // Apply alphamaps if paint operation is enabled
            if (targetSettings.settings.operationPaint && tempAlphamaps != null)
            {
                terrainData.SetAlphamaps(0, 0, tempAlphamaps);
            }

            // Apply holes if hole or fill is enabled
            if ((targetSettings.settings.operationHole || targetSettings.settings.operationFill) && tempHoles != null)
            {
                var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, tempHoles);
                TerraSplinesTool.ApplyHolesToTerrain(terrain, terrainHoles);
            }

            // Apply detail layers if add/remove detail is enabled
            if ((targetSettings.settings.operationAddDetail || targetSettings.settings.operationRemoveDetail) && tempDetailLayers != null)
            {
                TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, tempDetailLayers);
            }

            if ((targetSettings.settings.operationAddTree || targetSettings.settings.operationRemoveTree) && tempTreeInstances != null)
            {
                TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, tempTreeInstances);
            }

            Debug.Log($"Applied spline '{splineContainer.name}' to terrain");

            // Notify the main window to refresh its baseline data
            NotifyMainWindowOfTerrainChange();
        }

        bool TryApplyViaMainWindow(SplineContainer container)
        {
            if (container == null) return false;

            var windows = Resources.FindObjectsOfTypeAll<TerraSplinesWindow>();
            foreach (var window in windows)
            {
                if (window != null && window.TryApplySpline(container))
                {
                    window.Repaint();
                    return true;
                }
            }

            return false;
        }

        private void NotifyMainWindowOfTerrainChange()
        {
            // Find all terrain spline windows and refresh their baseline data
            var windows = Resources.FindObjectsOfTypeAll<TerraSplinesWindow>();
            foreach (var window in windows)
            {
                if (window != null)
                {
                    // Force the window to refresh its baseline data from the current terrain state
                    window.RefreshBaselineFromTerrain();
                    window.Repaint();
                }
            }
        }

        private void OnDuplicateClicked()
        {
            if (targetSettings == null || targetSettings.gameObject == null)
            {
                Debug.LogWarning("Cannot duplicate spline: No target settings or GameObject found");
                return;
            }

            var originalGameObject = targetSettings.gameObject;
            var parent = originalGameObject.transform.parent;

            if (parent == null)
            {
                Debug.LogWarning("Cannot duplicate spline: No parent transform found");
                return;
            }

            // Register undo operation
            Undo.RegisterCompleteObjectUndo(parent, "Duplicate Spline");

            // Duplicate the GameObject
            var duplicatedGameObject = UnityEngine.Object.Instantiate(originalGameObject, parent);

            // Position as next sibling
            int originalIndex = originalGameObject.transform.GetSiblingIndex();
            duplicatedGameObject.transform.SetSiblingIndex(originalIndex + 1);

            // Copy SplineTerrainSettings if it exists
            var originalSettings = originalGameObject.GetComponent<TerrainSplineSettings>();
            if (originalSettings != null)
            {
                var duplicatedSettings = duplicatedGameObject.GetComponent<TerrainSplineSettings>();
                if (duplicatedSettings == null)
                {
                    duplicatedSettings = duplicatedGameObject.AddComponent<TerrainSplineSettings>();
                }

                // Copy the settings
                duplicatedSettings.settings = new SplineStrokeSettings
                {
                    enabled = originalSettings.settings.enabled,
                    overrideMode = originalSettings.settings.overrideMode,
                    mode = originalSettings.settings.mode,
                    operationHeight = originalSettings.settings.operationHeight,
                    operationPaint = originalSettings.settings.operationPaint,
                    operationHole = originalSettings.settings.operationHole,
                    operationFill = originalSettings.settings.operationFill,
                    operationAddDetail = originalSettings.settings.operationAddDetail,
                    operationRemoveDetail = originalSettings.settings.operationRemoveDetail,
                    operationAddTree = originalSettings.settings.operationAddTree,
                    operationRemoveTree = originalSettings.settings.operationRemoveTree,
                    overrideBrush = originalSettings.settings.overrideBrush,
                    sizeMeters = originalSettings.settings.sizeMeters,
                    strength = originalSettings.settings.strength,
                    sampleStep = originalSettings.settings.sampleStep,
                    hardness = originalSettings.settings.hardness,
                    overrideSizeMultiplier = originalSettings.settings.overrideSizeMultiplier,
                    sizeMultiplier = originalSettings.settings.sizeMultiplier,
                    overridePaint = originalSettings.settings.overridePaint,
                    selectedLayerIndex = originalSettings.settings.selectedLayerIndex,
                    paintStrength = originalSettings.settings.paintStrength,
                    paintBlend = originalSettings.settings.paintBlend,
                    paintThreshold = originalSettings.settings.paintThreshold,
                    overrideDetail = originalSettings.settings.overrideDetail,
                    selectedDetailLayerIndex = originalSettings.settings.selectedDetailLayerIndex,
                    selectedDetailLayerIndices = originalSettings.settings.selectedDetailLayerIndices != null
                        ? new List<int>(originalSettings.settings.selectedDetailLayerIndices)
                        : new List<int>(),
                    detailStrength = originalSettings.settings.detailStrength,
                    detailMode = originalSettings.settings.detailMode,
                    detailSlopeLimitDegrees = originalSettings.settings.detailSlopeLimitDegrees,
                    detailFalloffPower = originalSettings.settings.detailFalloffPower,
                    detailSpreadRadius = originalSettings.settings.detailSpreadRadius,
                    detailRemoveThreshold = originalSettings.settings.detailRemoveThreshold,
                    detailNoiseLayers = CloneDetailNoiseLayers(originalSettings.settings.detailNoiseLayers),
                    overrideTrees = originalSettings.settings.overrideTrees,
                    selectedTreePrototypeIndex = originalSettings.settings.selectedTreePrototypeIndex,
                    selectedTreePrototypeIndices = originalSettings.settings.selectedTreePrototypeIndices != null
                        ? new List<int>(originalSettings.settings.selectedTreePrototypeIndices)
                        : new List<int>(),
                    treeStrength = originalSettings.settings.treeStrength,
                    treeTargetDensity = originalSettings.settings.treeTargetDensity,
                    treeSlopeLimitDegrees = originalSettings.settings.treeSlopeLimitDegrees,
                    treeFalloffPower = originalSettings.settings.treeFalloffPower,
                    treeHeightMultiplier = (originalSettings.settings.treeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve(),
                    treeRemoveThreshold = originalSettings.settings.treeRemoveThreshold,
                    treeNoiseLayers = CloneTreeNoiseLayers(originalSettings.settings.treeNoiseLayers)

                };
            }

            // Register the duplicated GameObject for undo
            Undo.RegisterCreatedObjectUndo(duplicatedGameObject, "Duplicate Spline");

            // Auto-select the newly created spline in the hierarchy
            Selection.activeGameObject = duplicatedGameObject;
            EditorGUIUtility.PingObject(duplicatedGameObject);

            Debug.Log($"Duplicated spline '{originalGameObject.name}' as '{duplicatedGameObject.name}'");
        }

        private void OnDeleteClicked()
        {
            if (targetSettings == null || targetSettings.gameObject == null)
            {
                Debug.LogWarning("Cannot delete spline: No target settings or GameObject found");
                return;
            }

            var gameObjectToDelete = targetSettings.gameObject;
            string objectName = gameObjectToDelete.name;

            // Register undo operation
            Undo.DestroyObjectImmediate(gameObjectToDelete);

            Debug.Log($"Deleted spline '{objectName}'");
        }

        private static List<DetailNoiseLayerSettings> CloneDetailNoiseLayers(List<DetailNoiseLayerSettings> source)
        {
            if (source == null || source.Count == 0) return new List<DetailNoiseLayerSettings>();
            var copy = new List<DetailNoiseLayerSettings>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                if (entry == null) continue;
                copy.Add(new DetailNoiseLayerSettings
                {
                    detailLayerIndex = entry.detailLayerIndex,
                    noiseTexture = entry.noiseTexture,
                    noiseStrength = 1f,
                    noiseWorldSizeMeters = entry.noiseWorldSizeMeters,
                    noiseOffset = entry.noiseOffset,
                    noiseThreshold = entry.noiseThreshold,
                    noiseInvert = entry.noiseInvert
                });
            }
            return copy;
        }

        private static List<TreeNoisePrototypeSettings> CloneTreeNoiseLayers(List<TreeNoisePrototypeSettings> source)
        {
            if (source == null || source.Count == 0) return new List<TreeNoisePrototypeSettings>();
            var copy = new List<TreeNoisePrototypeSettings>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                if (entry == null) continue;
                copy.Add(new TreeNoisePrototypeSettings
                {
                    treePrototypeIndex = entry.treePrototypeIndex,
                    noiseTexture = entry.noiseTexture,
                    noiseStrength = entry.noiseStrength,
                    noiseWorldSizeMeters = entry.noiseWorldSizeMeters,
                    noiseOffset = entry.noiseOffset,
                    noiseThreshold = entry.noiseThreshold,
                    noiseInvert = entry.noiseInvert
                });
            }
            return copy;
        }

        private void UpdateOperationWarning()
        {
            if (operationWarningLabel == null || targetSettings == null) return;

            var s = targetSettings.settings;

            if (!s.overrideMode)
            {
                SetWarningIconState(operationWarningLabel, false, FeatureDisabledTooltip);
                return;
            }

            bool requiresPaint = s.operationPaint;
            bool requiresHole = s.operationHole;
            bool requiresFill = s.operationFill;
            bool requiresDetail = s.operationAddDetail || s.operationRemoveDetail;
            bool requiresTree = s.operationAddTree || s.operationRemoveTree;

            bool featureHoleEnabled = LoadBoolPref("FeatureHoleEnabled", true);
            bool featurePaintEnabled = LoadBoolPref("FeaturePaintEnabled", true);
            bool featureDetailEnabled = LoadBoolPref("FeatureDetailEnabled", true);
            bool featureTreeEnabled = LoadBoolPref("FeatureTreeEnabled", true);

            bool shouldShow = false;
            if (requiresPaint && !featurePaintEnabled) shouldShow = true;
            else if ((requiresHole || requiresFill) && !featureHoleEnabled) shouldShow = true;
            else if (requiresDetail && !featureDetailEnabled) shouldShow = true;
            else if (requiresTree && !featureTreeEnabled) shouldShow = true;

            SetWarningIconState(
                operationWarningLabel,
                shouldShow,
                GetDisabledFeatureWarningTooltip(
                    requiresPaint,
                    requiresHole,
                    requiresFill,
                    requiresDetail,
                    requiresTree,
                    featureHoleEnabled,
                    featurePaintEnabled,
                    featureDetailEnabled,
                    featureTreeEnabled));
        }

        Toggle CreateOperationIconToggle(string iconName, string tooltip)
        {
            var toggle = new Toggle { tooltip = tooltip };
            toggle.AddToClassList("operation-icon-toggle");

            var icon = new Image();
            icon.AddToClassList("operation-icon");
            icon.image = Resources.Load<Texture2D>($"Icons/{iconName}");
            toggle.Add(icon);

            RefreshOperationToggleVisual(toggle);
            return toggle;
        }

        static void UpdateLastOperationToggleClass(VisualElement container)
        {
            if (container == null)
                return;

            for (int i = 0; i < container.childCount; i++)
            {
                if (container[i].ClassListContains("operation-icon-toggle"))
                    container[i].RemoveFromClassList("operation-icon-toggle-last");
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                if (!container[i].ClassListContains("operation-icon-toggle"))
                    continue;

                container[i].AddToClassList("operation-icon-toggle-last");
                break;
            }
        }

        void RefreshOperationToggleVisual(Toggle toggle)
        {
            if (toggle == null) return;
            var icon = toggle.Q<Image>(className: "operation-icon");
            if (icon == null) return;

            bool isBlocked = !toggle.enabledInHierarchy || !toggle.enabledSelf;
            bool isSelected = toggle.value;
            icon.tintColor = isBlocked ? opBlockedColor : (isSelected ? opSelectedColor : opAvailableColor);
        }

        void EnsureToggleCheckmarkVisible(Toggle toggle)
        {
            if (toggle == null) return;
            var input = toggle.Q<VisualElement>(className: "unity-toggle__input");
            if (input != null) input.style.display = DisplayStyle.Flex;
            var check = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (check != null) check.style.display = DisplayStyle.Flex;
        }

        void EnforceOperationRules(SplineStrokeSettings settings)
        {
            if (settings.operationHole)
            {
                // Hole is exclusive: disable everything else
                settings.operationHeight = false;
                settings.operationPaint = false;
                settings.operationFill = false;
                settings.operationAddDetail = false;
                settings.operationRemoveDetail = false;
                settings.operationAddTree = false;
                settings.operationRemoveTree = false;
            }

            if (settings.operationFill)
                settings.operationHole = false;

            if (settings.operationAddDetail)
                settings.operationRemoveDetail = false;
            else if (settings.operationRemoveDetail)
                settings.operationAddDetail = false;

            if (settings.operationAddTree)
                settings.operationRemoveTree = false;
            else if (settings.operationRemoveTree)
                settings.operationAddTree = false;

            if (!settings.operationHeight && !settings.operationPaint && !settings.operationHole && !settings.operationFill && !settings.operationAddDetail && !settings.operationRemoveDetail && !settings.operationAddTree && !settings.operationRemoveTree)
                settings.operationHeight = true;

            UpdateOperationInterlocks(settings);
        }

        void SyncOperationTogglesFromSettings(SplineStrokeSettings settings)
        {
            if (settings == null) return;
            opHeightToggle?.SetValueWithoutNotify(settings.operationHeight);
            opPaintToggle?.SetValueWithoutNotify(settings.operationPaint);
            opHoleToggle?.SetValueWithoutNotify(settings.operationHole);
            opFillToggle?.SetValueWithoutNotify(settings.operationFill);
            opAddDetailToggle?.SetValueWithoutNotify(settings.operationAddDetail);
            opRemoveDetailToggle?.SetValueWithoutNotify(settings.operationRemoveDetail);
            opAddTreeToggle?.SetValueWithoutNotify(settings.operationAddTree);
            opRemoveTreeToggle?.SetValueWithoutNotify(settings.operationRemoveTree);

            UpdateOperationInterlocks(settings);

            var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
            foreach (var toggle in toggles)
            {
                RefreshOperationToggleVisual(toggle);
            }

            if (!settings.overrideMode && globalModeIcon != null)
            {
                globalModeIcon.image = Resources.Load<Texture2D>("Icons/global");
                globalModeIcon.tintColor = opAvailableColor;
                globalModeIcon.style.display = DisplayStyle.Flex;
            }
        }

        void SetToggleEnabled(Toggle toggle, bool enabled)
        {
            if (toggle == null) return;
            toggle.SetEnabled(enabled);
            toggle.style.opacity = enabled ? 1f : 0.35f;
            RefreshOperationToggleVisual(toggle);
        }

        void ToggleOperationIconsDisplay(bool showIcons)
        {
            var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
            foreach (var toggle in toggles)
            {
                if (toggle == null) continue;
                toggle.style.display = showIcons ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void UpdateOperationInterlocks(SplineStrokeSettings settings)
        {
            if (settings == null) return;

            if (!settings.overrideMode)
            {
                ToggleOperationIconsDisplay(false);
                if (globalModeIcon != null)
                {
                    globalModeIcon.image = Resources.Load<Texture2D>("Icons/global");
                    globalModeIcon.tintColor = opAvailableColor;
                    globalModeIcon.style.display = DisplayStyle.Flex;
                }

                SetToggleEnabled(opHeightToggle, false);
                SetToggleEnabled(opPaintToggle, false);
                SetToggleEnabled(opHoleToggle, false);
                SetToggleEnabled(opFillToggle, false);
                SetToggleEnabled(opAddDetailToggle, false);
                SetToggleEnabled(opRemoveDetailToggle, false);
                SetToggleEnabled(opAddTreeToggle, false);
                SetToggleEnabled(opRemoveTreeToggle, false);
                return;
            }

            ToggleOperationIconsDisplay(true);
            if (globalModeIcon != null)
                globalModeIcon.style.display = DisplayStyle.None;

            // Hole blocks all others
            if (settings.operationHole)
            {
                SetToggleEnabled(opHeightToggle, false);
                SetToggleEnabled(opPaintToggle, false);
                SetToggleEnabled(opFillToggle, false);
                SetToggleEnabled(opAddDetailToggle, false);
                SetToggleEnabled(opRemoveDetailToggle, false);
                SetToggleEnabled(opAddTreeToggle, false);
                SetToggleEnabled(opRemoveTreeToggle, false);
                return;
            }

            // Fill blocks Hole
            bool holeBlocked = settings.operationFill;
            SetToggleEnabled(opHoleToggle, !holeBlocked);
            SetToggleEnabled(opFillToggle, true);

            // Detail modes block each other
            SetToggleEnabled(opAddDetailToggle, !settings.operationRemoveDetail);
            SetToggleEnabled(opRemoveDetailToggle, !settings.operationAddDetail);

            // Tree modes block each other
            SetToggleEnabled(opAddTreeToggle, !settings.operationRemoveTree);
            SetToggleEnabled(opRemoveTreeToggle, !settings.operationAddTree);

            // Height/Paint enabled unless Hole is active (handled above)
            SetToggleEnabled(opHeightToggle, true);
            SetToggleEnabled(opPaintToggle, true);
        }

        public override void OnWillBeDestroyed()
        {
            if (root != null)
            {
                root.UnregisterCallback<AttachToPanelEvent>(OnOverlayAttachToPanel);
                root.UnregisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);
            }

            Selection.selectionChanged -= OnSelectionChanged;
            SettingsChanged -= OnExternalSettingsChanged;
            FeatureFlagsChanged -= OnFeatureFlagsChanged;
            AssetThumbnailCache.PrefabThumbnailsChanged -= OnPrefabThumbnailsChanged;
            EditorApplication.update -= OnEditorUpdate;
            base.OnWillBeDestroyed();
        }
    }
}
