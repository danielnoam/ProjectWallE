using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Splines;

namespace GruelTerraSplines
{
    public partial class TerraSplinesWindow
    {
        void OnOverlaySettingsChanged(TerrainSplineSettings settings)
        {
            if (settings == null) return;

            var container = settings.GetComponent<SplineContainer>();
            if (container == null) return;

            var data = FindSplineItemDataByContainer(container);
            if (data == null) return;

            data.settings = settings.settings;

            var itemIndex = GetGlobalSplineIndex(data);
            if (itemIndex >= 0)
                splinesListView?.RefreshItem(itemIndex);
            else
                splinesListView?.RefreshItems();

            RefreshPreview();
        }

        void InsertSplineNoiseFoldoutIcon(Foldout foldout)
        {
            if (foldout == null) return;

            var toggle = foldout.Q<Toggle>();
            if (toggle == null) return;

            var existing = toggle.Q<Image>(className: "noise-header-icon");
            if (existing != null) return;

            var icon = new Image
            {
                image = LoadSplineIcon("noise"),
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.AddToClassList("noise-header-icon");

            var input = toggle.Q<VisualElement>(className: "unity-toggle__input");
            if (input != null)
            {
                var inputIndex = toggle.IndexOf(input);
                toggle.Insert(Mathf.Clamp(inputIndex + 1, 0, toggle.childCount), icon);
            }
            else
            {
                toggle.Insert(0, icon);
            }
        }

        void HideToggleLabel(Toggle toggle)
        {
            if (toggle == null) return;

            var label = toggle.Q<Label>(className: "unity-toggle__label");
            if (label != null)
            {
                label.style.display = DisplayStyle.None;
            }
        }

        void HideToggleInputAndLabel(Toggle toggle)
        {
            if (toggle == null) return;

            var input = toggle.Q<VisualElement>(className: "unity-toggle__input");
            if (input != null)
            {
                input.style.display = DisplayStyle.None;
            }

            HideToggleLabel(toggle);
        }

        void HideToggleInputCheckmarkAndLabel(Toggle toggle)
        {
            if (toggle == null) return;

            var input = toggle.Q<VisualElement>(className: "unity-toggle__input");
            if (input != null)
            {
                input.style.display = DisplayStyle.None;
            }

            var checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                checkmark.style.display = DisplayStyle.None;
            }

            var label = toggle.Q<Label>(className: "unity-label") ?? toggle.Q<Label>(className: "unity-toggle__label");
            if (label != null)
            {
                label.style.display = DisplayStyle.None;
            }
        }

        void ConfigureListRowFoldoutToggle(Toggle toggle)
        {
            HideToggleInputAndLabel(toggle);
        }

        void ConfigureTabOverrideToggle(Toggle toggle)
        {
            if (toggle == null) return;

            var label = toggle.Q<Label>(className: "unity-toggle__label") ?? toggle.Q<Label>(className: "unity-label");
            if (label != null)
            {
                label.style.marginLeft = 0;
                label.style.paddingLeft = 0;
            }
        }

        void ConfigureNoiseFoldout(Foldout foldout)
        {
            if (foldout == null) return;

            var toggle = foldout.Q<Toggle>();
            if (toggle == null) return;

            toggle.style.backgroundColor = new StyleColor(new Color32(48, 48, 48, 255));
            toggle.style.paddingTop = 4;
            toggle.style.paddingBottom = 4;
            toggle.style.marginTop = 0;
            toggle.style.marginBottom = 0;
            toggle.style.flexDirection = FlexDirection.Row;
            toggle.style.alignItems = Align.Center;
            toggle.style.minHeight = 22;

            var checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                checkmark.style.display = DisplayStyle.Flex;
                checkmark.style.opacity = 1;
                checkmark.style.marginLeft = 3;
                checkmark.style.marginRight = 6;
            }

            var label = toggle.Q<Label>(className: "unity-label");
            if (label != null)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Normal;
                label.style.marginLeft = 2;
                label.style.marginTop = 0;
                label.style.color = Color.white;
                label.style.alignSelf = Align.Center;
            }
        }

        void BindSplineItem(VisualElement element, SplineItemData data)
        {
            // Add comprehensive safety checks
            if (element == null || data == null)
            {
                Debug.LogWarning("BindSplineItem called with null element or data");
                return;
            }

            UnregisterSplineRowTabCallbacks(element);

            Action notifyOverlay = () => NotifyOverlaySettingsChanged(data.container);
            boundSplineRowElements.Add(element);

            // Get the content element (now directly in the container)
            var contentElement = element.Q<VisualElement>("spline-content");
            if (contentElement == null)
            {
                Debug.LogWarning("BindSplineItem: Could not find spline-content element");
                return;
            }

            // Bind spline item data to UI elements
            var foldoutToggle = element.Q<Toggle>("spline-foldout-toggle");
            var arrowLabel = foldoutToggle?.Q<Label>("spline-arrow");
            var enabledToggle = element.Q<Toggle>("enabled-toggle");
            var nameLabel = element.Q<Label>("spline-name-label");
            var overrideModeToggle = contentElement.Q<Toggle>("override-mode-toggle");
            var operationContainer = contentElement.Q<VisualElement>("operation-dropdown");
            Image globalModeIcon = null;
            Toggle opHeightToggle = null;
            Toggle opPaintToggle = null;
            Toggle opHoleToggle = null;
            Toggle opFillToggle = null;
            Toggle opAddDetailToggle = null;
            Toggle opRemoveDetailToggle = null;
            Toggle opAddTreeToggle = null;
            Toggle opRemoveTreeToggle = null;
            if (operationContainer != null)
            {
                operationContainer.Clear();
                operationContainer.RemoveFromClassList("operation-dropdown");
                operationContainer.style.flexDirection = FlexDirection.Row;
                operationContainer.style.flexWrap = Wrap.Wrap;
                operationContainer.style.marginLeft = 0;
                operationContainer.style.marginRight = 0;
                operationContainer.style.marginTop = 2;
                operationContainer.style.marginBottom = 2;

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

                opHeightToggle?.SetValueWithoutNotify(data.settings.operationHeight);
                opPaintToggle?.SetValueWithoutNotify(data.settings.operationPaint);
                opHoleToggle?.SetValueWithoutNotify(data.settings.operationHole);
                opFillToggle?.SetValueWithoutNotify(data.settings.operationFill);
                opAddDetailToggle?.SetValueWithoutNotify(data.settings.operationAddDetail);
                opRemoveDetailToggle?.SetValueWithoutNotify(data.settings.operationRemoveDetail);
                opAddTreeToggle?.SetValueWithoutNotify(data.settings.operationAddTree);
                opRemoveTreeToggle?.SetValueWithoutNotify(data.settings.operationRemoveTree);

                foreach (var toggle in toggles)
                {
                    RefreshOperationToggleVisual(toggle);
                }
            }

            var operationWarningLabel = contentElement.Q<Label>("operation-warning");
            ConfigureWarningIconLabel(operationWarningLabel);
            var overrideBrushToggle = contentElement.Q<Toggle>("override-brush-toggle");
            var overrideModeTab = contentElement.Q<Button>("override-mode-tab");
            var overrideBrushTab = contentElement.Q<Button>("override-brush-tab");
            var overridePaintTab = contentElement.Q<Button>("override-paint-tab");
            var overrideDetailTab = contentElement.Q<Button>("override-detail-tab");
            var overrideTreeTab = contentElement.Q<Button>("override-tree-tab");
            EnsureTabButtonContent(overrideModeTab, "Mode");
            EnsureTabButtonContent(overrideBrushTab, "Brush");
            EnsureTabButtonContent(overridePaintTab, "Paint");
            EnsureTabButtonContent(overrideDetailTab, "Detail");
            EnsureTabButtonContent(overrideTreeTab, "Trees");
            var overrideModeContent = contentElement.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = contentElement.Q<VisualElement>("override-brush-content");
            var overridePaintContent = contentElement.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = contentElement.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = contentElement.Q<VisualElement>("override-tree-content");
            var pathModeRadio = contentElement.Q<Button>("path-mode-radio");
            var shapeModeRadio = contentElement.Q<Button>("shape-mode-radio");
            EnsureModeButtonContent(pathModeRadio, "path", "Path");
            EnsureModeButtonContent(shapeModeRadio, "shape", "Shape");
            var brushPreviewImage = contentElement.Q<Image>("brush-preview-image");
            var brushHiddenField = contentElement.Q<ObjectField>("brush-hidden-field");
            var brushHardnessSlider = contentElement.Q<Slider>("brush-hardness-slider");
            var brushHardnessField = contentElement.Q<FloatField>("brush-hardness-field");
            var brushSizeSlider = contentElement.Q<Slider>("brush-size-slider");
            var brushSizeField = contentElement.Q<FloatField>("brush-size-field");
            var overrideBrushSizeMultiplierToggle = contentElement.Q<Toggle>("override-brush-size-multiplier-toggle");
            var brushSizeMultiplierSection = contentElement.Q<VisualElement>("brush-size-multiplier-section");
            var brushSizeMultiplierCurve = contentElement.Q<CurveField>("brush-size-multiplier-curve");
            var strengthSlider = contentElement.Q<Slider>("strength-slider");
            var strengthField = contentElement.Q<FloatField>("strength-field");
            var sampleStepSlider = contentElement.Q<Slider>("sample-step-slider");
            var sampleStepField = contentElement.Q<FloatField>("sample-step-field");
            var brushNoiseTextureField = contentElement.Q<ObjectField>("brush-noise-texture");
            var brushNoiseStrengthSlider = contentElement.Q<Slider>("brush-noise-strength-slider");
            var brushNoiseStrengthField = contentElement.Q<FloatField>("brush-noise-strength-field");
            var brushNoiseEdgeSlider = contentElement.Q<Slider>("brush-noise-edge-slider");
            var brushNoiseEdgeField = contentElement.Q<FloatField>("brush-noise-edge-field");
            var brushNoiseSizeSlider = contentElement.Q<Slider>("brush-noise-size-slider");
            var brushNoiseSizeField = contentElement.Q<FloatField>("brush-noise-size-field");
            var brushNoiseOffsetField = contentElement.Q<Vector2Field>("brush-noise-offset-field");
            var brushNoiseResponseCurve = contentElement.Q<CurveField>("brush-noise-response-curve");
            var brushNoiseInvertToggle = contentElement.Q<Toggle>("brush-noise-invert-toggle");
            var brushNoiseFoldout = contentElement.Q<Foldout>("brush-noise-foldout");
            var overridePaintToggle = contentElement.Q<Toggle>("override-paint-toggle");
            var paintLayerPalette = contentElement.Q<VisualElement>("paint-layer-palette");
            var paintStrengthSlider = contentElement.Q<Slider>("paint-strength-slider");
            var paintStrengthField = contentElement.Q<FloatField>("paint-strength-field");
            var paintBlendSlider = contentElement.Q<Slider>("paint-blend-slider");
            var paintBlendField = contentElement.Q<FloatField>("paint-blend-field");
            var paintThresholdSlider = contentElement.Q<Slider>("paint-threshold-slider");
            var paintThresholdField = contentElement.Q<FloatField>("paint-threshold-field");
            var paintNoiseTextureField = contentElement.Q<ObjectField>("paint-noise-texture");
            var paintNoiseStrengthSlider = contentElement.Q<Slider>("paint-noise-strength-slider");
            var paintNoiseStrengthField = contentElement.Q<FloatField>("paint-noise-strength-field");
            var paintNoiseEdgeSlider = contentElement.Q<Slider>("paint-noise-edge-slider");
            var paintNoiseEdgeField = contentElement.Q<FloatField>("paint-noise-edge-field");
            var paintNoiseSizeSlider = contentElement.Q<Slider>("paint-noise-size-slider");
            var paintNoiseSizeField = contentElement.Q<FloatField>("paint-noise-size-field");
            var paintNoiseOffsetField = contentElement.Q<Vector2Field>("paint-noise-offset-field");
            var paintNoiseResponseCurve = contentElement.Q<CurveField>("paint-noise-response-curve");
            var paintNoiseInvertToggle = contentElement.Q<Toggle>("paint-noise-invert-toggle");
            var paintNoiseFoldout = contentElement.Q<Foldout>("paint-noise-foldout");
            var overrideDetailToggle = contentElement.Q<Toggle>("override-detail-toggle");
            var detailModeContainer = contentElement.Q<VisualElement>("detail-mode-dropdown");
            detailModeContainer?.RemoveFromHierarchy();
            var detailLayerPalette = contentElement.Q<VisualElement>("detail-layer-palette");
            var detailStrengthSlider = contentElement.Q<Slider>("detail-strength-slider");
            var detailStrengthField = contentElement.Q<FloatField>("detail-strength-field");
            var detailSlopeLimitSlider = contentElement.Q<Slider>("detail-slope-slider");
            var detailSlopeLimitField = contentElement.Q<FloatField>("detail-slope-field");
            var detailFalloffSlider = contentElement.Q<Slider>("detail-falloff-slider");
            var detailFalloffField = contentElement.Q<FloatField>("detail-falloff-field");
            var detailSpreadSlider = contentElement.Q<SliderInt>("detail-spread-slider");
            var detailSpreadField = contentElement.Q<IntegerField>("detail-spread-field");
            var detailRemoveThresholdSlider = contentElement.Q<Slider>("detail-remove-threshold-slider");
            var detailRemoveThresholdField = contentElement.Q<FloatField>("detail-remove-threshold-field");
            var detailNoiseTextureField = contentElement.Q<ObjectField>("detail-noise-texture");
            var detailNoiseSizeSlider = contentElement.Q<Slider>("detail-noise-size-slider");
            var detailNoiseSizeField = contentElement.Q<FloatField>("detail-noise-size-field");
            var detailNoiseOffsetField = contentElement.Q<Vector2Field>("detail-noise-offset-field");
            var detailNoiseThresholdSlider = contentElement.Q<Slider>("detail-noise-threshold-slider");
            var detailNoiseThresholdField = contentElement.Q<FloatField>("detail-noise-threshold-field");
            var detailNoiseInvertToggle = contentElement.Q<Toggle>("detail-noise-invert-toggle");
            var detailNoiseFoldout = contentElement.Q<Foldout>("detail-noise-foldout");
            var overrideTreeToggle = contentElement.Q<Toggle>("override-tree-toggle");
            var treePrototypePalette = contentElement.Q<VisualElement>("tree-prototype-palette");
            var treeStrengthSlider = contentElement.Q<Slider>("tree-strength-slider");
            var treeStrengthField = contentElement.Q<FloatField>("tree-strength-field");
            var treeSlopeLimitSlider = contentElement.Q<Slider>("tree-slope-slider");
            var treeSlopeLimitField = contentElement.Q<FloatField>("tree-slope-field");
            var treeFalloffSlider = contentElement.Q<Slider>("tree-falloff-slider");
            var treeFalloffField = contentElement.Q<FloatField>("tree-falloff-field");
            var treeHeightMultiplierCurve = contentElement.Q<CurveField>("tree-height-multiplier-curve");
            var treeRemoveThresholdSlider = contentElement.Q<Slider>("tree-remove-threshold-slider");
            var treeRemoveThresholdField = contentElement.Q<FloatField>("tree-remove-threshold-field");
            var treeNoiseTextureField = contentElement.Q<ObjectField>("tree-noise-texture");
            var treeNoiseSizeSlider = contentElement.Q<Slider>("tree-noise-size-slider");
            var treeNoiseSizeField = contentElement.Q<FloatField>("tree-noise-size-field");
            var treeNoiseOffsetField = contentElement.Q<Vector2Field>("tree-noise-offset-field");
            var treeNoiseThresholdSlider = contentElement.Q<Slider>("tree-noise-threshold-slider");
            var treeNoiseThresholdField = contentElement.Q<FloatField>("tree-noise-threshold-field");
            var treeNoiseInvertToggle = contentElement.Q<Toggle>("tree-noise-invert-toggle");
            var treeNoiseFoldout = contentElement.Q<Foldout>("tree-noise-foldout");
            var previewThumbnail = contentElement.Q<Image>("preview-thumbnail");
            var noPreviewLabel = contentElement.Q<Label>("no-preview-label");

            CurvePresetDropdownUtility.AttachDropdown(brushSizeMultiplierCurve, CurvePresetListType.BrushSizeMultiplier);
            CurvePresetDropdownUtility.AttachDropdown(brushNoiseResponseCurve, CurvePresetListType.BrushNoiseContrast);
            CurvePresetDropdownUtility.AttachDropdown(paintNoiseResponseCurve, CurvePresetListType.PaintNoiseContrast);
            CurvePresetDropdownUtility.AttachDropdown(treeHeightMultiplierCurve, CurvePresetListType.TreeHeightDistribution);

            ConfigureListRowFoldoutToggle(foldoutToggle);
            HideToggleLabel(enabledToggle);
            ConfigureTabOverrideToggle(overrideModeToggle);
            ConfigureTabOverrideToggle(overrideBrushToggle);
            ConfigureTabOverrideToggle(overridePaintToggle);
            ConfigureTabOverrideToggle(overrideDetailToggle);
            ConfigureTabOverrideToggle(overrideTreeToggle);
            HideToggleInputAndLabel(opHeightToggle);
            HideToggleInputAndLabel(opPaintToggle);
            HideToggleInputAndLabel(opHoleToggle);
            HideToggleInputAndLabel(opFillToggle);
            HideToggleInputAndLabel(opAddDetailToggle);
            HideToggleInputAndLabel(opRemoveDetailToggle);
            HideToggleInputAndLabel(opAddTreeToggle);
            HideToggleInputAndLabel(opRemoveTreeToggle);
            HideToggleInputCheckmarkAndLabel(brushNoiseInvertToggle);
            HideToggleInputCheckmarkAndLabel(paintNoiseInvertToggle);
            HideToggleInputCheckmarkAndLabel(detailNoiseInvertToggle);
            HideToggleInputCheckmarkAndLabel(treeNoiseInvertToggle);

            if (detailNoiseFoldout != null)
            {
                ConfigureNoiseFoldout(detailNoiseFoldout);
                InsertSplineNoiseFoldoutIcon(detailNoiseFoldout);
            }
            if (treeNoiseFoldout != null)
            {
                ConfigureNoiseFoldout(treeNoiseFoldout);
                InsertSplineNoiseFoldoutIcon(treeNoiseFoldout);
            }
            if (paintNoiseFoldout != null)
            {
                ConfigureNoiseFoldout(paintNoiseFoldout);
                InsertSplineNoiseFoldoutIcon(paintNoiseFoldout);
            }
            if (brushNoiseFoldout != null)
            {
                ConfigureNoiseFoldout(brushNoiseFoldout);
                InsertSplineNoiseFoldoutIcon(brushNoiseFoldout);
            }

            void SetOperationIconsVisible(bool showIcons)
            {
                var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
                foreach (var toggle in toggles)
                {
                    if (toggle == null) continue;
                    toggle.style.display = showIcons ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            void SetModeUIEnabled(bool enabled)
            {
                pathModeRadio?.SetEnabled(enabled);
                shapeModeRadio?.SetEnabled(enabled);
                opHeightToggle?.SetEnabled(enabled);
                opPaintToggle?.SetEnabled(enabled);
                opHoleToggle?.SetEnabled(enabled);
                opFillToggle?.SetEnabled(enabled);
                opAddDetailToggle?.SetEnabled(enabled);
                opRemoveDetailToggle?.SetEnabled(enabled);
                opAddTreeToggle?.SetEnabled(enabled);
                opRemoveTreeToggle?.SetEnabled(enabled);

                if (enabled)
                {
                    SetOperationIconsVisible(true);
                    if (globalModeIcon != null) globalModeIcon.style.display = DisplayStyle.None;
                }
                else
                {
                    SetOperationIconsVisible(false);
                    if (globalModeIcon != null)
                    {
                        globalModeIcon.image = LoadSplineIcon("global");
                        globalModeIcon.style.display = DisplayStyle.Flex;
                    }
                    if (operationWarningLabel != null)
                    {
                        operationWarningLabel.style.display = DisplayStyle.None;
                    }
                }

                var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
                foreach (var toggle in toggles)
                {
                    RefreshOperationToggleVisual(toggle);
                }
            }

            void SetBrushUIEnabled(bool enabled)
            {
                brushPreviewImage?.SetEnabled(enabled);
                brushHiddenField?.SetEnabled(enabled);
                brushHardnessSlider?.SetEnabled(enabled);
                brushHardnessField?.SetEnabled(enabled);
                brushSizeSlider?.SetEnabled(enabled);
                brushSizeField?.SetEnabled(enabled);
                overrideBrushSizeMultiplierToggle?.SetEnabled(enabled);
                brushSizeMultiplierSection?.SetEnabled(enabled);
                CurvePresetDropdownUtility.SetEnabled(brushSizeMultiplierCurve, enabled);
                strengthSlider?.SetEnabled(enabled);
                strengthField?.SetEnabled(enabled);
                sampleStepSlider?.SetEnabled(enabled);
                sampleStepField?.SetEnabled(enabled);
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
            }

            void SetPaintUIEnabled(bool enabled)
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

            void SetTreeUIEnabled(bool enabled)
            {
                treePrototypePalette?.SetEnabled(enabled);
                treeStrengthSlider?.SetEnabled(enabled);
                treeStrengthField?.SetEnabled(enabled);
                treeSlopeLimitSlider?.SetEnabled(enabled);
                treeSlopeLimitField?.SetEnabled(enabled);
                treeFalloffSlider?.SetEnabled(enabled);
                treeFalloffField?.SetEnabled(enabled);
                CurvePresetDropdownUtility.SetEnabled(treeHeightMultiplierCurve, enabled);
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

            // Set initial content visibility and foldout state from data
            contentElement.style.display = data.isFoldoutExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            // Bind foldout toggle to data
            if (foldoutToggle != null)
            {
                foldoutToggle.SetValueWithoutNotify(data.isFoldoutExpanded);
            }

            // Update arrow direction based on state
            if (arrowLabel != null)
            {
                arrowLabel.text = data.isFoldoutExpanded ? "▼" : "▶";
            }

            // Register click handler for name label (for GameObject selection)
            if (nameLabel != null)
            {
                RegisterSplineNameLabelSelection(nameLabel);
                nameLabel.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());
            }

            if (data.container != null)
            {
                // Set name label text with index and name (with bold formatting for spline name)
                if (nameLabel != null)
                {
                    // Use shared indicator logic so all overrides (mode/brush/paint/detail) are reflected
                    nameLabel.enableRichText = true;
                    UpdateSplineNameIndicator(element, data);
                }

                if (enabledToggle != null)
                    enabledToggle.SetValueWithoutNotify(data.settings.enabled);
                if (overrideModeToggle != null)
                    overrideModeToggle.SetValueWithoutNotify(data.settings.overrideMode);
                SetModeUIEnabled(data.settings.overrideMode);
                if (!data.settings.overrideMode)
                    SetOperationIconsVisible(false);
                // Operation toggles are initialized above

                if (overrideBrushToggle != null)
                    overrideBrushToggle.SetValueWithoutNotify(data.settings.overrideBrush);
                SetBrushUIEnabled(data.settings.overrideBrush);
                if (overridePaintToggle != null)
                    overridePaintToggle.SetValueWithoutNotify(data.settings.overridePaint);
                SetPaintUIEnabled(data.settings.overridePaint);
                if (paintStrengthSlider != null)
                    paintStrengthSlider.SetValueWithoutNotify(data.settings.paintStrength);
                if (paintStrengthField != null)
                    paintStrengthField.SetValueWithoutNotify(data.settings.paintStrength);
                if (paintBlendSlider != null)
                    paintBlendSlider.SetValueWithoutNotify(data.settings.paintBlend);
                if (paintBlendField != null)
                    paintBlendField.SetValueWithoutNotify(data.settings.paintBlend);
                if (paintThresholdSlider != null)
                    paintThresholdSlider.SetValueWithoutNotify(data.settings.paintThreshold);
                if (paintThresholdField != null)
                    paintThresholdField.SetValueWithoutNotify(data.settings.paintThreshold);
                if (overrideDetailToggle != null)
                    overrideDetailToggle.SetValueWithoutNotify(data.settings.overrideDetail);
                if (overrideTreeToggle != null)
                    overrideTreeToggle.SetValueWithoutNotify(data.settings.overrideTrees);
                if (detailStrengthSlider != null)
                    detailStrengthSlider.SetValueWithoutNotify(data.settings.detailStrength);
                if (detailStrengthField != null)
                    detailStrengthField.SetValueWithoutNotify(data.settings.detailStrength);
                if (detailSlopeLimitSlider != null)
                    detailSlopeLimitSlider.SetValueWithoutNotify(data.settings.detailSlopeLimitDegrees);
                if (detailSlopeLimitField != null)
                    detailSlopeLimitField.SetValueWithoutNotify(data.settings.detailSlopeLimitDegrees);
                if (detailFalloffSlider != null)
                    detailFalloffSlider.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(data.settings.detailFalloffPower));
                if (detailFalloffField != null)
                    detailFalloffField.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(data.settings.detailFalloffPower));
                if (detailSpreadSlider != null)
                    detailSpreadSlider.SetValueWithoutNotify(data.settings.detailSpreadRadius);
                if (detailSpreadField != null)
                    detailSpreadField.SetValueWithoutNotify(data.settings.detailSpreadRadius);
                if (detailRemoveThresholdSlider != null)
                    detailRemoveThresholdSlider.SetValueWithoutNotify(data.settings.detailRemoveThreshold);
                if (detailRemoveThresholdField != null)
                    detailRemoveThresholdField.SetValueWithoutNotify(data.settings.detailRemoveThreshold);
                SetTreeUIEnabled(data.settings.overrideTrees);
                if (treeStrengthSlider != null)
                    treeStrengthSlider.SetValueWithoutNotify(data.settings.treeStrength);
                if (treeStrengthField != null)
                    treeStrengthField.SetValueWithoutNotify(data.settings.treeStrength);
                if (treeSlopeLimitSlider != null)
                    treeSlopeLimitSlider.SetValueWithoutNotify(data.settings.treeSlopeLimitDegrees);
                if (treeSlopeLimitField != null)
                    treeSlopeLimitField.SetValueWithoutNotify(data.settings.treeSlopeLimitDegrees);
                if (treeFalloffSlider != null)
                    treeFalloffSlider.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(data.settings.treeFalloffPower));
                if (treeFalloffField != null)
                    treeFalloffField.SetValueWithoutNotify(DetailFalloffMapping.ToNormalized(data.settings.treeFalloffPower));
                if (treeHeightMultiplierCurve != null)
                    treeHeightMultiplierCurve.SetValueWithoutNotify((data.settings.treeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve());
                if (treeRemoveThresholdSlider != null)
                    treeRemoveThresholdSlider.SetValueWithoutNotify(data.settings.treeRemoveThreshold);
                if (treeRemoveThresholdField != null)
                    treeRemoveThresholdField.SetValueWithoutNotify(data.settings.treeRemoveThreshold);

                var paintNoiseEntry = GetPaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                if (paintNoiseTextureField != null)
                    paintNoiseTextureField.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseTexture : null);
                if (paintNoiseStrengthSlider != null)
                    paintNoiseStrengthSlider.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseStrength : 1f);
                if (paintNoiseStrengthField != null)
                    paintNoiseStrengthField.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseStrength : 1f);
                if (paintNoiseEdgeSlider != null)
                    paintNoiseEdgeSlider.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseEdge : 0f);
                if (paintNoiseEdgeField != null)
                    paintNoiseEdgeField.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseEdge : 0f);
                if (paintNoiseSizeSlider != null)
                    paintNoiseSizeSlider.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseWorldSizeMeters : 10f);
                if (paintNoiseSizeField != null)
                    paintNoiseSizeField.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseWorldSizeMeters : 10f);
                if (paintNoiseOffsetField != null)
                    paintNoiseOffsetField.SetValueWithoutNotify(paintNoiseEntry != null ? paintNoiseEntry.noiseOffset : Vector2.zero);
                if (paintNoiseResponseCurve != null)
                    paintNoiseResponseCurve.SetValueWithoutNotify(((paintNoiseEntry != null ? paintNoiseEntry.noiseResponse : null) ?? SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve()).CloneCurve());
                if (paintNoiseInvertToggle != null)
                {
                    paintNoiseInvertToggle.SetValueWithoutNotify(paintNoiseEntry != null && paintNoiseEntry.noiseInvert);
                    UpdateNoiseInvertToggleVisual(paintNoiseInvertToggle);
                }
                if (paintNoiseFoldout != null)
                {
                    paintNoiseFoldout.text = $"Noise {GetPaintLayerDisplayName(data.settings.selectedLayerIndex)}";
                    paintNoiseFoldout.SetValueWithoutNotify(data.isPaintNoiseFoldoutExpanded);
                    SetNoiseHeaderIconTint(paintNoiseFoldout, paintNoiseEntry != null && paintNoiseEntry.noiseTexture != null);
                }

                DetailNoiseLayerSettings GetDetailNoiseLayer(List<DetailNoiseLayerSettings> list, int layerIndex)
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

                var selectedDetailLayerIndices = GetSelectedDetailLayerIndices();
                int selectedLayerIndex = selectedDetailLayerIndices[0];
                var noiseEntry = GetDetailNoiseLayer(data.settings.detailNoiseLayers, selectedLayerIndex);
                var noiseTexture = noiseEntry != null ? noiseEntry.noiseTexture : null;
                float noiseSize = noiseEntry != null ? noiseEntry.noiseWorldSizeMeters : 10f;
                Vector2 noiseOffset = noiseEntry != null ? noiseEntry.noiseOffset : Vector2.zero;
                float noiseThreshold = noiseEntry != null ? noiseEntry.noiseThreshold : 0f;
                bool noiseInvert = noiseEntry != null && noiseEntry.noiseInvert;

                bool mixedTexture = false;
                bool mixedSize = false;
                bool mixedOffset = false;
                bool mixedThreshold = false;
                bool mixedInvert = false;
                bool hasNoiseTexture = noiseTexture != null;

                for (int i = 1; i < selectedDetailLayerIndices.Count; i++)
                {
                    int layerIndex = selectedDetailLayerIndices[i];
                    var otherEntry = GetDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
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
                if (detailNoiseFoldout != null)
                {
                    detailNoiseFoldout.text = selectedDetailLayerIndices.Count > 1
                        ? "Noise Multiple Layers"
                        : $"Noise {GetDetailLayerDisplayName(selectedLayerIndex)}";
                    detailNoiseFoldout.SetValueWithoutNotify(data.isDetailNoiseFoldoutExpanded);
                    SetNoiseHeaderIconTint(detailNoiseFoldout, hasNoiseTexture);
                }

                TreeNoisePrototypeSettings GetTreeNoiseLayer(List<TreeNoisePrototypeSettings> list, int prototypeIndex)
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

                var selectedTreePrototypeIndices = GetSelectedTreePrototypeIndices();
                int selectedPrototypeIndex = selectedTreePrototypeIndices[0];
                var treeNoiseEntry = GetTreeNoiseLayer(data.settings.treeNoiseLayers, selectedPrototypeIndex);
                var treeNoiseTexture = treeNoiseEntry != null ? treeNoiseEntry.noiseTexture : null;
                float treeNoiseSize = treeNoiseEntry != null ? treeNoiseEntry.noiseWorldSizeMeters : 10f;
                Vector2 treeNoiseOffset = treeNoiseEntry != null ? treeNoiseEntry.noiseOffset : Vector2.zero;
                float treeNoiseThreshold = treeNoiseEntry != null ? treeNoiseEntry.noiseThreshold : 0f;
                bool treeNoiseInvert = treeNoiseEntry != null && treeNoiseEntry.noiseInvert;

                bool mixedTreeTexture = false;
                bool mixedTreeSize = false;
                bool mixedTreeOffset = false;
                bool mixedTreeThreshold = false;
                bool mixedTreeInvert = false;
                bool hasTreeNoiseTexture = treeNoiseTexture != null;

                for (int i = 1; i < selectedTreePrototypeIndices.Count; i++)
                {
                    int prototypeIndex = selectedTreePrototypeIndices[i];
                    var otherEntry = GetTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                    var otherTexture = otherEntry != null ? otherEntry.noiseTexture : null;
                    float otherSize = otherEntry != null ? otherEntry.noiseWorldSizeMeters : 10f;
                    Vector2 otherOffset = otherEntry != null ? otherEntry.noiseOffset : Vector2.zero;
                    float otherThreshold = otherEntry != null ? otherEntry.noiseThreshold : 0f;
                    bool otherInvert = otherEntry != null && otherEntry.noiseInvert;

                    if (otherTexture != treeNoiseTexture)
                        mixedTreeTexture = true;
                    if (!Mathf.Approximately(otherSize, treeNoiseSize))
                        mixedTreeSize = true;
                    if (otherOffset != treeNoiseOffset)
                        mixedTreeOffset = true;
                    if (!Mathf.Approximately(otherThreshold, treeNoiseThreshold))
                        mixedTreeThreshold = true;
                    if (otherInvert != treeNoiseInvert)
                        mixedTreeInvert = true;
                    if (otherTexture != null)
                        hasTreeNoiseTexture = true;
                }
                if (treeNoiseTextureField != null)
                {
                    treeNoiseTextureField.SetValueWithoutNotify(treeNoiseTexture);
                    treeNoiseTextureField.showMixedValue = mixedTreeTexture;
                }
                if (treeNoiseSizeSlider != null)
                {
                    treeNoiseSizeSlider.SetValueWithoutNotify(treeNoiseSize);
                    treeNoiseSizeSlider.showMixedValue = mixedTreeSize;
                }
                if (treeNoiseSizeField != null)
                {
                    treeNoiseSizeField.SetValueWithoutNotify(treeNoiseSize);
                    treeNoiseSizeField.showMixedValue = mixedTreeSize;
                }
                if (treeNoiseOffsetField != null)
                {
                    treeNoiseOffsetField.SetValueWithoutNotify(treeNoiseOffset);
                    treeNoiseOffsetField.showMixedValue = mixedTreeOffset;
                }
                if (treeNoiseThresholdSlider != null)
                {
                    treeNoiseThresholdSlider.SetValueWithoutNotify(treeNoiseThreshold);
                    treeNoiseThresholdSlider.showMixedValue = mixedTreeThreshold;
                }
                if (treeNoiseThresholdField != null)
                {
                    treeNoiseThresholdField.SetValueWithoutNotify(treeNoiseThreshold);
                    treeNoiseThresholdField.showMixedValue = mixedTreeThreshold;
                }
                if (treeNoiseInvertToggle != null)
                {
                    treeNoiseInvertToggle.SetValueWithoutNotify(treeNoiseInvert);
                    treeNoiseInvertToggle.showMixedValue = mixedTreeInvert;
                    UpdateNoiseInvertToggleVisual(treeNoiseInvertToggle);
                }
                if (treeNoiseFoldout != null)
                {
                    treeNoiseFoldout.text = selectedTreePrototypeIndices.Count > 1
                        ? "Noise Multiple Trees"
                        : $"Noise {GetTreePrototypeDisplayName(selectedPrototypeIndex)}";
                    treeNoiseFoldout.SetValueWithoutNotify(data.isTreeNoiseFoldoutExpanded);
                    SetNoiseHeaderIconTint(treeNoiseFoldout, hasTreeNoiseTexture);
                }

                // Initialize icons
                UpdateSplineIcons(element, data);

                // Set up tab states based on stored state
                bool isBrushTabActive = data.isOverrideBrushTabActive;
                bool isPaintTabActive = data.isOverridePaintTabActive;
                bool isDetailTabActive = data.isOverrideDetailTabActive;
                bool isTreeTabActive = data.isOverrideTreeTabActive;

                if (overrideModeTab != null)
                {
                    SetTabButtonActive(overrideModeTab, !isBrushTabActive && !isPaintTabActive && !isDetailTabActive && !isTreeTabActive);
                    UpdateTabText(overrideModeTab, "Mode", data.settings.overrideMode);
                }

                if (overrideBrushTab != null)
                {
                    SetTabButtonActive(overrideBrushTab, isBrushTabActive);
                    UpdateTabText(overrideBrushTab, "Brush", data.settings.overrideBrush);
                }

                if (overridePaintTab != null)
                {
                    SetTabButtonActive(overridePaintTab, isPaintTabActive);
                    UpdateTabText(overridePaintTab, "Paint", data.settings.overridePaint);
                }
                if (overrideDetailTab != null)
                {
                    SetTabButtonActive(overrideDetailTab, isDetailTabActive);
                    UpdateTabText(overrideDetailTab, "Detail", data.settings.overrideDetail);
                }
                if (overrideTreeTab != null)
                {
                    SetTabButtonActive(overrideTreeTab, isTreeTabActive);
                    UpdateTabText(overrideTreeTab, "Trees", data.settings.overrideTrees);
                }

                if (overrideModeContent != null)
                    overrideModeContent.style.display = (!isBrushTabActive && !isPaintTabActive && !isDetailTabActive && !isTreeTabActive) ? DisplayStyle.Flex : DisplayStyle.None;
                if (overrideBrushContent != null)
                    overrideBrushContent.style.display = isBrushTabActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (overridePaintContent != null)
                    overridePaintContent.style.display = isPaintTabActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (overrideDetailContent != null)
                    overrideDetailContent.style.display = isDetailTabActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (overrideTreeContent != null)
                    overrideTreeContent.style.display = isTreeTabActive ? DisplayStyle.Flex : DisplayStyle.None;

                if (pathModeRadio != null)
                    SetModeButtonSelected(pathModeRadio, data.settings.mode == SplineApplyMode.Path);
                if (shapeModeRadio != null)
                    SetModeButtonSelected(shapeModeRadio, data.settings.mode == SplineApplyMode.Shape);
                if (brushPreviewImage != null)
                    brushPreviewImage.image = BrushFalloffUtils.GenerateBrushPreviewTexture(64, data.settings.hardness);
                if (brushHardnessSlider != null)
                    brushHardnessSlider.SetValueWithoutNotify(data.settings.hardness);
                if (brushHardnessField != null)
                    brushHardnessField.SetValueWithoutNotify(data.settings.hardness);
                if (brushSizeSlider != null)
                    brushSizeSlider.SetValueWithoutNotify(data.settings.sizeMeters);
                if (brushSizeField != null)
                    brushSizeField.SetValueWithoutNotify(data.settings.sizeMeters);
                if (overrideBrushSizeMultiplierToggle != null)
                    overrideBrushSizeMultiplierToggle.SetValueWithoutNotify(data.settings.overrideSizeMultiplier);
                if (brushSizeMultiplierCurve != null)
                    brushSizeMultiplierCurve.SetValueWithoutNotify(data.settings.sizeMultiplier.CloneCurve());
                if (strengthSlider != null)
                    strengthSlider.SetValueWithoutNotify(data.settings.strength);
                if (strengthField != null)
                    strengthField.SetValueWithoutNotify(data.settings.strength);
                if (sampleStepSlider != null)
                    sampleStepSlider.SetValueWithoutNotify(data.settings.sampleStep);
                if (sampleStepField != null)
                    sampleStepField.SetValueWithoutNotify(data.settings.sampleStep);
                var brushNoiseEntry = data.settings.brushNoise ?? new BrushNoiseSettings();
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
                    brushNoiseFoldout.SetValueWithoutNotify(data.isBrushNoiseFoldoutExpanded);
                    SetNoiseHeaderIconTint(brushNoiseFoldout, brushNoiseEntry.noiseTexture != null);
                }

                // Show/hide brush size multiplier curve based on override setting
                if (brushSizeMultiplierCurve != null)
                    CurvePresetDropdownUtility.SetDisplay(brushSizeMultiplierCurve, data.settings.overrideBrush && data.settings.overrideSizeMultiplier);

                // Populate per-spline layer palette
                if (paintLayerPalette != null)
                {
                    paintLayerPalette.Clear();

                    var terrains = GetAllTerrains();
                    if (terrains.Count > 0)
                    {
                        // Use first terrain's layers
                        var firstTerrain = terrains[0];
                        if (firstTerrain != null && firstTerrain.terrainData != null)
                        {
                            var terrainLayers = firstTerrain.terrainData.terrainLayers;
                        if (terrainLayers != null && terrainLayers.Length > 0)
                        {
                            for (int i = 0; i < terrainLayers.Length; i++)
                            {
                                var layer = terrainLayers[i];
                                if (layer == null) continue;

                                int layerIndex = i; // Capture for closure
                                var layerButton = CreateLayerButton(layer, i, data.settings.selectedLayerIndex);
                                var noiseEntryForLayer = GetPaintNoiseLayer(data.settings.paintNoiseLayers, layerIndex);
                                if (noiseEntryForLayer != null && noiseEntryForLayer.noiseTexture != null)
                                {
                                    var noiseBadge = new Image();
                                    noiseBadge.AddToClassList("layer-noise-badge");
                                    noiseBadge.image = LoadSplineIcon("noise");
                                    noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                                    layerButton.Add(noiseBadge);
                                }
                                layerButton.clicked += () =>
                                {
                                    data.settings.selectedLayerIndex = layerIndex;
                                    RefreshTopLevelListItem(data);
                                    RefreshPreview();
                                };
                                paintLayerPalette.Add(layerButton);
                            }
                        }
                        else
                        {
                            var noLayersLabel = new Label("No layers");
                            noLayersLabel.AddToClassList("no-layers-label");
                            paintLayerPalette.Add(noLayersLabel);
                        }
                        }
                    }
                }

                // Populate per-spline detail palette (uses grass UXML IDs)
                if (detailLayerPalette != null)
                {
                    detailLayerPalette.Clear();

                    if (data.settings.selectedDetailLayerIndices == null)
                        data.settings.selectedDetailLayerIndices = new List<int>();
                    var selectedDetailSet = (data.settings.selectedDetailLayerIndices.Count > 0)
                        ? new HashSet<int>(data.settings.selectedDetailLayerIndices)
                        : new HashSet<int> { data.settings.selectedDetailLayerIndex };

                    var terrains = GetAllTerrains();
                    if (terrains.Count > 0)
                    {
                        var firstTerrain = terrains[0];
                        if (firstTerrain != null && firstTerrain.terrainData != null)
                        {
                            var detailPrototypes = firstTerrain.terrainData.detailPrototypes;
                            if (detailPrototypes != null && detailPrototypes.Length > 0)
                            {
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
                                    if (prototype.prototypeTexture != null)
                                    {
                                        thumbnail.image = prototype.prototypeTexture;
                                    }
                                    else
                                    {
                                        thumbnail.image = Texture2D.grayTexture;
                                    }

                                    layerButton.Add(thumbnail);

                                    var noiseEntryForLayer = GetDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                                    if (noiseEntryForLayer != null && noiseEntryForLayer.noiseTexture != null)
                                    {
                                        var noiseBadge = new Image();
                                        noiseBadge.AddToClassList("layer-noise-badge");
                                        noiseBadge.image = LoadSplineIcon("noise");
                                        noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                                        layerButton.Add(noiseBadge);
                                    }

                                    string layerNameText = prototype.prototype != null ? prototype.prototype.name : $"Layer {i}";
                                    var layerName = new Label(layerNameText);
                                    layerName.AddToClassList("layer-name");
                                    layerButton.Add(layerName);

                                    layerButton.RegisterCallback<ClickEvent>(evt =>
                                    {
                                        if (data.settings.selectedDetailLayerIndices == null)
                                            data.settings.selectedDetailLayerIndices = new List<int>();

                                        if (!evt.shiftKey)
                                        {
                                            data.settings.selectedDetailLayerIndices.Clear();
                                            data.settings.selectedDetailLayerIndex = layerIndex;
                                        }
                                        else
                                        {
                                            if (data.settings.selectedDetailLayerIndices.Count == 0)
                                                data.settings.selectedDetailLayerIndices.Add(data.settings.selectedDetailLayerIndex);

                                            if (data.settings.selectedDetailLayerIndices.Contains(layerIndex))
                                                data.settings.selectedDetailLayerIndices.Remove(layerIndex);
                                            else
                                                data.settings.selectedDetailLayerIndices.Add(layerIndex);

                                            if (data.settings.selectedDetailLayerIndices.Count == 0)
                                                data.settings.selectedDetailLayerIndices.Add(layerIndex);

                                            data.settings.selectedDetailLayerIndex = layerIndex;
                                            data.settings.selectedDetailLayerIndices.Sort();
                                        }

                                        notifyOverlay();
                                        RefreshTopLevelListItem(data);
                                        RefreshPreview();
                                    });

                                    detailLayerPalette.Add(layerButton);
                                }
                            }
                            else
                            {
                                var noLayersLabel = new Label("No detail layers");
                                noLayersLabel.AddToClassList("no-layers-label");
                                detailLayerPalette.Add(noLayersLabel);
                            }
                        }
                    }
                    else
                    {
                        var noLayersLabel = new Label("No terrain selected");
                        noLayersLabel.AddToClassList("no-layers-label");
                        detailLayerPalette.Add(noLayersLabel);
                    }
                }

                if (treePrototypePalette != null)
                {
                    treePrototypePalette.Clear();

                    if (data.settings.selectedTreePrototypeIndices == null)
                        data.settings.selectedTreePrototypeIndices = new List<int>();
                    var selectedTreeSet = (data.settings.selectedTreePrototypeIndices.Count > 0)
                        ? new HashSet<int>(data.settings.selectedTreePrototypeIndices)
                        : new HashSet<int> { data.settings.selectedTreePrototypeIndex };

                    var terrains = GetAllTerrains();
                    if (terrains.Count > 0)
                    {
                        var firstTerrain = terrains[0];
                        if (firstTerrain != null && firstTerrain.terrainData != null)
                        {
                            var treePrototypes = firstTerrain.terrainData.treePrototypes;
                            if (treePrototypes != null && treePrototypes.Length > 0)
                            {
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
                                    var prefab = prototype != null ? prototype.prefab : null;
                                    thumbnail.image = AssetThumbnailCache.GetPrefabThumbnail(prefab);
                                    prototypeButton.Add(thumbnail);

                                    var noiseEntryForPrototype = GetTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                                    if (noiseEntryForPrototype != null && noiseEntryForPrototype.noiseTexture != null)
                                    {
                                        var noiseBadge = new Image();
                                        noiseBadge.AddToClassList("layer-noise-badge");
                                        noiseBadge.image = LoadSplineIcon("noise");
                                        noiseBadge.scaleMode = ScaleMode.ScaleToFit;
                                        prototypeButton.Add(noiseBadge);
                                    }

                                    string prototypeNameText = GetTreePrototypeDisplayName(prototypeIndex);
                                    var prototypeName = new Label(prototypeNameText);
                                    prototypeName.AddToClassList("layer-name");
                                    prototypeButton.Add(prototypeName);

                                    prototypeButton.RegisterCallback<ClickEvent>(evt =>
                                    {
                                        if (data.settings.selectedTreePrototypeIndices == null)
                                            data.settings.selectedTreePrototypeIndices = new List<int>();

                                        if (!evt.shiftKey)
                                        {
                                            data.settings.selectedTreePrototypeIndices.Clear();
                                            data.settings.selectedTreePrototypeIndex = prototypeIndex;
                                        }
                                        else
                                        {
                                            if (data.settings.selectedTreePrototypeIndices.Count == 0)
                                                data.settings.selectedTreePrototypeIndices.Add(data.settings.selectedTreePrototypeIndex);

                                            if (data.settings.selectedTreePrototypeIndices.Contains(prototypeIndex))
                                                data.settings.selectedTreePrototypeIndices.Remove(prototypeIndex);
                                            else
                                                data.settings.selectedTreePrototypeIndices.Add(prototypeIndex);

                                            if (data.settings.selectedTreePrototypeIndices.Count == 0)
                                                data.settings.selectedTreePrototypeIndices.Add(prototypeIndex);

                                            data.settings.selectedTreePrototypeIndex = prototypeIndex;
                                            data.settings.selectedTreePrototypeIndices.Sort();
                                        }

                                        notifyOverlay();
                                        RefreshTopLevelListItem(data);
                                        RefreshPreview();
                                    });

                                    treePrototypePalette.Add(prototypeButton);
                                }
                            }
                            else
                            {
                                var noTreesLabel = new Label("No tree prototypes");
                                noTreesLabel.AddToClassList("no-layers-label");
                                treePrototypePalette.Add(noTreesLabel);
                            }
                        }
                    }
                    else
                    {
                        var noTreesLabel = new Label("No terrain selected");
                        noTreesLabel.AddToClassList("no-layers-label");
                        treePrototypePalette.Add(noTreesLabel);
                    }
                }

                // Preview thumbnail
                if (data.previewTexture != null)
                {
                    if (previewThumbnail != null)
                    {
                        previewThumbnail.image = data.previewTexture;
                        previewThumbnail.style.display = DisplayStyle.Flex;
                    }

                    if (noPreviewLabel != null)
                        noPreviewLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    if (previewThumbnail != null)
                        previewThumbnail.style.display = DisplayStyle.None;
                    if (noPreviewLabel != null)
                        noPreviewLabel.style.display = DisplayStyle.Flex;
                }
            }

            RegisterPrimarySplineCallbacks(
                element,
                data,
                contentElement,
                foldoutToggle,
                arrowLabel,
                enabledToggle,
                overrideModeToggle,
                overrideModeTab,
                operationContainer,
                globalModeIcon,
                opHeightToggle,
                opPaintToggle,
                opHoleToggle,
                opFillToggle,
                opAddDetailToggle,
                opRemoveDetailToggle,
                opAddTreeToggle,
                opRemoveTreeToggle,
                operationWarningLabel,
                SetModeUIEnabled,
                SetOperationIconsVisible);


        void UnregisterSplineRowTabCallbacks(VisualElement element)
        {
            element.UnregisterCallback<ClickEvent>(OnOverrideModeTabClicked);
            element.UnregisterCallback<ClickEvent>(OnOverrideBrushTabClicked);
            element.UnregisterCallback<ClickEvent>(OnOverridePaintTabClicked);
            element.UnregisterCallback<ClickEvent>(OnOverrideDetailTabClicked);
            element.UnregisterCallback<ClickEvent>(OnOverrideTreeTabClicked);
        }

        void RegisterSplineNameLabelSelection(Label nameLabel)
        {
            nameLabel.UnregisterCallback<ClickEvent>(OnNameLabelClicked);
            nameLabel.RegisterCallback<ClickEvent>(OnNameLabelClicked);
        }

        Action RegisterPrimarySplineCallbacks(
            VisualElement element,
            SplineItemData data,
            VisualElement contentElement,
            Toggle foldoutToggle,
            Label arrowLabel,
            Toggle enabledToggle,
            Toggle overrideModeToggle,
            Button overrideModeTab,
            VisualElement operationContainer,
            Image globalModeIcon,
            Toggle opHeightToggle,
            Toggle opPaintToggle,
            Toggle opHoleToggle,
            Toggle opFillToggle,
            Toggle opAddDetailToggle,
            Toggle opRemoveDetailToggle,
            Toggle opAddTreeToggle,
            Toggle opRemoveTreeToggle,
            Label operationWarningLabel,
            Action<bool> setModeUIEnabled,
            Action<bool> setOperationIconsVisible)
        {
            Action updateOperationWarning = null;

            if (foldoutToggle != null)
            {
                foldoutToggle.RegisterValueChangedCallback(evt =>
                {
                    data.isFoldoutExpanded = evt.newValue;
                    if (contentElement != null)
                    {
                        contentElement.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    if (arrowLabel != null)
                    {
                        arrowLabel.text = evt.newValue ? "▼" : "▶";
                    }
                });
            }

            if (enabledToggle != null)
            {
                enabledToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.enabled = evt.newValue;
                    RefreshPreview();
                });
            }

            if (overrideModeToggle != null)
            {
                overrideModeToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.overrideMode = evt.newValue;
                    UpdateTabText(overrideModeTab, "Mode", evt.newValue);
                    setModeUIEnabled(evt.newValue);
                    updateOperationWarning?.Invoke();
                    UpdateSplineNameIndicator(element, data);
                    RefreshPreview();
                    var settingsComponent = data.container != null ? data.container.GetComponent<TerrainSplineSettings>() : null;
                    TerrainSplineSettingsOverlay.NotifySettingsChanged(settingsComponent);
                });
            }

            if (operationContainer == null)
            {
                return updateOperationWarning;
            }

            void SyncOperationToggles()
            {
                if (!data.settings.overrideMode)
                {
                    setOperationIconsVisible(false);
                    if (globalModeIcon != null)
                    {
                        globalModeIcon.image = LoadSplineIcon("global");
                        globalModeIcon.tintColor = opAvailableColor;
                        globalModeIcon.style.display = DisplayStyle.Flex;
                    }
                    return;
                }

                setOperationIconsVisible(true);
                if (globalModeIcon != null) globalModeIcon.style.display = DisplayStyle.None;

                opHeightToggle?.SetValueWithoutNotify(data.settings.operationHeight);
                opPaintToggle?.SetValueWithoutNotify(data.settings.operationPaint);
                opHoleToggle?.SetValueWithoutNotify(data.settings.operationHole);
                opFillToggle?.SetValueWithoutNotify(data.settings.operationFill);
                opAddDetailToggle?.SetValueWithoutNotify(data.settings.operationAddDetail);
                opRemoveDetailToggle?.SetValueWithoutNotify(data.settings.operationRemoveDetail);
                opAddTreeToggle?.SetValueWithoutNotify(data.settings.operationAddTree);
                opRemoveTreeToggle?.SetValueWithoutNotify(data.settings.operationRemoveTree);

                UpdateOperationInterlocks();

                var settingsComponent = data.container != null ? data.container.GetComponent<TerrainSplineSettings>() : null;
                TerrainSplineSettingsOverlay.NotifySettingsChanged(settingsComponent);

                var toggles = new[] { opHeightToggle, opPaintToggle, opHoleToggle, opFillToggle, opAddDetailToggle, opRemoveDetailToggle, opAddTreeToggle, opRemoveTreeToggle };
                foreach (var toggle in toggles)
                {
                    RefreshOperationToggleVisual(toggle);
                }
            }

            void EnforceOperationRules()
            {
                if (data.settings.operationHole)
                {
                    data.settings.operationHeight = false;
                    data.settings.operationPaint = false;
                    data.settings.operationFill = false;
                    data.settings.operationAddDetail = false;
                    data.settings.operationRemoveDetail = false;
                    data.settings.operationAddTree = false;
                    data.settings.operationRemoveTree = false;
                }

                if (data.settings.operationFill)
                {
                    data.settings.operationHole = false;
                }

                if (data.settings.operationAddDetail)
                {
                    data.settings.operationRemoveDetail = false;
                }
                else if (data.settings.operationRemoveDetail)
                {
                    data.settings.operationAddDetail = false;
                }

                if (data.settings.operationAddTree)
                {
                    data.settings.operationRemoveTree = false;
                }
                else if (data.settings.operationRemoveTree)
                {
                    data.settings.operationAddTree = false;
                }

                if (!data.settings.operationHeight && !data.settings.operationPaint && !data.settings.operationHole && !data.settings.operationFill && !data.settings.operationAddDetail && !data.settings.operationRemoveDetail && !data.settings.operationAddTree && !data.settings.operationRemoveTree)
                {
                    data.settings.operationHeight = true;
                }
            }

            void SetToggleEnabled(Toggle toggle, bool enabled)
            {
                if (toggle == null) return;
                toggle.SetEnabled(enabled);
                toggle.style.opacity = enabled ? 1f : 0.35f;
                RefreshOperationToggleVisual(toggle);
            }

            void UpdateOperationInterlocks()
            {
                if (data.settings.operationHole)
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

                bool holeBlocked = data.settings.operationFill;
                SetToggleEnabled(opHoleToggle, !holeBlocked);
                SetToggleEnabled(opFillToggle, true);
                SetToggleEnabled(opAddDetailToggle, !data.settings.operationRemoveDetail);
                SetToggleEnabled(opRemoveDetailToggle, !data.settings.operationAddDetail);
                SetToggleEnabled(opAddTreeToggle, !data.settings.operationRemoveTree);
                SetToggleEnabled(opRemoveTreeToggle, !data.settings.operationAddTree);
                SetToggleEnabled(opHeightToggle, true);
                SetToggleEnabled(opPaintToggle, true);
            }

            void UpdateOperationWarning()
            {
                if (operationWarningLabel == null) return;

                if (!data.settings.overrideMode)
                {
                    SetWarningIconState(operationWarningLabel, false, FeatureDisabledTooltip);
                    return;
                }

                bool requiresPaint = data.settings.operationPaint;
                bool requiresHole = data.settings.operationHole;
                bool requiresFill = data.settings.operationFill;
                bool requiresDetail = data.settings.operationAddDetail || data.settings.operationRemoveDetail;
                bool requiresTree = data.settings.operationAddTree || data.settings.operationRemoveTree;

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
                        FeatureDetailEnabled,
                        FeatureTreeEnabled));
            }

            updateOperationWarning = UpdateOperationWarning;

            void OnOperationToggleChanged()
            {
                data.settings.operationHeight = opHeightToggle?.value ?? false;
                data.settings.operationPaint = opPaintToggle?.value ?? false;
                data.settings.operationHole = opHoleToggle?.value ?? false;
                data.settings.operationFill = opFillToggle?.value ?? false;
                data.settings.operationAddDetail = opAddDetailToggle?.value ?? false;
                data.settings.operationRemoveDetail = opRemoveDetailToggle?.value ?? false;
                data.settings.operationAddTree = opAddTreeToggle?.value ?? false;
                data.settings.operationRemoveTree = opRemoveTreeToggle?.value ?? false;

                EnforceOperationRules();
                data.settings.detailMode = data.settings.operationRemoveDetail ? DetailOperationMode.Remove : DetailOperationMode.Add;
                SyncOperationToggles();
                UpdateOperationWarning();
                UpdateSplineNameIndicator(element, data);
                NotifyOverlaySettingsChanged(data.container);
                RefreshPreview();
            }

            opHeightToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opPaintToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opHoleToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opFillToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opAddDetailToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opRemoveDetailToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opAddTreeToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());
            opRemoveTreeToggle?.RegisterValueChangedCallback(_ => OnOperationToggleChanged());

            boundSplineRowFeatureRefreshers[element] = () =>
            {
                UpdateSplineIcons(element, data);
                updateOperationWarning?.Invoke();
            };

            UpdateOperationWarning();
            SyncOperationToggles();
            return updateOperationWarning;
        }
            if (overrideBrushToggle != null)
                overrideBrushToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.overrideBrush = evt.newValue;
                    UpdateTabText(overrideBrushTab, "Brush", evt.newValue);
                    SetBrushUIEnabled(evt.newValue);
                    if (brushSizeMultiplierCurve != null)
                        CurvePresetDropdownUtility.SetDisplay(brushSizeMultiplierCurve, evt.newValue && data.settings.overrideSizeMultiplier);
                    UpdateSplineNameIndicator(element, data);
                    RefreshPreview();
                });
            if (overridePaintToggle != null)
                overridePaintToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.overridePaint = evt.newValue;
                    UpdateTabText(overridePaintTab, "Paint", evt.newValue);
                    SetPaintUIEnabled(evt.newValue);
                    UpdateSplineNameIndicator(element, data);
                    RefreshPreview();
                });
            if (paintStrengthSlider != null)
                paintStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintStrength = evt.newValue;
                    if (paintStrengthField != null)
                        paintStrengthField.value = evt.newValue;
                    RefreshPreview();
                });
            if (paintStrengthField != null)
                paintStrengthField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintStrength = evt.newValue;
                    if (paintStrengthSlider != null)
                        paintStrengthSlider.value = evt.newValue;
                    RefreshPreview();
                });

            if (paintBlendSlider != null)
                paintBlendSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintBlend = evt.newValue;
                    if (paintBlendField != null)
                        paintBlendField.value = evt.newValue;
                    RefreshPreview();
                });

            if (paintBlendField != null)
                paintBlendField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintBlend = evt.newValue;
                    if (paintBlendSlider != null)
                        paintBlendSlider.value = evt.newValue;
                    RefreshPreview();
                });

            if (paintThresholdSlider != null)
                paintThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintThreshold = evt.newValue;
                    if (paintThresholdField != null)
                        paintThresholdField.value = evt.newValue;
                    RefreshPreview();
                });

            if (paintThresholdField != null)
                paintThresholdField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.paintThreshold = evt.newValue;
                    if (paintThresholdSlider != null)
                        paintThresholdSlider.value = evt.newValue;
                    RefreshPreview();
                });

            if (detailStrengthSlider != null)
                detailStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailStrength = evt.newValue;
                    if (detailStrengthField != null)
                        detailStrengthField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });
            if (detailStrengthField != null)
                detailStrengthField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailStrength = evt.newValue;
                    if (detailStrengthSlider != null)
                        detailStrengthSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (overrideDetailToggle != null)
            {
                bool enable = overrideDetailToggle.value;
                if (detailLayerPalette != null) detailLayerPalette.SetEnabled(enable);
                if (detailStrengthSlider != null) detailStrengthSlider.SetEnabled(enable);
                if (detailStrengthField != null) detailStrengthField.SetEnabled(enable);
                if (detailSlopeLimitSlider != null) detailSlopeLimitSlider.SetEnabled(enable);
                if (detailSlopeLimitField != null) detailSlopeLimitField.SetEnabled(enable);
                if (detailFalloffSlider != null) detailFalloffSlider.SetEnabled(enable);
                if (detailFalloffField != null) detailFalloffField.SetEnabled(enable);
                if (detailSpreadSlider != null) detailSpreadSlider.SetEnabled(enable);
                if (detailSpreadField != null) detailSpreadField.SetEnabled(enable);
                if (detailRemoveThresholdSlider != null) detailRemoveThresholdSlider.SetEnabled(enable);
                if (detailRemoveThresholdField != null) detailRemoveThresholdField.SetEnabled(enable);
                if (detailNoiseTextureField != null) detailNoiseTextureField.SetEnabled(enable);
                if (detailNoiseSizeSlider != null) detailNoiseSizeSlider.SetEnabled(enable);
                if (detailNoiseSizeField != null) detailNoiseSizeField.SetEnabled(enable);
                if (detailNoiseOffsetField != null) detailNoiseOffsetField.SetEnabled(enable);
                if (detailNoiseThresholdSlider != null) detailNoiseThresholdSlider.SetEnabled(enable);
                if (detailNoiseThresholdField != null) detailNoiseThresholdField.SetEnabled(enable);
                if (detailNoiseInvertToggle != null) detailNoiseInvertToggle.SetEnabled(enable);
            }

            if (overrideTreeToggle != null)
            {
                SetTreeUIEnabled(overrideTreeToggle.value);
            }

            if (detailSlopeLimitSlider != null)
                detailSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailSlopeLimitDegrees = evt.newValue;
                    if (detailSlopeLimitField != null)
                        detailSlopeLimitField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailSlopeLimitField != null)
                detailSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailSlopeLimitDegrees = evt.newValue;
                    if (detailSlopeLimitSlider != null)
                        detailSlopeLimitSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailFalloffSlider != null)
                detailFalloffSlider.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    data.settings.detailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (detailFalloffField != null)
                        detailFalloffField.SetValueWithoutNotify(normalized);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailFalloffField != null)
                detailFalloffField.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    data.settings.detailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (detailFalloffSlider != null)
                        detailFalloffSlider.SetValueWithoutNotify(normalized);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailSpreadSlider != null)
                detailSpreadSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailSpreadRadius = evt.newValue;
                    if (detailSpreadField != null)
                        detailSpreadField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailSpreadField != null)
                detailSpreadField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailSpreadRadius = evt.newValue;
                    if (detailSpreadSlider != null)
                        detailSpreadSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailRemoveThresholdSlider != null)
                detailRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailRemoveThreshold = evt.newValue;
                    if (detailRemoveThresholdField != null)
                        detailRemoveThresholdField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailRemoveThresholdField != null)
                detailRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.detailRemoveThreshold = evt.newValue;
                    if (detailRemoveThresholdSlider != null)
                        detailRemoveThresholdSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeStrengthSlider != null)
                treeStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeStrength = evt.newValue;
                    if (treeStrengthField != null)
                        treeStrengthField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeStrengthField != null)
                treeStrengthField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeStrength = evt.newValue;
                    if (treeStrengthSlider != null)
                        treeStrengthSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeSlopeLimitSlider != null)
                treeSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeSlopeLimitDegrees = evt.newValue;
                    if (treeSlopeLimitField != null)
                        treeSlopeLimitField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeSlopeLimitField != null)
                treeSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeSlopeLimitDegrees = evt.newValue;
                    if (treeSlopeLimitSlider != null)
                        treeSlopeLimitSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeFalloffSlider != null)
                treeFalloffSlider.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    data.settings.treeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (treeFalloffField != null)
                        treeFalloffField.SetValueWithoutNotify(normalized);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeFalloffField != null)
                treeFalloffField.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    data.settings.treeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (treeFalloffSlider != null)
                        treeFalloffSlider.SetValueWithoutNotify(normalized);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeHeightMultiplierCurve != null)
                treeHeightMultiplierCurve.RegisterValueChangedCallback(evt =>
                {
                    var settingsComponent = data.container != null ? data.container.GetComponent<TerrainSplineSettings>() : null;
                    if (settingsComponent != null)
                        Undo.RecordObject(settingsComponent, "Modify Tree Height Curve");

                    data.settings.treeHeightMultiplier = evt.newValue?.CloneCurve() ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve();

                    if (settingsComponent != null)
                        EditorUtility.SetDirty(settingsComponent);

                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeRemoveThresholdSlider != null)
                treeRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeRemoveThreshold = evt.newValue;
                    if (treeRemoveThresholdField != null)
                        treeRemoveThresholdField.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeRemoveThresholdField != null)
                treeRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.treeRemoveThreshold = evt.newValue;
                    if (treeRemoveThresholdSlider != null)
                        treeRemoveThresholdSlider.value = evt.newValue;
                    notifyOverlay();
                    RefreshPreview();
                });

            PaintNoiseLayerSettings GetOrCreatePaintNoiseLayer(List<PaintNoiseLayerSettings> list, int layerIndex)
            {
                if (list == null)
                {
                    list = new List<PaintNoiseLayerSettings>();
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (entry != null && entry.paintLayerIndex == layerIndex)
                        return entry;
                }

                var newEntry = new PaintNoiseLayerSettings { paintLayerIndex = layerIndex };
                list.Add(newEntry);
                return newEntry;
            }

            if (paintNoiseTextureField != null)
                paintNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseTexture = evt.newValue as Texture2D;
                    SetNoiseHeaderIconTint(paintNoiseFoldout, entry.noiseTexture != null);
                    var itemIndex = GetGlobalSplineIndex(data);
                    if (itemIndex >= 0)
                        splinesListView?.RefreshItem(itemIndex);
                    else
                        splinesListView?.RefreshItems();
                    notifyOverlay();
                    RefreshPreview();
                });

            if (brushNoiseTextureField != null)
                brushNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseTexture = evt.newValue as Texture2D;
                    SetNoiseHeaderIconTint(brushNoiseFoldout, data.settings.brushNoise.noiseTexture != null);
                    RefreshPreview();
                });

            if (paintNoiseStrengthSlider != null)
                paintNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (paintNoiseStrengthField != null)
                        paintNoiseStrengthField.value = evt.newValue;
                    RefreshPreview();
                });

            if (brushNoiseStrengthSlider != null)
                brushNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (brushNoiseStrengthField != null)
                        brushNoiseStrengthField.value = data.settings.brushNoise.noiseStrength;
                    RefreshPreview();
                });

            if (paintNoiseStrengthField != null)
                paintNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (paintNoiseStrengthSlider != null)
                        paintNoiseStrengthSlider.value = entry.noiseStrength;
                    RefreshPreview();
                });

            if (brushNoiseStrengthField != null)
                brushNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (brushNoiseStrengthSlider != null)
                        brushNoiseStrengthSlider.value = data.settings.brushNoise.noiseStrength;
                    RefreshPreview();
                });

            if (paintNoiseEdgeSlider != null)
                paintNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (paintNoiseEdgeField != null)
                        paintNoiseEdgeField.value = evt.newValue;
                    RefreshPreview();
                });

            if (brushNoiseEdgeSlider != null)
                brushNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (brushNoiseEdgeField != null)
                        brushNoiseEdgeField.value = data.settings.brushNoise.noiseEdge;
                    RefreshPreview();
                });

            if (paintNoiseEdgeField != null)
                paintNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (paintNoiseEdgeSlider != null)
                        paintNoiseEdgeSlider.value = entry.noiseEdge;
                    RefreshPreview();
                });

            if (brushNoiseEdgeField != null)
                brushNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (brushNoiseEdgeSlider != null)
                        brushNoiseEdgeSlider.value = data.settings.brushNoise.noiseEdge;
                    RefreshPreview();
                });

            if (paintNoiseSizeSlider != null)
                paintNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (paintNoiseSizeField != null)
                        paintNoiseSizeField.value = entry.noiseWorldSizeMeters;
                    RefreshPreview();
                });

            if (brushNoiseSizeSlider != null)
                brushNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (brushNoiseSizeField != null)
                        brushNoiseSizeField.value = data.settings.brushNoise.noiseWorldSizeMeters;
                    RefreshPreview();
                });

            if (paintNoiseSizeField != null)
                paintNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (paintNoiseSizeSlider != null)
                        paintNoiseSizeSlider.value = entry.noiseWorldSizeMeters;
                    RefreshPreview();
                });

            if (brushNoiseSizeField != null)
                brushNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (brushNoiseSizeSlider != null)
                        brushNoiseSizeSlider.value = data.settings.brushNoise.noiseWorldSizeMeters;
                    RefreshPreview();
                });

            if (paintNoiseOffsetField != null)
                paintNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseOffset = evt.newValue;
                    RefreshPreview();
                });

            if (paintNoiseResponseCurve != null)
                paintNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve();
                    RefreshPreview();
                });

            if (brushNoiseOffsetField != null)
                brushNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseOffset = evt.newValue;
                    RefreshPreview();
                });

            if (brushNoiseResponseCurve != null)
                brushNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve();
                    RefreshPreview();
                });

            if (paintNoiseInvertToggle != null)
                paintNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.paintNoiseLayers == null)
                        data.settings.paintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(data.settings.paintNoiseLayers, data.settings.selectedLayerIndex);
                    entry.noiseInvert = evt.newValue;
                    UpdateNoiseInvertToggleVisual(paintNoiseInvertToggle);
                    RefreshPreview();
                });

            if (brushNoiseInvertToggle != null)
                brushNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.brushNoise == null)
                        data.settings.brushNoise = new BrushNoiseSettings();
                    data.settings.brushNoise.noiseInvert = evt.newValue;
                    UpdateNoiseInvertToggleVisual(brushNoiseInvertToggle);
                    RefreshPreview();
                });

            if (paintNoiseFoldout != null)
                paintNoiseFoldout.RegisterValueChangedCallback(evt =>
                {
                    data.isPaintNoiseFoldoutExpanded = evt.newValue;
                });

            if (brushNoiseFoldout != null)
                brushNoiseFoldout.RegisterValueChangedCallback(evt =>
                {
                    data.isBrushNoiseFoldoutExpanded = evt.newValue;
                });

            DetailNoiseLayerSettings GetOrCreateDetailNoiseLayer(List<DetailNoiseLayerSettings> list, int layerIndex)
            {
                if (list == null)
                {
                    list = new List<DetailNoiseLayerSettings>();
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (entry != null && entry.detailLayerIndex == layerIndex)
                        return entry;
                }

                var newEntry = new DetailNoiseLayerSettings { detailLayerIndex = layerIndex };
                list.Add(newEntry);
                return newEntry;
            }

            TreeNoisePrototypeSettings GetOrCreateTreeNoiseLayer(List<TreeNoisePrototypeSettings> list, int prototypeIndex)
            {
                if (list == null)
                {
                    list = new List<TreeNoisePrototypeSettings>();
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (entry != null && entry.treePrototypeIndex == prototypeIndex)
                        return entry;
                }

                var newEntry = new TreeNoisePrototypeSettings { treePrototypeIndex = prototypeIndex };
                list.Add(newEntry);
                return newEntry;
            }

            List<int> GetSelectedDetailLayerIndices()
            {
                if (data.settings.selectedDetailLayerIndices != null && data.settings.selectedDetailLayerIndices.Count > 0)
                    return data.settings.selectedDetailLayerIndices.Distinct().ToList();

                return new List<int> { data.settings.selectedDetailLayerIndex };
            }

            List<int> GetSelectedTreePrototypeIndices()
            {
                if (data.settings.selectedTreePrototypeIndices != null && data.settings.selectedTreePrototypeIndices.Count > 0)
                    return data.settings.selectedTreePrototypeIndices.Distinct().ToList();

                return new List<int> { data.settings.selectedTreePrototypeIndex };
            }

            if (detailNoiseTextureField != null)
                detailNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    bool hasNoiseTexture = false;
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseTexture = evt.newValue as Texture2D;
                        if (entry.noiseTexture != null)
                            hasNoiseTexture = true;
                    }
                    SetNoiseHeaderIconTint(detailNoiseFoldout, hasNoiseTexture);
                    var itemIndex = GetGlobalSplineIndex(data);
                    if (itemIndex >= 0)
                        splinesListView?.RefreshItem(itemIndex);
                    else
                        splinesListView?.RefreshItems();
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseSizeSlider != null)
                detailNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (detailNoiseSizeField != null)
                        detailNoiseSizeField.value = noiseSize;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseSizeField != null)
                detailNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (detailNoiseSizeSlider != null)
                        detailNoiseSizeSlider.value = noiseSize;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseOffsetField != null)
                detailNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseOffset = evt.newValue;
                    }
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseThresholdSlider != null)
                detailNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (detailNoiseThresholdField != null)
                        detailNoiseThresholdField.value = noiseThreshold;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseThresholdField != null)
                detailNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (detailNoiseThresholdSlider != null)
                        detailNoiseThresholdSlider.value = noiseThreshold;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseInvertToggle != null)
                detailNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.detailNoiseLayers == null)
                        data.settings.detailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetSelectedDetailLayerIndices();
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(data.settings.detailNoiseLayers, layerIndex);
                        entry.noiseInvert = evt.newValue;
                    }
                    UpdateNoiseInvertToggleVisual(detailNoiseInvertToggle);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (detailNoiseFoldout != null)
                detailNoiseFoldout.RegisterValueChangedCallback(evt =>
                {
                    data.isDetailNoiseFoldoutExpanded = evt.newValue;
                });

            if (treeNoiseTextureField != null)
                treeNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    bool hasNoiseTexture = false;
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseTexture = evt.newValue as Texture2D;
                        if (entry.noiseTexture != null)
                            hasNoiseTexture = true;
                    }
                    SetNoiseHeaderIconTint(treeNoiseFoldout, hasNoiseTexture);
                    var itemIndex = GetGlobalSplineIndex(data);
                    if (itemIndex >= 0)
                        splinesListView?.RefreshItem(itemIndex);
                    else
                        splinesListView?.RefreshItems();
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseSizeSlider != null)
                treeNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (treeNoiseSizeField != null)
                        treeNoiseSizeField.value = noiseSize;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseSizeField != null)
                treeNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (treeNoiseSizeSlider != null)
                        treeNoiseSizeSlider.value = noiseSize;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseOffsetField != null)
                treeNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseOffset = evt.newValue;
                    }
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseThresholdSlider != null)
                treeNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (treeNoiseThresholdField != null)
                        treeNoiseThresholdField.value = noiseThreshold;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseThresholdField != null)
                treeNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (treeNoiseThresholdSlider != null)
                        treeNoiseThresholdSlider.value = noiseThreshold;
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseInvertToggle != null)
                treeNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (data.settings.treeNoiseLayers == null)
                        data.settings.treeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetSelectedTreePrototypeIndices();
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(data.settings.treeNoiseLayers, prototypeIndex);
                        entry.noiseInvert = evt.newValue;
                    }
                    UpdateNoiseInvertToggleVisual(treeNoiseInvertToggle);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (treeNoiseFoldout != null)
                treeNoiseFoldout.RegisterValueChangedCallback(evt =>
                {
                    data.isTreeNoiseFoldoutExpanded = evt.newValue;
                });

            if (overrideDetailToggle != null)
                overrideDetailToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.overrideDetail = evt.newValue;
                    UpdateTabText(overrideDetailTab, "Detail", evt.newValue);
                    UpdateSplineNameIndicator(element, data);
                    if (detailLayerPalette != null) detailLayerPalette.SetEnabled(evt.newValue);
                    if (detailStrengthSlider != null) detailStrengthSlider.SetEnabled(evt.newValue);
                    if (detailStrengthField != null) detailStrengthField.SetEnabled(evt.newValue);
                    if (detailSlopeLimitSlider != null) detailSlopeLimitSlider.SetEnabled(evt.newValue);
                    if (detailSlopeLimitField != null) detailSlopeLimitField.SetEnabled(evt.newValue);
                    if (detailFalloffSlider != null) detailFalloffSlider.SetEnabled(evt.newValue);
                    if (detailFalloffField != null) detailFalloffField.SetEnabled(evt.newValue);
                    if (detailSpreadSlider != null) detailSpreadSlider.SetEnabled(evt.newValue);
                    if (detailSpreadField != null) detailSpreadField.SetEnabled(evt.newValue);
                    if (detailRemoveThresholdSlider != null) detailRemoveThresholdSlider.SetEnabled(evt.newValue);
                    if (detailRemoveThresholdField != null) detailRemoveThresholdField.SetEnabled(evt.newValue);
                    if (detailNoiseTextureField != null) detailNoiseTextureField.SetEnabled(evt.newValue);
                    if (detailNoiseSizeSlider != null) detailNoiseSizeSlider.SetEnabled(evt.newValue);
                    if (detailNoiseSizeField != null) detailNoiseSizeField.SetEnabled(evt.newValue);
                    if (detailNoiseOffsetField != null) detailNoiseOffsetField.SetEnabled(evt.newValue);
                    if (detailNoiseThresholdSlider != null) detailNoiseThresholdSlider.SetEnabled(evt.newValue);
                    if (detailNoiseThresholdField != null) detailNoiseThresholdField.SetEnabled(evt.newValue);
                    if (detailNoiseInvertToggle != null) detailNoiseInvertToggle.SetEnabled(evt.newValue);
                    notifyOverlay();
                    RefreshPreview();
                });

            if (overrideTreeToggle != null)
                overrideTreeToggle.RegisterValueChangedCallback(evt =>
                {
                    data.settings.overrideTrees = evt.newValue;
                    UpdateTabText(overrideTreeTab, "Trees", evt.newValue);
                    UpdateSplineNameIndicator(element, data);
                    SetTreeUIEnabled(evt.newValue);
                    notifyOverlay();
                    RefreshPreview();
                });

            // Tab switching logic
            if (overrideModeTab != null)
            {
                overrideModeTab.RegisterCallback<ClickEvent>(OnOverrideModeTabClicked);
            }

            if (overrideBrushTab != null)
            {
                overrideBrushTab.RegisterCallback<ClickEvent>(OnOverrideBrushTabClicked);
            }

            if (overridePaintTab != null)
            {
                overridePaintTab.RegisterCallback<ClickEvent>(OnOverridePaintTabClicked);
            }

            if (overrideDetailTab != null)
            {
                overrideDetailTab.RegisterCallback<ClickEvent>(OnOverrideDetailTabClicked);
            }

            if (overrideTreeTab != null)
            {
                overrideTreeTab.RegisterCallback<ClickEvent>(OnOverrideTreeTabClicked);
            }

            var applyButton = element.Q<Button>("apply-spline-button");
            var duplicateButton = element.Q<Button>("duplicate-spline-button");
            var deleteButton = element.Q<Button>("delete-spline-button");

            // Load and assign icon images
            var applyIcon = element.Q<Image>("apply-icon");
            var duplicateIcon = element.Q<Image>("duplicate-icon");
            var deleteIcon = element.Q<Image>("delete-icon");

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

            if (applyButton != null)
            {
                applyButton.clicked += () => OnApplySplineClicked(data);
            }

            if (duplicateButton != null)
            {
                duplicateButton.clicked += () => OnDuplicateSplineClicked(data);
            }

            if (deleteButton != null)
            {
                deleteButton.clicked += () => OnDeleteSplineClicked(data);
            }

            if (pathModeRadio != null)
            {
                pathModeRadio.clicked += () =>
                {
                    data.settings.mode = SplineApplyMode.Path;
                    SetModeButtonSelected(pathModeRadio, true);
                    if (shapeModeRadio != null)
                        SetModeButtonSelected(shapeModeRadio, false);
                    UpdateSplineIcons(element, data);
                    RefreshPreview();
                };
            }

            if (shapeModeRadio != null)
            {
                shapeModeRadio.clicked += () =>
                {
                    data.settings.mode = SplineApplyMode.Shape;
                    if (pathModeRadio != null)
                        SetModeButtonSelected(pathModeRadio, false);
                    SetModeButtonSelected(shapeModeRadio, true);
                    UpdateSplineIcons(element, data);
                    RefreshPreview();
                };
            }

            if (brushHiddenField != null)
                brushHiddenField.RegisterValueChangedCallback(evt =>
                {
                    try
                    {
                        if (brushPreviewImage != null)
                            brushPreviewImage.image = evt.newValue as Texture2D;
                        RefreshPreview();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error handling per-spline brush field change: {e.Message}");
                    }
                });

            if (brushSizeSlider != null)
                brushSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                    data.settings.sizeMeters = clampedValue;
                    if (brushSizeField != null)
                        brushSizeField.SetValueWithoutNotify(clampedValue);
                    RefreshPreview();
                });
            if (brushSizeField != null)
                brushSizeField.RegisterValueChangedCallback(evt =>
                {
                    float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                    data.settings.sizeMeters = clampedValue;
                    if (brushSizeSlider != null)
                        brushSizeSlider.SetValueWithoutNotify(clampedValue);
                    if (brushSizeField != null)
                        brushSizeField.SetValueWithoutNotify(clampedValue);
                    RefreshPreview();
                });

            // Per-spline brush hardness controls
            if (brushHardnessSlider != null)
                brushHardnessSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.hardness = evt.newValue;
                    if (brushHardnessField != null)
                        brushHardnessField.SetValueWithoutNotify(evt.newValue);
                    if (brushPreviewImage != null)
                        brushPreviewImage.image = BrushFalloffUtils.GenerateBrushPreviewTexture(64, evt.newValue);
                    RefreshPreview();
                });
            if (brushHardnessField != null)
                brushHardnessField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.hardness = evt.newValue;
                    if (brushHardnessSlider != null)
                        brushHardnessSlider.SetValueWithoutNotify(evt.newValue);
                    if (brushPreviewImage != null)
                        brushPreviewImage.image = BrushFalloffUtils.GenerateBrushPreviewTexture(64, evt.newValue);
                    RefreshPreview();
                });

            if (overrideBrushSizeMultiplierToggle != null)
                overrideBrushSizeMultiplierToggle.RegisterValueChangedCallback(evt =>
                {
                    var settingsComponent = data.container != null ? data.container.GetComponent<TerrainSplineSettings>() : null;
                    if (settingsComponent != null)
                        Undo.RecordObject(settingsComponent, "Toggle Size Multiplier Override");

                    data.settings.overrideSizeMultiplier = evt.newValue;
                    if (brushSizeMultiplierCurve != null)
                        CurvePresetDropdownUtility.SetDisplay(brushSizeMultiplierCurve, data.settings.overrideBrush && evt.newValue);

                    if (settingsComponent != null)
                        EditorUtility.SetDirty(settingsComponent);

                    notifyOverlay();
                    RefreshPreview();
                });
            if (brushSizeMultiplierCurve != null)
                brushSizeMultiplierCurve.RegisterValueChangedCallback(evt =>
                {
                    var settingsComponent = data.container != null ? data.container.GetComponent<TerrainSplineSettings>() : null;
                    if (settingsComponent != null)
                        Undo.RecordObject(settingsComponent, "Modify Size Multiplier Curve");

                    data.settings.sizeMultiplier = evt.newValue.CloneCurve();
                    // Cache invalidation is handled automatically by IsDirty() method

                    if (settingsComponent != null)
                        EditorUtility.SetDirty(settingsComponent);

                    notifyOverlay();
                    RefreshPreview();
                });
            if (strengthSlider != null)
                strengthSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.strength = evt.newValue;
                    if (strengthField != null)
                        strengthField.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            if (strengthField != null)
                strengthField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.strength = evt.newValue;
                    if (strengthSlider != null)
                        strengthSlider.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            if (sampleStepSlider != null)
                sampleStepSlider.RegisterValueChangedCallback(evt =>
                {
                    data.settings.sampleStep = evt.newValue;
                    if (sampleStepField != null)
                        sampleStepField.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            if (sampleStepField != null)
                sampleStepField.RegisterValueChangedCallback(evt =>
                {
                    data.settings.sampleStep = evt.newValue;
                    if (sampleStepSlider != null)
                        sampleStepSlider.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
        }

        void BindGroupItem(VisualElement element, GroupItemData data)
        {
            // Add comprehensive safety checks
            if (element == null || data == null)
            {
                Debug.LogWarning("BindGroupItem called with null element or data");
                return;
            }

            // Get UI elements (template is already loaded in bindItem)
            // The template is added as a child, so we need to query from the first child
            var templateElement = element.Q<VisualElement>("group-item-container");
            if (templateElement == null)
            {
                Debug.LogError("BindGroupItem: Could not find group-item-container in loaded template");
                return;
            }

            var groupNameLabel = templateElement.Q<Label>("group-name-label");
            var groupFoldoutToggle = templateElement.Q<Toggle>("group-foldout-toggle");
            var groupArrowLabel = groupFoldoutToggle?.Q<Label>("group-arrow");
            var groupContent = templateElement.Q<VisualElement>("group-content");

            ConfigureListRowFoldoutToggle(groupFoldoutToggle);

            if (groupNameLabel == null || groupFoldoutToggle == null || groupContent == null)
            {
                Debug.LogError("BindGroupItem: Could not find required UI elements in group template");
                return;
            }

            // Set group name
            groupNameLabel.text = data.hierarchyPath;
            groupNameLabel.enableRichText = true;

            // Set foldout toggle state
            groupFoldoutToggle.SetValueWithoutNotify(data.isFoldoutExpanded);

            // Update arrow direction based on state
            if (groupArrowLabel != null)
            {
                groupArrowLabel.text = data.isFoldoutExpanded ? "▼" : "▶";
            }

            SyncGroupContent(groupContent, data, data.isFoldoutExpanded);

            // Add foldout toggle change handler
            if (groupFoldoutToggle != null)
            {
                groupFoldoutToggle.RegisterValueChangedCallback(evt =>
                {
                    data.isFoldoutExpanded = evt.newValue;

                    // Update arrow direction
                    if (groupArrowLabel != null)
                    {
                        groupArrowLabel.text = evt.newValue ? "▼" : "▶";
                    }

                    SyncGroupContent(groupContent, data, evt.newValue);
                });
            }

            // Add click handler for name label
            if (groupNameLabel != null)
            {
                groupNameLabel.RegisterCallback<ClickEvent>(OnGroupNameClicked);
                groupNameLabel.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());
            }
        }

        VisualElement CreateSplineElement(SplineItemData splineData)
        {
            var template = LoadSplinesListItemTemplate("UI/SplineListItem", "nested spline");
            if (template == null)
            {
                Debug.LogError("Failed to load SplineListItem template for nested spline");
                var fallback = new Label("Failed to load spline template");
                fallback.style.color = Color.red;
                return fallback;
            }

            var splineElement = template.CloneTree();

            // Add nested styling
            splineElement.AddToClassList("spline-item-nested");

            // Bind the spline data
            RunWithSuppressedRefreshPreviewRequests(() => BindSplineItem(splineElement, splineData));

            return splineElement;
        }

        void EnsureGroupSplineElementsBuilt(GroupItemData data)
        {
            if (data == null || data.splineElementsBuilt)
                return;

            data.cachedSplineElements.Clear();

            foreach (var splineData in data.splines)
            {
                var splineElement = CreateSplineElement(splineData);
                data.cachedSplineElements.Add(splineElement);
            }

            data.splineElementsBuilt = true;
        }

        void SyncGroupContent(VisualElement groupContent, GroupItemData data, bool expanded)
        {
            if (groupContent == null || data == null)
                return;

            if (!expanded)
            {
                groupContent.style.display = DisplayStyle.None;
                return;
            }

            EnsureGroupSplineElementsBuilt(data);

            groupContent.style.display = DisplayStyle.Flex;

            bool contentMatches = groupContent.childCount == data.cachedSplineElements.Count;
            if (contentMatches)
            {
                for (int i = 0; i < data.cachedSplineElements.Count; i++)
                {
                    if (groupContent.ElementAt(i) != data.cachedSplineElements[i])
                    {
                        contentMatches = false;
                        break;
                    }
                }
            }

            if (contentMatches)
                return;

            groupContent.Clear();
            for (int i = 0; i < data.cachedSplineElements.Count; i++)
            {
                groupContent.Add(data.cachedSplineElements[i]);
            }
        }

        void OnGroupNameClicked(ClickEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            if (element == null) return;

            // Get the group item container (parent of header row)
            var groupItemContainer = element.parent?.parent;
            if (groupItemContainer == null) return;

            // Find the group item data
            var groupData = FindGroupItemDataForElement(groupItemContainer);
            if (groupData == null) return;

            // Only select GameObject in hierarchy - do NOT toggle foldout state
            if (groupData.groupTransform != null && groupData.groupTransform.gameObject != null)
            {
                Selection.activeGameObject = groupData.groupTransform.gameObject;
                EditorGUIUtility.PingObject(groupData.groupTransform.gameObject);
            }

            RefreshPreview();
        }

        void OnOverrideModeTabClicked(ClickEvent evt)
        {
            var button = evt.target as Button;
            if (button == null) return;

            // Find the spline item container
            var splineItemContainer = button.GetFirstAncestorOfType<VisualElement>();
            while (splineItemContainer != null && !splineItemContainer.ClassListContains("spline-item-container"))
            {
                splineItemContainer = splineItemContainer.parent;
            }

            if (splineItemContainer == null) return;

            // Find the data for this spline item
            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData != null)
            {
                splineItemData.isOverrideBrushTabActive = false;
                splineItemData.isOverridePaintTabActive = false;
                splineItemData.isOverrideDetailTabActive = false;
                splineItemData.isOverrideTreeTabActive = false;
            }

            var overrideModeTab = splineItemContainer.Q<Button>("override-mode-tab");
            var overrideBrushTab = splineItemContainer.Q<Button>("override-brush-tab");
            var overridePaintTab = splineItemContainer.Q<Button>("override-paint-tab");
            var overrideDetailTab = splineItemContainer.Q<Button>("override-detail-tab");
            var overrideTreeTab = splineItemContainer.Q<Button>("override-tree-tab");
            var overrideModeContent = splineItemContainer.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = splineItemContainer.Q<VisualElement>("override-brush-content");
            var overridePaintContent = splineItemContainer.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = splineItemContainer.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = splineItemContainer.Q<VisualElement>("override-tree-content");

            SetTabButtonActive(overrideModeTab, true);
            SetTabButtonActive(overrideBrushTab, false);
            SetTabButtonActive(overridePaintTab, false);
            SetTabButtonActive(overrideDetailTab, false);
            SetTabButtonActive(overrideTreeTab, false);
            if (overrideModeContent != null)
                overrideModeContent.style.display = DisplayStyle.Flex;
            if (overrideBrushContent != null)
                overrideBrushContent.style.display = DisplayStyle.None;
            if (overridePaintContent != null)
                overridePaintContent.style.display = DisplayStyle.None;
            if (overrideDetailContent != null)
                overrideDetailContent.style.display = DisplayStyle.None;
            if (overrideTreeContent != null)
                overrideTreeContent.style.display = DisplayStyle.None;

            ScheduleSplineTabLayoutRefresh(splineItemData, splineItemContainer);
        }

        void OnOverrideBrushTabClicked(ClickEvent evt)
        {
            var button = evt.target as Button;
            if (button == null) return;

            // Find the spline item container
            var splineItemContainer = button.GetFirstAncestorOfType<VisualElement>();
            while (splineItemContainer != null && !splineItemContainer.ClassListContains("spline-item-container"))
            {
                splineItemContainer = splineItemContainer.parent;
            }

            if (splineItemContainer == null) return;

            // Find the data for this spline item
            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData != null)
            {
                splineItemData.isOverrideBrushTabActive = true;
                splineItemData.isOverridePaintTabActive = false;
                splineItemData.isOverrideDetailTabActive = false;
                splineItemData.isOverrideTreeTabActive = false;
            }

            var overrideModeTab = splineItemContainer.Q<Button>("override-mode-tab");
            var overrideBrushTab = splineItemContainer.Q<Button>("override-brush-tab");
            var overridePaintTab = splineItemContainer.Q<Button>("override-paint-tab");
            var overrideDetailTab = splineItemContainer.Q<Button>("override-detail-tab");
            var overrideTreeTab = splineItemContainer.Q<Button>("override-tree-tab");
            var overrideModeContent = splineItemContainer.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = splineItemContainer.Q<VisualElement>("override-brush-content");
            var overridePaintContent = splineItemContainer.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = splineItemContainer.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = splineItemContainer.Q<VisualElement>("override-tree-content");

            SetTabButtonActive(overrideModeTab, false);
            SetTabButtonActive(overrideBrushTab, true);
            SetTabButtonActive(overridePaintTab, false);
            SetTabButtonActive(overrideDetailTab, false);
            SetTabButtonActive(overrideTreeTab, false);
            if (overrideModeContent != null)
                overrideModeContent.style.display = DisplayStyle.None;
            if (overrideBrushContent != null)
                overrideBrushContent.style.display = DisplayStyle.Flex;
            if (overridePaintContent != null)
                overridePaintContent.style.display = DisplayStyle.None;
            if (overrideDetailContent != null)
                overrideDetailContent.style.display = DisplayStyle.None;
            if (overrideTreeContent != null)
                overrideTreeContent.style.display = DisplayStyle.None;

            ScheduleSplineTabLayoutRefresh(splineItemData, splineItemContainer);
        }

        void OnOverridePaintTabClicked(ClickEvent evt)
        {
            var button = evt.target as Button;
            if (button == null) return;

            // Find the spline item container
            var splineItemContainer = button.GetFirstAncestorOfType<VisualElement>();
            while (splineItemContainer != null && !splineItemContainer.ClassListContains("spline-item-container"))
            {
                splineItemContainer = splineItemContainer.parent;
            }

            if (splineItemContainer == null) return;

            // Find the data for this spline item
            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData != null)
            {
                splineItemData.isOverrideBrushTabActive = false;
                splineItemData.isOverridePaintTabActive = true;
                splineItemData.isOverrideDetailTabActive = false;
                splineItemData.isOverrideTreeTabActive = false;
            }

            var overrideModeTab = splineItemContainer.Q<Button>("override-mode-tab");
            var overrideBrushTab = splineItemContainer.Q<Button>("override-brush-tab");
            var overridePaintTab = splineItemContainer.Q<Button>("override-paint-tab");
            var overrideDetailTab = splineItemContainer.Q<Button>("override-detail-tab");
            var overrideTreeTab = splineItemContainer.Q<Button>("override-tree-tab");
            var overrideModeContent = splineItemContainer.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = splineItemContainer.Q<VisualElement>("override-brush-content");
            var overridePaintContent = splineItemContainer.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = splineItemContainer.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = splineItemContainer.Q<VisualElement>("override-tree-content");

            SetTabButtonActive(overrideModeTab, false);
            SetTabButtonActive(overrideBrushTab, false);
            SetTabButtonActive(overridePaintTab, true);
            SetTabButtonActive(overrideDetailTab, false);
            SetTabButtonActive(overrideTreeTab, false);
            if (overrideModeContent != null)
                overrideModeContent.style.display = DisplayStyle.None;
            if (overrideBrushContent != null)
                overrideBrushContent.style.display = DisplayStyle.None;
            if (overridePaintContent != null)
                overridePaintContent.style.display = DisplayStyle.Flex;
            if (overrideDetailContent != null)
                overrideDetailContent.style.display = DisplayStyle.None;
            if (overrideTreeContent != null)
                overrideTreeContent.style.display = DisplayStyle.None;

            ScheduleSplineTabLayoutRefresh(splineItemData, splineItemContainer);
        }

        void OnOverrideDetailTabClicked(ClickEvent evt)
        {
            var button = evt.target as Button;
            if (button == null) return;

            var splineItemContainer = button.GetFirstAncestorOfType<VisualElement>();
            while (splineItemContainer != null && !splineItemContainer.ClassListContains("spline-item-container"))
            {
                splineItemContainer = splineItemContainer.parent;
            }

            if (splineItemContainer == null) return;

            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData != null)
            {
                splineItemData.isOverrideBrushTabActive = false;
                splineItemData.isOverridePaintTabActive = false;
                splineItemData.isOverrideDetailTabActive = true;
                splineItemData.isOverrideTreeTabActive = false;
            }

            var overrideModeTab = splineItemContainer.Q<Button>("override-mode-tab");
            var overrideBrushTab = splineItemContainer.Q<Button>("override-brush-tab");
            var overridePaintTab = splineItemContainer.Q<Button>("override-paint-tab");
            var overrideDetailTab = splineItemContainer.Q<Button>("override-detail-tab");
            var overrideTreeTab = splineItemContainer.Q<Button>("override-tree-tab");
            var overrideModeContent = splineItemContainer.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = splineItemContainer.Q<VisualElement>("override-brush-content");
            var overridePaintContent = splineItemContainer.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = splineItemContainer.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = splineItemContainer.Q<VisualElement>("override-tree-content");

            SetTabButtonActive(overrideModeTab, false);
            SetTabButtonActive(overrideBrushTab, false);
            SetTabButtonActive(overridePaintTab, false);
            SetTabButtonActive(overrideDetailTab, true);
            SetTabButtonActive(overrideTreeTab, false);
            if (overrideModeContent != null)
                overrideModeContent.style.display = DisplayStyle.None;
            if (overrideBrushContent != null)
                overrideBrushContent.style.display = DisplayStyle.None;
            if (overridePaintContent != null)
                overridePaintContent.style.display = DisplayStyle.None;
            if (overrideDetailContent != null)
                overrideDetailContent.style.display = DisplayStyle.Flex;
            if (overrideTreeContent != null)
                overrideTreeContent.style.display = DisplayStyle.None;

            ScheduleSplineTabLayoutRefresh(splineItemData, splineItemContainer);
        }

        void OnOverrideTreeTabClicked(ClickEvent evt)
        {
            var button = evt.target as Button;
            if (button == null) return;

            var splineItemContainer = button.GetFirstAncestorOfType<VisualElement>();
            while (splineItemContainer != null && !splineItemContainer.ClassListContains("spline-item-container"))
            {
                splineItemContainer = splineItemContainer.parent;
            }

            if (splineItemContainer == null) return;

            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData != null)
            {
                splineItemData.isOverrideBrushTabActive = false;
                splineItemData.isOverridePaintTabActive = false;
                splineItemData.isOverrideDetailTabActive = false;
                splineItemData.isOverrideTreeTabActive = true;
            }

            var overrideModeTab = splineItemContainer.Q<Button>("override-mode-tab");
            var overrideBrushTab = splineItemContainer.Q<Button>("override-brush-tab");
            var overridePaintTab = splineItemContainer.Q<Button>("override-paint-tab");
            var overrideDetailTab = splineItemContainer.Q<Button>("override-detail-tab");
            var overrideTreeTab = splineItemContainer.Q<Button>("override-tree-tab");
            var overrideModeContent = splineItemContainer.Q<VisualElement>("override-mode-content");
            var overrideBrushContent = splineItemContainer.Q<VisualElement>("override-brush-content");
            var overridePaintContent = splineItemContainer.Q<VisualElement>("override-paint-content");
            var overrideDetailContent = splineItemContainer.Q<VisualElement>("override-detail-content");
            var overrideTreeContent = splineItemContainer.Q<VisualElement>("override-tree-content");

            SetTabButtonActive(overrideModeTab, false);
            SetTabButtonActive(overrideBrushTab, false);
            SetTabButtonActive(overridePaintTab, false);
            SetTabButtonActive(overrideDetailTab, false);
            SetTabButtonActive(overrideTreeTab, true);
            if (overrideModeContent != null)
                overrideModeContent.style.display = DisplayStyle.None;
            if (overrideBrushContent != null)
                overrideBrushContent.style.display = DisplayStyle.None;
            if (overridePaintContent != null)
                overridePaintContent.style.display = DisplayStyle.None;
            if (overrideDetailContent != null)
                overrideDetailContent.style.display = DisplayStyle.None;
            if (overrideTreeContent != null)
                overrideTreeContent.style.display = DisplayStyle.Flex;

            ScheduleSplineTabLayoutRefresh(splineItemData, splineItemContainer);
        }

        void ScheduleSplineTabLayoutRefresh(SplineItemData splineItemData, VisualElement splineItemContainer)
        {
            if (splineItemContainer == null)
                return;

            splineItemContainer.MarkDirtyRepaint();

            splineItemContainer.schedule.Execute(() =>
            {
                if (splinesListView == null)
                    return;

                RefreshTopLevelListItem(splineItemData);
            }).ExecuteLater(0);
        }

        void OnNameLabelClicked(ClickEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            if (element == null) return;

            // Get the spline item container (parent of header row)
            var splineItemContainer = element.parent?.parent;
            if (splineItemContainer == null) return;

            // Find the spline item data
            var splineItemData = FindSplineItemDataForElement(splineItemContainer);
            if (splineItemData == null) return;

            // Only select GameObject in hierarchy - do NOT toggle foldout state
            if (splineItemData.container != null && splineItemData.container.gameObject != null)
            {
                Selection.activeGameObject = splineItemData.container.gameObject;
                EditorGUIUtility.PingObject(splineItemData.container.gameObject);
            }

            RefreshPreview();
        }

        SplineItemData FindSplineItemDataForElement(VisualElement element)
        {
            // Find the name label to get the spline name
            var nameLabel = element.Q<Label>("spline-name-label");
            if (nameLabel == null) return null;

            string nameText = nameLabel.text;
            // Extract the index from the name text (format: "[0] GameObjectName")
            if (nameText.StartsWith("[") && nameText.Contains("]"))
            {
                int endBracket = nameText.IndexOf("]");
                if (int.TryParse(nameText.Substring(1, endBracket - 1), out int index))
                {
                    if (index >= 0)
                    {
                        return GetSplineItemByIndex(index);
                    }
                }
            }

            return null;
        }

        GroupItemData FindGroupItemDataForElement(VisualElement element)
        {
            // Find the group name label to get the group hierarchy path
            var nameLabel = element.Q<Label>("group-name-label");
            if (nameLabel == null) return null;

            string hierarchyPath = nameLabel.text;

            // Find the group with matching hierarchy path
            foreach (var item in splineItems)
            {
                if (item is GroupItemData group && group.hierarchyPath == hierarchyPath)
                {
                    return group;
                }
            }

            return null;
        }

        SplineItemData FindSplineItemForContainer(SplineContainer container)
        {
            if (container == null) return null;

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem && splineItem.container == container)
                {
                    return splineItem;
                }

                if (item is GroupItemData groupData)
                {
                    foreach (var child in groupData.splines)
                    {
                        if (child?.container == container)
                        {
                            return child;
                        }
                    }
                }
            }

            return null;
        }

        int GetTopLevelListIndexForSplineItem(SplineItemData targetSpline)
        {
            if (targetSpline == null)
                return -1;

            for (int i = 0; i < splineItems.Count; i++)
            {
                if (splineItems[i] is SplineItemData splineItem && ReferenceEquals(splineItem, targetSpline))
                {
                    return i;
                }

                if (splineItems[i] is GroupItemData groupItem)
                {
                    for (int childIndex = 0; childIndex < groupItem.splines.Count; childIndex++)
                    {
                        if (ReferenceEquals(groupItem.splines[childIndex], targetSpline))
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        void RefreshTopLevelListItem(SplineItemData targetSpline)
        {
            if (splinesListView == null || targetSpline == null)
                return;

            int itemIndex = GetTopLevelListIndexForSplineItem(targetSpline);
            if (itemIndex >= 0)
            {
                splinesListView.RefreshItem(itemIndex);
                splinesListView.MarkDirtyRepaint();
                return;
            }

            splinesListView.RefreshItems();
            splinesListView.MarkDirtyRepaint();
        }

        void RefreshTopLevelListItems(IEnumerable<int> itemIndices)
        {
            if (splinesListView == null)
                return;

            bool refreshedAny = false;
            var uniqueIndices = new HashSet<int>();

            if (itemIndices != null)
            {
                foreach (var itemIndex in itemIndices)
                {
                    if (itemIndex < 0 || itemIndex >= splineItems.Count || !uniqueIndices.Add(itemIndex))
                        continue;

                    splinesListView.RefreshItem(itemIndex);
                    refreshedAny = true;
                }
            }

            if (!refreshedAny)
            {
                splinesListView.RefreshItems();
            }

            splinesListView.MarkDirtyRepaint();
        }

        void CollapseAllSplines()
        {
            // Collapse all groups and splines
            foreach (var item in splineItems)
            {
                item.isFoldoutExpanded = false;

                // If it's a group, also collapse all its splines
                if (item is GroupItemData groupData)
                {
                    foreach (var spline in groupData.splines)
                    {
                        spline.isFoldoutExpanded = false;
                    }
                }
            }

            // Refresh the list to update the UI
            if (splinesListView != null)
            {
                splinesListView.RefreshItems();
            }
        }

        void ExpandAllSplines()
        {
            // Expand all groups and splines
            foreach (var item in splineItems)
            {
                item.isFoldoutExpanded = true;

                // If it's a group, also expand all its splines
                if (item is GroupItemData groupData)
                {
                    foreach (var spline in groupData.splines)
                    {
                        spline.isFoldoutExpanded = true;
                    }
                }
            }

            // Refresh the list to update the UI
            if (splinesListView != null)
            {
                splinesListView.RefreshItems();
            }
        }

        void RefreshChildren(bool markPreviewTexturesDirty = true)
        {
            // Validate references first
            ValidateAndResetReferences();

            // Save existing foldout states before clearing
            SaveFoldoutStates();
            SaveTabStates();

            // Clear existing items
            splineItems.Clear();

            // Check if splineGroup is null or if the GameObject has been destroyed
            if (splineGroup == null || splineGroup.gameObject == null)
            {
                // Update ListView with empty data
                RebuildListView();
                return;
            }

            // Collect all valid spline containers (including nested ones)
            var splineContainers = splineGroup.GetComponentsInChildren<SplineContainer>();

            if (splineContainers.Length == 0)
            {
                RebuildListView();
                return;
            }

            // Collect all direct children of the spline group (both individual splines and group transforms)
            var directChildren = new List<(Transform transform, bool isSpline, SplineHierarchyInfo splineInfo)>();

            // Get all direct children of splineGroup
            for (int i = 0; i < splineGroup.childCount; i++)
            {
                Transform child = splineGroup.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy) continue;

                // Check if this child is a SplineContainer
                var splineContainer = child.GetComponent<SplineContainer>();
                if (splineContainer != null)
                {
                    // This is an individual spline
                    directChildren.Add((child, true, new SplineHierarchyInfo
                    {
                        container = splineContainer,
                        parent = splineGroup,
                        siblingIndex = child.GetSiblingIndex()
                    }));
                }
                else
                {
                    // This might be a group - check if it contains any SplineContainers
                    var splinesInGroup = child.GetComponentsInChildren<SplineContainer>();
                    if (splinesInGroup.Length > 0)
                    {
                        // This is a group transform
                        directChildren.Add((child, false, null));
                    }
                }
            }

            // Sort direct children by sibling index to maintain hierarchy order
            directChildren.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            // Build final ordered list maintaining exact hierarchy order
            foreach (var childInfo in directChildren)
            {
                if (childInfo.isSpline)
                {
                    // Add individual spline
                    var item = CreateSplineItemData(childInfo.splineInfo.container);
                    if (item != null)
                    {
                        splineItems.Add(item);
                    }
                }
                else
                {
                    // Add group
                    Transform groupTransform = childInfo.transform;
                    string hierarchyPath = GetHierarchyPath(groupTransform, splineGroup);

                    var groupItem = new GroupItemData
                    {
                        groupTransform = groupTransform,
                        hierarchyPath = hierarchyPath,
                        isFoldoutExpanded = GetSavedGroupFoldoutState(groupTransform)
                    };

                    // Get all splines in this group and sort them by sibling index
                    var splinesInGroup = groupTransform.GetComponentsInChildren<SplineContainer>();
                    var groupSplineInfos = new List<SplineHierarchyInfo>();

                    foreach (var spline in splinesInGroup)
                    {
                        if (spline != null && spline.gameObject.activeInHierarchy)
                        {
                            groupSplineInfos.Add(new SplineHierarchyInfo
                            {
                                container = spline,
                                parent = groupTransform,
                                siblingIndex = spline.transform.GetSiblingIndex()
                            });
                        }
                    }

                    // Sort splines within group by sibling index
                    groupSplineInfos.Sort((a, b) => a.siblingIndex.CompareTo(b.siblingIndex));

                    // Add splines to group
                    foreach (var splineInfo in groupSplineInfos)
                    {
                        var splineItem = CreateSplineItemData(splineInfo.container);
                        if (splineItem != null)
                        {
                            groupItem.splines.Add(splineItem);
                        }
                    }

                    splineItems.Add(groupItem);
                }
            }

            // Rebuild ListView with new data
            RebuildListView();

            // Update spline count display
            UpdateSplineCountLabel();

            // Mark that preview textures need updating after refresh
            if (markPreviewTexturesDirty)
            {
                previewTexturesNeedUpdate = true;
            }
        }

        SplineItemData CreateSplineItemData(SplineContainer container)
        {
            if (container == null)
            {
                Debug.LogWarning($"{LogPrefix} CreateSplineItemData called with null container.");
                return null;
            }

            var settingsComponent = GetOrCreateTerrainSplineSettings(container);
            var settings = GetOrCreateSplineStrokeSettings(settingsComponent);

            var data = new SplineItemData
            {
                container = container,
                settings = settings,
                isFoldoutExpanded = GetSavedSplineFoldoutState(container)
            };

            // Restore tab state if previously saved
            if (splineTabStates.TryGetValue(container, out var tabs))
            {
                data.isOverrideBrushTabActive = tabs.brush;
                data.isOverridePaintTabActive = tabs.paint;
                data.isOverrideDetailTabActive = tabs.detail;
                data.isOverrideTreeTabActive = tabs.tree;
            }

            return data;
        }

        TerrainSplineSettings GetOrCreateTerrainSplineSettings(SplineContainer container)
        {
            var settingsComponent = container.GetComponent<TerrainSplineSettings>();
            if (settingsComponent != null)
            {
                return settingsComponent;
            }

            if (container.gameObject == null)
            {
                Debug.LogWarning($"{LogPrefix} SplineContainer '{container.name}' has no associated GameObject.");
                return null;
            }

            RunWithSuppressedSplineChangeNotifications(() =>
            {
                settingsComponent = container.gameObject.AddComponent<TerrainSplineSettings>();
            });

            return settingsComponent;
        }

        SplineStrokeSettings GetOrCreateSplineStrokeSettings(TerrainSplineSettings settingsComponent)
        {
            if (settingsComponent == null)
            {
                return new SplineStrokeSettings();
            }

            if (settingsComponent.settings != null)
            {
                return settingsComponent.settings;
            }

            var settings = new SplineStrokeSettings();
            RunWithSuppressedSplineChangeNotifications(() =>
            {
                settingsComponent.settings = settings;
            });

            return settings;
        }

        void SaveTabStates()
        {
            splineTabStates.Clear();

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem && splineItem.container != null)
                {
                    splineTabStates[splineItem.container] = (splineItem.isOverrideBrushTabActive, splineItem.isOverridePaintTabActive, splineItem.isOverrideDetailTabActive, splineItem.isOverrideTreeTabActive);
                }
                else if (item is GroupItemData groupItem)
                {
                    foreach (var spline in groupItem.splines)
                    {
                        if (spline.container != null)
                        {
                            splineTabStates[spline.container] = (spline.isOverrideBrushTabActive, spline.isOverridePaintTabActive, spline.isOverrideDetailTabActive, spline.isOverrideTreeTabActive);
                        }
                    }
                }
            }
        }

        // Helper method to get all spline items from the hierarchical structure
        List<SplineItemData> GetAllSplineItems()
        {
            var allSplines = new List<SplineItemData>();

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem)
                {
                    allSplines.Add(splineItem);
                }
                else if (item is GroupItemData groupItem)
                {
                    allSplines.AddRange(groupItem.splines);
                }
            }

            return allSplines;
        }

        // Helper method to get spline item by index (for backward compatibility)
        SplineItemData GetSplineItemByIndex(int index)
        {
            int currentIndex = 0;

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem)
                {
                    if (currentIndex == index) return splineItem;
                    currentIndex++;
                }
                else if (item is GroupItemData groupItem)
                {
                    if (index >= currentIndex && index < currentIndex + groupItem.splines.Count)
                    {
                        return groupItem.splines[index - currentIndex];
                    }

                    currentIndex += groupItem.splines.Count;
                }
            }

            return null;
        }

        // Helper method to get global index of a spline item
        int GetGlobalSplineIndex(SplineItemData targetSpline)
        {
            int currentIndex = 0;

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem)
                {
                    if (splineItem == targetSpline) return currentIndex;
                    currentIndex++;
                }
                else if (item is GroupItemData groupItem)
                {
                    for (int i = 0; i < groupItem.splines.Count; i++)
                    {
                        if (groupItem.splines[i] == targetSpline) return currentIndex;
                        currentIndex++;
                    }
                }
            }

            return -1; // Not found
        }

        // Helper method to find spline data by container reference
        SplineItemData FindSplineItemDataByContainer(SplineContainer container)
        {
            if (container == null) return null;

            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem)
                {
                    if (splineItem.container == container) return splineItem;
                }
                else if (item is GroupItemData groupItem)
                {
                    for (int i = 0; i < groupItem.splines.Count; i++)
                    {
                        if (groupItem.splines[i].container == container) return groupItem.splines[i];
                    }
                }
            }

            return null;
        }

        // Helper methods for foldout state persistence
        void SaveFoldoutStates()
        {
            // Clear existing saved states
            splineFoldoutStates.Clear();
            groupFoldoutStates.Clear();

            // Save current foldout states
            foreach (var item in splineItems)
            {
                if (item is SplineItemData splineItem && splineItem.container != null)
                {
                    EntityId instanceId = UnityObjectIdUtility.GetEntityId(splineItem.container);
                    splineFoldoutStates[instanceId] = splineItem.isFoldoutExpanded;
                }
                else if (item is GroupItemData groupItem && groupItem.groupTransform != null)
                {
                    EntityId instanceId = UnityObjectIdUtility.GetEntityId(groupItem.groupTransform);
                    groupFoldoutStates[instanceId] = groupItem.isFoldoutExpanded;
                }
            }
        }

        bool GetSavedSplineFoldoutState(SplineContainer container)
        {
            if (container == null) return true; // Default to expanded

            EntityId instanceId = UnityObjectIdUtility.GetEntityId(container);
            return splineFoldoutStates.TryGetValue(instanceId, out bool state) ? state : true;
        }

        bool GetSavedGroupFoldoutState(Transform groupTransform)
        {
            if (groupTransform == null) return true; // Default to expanded

            EntityId instanceId = UnityObjectIdUtility.GetEntityId(groupTransform);
            return groupFoldoutStates.TryGetValue(instanceId, out bool state) ? state : true;
        }

        void RebuildListView()
        {
            if (splinesListView == null)
            {
                return;
            }

            bool extendPreviewRefreshSuppression = suppressRefreshPreviewRequestCount > 0;
            if (extendPreviewRefreshSuppression)
            {
                ExtendSuppressedRefreshPreviewRequestsToNextUiTick();
            }

            try
            {
                // Clear any cached height calculations
                splinesListView.style.height = StyleKeyword.Auto;

                // IMPORTANT: Clear itemsSource first to reset ListView state
                splinesListView.itemsSource = null;

                // Force a rebuild of the ListView
                splinesListView.Rebuild();

                // Set the new data source
                splinesListView.itemsSource = splineItems;

                // Force a refresh
                splinesListView.RefreshItems();

                // Force geometry update after rebuild
                splinesListView.MarkDirtyRepaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error rebuilding ListView: {e.Message}\n{e.StackTrace}");
            }
        }

        Button CreateLayerButton(TerrainLayer layer, int index, int selectedIndex)
        {
            var layerButton = new Button();
            layerButton.AddToClassList("layer-button");
            layerButton.AddToClassList(index == selectedIndex ? "layer-button-selected" : "layer-button-unselected");
            layerButton.tooltip = $"Click to select terrain layer '{layer.name}' for painting this spline";

            var thumbnail = new Image();
            thumbnail.AddToClassList("layer-thumbnail");

            if (layer.diffuseTexture != null)
            {
                thumbnail.image = layer.diffuseTexture;
            }
            else
            {
                // Create placeholder
                var placeholder = new Texture2D(32, 32);
                var colors = new Color32[32 * 32];
                for (int j = 0; j < colors.Length; j++)
                    colors[j] = new Color32(128, 128, 128, 255);
                placeholder.SetPixels32(colors);
                placeholder.Apply();
                thumbnail.image = placeholder;
            }

            layerButton.Add(thumbnail);

            var layerName = new Label(layer.name);
            layerName.AddToClassList("layer-name");
            layerButton.Add(layerName);

            return layerButton;
        }
    }
}
