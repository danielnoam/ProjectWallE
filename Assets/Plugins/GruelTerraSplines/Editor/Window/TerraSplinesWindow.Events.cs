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
        void BindEvents()
        {
            // Bind all button click events and value change callbacks

            // Bind foldout state
            if (targetsFoldout != null)
            {
                targetsFoldout.RegisterValueChangedCallback(evt =>
                {
                    foldoutTargets = evt.newValue;
                    EditorUtility.SetDirty(this);
                });
            }

            if (terrainSplinesGroupsFoldout != null)
            {
                terrainSplinesGroupsFoldout.RegisterValueChangedCallback(evt =>
                {
                    foldoutTargetGroups = evt.newValue;
                    EditorUtility.SetDirty(this);
                });
            }

            // Bind debug foldout state
            var debugFoldout = root.Q<Foldout>("debug-foldout");
            if (debugFoldout != null)
            {
                debugFoldout.RegisterValueChangedCallback(evt =>
                {
                    foldoutDebug = evt.newValue;
                    EditorUtility.SetDirty(this);
                });
            }

            // Target controls
            if (terrainField != null)
            {
                terrainField.objectType = typeof(GameObject);
                terrainField.RegisterValueChangedCallback(evt =>
                {
                    GameObject newGroup = evt.newValue as GameObject;
                    
                    // Legacy: Handle Terrain component directly (backward compatibility)
                    if (newGroup == null && evt.newValue is Terrain terrain && terrain != null)
                    {
                        // Auto-migrate: create or find GameObject parent
                        if (terrain.transform != null && terrain.transform.parent != null)
                        {
                            newGroup = terrain.transform.parent.gameObject;
                        }
                        else if (terrain.transform != null)
                        {
                            var groupGO = new GameObject("Terrain Group");
                            terrain.transform.SetParent(groupGO.transform);
                            newGroup = groupGO;
                        }
                    }

                        ApplyTargetReferences(newGroup, splineGroup);
                });
            }

            if (splineGroupField != null)
            {
                splineGroupField.RegisterValueChangedCallback(evt =>
                {
                    Transform newSplineGroup = null;

                    // Handle both GameObject and Transform assignments
                    if (evt.newValue is GameObject gameObject)
                    {
                        newSplineGroup = gameObject.transform;
                    }
                    else if (evt.newValue is Transform transform)
                    {
                        newSplineGroup = transform;
                    }
                    else if (evt.newValue == null)
                    {
                        newSplineGroup = null;
                    }
                    else
                    {
                        Debug.LogWarning($"Unexpected type assigned to spline group field: {evt.newValue.GetType()}");
                    }

                    // Check if the new value is valid
                    if (newSplineGroup != null && newSplineGroup.gameObject == null)
                    {
                        splineGroupField.SetValueWithoutNotify(null);
                        newSplineGroup = null;
                    }

                    ApplyTargetReferences(targetTerrainGroup, newSplineGroup);
                });
            }

            // Leveling controls
            if (levelingValueSlider != null)
            {
                levelingValueSlider.RegisterValueChangedCallback(evt =>
                {
                    levelingValue = evt.newValue;
                    if (levelingValueField != null)
                        levelingValueField.SetValueWithoutNotify(evt.newValue);
                });
            }

            if (levelingValueField != null)
            {
                levelingValueField.RegisterValueChangedCallback(evt =>
                {
                    levelingValue = evt.newValue;
                    if (levelingValueSlider != null)
                        levelingValueSlider.SetValueWithoutNotify(evt.newValue);
                });
            }

            if (offsetButton != null)
            {
                offsetButton.clicked += OnOffsetButtonClicked;
            }

            if (levelButton != null)
            {
                levelButton.clicked += OnLevelButtonClicked;
            }

            if (fillHolesButton != null)
            {
                fillHolesButton.clicked += OnFillHolesButtonClicked;
            }

            if (clearPaintButton != null)
            {
                clearPaintButton.clicked += OnClearPaintButtonClicked;
            }

            if (clearDetailButton != null)
            {
                clearDetailButton.clicked += OnClearDetailButtonClicked;
            }

            if (clearTreesButton != null)
            {
                clearTreesButton.clicked += OnClearTreesButtonClicked;
            }

            // Global controls
            if (pathModeButton != null)
            {
                pathModeButton.clicked += () =>
                {
                    globalMode = SplineApplyMode.Path;
                    SetModeButtonSelected(pathModeButton, true);
                    SetModeButtonSelected(shapeModeButton, false);
                    UpdateGlobalIcons();
                    RefreshPreview();
                };
            }

            if (shapeModeButton != null)
            {
                shapeModeButton.clicked += () =>
                {
                    globalMode = SplineApplyMode.Shape;
                    SetModeButtonSelected(pathModeButton, false);
                    SetModeButtonSelected(shapeModeButton, true);
                    UpdateGlobalIcons();
                    RefreshPreview();
                };
            }

            // Global tab controls
            if (globalModeTab != null)
            {
                globalModeTab.clicked += () => OnGlobalTabClicked(0);
            }

            if (globalBrushTab != null)
            {
                globalBrushTab.clicked += () => OnGlobalTabClicked(1);
            }

            if (globalPaintTab != null)
            {
                globalPaintTab.clicked += () => OnGlobalTabClicked(2);
            }

            if (globalDetailTab != null)
            {
                globalDetailTab.clicked += () => OnGlobalTabClicked(3);
            }

            if (globalTreeTab != null)
            {
                globalTreeTab.clicked += () => OnGlobalTabClicked(4);
            }

            if (creditsContainer != null)
            {
                creditsContainer.RegisterCallback<ClickEvent>(_ =>
                {
                    Application.OpenURL(DiscordInviteUrl);
                });
            }

            if (globalBrushSizeSlider != null)
            {
                globalBrushSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                    globalBrushSize = clampedValue;
                    if (globalBrushSizeField != null)
                        globalBrushSizeField.SetValueWithoutNotify(clampedValue);
                    RefreshPreview();
                });
            }

            if (globalBrushSizeField != null)
            {
                globalBrushSizeField.RegisterValueChangedCallback(evt =>
                {
                    float clampedValue = Mathf.Clamp(evt.newValue, 1f, 200f);
                    globalBrushSize = clampedValue;
                    if (globalBrushSizeSlider != null)
                        globalBrushSizeSlider.SetValueWithoutNotify(clampedValue);
                    if (globalBrushSizeField != null)
                        globalBrushSizeField.SetValueWithoutNotify(clampedValue);
                    RefreshPreview();
                });
            }

            // Brush hardness controls
            if (globalBrushHardnessSlider != null)
            {
                globalBrushHardnessSlider.RegisterValueChangedCallback(evt =>
                {
                    globalBrushHardness = evt.newValue;
                    if (globalBrushHardnessField != null)
                        globalBrushHardnessField.SetValueWithoutNotify(evt.newValue);
                    UpdateBrushPreview();
                    RefreshPreview();
                });
            }

            if (globalBrushHardnessField != null)
            {
                globalBrushHardnessField.RegisterValueChangedCallback(evt =>
                {
                    globalBrushHardness = evt.newValue;
                    if (globalBrushHardnessSlider != null)
                        globalBrushHardnessSlider.SetValueWithoutNotify(evt.newValue);
                    UpdateBrushPreview();
                    RefreshPreview();
                });
            }

            if (globalStrengthSlider != null)
            {
                globalStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    globalStrength = evt.newValue;
                    if (globalStrengthField != null)
                        globalStrengthField.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            }

            if (globalStrengthField != null)
            {
                globalStrengthField.RegisterValueChangedCallback(evt =>
                {
                    globalStrength = evt.newValue;
                    if (globalStrengthSlider != null)
                        globalStrengthSlider.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            }

            if (globalSampleStepSlider != null)
            {
                globalSampleStepSlider.RegisterValueChangedCallback(evt =>
                {
                    globalSampleStep = evt.newValue;
                    if (globalSampleStepField != null)
                        globalSampleStepField.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            }

            if (globalSampleStepField != null)
            {
                globalSampleStepField.RegisterValueChangedCallback(evt =>
                {
                    globalSampleStep = evt.newValue;
                    if (globalSampleStepSlider != null)
                        globalSampleStepSlider.SetValueWithoutNotify(evt.newValue);
                    RefreshPreview();
                });
            }

            // Global brush noise settings
            if (globalBrushNoiseTextureField != null)
            {
                globalBrushNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseTexture = evt.newValue as Texture2D;
                    SetNoiseHeaderIconTint(globalBrushNoiseFoldout, globalBrushNoise.noiseTexture != null);
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseStrengthSlider != null)
            {
                globalBrushNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (globalBrushNoiseStrengthField != null)
                        globalBrushNoiseStrengthField.value = globalBrushNoise.noiseStrength;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseStrengthField != null)
            {
                globalBrushNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (globalBrushNoiseStrengthSlider != null)
                        globalBrushNoiseStrengthSlider.value = globalBrushNoise.noiseStrength;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseEdgeSlider != null)
            {
                globalBrushNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (globalBrushNoiseEdgeField != null)
                        globalBrushNoiseEdgeField.value = globalBrushNoise.noiseEdge;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseEdgeField != null)
            {
                globalBrushNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (globalBrushNoiseEdgeSlider != null)
                        globalBrushNoiseEdgeSlider.value = globalBrushNoise.noiseEdge;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseSizeSlider != null)
            {
                globalBrushNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (globalBrushNoiseSizeField != null)
                        globalBrushNoiseSizeField.value = globalBrushNoise.noiseWorldSizeMeters;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseSizeField != null)
            {
                globalBrushNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (globalBrushNoiseSizeSlider != null)
                        globalBrushNoiseSizeSlider.value = globalBrushNoise.noiseWorldSizeMeters;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseOffsetField != null)
            {
                globalBrushNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseOffset = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseResponseCurve != null)
            {
                globalBrushNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve();
                    RefreshPreview();
                });
            }

            if (globalBrushNoiseInvertToggle != null)
            {
                globalBrushNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (globalBrushNoise == null)
                        globalBrushNoise = new BrushNoiseSettings();
                    globalBrushNoise.noiseInvert = evt.newValue;
                    UpdateNoiseInvertToggleVisual(globalBrushNoiseInvertToggle);
                    RefreshPreview();
                });
            }

            // Global paint settings
            if (globalPaintStrengthSlider != null)
            {
                globalPaintStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    globalPaintStrength = evt.newValue;
                    if (globalPaintStrengthField != null)
                        globalPaintStrengthField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintStrengthField != null)
            {
                globalPaintStrengthField.RegisterValueChangedCallback(evt =>
                {
                    globalPaintStrength = evt.newValue;
                    if (globalPaintStrengthSlider != null)
                        globalPaintStrengthSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintBlendSlider != null)
            {
                globalPaintBlendSlider.RegisterValueChangedCallback(evt =>
                {
                    globalPaintBlend = evt.newValue;
                    if (globalPaintBlendField != null)
                        globalPaintBlendField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintBlendField != null)
            {
                globalPaintBlendField.RegisterValueChangedCallback(evt =>
                {
                    globalPaintBlend = evt.newValue;
                    if (globalPaintBlendSlider != null)
                        globalPaintBlendSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintThresholdSlider != null)
            {
                globalPaintThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    globalPaintThreshold = evt.newValue;
                    if (globalPaintThresholdField != null)
                        globalPaintThresholdField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintThresholdField != null)
            {
                globalPaintThresholdField.RegisterValueChangedCallback(evt =>
                {
                    globalPaintThreshold = evt.newValue;
                    if (globalPaintThresholdSlider != null)
                        globalPaintThresholdSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseTextureField != null)
            {
                globalPaintNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseTexture = evt.newValue as Texture2D;
                    SetNoiseHeaderIconTint(globalPaintNoiseFoldout, entry.noiseTexture != null);
                    PopulateLayerPalette();
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseStrengthSlider != null)
            {
                globalPaintNoiseStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (globalPaintNoiseStrengthField != null)
                        globalPaintNoiseStrengthField.value = entry.noiseStrength;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseStrengthField != null)
            {
                globalPaintNoiseStrengthField.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseStrength = Mathf.Clamp01(evt.newValue);
                    if (globalPaintNoiseStrengthSlider != null)
                        globalPaintNoiseStrengthSlider.value = entry.noiseStrength;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseEdgeSlider != null)
            {
                globalPaintNoiseEdgeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (globalPaintNoiseEdgeField != null)
                        globalPaintNoiseEdgeField.value = entry.noiseEdge;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseEdgeField != null)
            {
                globalPaintNoiseEdgeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseEdge = Mathf.Clamp(evt.newValue, -1f, 1f);
                    if (globalPaintNoiseEdgeSlider != null)
                        globalPaintNoiseEdgeSlider.value = entry.noiseEdge;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseSizeSlider != null)
            {
                globalPaintNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (globalPaintNoiseSizeField != null)
                        globalPaintNoiseSizeField.value = entry.noiseWorldSizeMeters;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseSizeField != null)
            {
                globalPaintNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseWorldSizeMeters = Mathf.Max(0.001f, evt.newValue);
                    if (globalPaintNoiseSizeSlider != null)
                        globalPaintNoiseSizeSlider.value = entry.noiseWorldSizeMeters;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseOffsetField != null)
            {
                globalPaintNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseOffset = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseResponseCurve != null)
            {
                globalPaintNoiseResponseCurve.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseResponse = evt.newValue != null ? evt.newValue.CloneCurve() : SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve();
                    RefreshPreview();
                });
            }

            if (globalPaintNoiseInvertToggle != null)
            {
                globalPaintNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (globalPaintNoiseLayers == null)
                        globalPaintNoiseLayers = new List<PaintNoiseLayerSettings>();
                    var entry = GetOrCreatePaintNoiseLayer(globalPaintNoiseLayers, globalSelectedLayerIndex);
                    entry.noiseInvert = evt.newValue;
                    UpdateNoiseInvertToggleVisual(globalPaintNoiseInvertToggle);
                    RefreshPreview();
                });
            }

            // Global detail settings
            if (globalDetailStrengthSlider != null)
            {
                globalDetailStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    globalDetailStrength = evt.newValue;
                    if (globalDetailStrengthField != null)
                        globalDetailStrengthField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailStrengthField != null)
            {
                globalDetailStrengthField.RegisterValueChangedCallback(evt =>
                {
                    globalDetailStrength = evt.newValue;
                    if (globalDetailStrengthSlider != null)
                        globalDetailStrengthSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailSlopeLimitSlider != null)
            {
                globalDetailSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    globalDetailSlopeLimitDegrees = evt.newValue;
                    if (globalDetailSlopeLimitField != null)
                        globalDetailSlopeLimitField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailSlopeLimitField != null)
            {
                globalDetailSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    globalDetailSlopeLimitDegrees = evt.newValue;
                    if (globalDetailSlopeLimitSlider != null)
                        globalDetailSlopeLimitSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailFalloffPowerSlider != null)
            {
                globalDetailFalloffPowerSlider.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    globalDetailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (globalDetailFalloffPowerField != null)
                        globalDetailFalloffPowerField.SetValueWithoutNotify(normalized);
                    RefreshPreview();
                });
            }

            if (globalDetailFalloffPowerField != null)
            {
                globalDetailFalloffPowerField.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    globalDetailFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (globalDetailFalloffPowerSlider != null)
                        globalDetailFalloffPowerSlider.SetValueWithoutNotify(normalized);
                    RefreshPreview();
                });
            }

            if (globalDetailSpreadRadiusSlider != null)
            {
                globalDetailSpreadRadiusSlider.RegisterValueChangedCallback(evt =>
                {
                    globalDetailSpreadRadius = evt.newValue;
                    if (globalDetailSpreadRadiusField != null)
                        globalDetailSpreadRadiusField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailSpreadRadiusField != null)
            {
                globalDetailSpreadRadiusField.RegisterValueChangedCallback(evt =>
                {
                    globalDetailSpreadRadius = evt.newValue;
                    if (globalDetailSpreadRadiusSlider != null)
                        globalDetailSpreadRadiusSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailRemoveThresholdSlider != null)
            {
                globalDetailRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    globalDetailRemoveThreshold = evt.newValue;
                    if (globalDetailRemoveThresholdField != null)
                        globalDetailRemoveThresholdField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailRemoveThresholdField != null)
            {
                globalDetailRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    globalDetailRemoveThreshold = evt.newValue;
                    if (globalDetailRemoveThresholdSlider != null)
                        globalDetailRemoveThresholdSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseTextureField != null)
            {
                globalDetailNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    bool hasNoiseTexture = false;
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseTexture = evt.newValue as Texture2D;
                        if (entry.noiseTexture != null)
                            hasNoiseTexture = true;
                    }
                    SetNoiseHeaderIconTint(globalDetailNoiseFoldout, hasNoiseTexture);
                    PopulateDetailLayerPalette();
                    RefreshPreview();
                });
            }


            if (globalDetailNoiseSizeSlider != null)
            {
                globalDetailNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (globalDetailNoiseSizeField != null)
                        globalDetailNoiseSizeField.value = noiseSize;
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseSizeField != null)
            {
                globalDetailNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (globalDetailNoiseSizeSlider != null)
                        globalDetailNoiseSizeSlider.value = noiseSize;
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseOffsetField != null)
            {
                globalDetailNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseOffset = evt.newValue;
                    }
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseThresholdSlider != null)
            {
                globalDetailNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (globalDetailNoiseThresholdField != null)
                        globalDetailNoiseThresholdField.value = noiseThreshold;
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseThresholdField != null)
            {
                globalDetailNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (globalDetailNoiseThresholdSlider != null)
                        globalDetailNoiseThresholdSlider.value = noiseThreshold;
                    RefreshPreview();
                });
            }

            if (globalDetailNoiseInvertToggle != null)
            {
                globalDetailNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (globalDetailNoiseLayers == null)
                        globalDetailNoiseLayers = new List<DetailNoiseLayerSettings>();
                    var selectedLayerIndices = GetGlobalSelectedDetailLayerIndices();
                    foreach (var layerIndex in selectedLayerIndices)
                    {
                        var entry = GetOrCreateDetailNoiseLayer(globalDetailNoiseLayers, layerIndex);
                        entry.noiseInvert = evt.newValue;
                    }
                    UpdateNoiseInvertToggleVisual(globalDetailNoiseInvertToggle);
                    RefreshPreview();
                });
            }

            // Global tree settings
            if (globalTreeStrengthSlider != null)
            {
                globalTreeStrengthSlider.RegisterValueChangedCallback(evt =>
                {
                    globalTreeStrength = evt.newValue;
                    if (globalTreeStrengthField != null)
                        globalTreeStrengthField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeStrengthField != null)
            {
                globalTreeStrengthField.RegisterValueChangedCallback(evt =>
                {
                    globalTreeStrength = evt.newValue;
                    if (globalTreeStrengthSlider != null)
                        globalTreeStrengthSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeSlopeLimitSlider != null)
            {
                globalTreeSlopeLimitSlider.RegisterValueChangedCallback(evt =>
                {
                    globalTreeSlopeLimitDegrees = evt.newValue;
                    if (globalTreeSlopeLimitField != null)
                        globalTreeSlopeLimitField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeSlopeLimitField != null)
            {
                globalTreeSlopeLimitField.RegisterValueChangedCallback(evt =>
                {
                    globalTreeSlopeLimitDegrees = evt.newValue;
                    if (globalTreeSlopeLimitSlider != null)
                        globalTreeSlopeLimitSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeFalloffPowerSlider != null)
            {
                globalTreeFalloffPowerSlider.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    globalTreeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (globalTreeFalloffPowerField != null)
                        globalTreeFalloffPowerField.SetValueWithoutNotify(normalized);
                    RefreshPreview();
                });
            }

            if (globalTreeFalloffPowerField != null)
            {
                globalTreeFalloffPowerField.RegisterValueChangedCallback(evt =>
                {
                    float normalized = Mathf.Clamp01(evt.newValue);
                    globalTreeFalloffPower = DetailFalloffMapping.ToPower(normalized);
                    if (globalTreeFalloffPowerSlider != null)
                        globalTreeFalloffPowerSlider.SetValueWithoutNotify(normalized);
                    RefreshPreview();
                });
            }

            if (globalTreeHeightMultiplierCurve != null)
            {
                globalTreeHeightMultiplierCurve.RegisterValueChangedCallback(evt =>
                {
                    globalTreeHeightMultiplier = evt.newValue?.CloneCurve() ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve();
                    RefreshPreview();
                });
            }

            if (globalTreeRemoveThresholdSlider != null)
            {
                globalTreeRemoveThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    globalTreeRemoveThreshold = evt.newValue;
                    if (globalTreeRemoveThresholdField != null)
                        globalTreeRemoveThresholdField.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeRemoveThresholdField != null)
            {
                globalTreeRemoveThresholdField.RegisterValueChangedCallback(evt =>
                {
                    globalTreeRemoveThreshold = evt.newValue;
                    if (globalTreeRemoveThresholdSlider != null)
                        globalTreeRemoveThresholdSlider.value = evt.newValue;
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseTextureField != null)
            {
                globalTreeNoiseTextureField.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    bool hasNoiseTexture = false;
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseTexture = evt.newValue as Texture2D;
                        if (entry.noiseTexture != null)
                            hasNoiseTexture = true;
                    }
                    SetNoiseHeaderIconTint(globalTreeNoiseFoldout, hasNoiseTexture);
                    PopulateTreePrototypePalette();
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseSizeSlider != null)
            {
                globalTreeNoiseSizeSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (globalTreeNoiseSizeField != null)
                        globalTreeNoiseSizeField.value = noiseSize;
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseSizeField != null)
            {
                globalTreeNoiseSizeField.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    float noiseSize = Mathf.Max(0.001f, evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseWorldSizeMeters = noiseSize;
                    }
                    if (globalTreeNoiseSizeSlider != null)
                        globalTreeNoiseSizeSlider.value = noiseSize;
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseOffsetField != null)
            {
                globalTreeNoiseOffsetField.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseOffset = evt.newValue;
                    }
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseThresholdSlider != null)
            {
                globalTreeNoiseThresholdSlider.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (globalTreeNoiseThresholdField != null)
                        globalTreeNoiseThresholdField.value = noiseThreshold;
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseThresholdField != null)
            {
                globalTreeNoiseThresholdField.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    float noiseThreshold = Mathf.Clamp01(evt.newValue);
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseThreshold = noiseThreshold;
                    }
                    if (globalTreeNoiseThresholdSlider != null)
                        globalTreeNoiseThresholdSlider.value = noiseThreshold;
                    RefreshPreview();
                });
            }

            if (globalTreeNoiseInvertToggle != null)
            {
                globalTreeNoiseInvertToggle.RegisterValueChangedCallback(evt =>
                {
                    if (globalTreeNoiseLayers == null)
                        globalTreeNoiseLayers = new List<TreeNoisePrototypeSettings>();
                    var selectedPrototypeIndices = GetGlobalSelectedTreePrototypeIndices();
                    foreach (var prototypeIndex in selectedPrototypeIndices)
                    {
                        var entry = GetOrCreateTreeNoiseLayer(globalTreeNoiseLayers, prototypeIndex);
                        entry.noiseInvert = evt.newValue;
                    }
                    UpdateNoiseInvertToggleVisual(globalTreeNoiseInvertToggle);
                    RefreshPreview();
                });
            }

            // Action buttons
            if (undoButton != null)
            {
                undoButton.clicked += OnUndoButtonClicked;
            }

            if (applyButton != null)
            {
                applyButton.clicked += OnApplyButtonClicked;
            }

            if (paintAllButton != null)
            {
                paintAllButton.clicked += OnPaintAllButtonClicked;
            }

            if (redoButton != null)
            {
                redoButton.clicked += OnRedoButtonClicked;
            }

            // Debug controls
            if (updateIntervalSlider != null)
            {
                updateIntervalSlider.RegisterValueChangedCallback(evt =>
                {
                    updateInterval = evt.newValue;
                    if (updateIntervalField != null)
                        updateIntervalField.SetValueWithoutNotify(evt.newValue);
                    SaveEditorSettings();
                });
            }

            if (updateIntervalField != null)
            {
                updateIntervalField.RegisterValueChangedCallback(evt =>
                {
                    updateInterval = evt.newValue;
                    if (updateIntervalSlider != null)
                        updateIntervalSlider.SetValueWithoutNotify(evt.newValue);
                    SaveEditorSettings();
                });
            }

            if (debugDocsButton != null)
            {
                debugDocsButton.clicked += () => Application.OpenURL(DocumentationUrl);
            }

            if (pauseResumeButton != null)
            {
                pauseResumeButton.clicked += () =>
                {
                    TogglePauseResume();
                    UpdateUIState();
                };
            }

            if (updatePreviewOnceButton != null)
            {
                updatePreviewOnceButton.clicked += () =>
                {
                    // Only meaningful when paused; UI ensures enablement
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    manualUpdateRequested = true;

                    // Schedule a callback to log the completion time
                    EditorApplication.delayCall += () => { stopwatch.Stop(); };
                };
            }

            if (rebuildPreviewButton != null)
            {
                rebuildPreviewButton.clicked += OnRebuildPreviewButtonClicked;
            }

            if (clearCacheButton != null)
            {
                clearCacheButton.clicked += OnClearCacheButtonClicked;
            }

            if (cpuFallbackButton != null)
            {
                cpuFallbackButton.clicked += OnCpuFallbackButtonClicked;
            }

            if (gpuPerformantButton != null)
            {
                gpuPerformantButton.clicked += OnGpuPerformantButtonClicked;
            }

            if (heightmapPreviewToggle != null)
            {
                heightmapPreviewToggle.RegisterValueChangedCallback(evt =>
                {
                    heightmapPreviewEnabled = evt.newValue;
                    SaveEditorSettings();
                    // Update preview texture visibility
                    UpdatePreviewTextureVisibility();
                });
            }

            if (supportOutsideChangesToggle != null)
            {
                supportOutsideChangesToggle.RegisterValueChangedCallback(evt =>
                {
                    supportOutsideChanges = evt.newValue;

                    // Swap strategy based on toggle value
                    baselineUpdateStrategy = supportOutsideChanges
                        ? new DynamicBaselineUpdateStrategy()
                        : new StaticBaselineUpdateStrategy();

                    if (terrainStateManager != null)
                    {
                        terrainStateManager.SetBaselineUpdateStrategy(baselineUpdateStrategy);
                    }

                    if (supportOutsideChanges)
                    {
                        MarkPipelineDirty(baseline: true, preview: true, apply: true, heightRange: true);
                    }

                    SaveEditorSettings();
                });
            }

            if (featureHoleToggle != null)
            {
                featureHoleToggle.RegisterValueChangedCallback(evt =>
                {
                    featureHoleEnabled = evt.newValue;
                    SaveEditorSettings();
                    UpdateGlobalOperationWarning();
                    UpdateGlobalIcons();
                    TerrainSplineSettingsOverlay.NotifyFeatureFlagsChanged();
                    UpdateVisibleSplineRowFeatureStates();
                });
            }

            if (featurePaintToggle != null)
            {
                featurePaintToggle.RegisterValueChangedCallback(evt =>
                {
                    featurePaintEnabled = evt.newValue;
                    SaveEditorSettings();
                    UpdateGlobalOperationWarning();
                    UpdateGlobalIcons();
                    TerrainSplineSettingsOverlay.NotifyFeatureFlagsChanged();
                    UpdateVisibleSplineRowFeatureStates();
                });
            }

            if (featureDetailToggle != null)
            {
                featureDetailToggle.RegisterValueChangedCallback(evt =>
                {
                    featureDetailEnabled = evt.newValue;
                    SaveEditorSettings();
                    UpdateGlobalOperationWarning();
                    UpdateGlobalIcons();
                    TerrainSplineSettingsOverlay.NotifyFeatureFlagsChanged();
                    UpdateVisibleSplineRowFeatureStates();
                });
            }

            if (featureTreeToggle != null)
            {
                featureTreeToggle.RegisterValueChangedCallback(evt =>
                {
                    featureTreeEnabled = evt.newValue;
                    SaveEditorSettings();
                    UpdateGlobalOperationWarning();
                    UpdateGlobalIcons();
                    TerrainSplineSettingsOverlay.NotifyFeatureFlagsChanged();
                    UpdateVisibleSplineRowFeatureStates();
                });
            }

            if (benchmarkCacheButton != null)
            {
                benchmarkCacheButton.clicked += OnBenchmarkCacheButtonClicked;
            }

            if (featureGizmosToggle != null)
            {
                featureGizmosToggle.RegisterValueChangedCallback(evt =>
                {
                    featureGizmosEnabled = evt.newValue;
                    SaveEditorSettings();
                    TerrainSplineSettingsEditor.SetGizmosFeatureEnabled(evt.newValue);
                    SceneView.RepaintAll();
                });
            }

            if (setupGroupButton != null)
            {
                setupGroupButton.clicked += OnSetupGroupClicked;
            }

            if (findRefsButton != null)
            {
                findRefsButton.clicked += OnFindRefsClicked;
            }

            // Expand/Collapse buttons
            if (collapseAllButton != null)
            {
                collapseAllButton.clicked += CollapseAllSplines;

                // Load and assign collapse icon
                var collapseIcon = collapseAllButton.Q<Image>("collapse-all-icon");
                if (collapseIcon != null)
                {
                    var collapseTexture = Resources.Load<Texture2D>("Icons/colapse");
                    if (collapseTexture != null)
                        collapseIcon.image = collapseTexture;
                }
            }

            if (expandAllButton != null)
            {
                expandAllButton.clicked += ExpandAllSplines;

                // Load and assign expand icon
                var expandIcon = expandAllButton.Q<Image>("expand-all-icon");
                if (expandIcon != null)
                {
                    var expandTexture = Resources.Load<Texture2D>("Icons/expand");
                    if (expandTexture != null)
                        expandIcon.image = expandTexture;
                }
            }

            // Update UI state
            UpdateUIState();
        }

        void OnOffsetButtonClicked()
        {
            // Validate references first
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot apply offset: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            // Apply offset to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.BaselineHeights == null) continue;

                // Register undo operation with Unity
                Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Offset Terrain");

                // Apply offset to baseline heights directly
                TerraSplinesTool.ApplyOffsetToHeights(terrain, state.BaselineHeights, state.BaselineHeights, levelingValue);
                state.BaselineTexture = TerraSplinesTool.HeightsToTexture(terrain, state.BaselineHeights);
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHeights = firstState.BaselineHeights;
            }

            // Update combined baseline texture
            UpdateCombinedBaselineTexture();

            // Rebuild preview to show the updated baseline
            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;

            suppressBaselineUpdate = false;

            // Update UI state to enable undo button
            UpdateUIState();
        }

        void OnLevelButtonClicked()
        {
            // Validate references first
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot apply level: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            // Apply level to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.BaselineHeights == null) continue;

                // Register undo operation with Unity
                Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Level Terrain");

                // Apply level to baseline heights directly
                TerraSplinesTool.ApplyLevelToHeights(terrain, state.BaselineHeights, state.BaselineHeights, levelingValue);
                state.BaselineTexture = TerraSplinesTool.HeightsToTexture(terrain, state.BaselineHeights);
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHeights = firstState.BaselineHeights;
            }

            // Update combined baseline texture
            UpdateCombinedBaselineTexture();

            // Rebuild preview to show the updated baseline
            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;

            suppressBaselineUpdate = false;

            // Update UI state to enable undo button
            UpdateUIState();
        }

        void OnFillHolesButtonClicked()
        {
            // Validate references first
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot fill holes: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            // Fill holes for all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.BaselineHoles == null) continue;

                // Fill all holes in baseline holes array
                TerraSplinesTool.FillAllHoles(terrain, state.BaselineHoles);

                // Copy to working holes
                if (state.WorkingHoles != null)
                {
                    state.WorkingHoles = TerraSplinesTool.CopyHoles(state.BaselineHoles);
                }

                // Apply holes to terrain
                var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, state.BaselineHoles);
                terrain.terrainData.SetHoles(0, 0, terrainHoles);
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHoles = firstState.BaselineHoles;
                workingHoles = firstState.WorkingHoles;
            }

            // Rebuild preview to show the updated baseline
            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;

            suppressBaselineUpdate = false;

            // Update UI state to enable undo button
            UpdateUIState();
        }

        void OnClearPaintButtonClicked()
        {
            // Validate references first
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot clear paint: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null) continue;

                var td = terrain.terrainData;
                if (td == null) continue;

                Undo.RegisterCompleteObjectUndo(td, "Clear Paint Data");

                // Reset alphamaps to the first layer
                if (td.alphamapLayers > 0)
                {
                    var cleared = new float[td.alphamapHeight, td.alphamapWidth, td.alphamapLayers];
                    for (int z = 0; z < td.alphamapHeight; z++)
                    {
                        for (int x = 0; x < td.alphamapWidth; x++)
                        {
                            cleared[z, x, 0] = 1f;
                        }
                    }

                    td.SetAlphamaps(0, 0, cleared);
                    state.BaselineAlphamaps = TerraSplinesTool.CopyAlphamaps(cleared);
                    state.WorkingAlphamaps = TerraSplinesTool.CopyAlphamaps(cleared);
                }
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineAlphamaps = firstState.BaselineAlphamaps;
                workingAlphamaps = firstState.WorkingAlphamaps;
            }

            RebuildPreview(applyToTerrain: true);
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnClearDetailButtonClicked()
        {
            // Validate references first
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot clear detail: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null) continue;

                var td = terrain.terrainData;
                if (td == null) continue;

                if (td.detailPrototypes != null && td.detailPrototypes.Length > 0 && td.detailWidth > 0 && td.detailHeight > 0)
                {
                    Undo.RegisterCompleteObjectUndo(td, "Clear Detail Data");

                    int layerCount = td.detailPrototypes.Length;
                    var clearedDetails = new int[layerCount][,];
                    for (int layer = 0; layer < layerCount; layer++)
                    {
                        clearedDetails[layer] = new int[td.detailHeight, td.detailWidth];
                    }

                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, clearedDetails);
                    state.BaselineDetailLayers = TerraSplinesTool.CopyDetailLayers(clearedDetails);
                    state.WorkingDetailLayers = TerraSplinesTool.CopyDetailLayers(clearedDetails);
                }
            }

            RebuildPreview(applyToTerrain: true);
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnClearTreesButtonClicked()
        {
            ValidateAndResetReferences();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot clear trees: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            suppressBaselineUpdate = true;
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null) continue;

                var td = terrain.terrainData;
                if (td == null) continue;

                Undo.RegisterCompleteObjectUndo(td, "Clear Tree Data");

                var clearedTrees = System.Array.Empty<TreeInstance>();
                TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, clearedTrees);
                state.BaselineTreeInstances = TerraSplinesTool.CopyTreeInstances(clearedTrees);
                state.WorkingTreeInstances = TerraSplinesTool.CopyTreeInstances(clearedTrees);
            }

            RebuildPreview(applyToTerrain: true);
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnUndoButtonClicked()
        {
            ResumeAutoPipeline();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0 || !terrainStateManager.CanUndo) return;

            suppressBaselineUpdate = true;

            // Perform undo for all terrains via TerrainStateManager
            terrainStateManager.PerformUndo();
            
            // Also handle window-level undo for backward compatibility
            var redoSnapshot = CaptureSnapshot();
            if (redoSnapshot != null)
            {
                redoStack.Push(redoSnapshot);
            }

            if (undoStack.Count > 0)
            {
                var snapshot = undoStack.Pop();
                RestoreSnapshot(snapshot);
            }

            // Apply baseline to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null && state.BaselineHeights != null)
                {
                    var td = terrain.terrainData;
                    td.SetHeights(0, 0, state.BaselineHeights);
                    
                    if (state.BaselineHoles != null)
                    {
                        var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, state.BaselineHoles);
                        td.SetHoles(0, 0, terrainHoles);
                    }
                    
                    if (state.BaselineAlphamaps != null)
                    {
                        td.SetAlphamaps(0, 0, state.BaselineAlphamaps);
                    }

                    if (state.BaselineDetailLayers != null)
                    {
                        TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.BaselineDetailLayers);
                    }

                    if (state.BaselineTreeInstances != null)
                    {
                        TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.BaselineTreeInstances);
                    }
                    
                    TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles, state.BaselineAlphamaps);
                }
            }

            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnApplyButtonClicked()
        {
            ResumeAutoPipeline();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;
            
            suppressBaselineUpdate = true;

            // Rebuild preview with ALL features enabled for final apply
            // This ensures Apply operation always includes everything regardless of toggles
            var savedHoleEnabled = featureHoleEnabled;
            var savedPaintEnabled = featurePaintEnabled;
            var savedDetailEnabled = featureDetailEnabled;
            var savedTreeEnabled = featureTreeEnabled;
            featureHoleEnabled = true;
            featurePaintEnabled = true;
            featureDetailEnabled = true;
            featureTreeEnabled = true;
            RebuildPreview(applyToTerrain: false);
            featureHoleEnabled = savedHoleEnabled;
            featurePaintEnabled = savedPaintEnabled;
            featureDetailEnabled = savedDetailEnabled;
            featureTreeEnabled = savedTreeEnabled;

            // Save current baseline to undo buffer before applying (for all terrains)
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot(); // Also save window-level snapshot for backward compatibility

            // Apply to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.WorkingHeights == null) continue;

                // Apply heights with undo support
                TerraSplinesTool.ApplyFinalToTerrainWithUndo(terrain, state.WorkingHeights, "Apply Spline Terrain");

                // Apply alphamaps if they exist
                if (state.WorkingAlphamaps != null)
                {
                    terrain.terrainData.SetAlphamaps(0, 0, state.WorkingAlphamaps);
                    state.BaselineAlphamaps = TerraSplinesTool.CopyAlphamaps(state.WorkingAlphamaps);
                }

                if (state.WorkingHoles != null)
                {
                    var terrainHolesForApply = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, state.WorkingHoles);
                    TerraSplinesTool.ApplyHolesToTerrain(terrain, terrainHolesForApply);
                }

                if (state.WorkingDetailLayers != null)
                {
                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.WorkingDetailLayers);
                    state.BaselineDetailLayers = TerraSplinesTool.CopyDetailLayers(state.WorkingDetailLayers);
                }

                if (state.WorkingTreeInstances != null)
                {
                    TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.WorkingTreeInstances);
                    state.BaselineTreeInstances = TerraSplinesTool.CopyTreeInstances(state.WorkingTreeInstances);
                }

                // Refresh baseline to current terrain state
                var td = terrain.terrainData;
                state.BaselineHeights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
                state.BaselineTexture = TerraSplinesTool.HeightsToTexture(terrain, state.BaselineHeights);
                var terrainHolesForBaseline = TerraSplinesTool.GetTerrainHoles(terrain);
                state.BaselineHoles = TerraSplinesTool.ConvertTerrainHolesToHeightmapHoles(terrain, terrainHolesForBaseline);

                // Update baseline alphamaps if they exist
                if (td.alphamapLayers > 0)
                {
                    state.BaselineAlphamaps = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
                }
            }
            
            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHeights = firstState.BaselineHeights;
                baselineHoles = firstState.BaselineHoles;
                baselineAlphamaps = firstState.BaselineAlphamaps;
                workingHeights = firstState.WorkingHeights;
                workingHoles = firstState.WorkingHoles;
                workingAlphamaps = firstState.WorkingAlphamaps;
            }

            // Update combined baseline texture
            UpdateCombinedBaselineTexture();
            
            heightRangeNeedsUpdate = true;
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnPaintAllButtonClicked()
        {
            ResumeAutoPipeline();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot paint all: No terrains assigned. Please assign a terrain group first.");
                return;
            }

            // Ensure baseline is loaded before paint operation
            EnsureBaseline();

            // Save current alphamaps to tool's internal undo buffer
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            // Paint all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null) continue;

                var td = terrain.terrainData;
                if (td == null || td.alphamapLayers == 0)
                {
                    Debug.LogWarning($"Cannot paint all: Terrain {terrain.name} has no layers configured");
                    continue;
                }

                if (globalSelectedLayerIndex < 0 || globalSelectedLayerIndex >= td.alphamapLayers)
                {
                    Debug.LogWarning($"Cannot paint all: Invalid layer index for terrain {terrain.name}");
                    continue;
                }

                // Get current alphamaps
                int alphamapWidth = td.alphamapWidth;
                int alphamapHeight = td.alphamapHeight;
                int layers = td.alphamapLayers;
                float[,,] alphamaps = td.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);

                // Paint entire terrain with selected layer
                for (int y = 0; y < alphamapHeight; y++)
                {
                    for (int x = 0; x < alphamapWidth; x++)
                    {
                        // Blend toward target layer using paint strength
                        float currentWeight = alphamaps[y, x, globalSelectedLayerIndex];
                        float targetWeight = 1.0f;
                        alphamaps[y, x, globalSelectedLayerIndex] = Mathf.Lerp(currentWeight, targetWeight, globalPaintStrength);

                        // Normalize other layers
                        float totalWeight = 0f;
                        for (int l = 0; l < layers; l++)
                        {
                            if (l != globalSelectedLayerIndex)
                            {
                                totalWeight += alphamaps[y, x, l];
                            }
                        }

                        if (totalWeight > 0f)
                        {
                            float remainingWeight = 1.0f - alphamaps[y, x, globalSelectedLayerIndex];
                            for (int l = 0; l < layers; l++)
                            {
                                if (l != globalSelectedLayerIndex)
                                {
                                    alphamaps[y, x, l] = (alphamaps[y, x, l] / totalWeight) * remainingWeight;
                                }
                            }
                        }
                        else
                        {
                            // If no other layers had weight, zero them out
                            for (int l = 0; l < layers; l++)
                            {
                                if (l != globalSelectedLayerIndex)
                                {
                                    alphamaps[y, x, l] = 0f;
                                }
                            }
                        }
                    }
                }

                // Apply to terrain
                td.SetAlphamaps(0, 0, alphamaps);

                // Update baseline to reflect the change
                state.BaselineAlphamaps = TerraSplinesTool.CopyAlphamaps(alphamaps);
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineAlphamaps = firstState.BaselineAlphamaps;
            }

            // Force UI state update after paint operation
            UpdateUIState();

            // Also rebuild preview to ensure everything is in sync
            RebuildPreview(applyToTerrain: true);
        }

        void OnApplySplineClicked(SplineItemData splineData)
        {
            var terrains = GetAllTerrains();
            if (terrains.Count == 0 || splineData?.container == null)
            {
                Debug.LogWarning("Cannot apply single spline: Missing terrains or spline data");
                return;
            }

            suppressBaselineUpdate = true;

            // Save current baseline to undo buffer before applying
            terrainStateManager.SaveUndoState();
            PushUndoSnapshot();

            var globals = new TerraSplinesTool.GlobalSettings
            {
                mode = globalMode,
                brushSizeMeters = globalBrushSize,
                strength = globalStrength,
                sampleStepMeters = globalSampleStep,
                brushHardness = globalBrushHardness,
                brushNoise = globalBrushNoise,
                operationHeight = globalOpHeight,
                operationPaint = globalOpPaint,
                operationHole = globalOpHole,
                operationFill = globalOpFill,
                operationAddDetail = globalOpAddDetail,
                operationRemoveDetail = globalOpRemoveDetail,
                operationAddTree = globalOpAddTree,
                operationRemoveTree = globalOpRemoveTree,
                globalSelectedLayerIndex = globalSelectedLayerIndex,
                globalPaintStrength = globalPaintStrength,
                globalPaintBlend = globalPaintBlend,
                globalPaintThreshold = globalPaintThreshold,
                globalPaintNoiseLayers = (globalPaintNoiseLayers != null && globalPaintNoiseLayers.Count > 0)
                    ? new List<PaintNoiseLayerSettings>(globalPaintNoiseLayers)
                    : null,
                globalSelectedDetailLayerIndex = globalSelectedDetailLayerIndex,
                globalSelectedDetailLayerIndices = (globalSelectedDetailLayerIndices != null && globalSelectedDetailLayerIndices.Count > 0) ? globalSelectedDetailLayerIndices.ToArray() : null,
                globalDetailStrength = globalDetailStrength,
                globalDetailMode = globalDetailMode,
                globalDetailSlopeLimitDegrees = globalDetailSlopeLimitDegrees,
                globalDetailFalloffPower = globalDetailFalloffPower,
                globalDetailSpreadRadius = globalDetailSpreadRadius,
                globalDetailRemoveThreshold = globalDetailRemoveThreshold,
                globalDetailNoiseLayers = (globalDetailNoiseLayers != null && globalDetailNoiseLayers.Count > 0)
                    ? new List<DetailNoiseLayerSettings>(globalDetailNoiseLayers)
                    : null,
                globalSelectedTreePrototypeIndex = globalSelectedTreePrototypeIndex,
                globalSelectedTreePrototypeIndices = (globalSelectedTreePrototypeIndices != null && globalSelectedTreePrototypeIndices.Count > 0)
                    ? globalSelectedTreePrototypeIndices.ToArray()
                    : null,
                globalTreeStrength = globalTreeStrength,
                globalTreeTargetDensity = globalTreeTargetDensity,
                globalTreeSlopeLimitDegrees = globalTreeSlopeLimitDegrees,
                globalTreeFalloffPower = globalTreeFalloffPower,
                globalTreeHeightMultiplier = (globalTreeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve(),
                globalTreeRemoveThreshold = globalTreeRemoveThreshold,
                globalTreeNoiseLayers = (globalTreeNoiseLayers != null && globalTreeNoiseLayers.Count > 0)
                    ? new List<TreeNoisePrototypeSettings>(globalTreeNoiseLayers)
                    : null,
            };

            // Apply spline to all overlapping terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.BaselineHeights == null) continue;

                // Calculate spline bounds for this terrain to check overlap
                var splineBounds = CalculateSplineBounds(terrain, splineData.container, splineData.settings, globals);
                var terrainBounds = TerrainCoordinates.GetTerrainBounds(terrain);
                if (!splineBounds.Intersects(terrainBounds)) continue;

                var td = terrain.terrainData;
                int res = td.heightmapResolution;
                var tempHeights = new float[res, res];

                bool opHeight = splineData.settings.overrideMode ? splineData.settings.operationHeight : globals.operationHeight;
                bool opPaint = splineData.settings.overrideMode ? splineData.settings.operationPaint : globals.operationPaint;
                bool opHole = splineData.settings.overrideMode ? splineData.settings.operationHole : globals.operationHole;
                bool opFill = splineData.settings.overrideMode ? splineData.settings.operationFill : globals.operationFill;
                bool opAddDetail = splineData.settings.overrideMode ? splineData.settings.operationAddDetail : globals.operationAddDetail;
                bool opRemoveDetail = splineData.settings.overrideMode ? splineData.settings.operationRemoveDetail : globals.operationRemoveDetail;
                bool opAddTree = splineData.settings.overrideMode ? splineData.settings.operationAddTree : globals.operationAddTree;
                bool opRemoveTree = splineData.settings.overrideMode ? splineData.settings.operationRemoveTree : globals.operationRemoveTree;

                opFill = opFill && !opHole;
                opRemoveDetail = opRemoveDetail && !opAddDetail;
                opRemoveTree = opRemoveTree && !opAddTree;

                var tempHoles = (opHole || opFill) && state.BaselineHoles != null ? new bool[res, res] : null;

                float[,,] tempAlphamaps = null;
                bool canPaint = opPaint && td.alphamapLayers > 0 && state.BaselineAlphamaps != null;
                if (canPaint)
                {
                    int alphamapHeight = td.alphamapHeight;
                    int alphamapWidth = td.alphamapWidth;
                    int layers = td.alphamapLayers;
                    tempAlphamaps = new float[alphamapHeight, alphamapWidth, layers];
                }

                bool usesDetail = (opAddDetail || opRemoveDetail)
                    && state.BaselineDetailLayers != null
                    && td.detailPrototypes != null
                    && td.detailPrototypes.Length > 0
                    && td.detailWidth > 0
                    && td.detailHeight > 0
                    && state.BaselineDetailLayers.Length == td.detailPrototypes.Length;

                int[][,] tempDetailLayers = null;
                if (usesDetail)
                {
                    int detailLayerCount = td.detailPrototypes.Length;
                    tempDetailLayers = new int[detailLayerCount][,];
                    for (int layer = 0; layer < detailLayerCount; layer++)
                    {
                        tempDetailLayers[layer] = new int[td.detailHeight, td.detailWidth];
                    }
                }

                bool usesTree = (opAddTree || opRemoveTree)
                    && state.BaselineTreeInstances != null
                    && td.treePrototypes != null
                    && td.treePrototypes.Length > 0;

                TreeInstance[] tempTreeInstances = null;

                TerraSplinesTool.ApplySingleSplineToBaseline(
                    terrain, splineData.container, splineData.settings, globals,
                    state.BaselineHeights, tempHeights,
                    tempHoles != null ? state.BaselineHoles : null, tempHoles,
                    canPaint ? state.BaselineAlphamaps : null, tempAlphamaps,
                    usesDetail ? state.BaselineDetailLayers : null, tempDetailLayers,
                    usesTree ? state.BaselineTreeInstances : null,
                    trees => tempTreeInstances = trees);

                TerraSplinesTool.ApplyFinalToTerrainWithUndo(terrain, tempHeights, $"Apply Spline '{splineData.container.name}'");

                if (canPaint && tempAlphamaps != null)
                {
                    terrain.terrainData.SetAlphamaps(0, 0, tempAlphamaps);
                    state.BaselineAlphamaps = TerraSplinesTool.CopyAlphamaps(tempAlphamaps);
                }

                if (tempHoles != null)
                {
                    var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, tempHoles);
                    TerraSplinesTool.ApplyHolesToTerrain(terrain, terrainHoles);
                    var terrainHolesForBaseline = TerraSplinesTool.GetTerrainHoles(terrain);
                    state.BaselineHoles = TerraSplinesTool.ConvertTerrainHolesToHeightmapHoles(terrain, terrainHolesForBaseline);
                }

                if (usesDetail && tempDetailLayers != null)
                {
                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, tempDetailLayers);
                    state.BaselineDetailLayers = TerraSplinesTool.CopyDetailLayers(tempDetailLayers);
                }

                if (usesTree && tempTreeInstances != null)
                {
                    TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, tempTreeInstances);
                    state.BaselineTreeInstances = TerraSplinesTool.CopyTreeInstances(tempTreeInstances);
                }

                state.BaselineHeights = td.GetHeights(0, 0, res, res);
                state.BaselineTexture = TerraSplinesTool.HeightsToTexture(terrain, state.BaselineHeights);
            }

            // Sync first terrain's state to window fields for backward compatibility
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHeights = firstState.BaselineHeights;
                baselineHoles = firstState.BaselineHoles;
                baselineAlphamaps = firstState.BaselineAlphamaps;
            }

            // Update combined baseline texture
            UpdateCombinedBaselineTexture();

            // Rebuild preview (the applied spline will no longer appear since it's now in baseline)
            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnDuplicateSplineClicked(SplineItemData splineData)
        {
            if (splineData?.container == null)
            {
                Debug.LogWarning("Cannot duplicate spline: Missing spline data");
                return;
            }

            var originalGameObject = splineData.container.gameObject;
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
                    brushNoise = originalSettings.settings.brushNoise != null
                        ? new BrushNoiseSettings
                        {
                            noiseTexture = originalSettings.settings.brushNoise.noiseTexture,
                            noiseStrength = originalSettings.settings.brushNoise.noiseStrength,
                            noiseEdge = originalSettings.settings.brushNoise.noiseEdge,
                            noiseWorldSizeMeters = originalSettings.settings.brushNoise.noiseWorldSizeMeters,
                            noiseOffset = originalSettings.settings.brushNoise.noiseOffset,
                            noiseInvert = originalSettings.settings.brushNoise.noiseInvert
                        }
                        : new BrushNoiseSettings(),
                    overrideSizeMultiplier = originalSettings.settings.overrideSizeMultiplier,
                    sizeMultiplier = originalSettings.settings.sizeMultiplier,
                    overridePaint = originalSettings.settings.overridePaint,
                    selectedLayerIndex = originalSettings.settings.selectedLayerIndex,
                    paintStrength = originalSettings.settings.paintStrength,
                    paintNoiseLayers = ClonePaintNoiseLayers(originalSettings.settings.paintNoiseLayers),
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

            // Refresh the spline list to show the new spline
            RefreshChildren();

            // Auto-select the newly created spline in the hierarchy
            Selection.activeGameObject = duplicatedGameObject;
            EditorGUIUtility.PingObject(duplicatedGameObject);
        }

        void OnDeleteSplineClicked(SplineItemData splineData)
        {
            if (splineData?.container == null)
            {
                Debug.LogWarning("Cannot delete spline: Missing spline data");
                return;
            }

            var gameObjectToDelete = splineData.container.gameObject;

            // Register undo operation
            Undo.DestroyObjectImmediate(gameObjectToDelete);

            // Refresh the spline list to remove the deleted spline
            RefreshChildren();
        }

        static List<DetailNoiseLayerSettings> CloneDetailNoiseLayers(List<DetailNoiseLayerSettings> source)
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

        static List<PaintNoiseLayerSettings> ClonePaintNoiseLayers(List<PaintNoiseLayerSettings> source)
        {
            if (source == null || source.Count == 0) return new List<PaintNoiseLayerSettings>();
            var copy = new List<PaintNoiseLayerSettings>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                if (entry == null) continue;
                copy.Add(new PaintNoiseLayerSettings
                {
                    paintLayerIndex = entry.paintLayerIndex,
                    noiseTexture = entry.noiseTexture,
                    noiseStrength = entry.noiseStrength,
                    noiseEdge = entry.noiseEdge,
                    noiseWorldSizeMeters = entry.noiseWorldSizeMeters,
                    noiseOffset = entry.noiseOffset,
                    noiseInvert = entry.noiseInvert
                });
            }
            return copy;
        }

        static List<TreeNoisePrototypeSettings> CloneTreeNoiseLayers(List<TreeNoisePrototypeSettings> source)
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

        void OnRedoButtonClicked()
        {
            ResumeAutoPipeline();

            var terrains = GetAllTerrains();
            if (terrains.Count == 0 || !terrainStateManager.CanRedo) return;

            suppressBaselineUpdate = true;

            // Perform redo for all terrains via TerrainStateManager
            terrainStateManager.PerformRedo();
            
            // Also handle window-level redo for backward compatibility
            var undoSnapshot = CaptureSnapshot();
            if (undoSnapshot != null)
            {
                undoStack.Push(undoSnapshot);
            }

            if (redoStack.Count > 0)
            {
                var snapshot = redoStack.Pop();
                RestoreSnapshot(snapshot);
            }

            // Apply baseline to all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null && state.BaselineHeights != null)
                {
                    var td = terrain.terrainData;
                    td.SetHeights(0, 0, state.BaselineHeights);
                    
                    if (state.BaselineHoles != null)
                    {
                        var terrainHoles = TerraSplinesTool.ConvertHeightmapHolesToTerrainHoles(terrain, state.BaselineHoles);
                        td.SetHoles(0, 0, terrainHoles);
                    }
                    
                    if (state.BaselineAlphamaps != null)
                    {
                        td.SetAlphamaps(0, 0, state.BaselineAlphamaps);
                    }

                    if (state.BaselineDetailLayers != null)
                    {
                        TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.BaselineDetailLayers);
                    }

                    if (state.BaselineTreeInstances != null)
                    {
                        TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.BaselineTreeInstances);
                    }
                    
                    TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles, state.BaselineAlphamaps);
                }
            }

            RebuildPreview(applyToTerrain: true);
            heightRangeNeedsUpdate = true;
            UpdateUIState();

            suppressBaselineUpdate = false;
        }

        void OnRebuildPreviewButtonClicked()
        {
            ResumeAutoPipeline();

            // Reset both original and baseline to capture fresh terrain state
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;
            
            // Clear state for all terrains
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null)
                {
                    state.OriginalHeights = null;
                    state.BaselineHeights = null;
                }
            }
            
            originalHeights = null;
            baselineHeights = null;
            EnsureBaseline();
            RebuildPreview(applyToTerrain: false);
            
            // Apply preview to all terrains for visualization
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null && state.WorkingHeights != null)
                {
                    TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.WorkingHeights);
                    isPreviewAppliedToTerrain = true;
                }
            }
        }

        void OnClearCacheButtonClicked()
        {
            TerraSplinesTool.ClearShapeHeightmapCache();
            UpdateUIState();
        }

        void OnCpuFallbackButtonClicked()
        {
            TerraSplinesTool.ForceCPUFallback();
            SaveEditorSettings();
            UpdateUIState();
        }

        void OnGpuPerformantButtonClicked()
        {
            TerraSplinesTool.SwitchToGPU();
            SaveEditorSettings();
            UpdateUIState();
        }

        void OnBenchmarkCacheButtonClicked()
        {
            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                Debug.LogWarning("Cannot run benchmark: No terrains assigned.");
                return;
            }

            if (splineItems.Count == 0)
            {
                Debug.LogWarning("Cannot run benchmark: No splines available.");
                return;
            }

            Debug.Log("=== Starting Cache Performance Benchmark ===");

            // Prepare test data
            var ordered = new List<(SplineContainer container, SplineStrokeSettings settings, int priority)>();
            if (splineGroup != null && splineItems.Count > 0)
            {
                var allSplines = GetAllSplineItems();
                for (int i = 0; i < allSplines.Count; i++)
                {
                    var item = allSplines[i];
                    if (item.container != null && item.container.gameObject.activeInHierarchy)
                    {
                        ordered.Add((item.container, item.settings, i));
                    }
                }
            }

            if (ordered.Count == 0)
            {
                Debug.LogWarning("Cannot run benchmark: No active splines found.");
                return;
            }

            var globals = new TerraSplinesTool.GlobalSettings
            {
                mode = globalMode,
                brushSizeMeters = globalBrushSize,
                strength = globalStrength,
                sampleStepMeters = globalSampleStep,
                brushHardness = globalBrushHardness,
                brushNoise = globalBrushNoise,
                operationHeight = globalOpHeight,
                operationPaint = globalOpPaint,
                operationHole = globalOpHole,
                operationFill = globalOpFill,
                operationAddDetail = globalOpAddDetail,
                operationRemoveDetail = globalOpRemoveDetail,
                operationAddTree = globalOpAddTree,
                operationRemoveTree = globalOpRemoveTree,
                globalSelectedLayerIndex = globalSelectedLayerIndex,
                globalPaintStrength = globalPaintStrength,
                globalPaintBlend = globalPaintBlend,
                globalPaintThreshold = globalPaintThreshold,
                globalPaintNoiseLayers = (globalPaintNoiseLayers != null && globalPaintNoiseLayers.Count > 0)
                    ? new List<PaintNoiseLayerSettings>(globalPaintNoiseLayers)
                    : null,
                globalSelectedDetailLayerIndex = globalSelectedDetailLayerIndex,
                globalSelectedDetailLayerIndices = (globalSelectedDetailLayerIndices != null && globalSelectedDetailLayerIndices.Count > 0) ? globalSelectedDetailLayerIndices.ToArray() : null,
                globalDetailStrength = globalDetailStrength,
                globalDetailMode = globalOpRemoveDetail ? DetailOperationMode.Remove : DetailOperationMode.Add,
                globalDetailSlopeLimitDegrees = globalDetailSlopeLimitDegrees,
                globalDetailFalloffPower = globalDetailFalloffPower,
                globalDetailSpreadRadius = globalDetailSpreadRadius,
                globalDetailRemoveThreshold = globalDetailRemoveThreshold,
                globalDetailNoiseLayers = (globalDetailNoiseLayers != null && globalDetailNoiseLayers.Count > 0)
                    ? new List<DetailNoiseLayerSettings>(globalDetailNoiseLayers)
                    : null,
                globalSelectedTreePrototypeIndex = globalSelectedTreePrototypeIndex,
                globalSelectedTreePrototypeIndices = (globalSelectedTreePrototypeIndices != null && globalSelectedTreePrototypeIndices.Count > 0)
                    ? globalSelectedTreePrototypeIndices.ToArray()
                    : null,
                globalTreeStrength = globalTreeStrength,
                globalTreeTargetDensity = globalTreeTargetDensity,
                globalTreeSlopeLimitDegrees = globalTreeSlopeLimitDegrees,
                globalTreeFalloffPower = globalTreeFalloffPower,
                globalTreeHeightMultiplier = (globalTreeHeightMultiplier ?? SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve()).CloneCurve(),
                globalTreeRemoveThreshold = globalTreeRemoveThreshold,
                globalTreeNoiseLayers = (globalTreeNoiseLayers != null && globalTreeNoiseLayers.Count > 0)
                    ? new List<TreeNoisePrototypeSettings>(globalTreeNoiseLayers)
                    : null,
            };

            // Ensure we have baseline heights
            EnsureBaseline();
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState == null || firstState.BaselineHeights == null)
            {
                Debug.LogError("Cannot run benchmark: No baseline heights available.");
                return;
            }

            var baselineHeights = firstState.BaselineHeights;
            int testRuns = 10;
            var results = new Dictionary<string, List<float>>();

            // Test 1: Individual mode with clear cache
            Debug.Log("Testing: Individual mode with clear cache...");
            results["Individual + Clear Cache"] = new List<float>();
            for (int i = 0; i < testRuns; i++)
            {
                TerraSplinesTool.ClearAllCaches();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var testHeights = TerraSplinesTool.CopyHeights(baselineHeights);
                TerraSplinesTool.RasterizeSplines(firstTerrain, ordered, globals, baselineHeights, testHeights, splineGroup);
                stopwatch.Stop();
                results["Individual + Clear Cache"].Add((float)stopwatch.Elapsed.TotalMilliseconds);
            }

            // Test 2: Individual mode with cache (warm cache)
            Debug.Log("Testing: Individual mode with warm cache...");
            results["Individual + Warm Cache"] = new List<float>();
            for (int i = 0; i < testRuns; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var testHeights = TerraSplinesTool.CopyHeights(baselineHeights);
                TerraSplinesTool.RasterizeSplines(firstTerrain, ordered, globals, baselineHeights, testHeights, splineGroup);
                stopwatch.Stop();
                results["Individual + Warm Cache"].Add((float)stopwatch.Elapsed.TotalMilliseconds);
            }

            // Calculate and log averages
            Debug.Log("=== Benchmark Results (Average of 10 runs) ===");
            foreach (var kvp in results)
            {
                float average = kvp.Value.Average();
                float min = kvp.Value.Min();
                float max = kvp.Value.Max();
                Debug.Log($"{kvp.Key}: {average:F2}ms (min: {min:F2}ms, max: {max:F2}ms)");
            }

            // Find fastest
            var fastest = results.OrderBy(kvp => kvp.Value.Average()).First();
            Debug.Log($"=== Fastest Method: {fastest.Key} at {fastest.Value.Average():F2}ms average ===");

            // Refresh preview
            RefreshPreview();

            Debug.Log("=== Benchmark Complete ===");
        }

        void OnGlobalTabClicked(int tabIndex)
        {
            globalActiveTab = tabIndex;

            // Update tab button states
            SetTabButtonActive(globalModeTab, tabIndex == 0);
            SetTabButtonActive(globalBrushTab, tabIndex == 1);
            SetTabButtonActive(globalPaintTab, tabIndex == 2);
            SetTabButtonActive(globalDetailTab, tabIndex == 3);
            SetTabButtonActive(globalTreeTab, tabIndex == 4);

            // Show/hide tab content
            if (globalModeContent != null)
                globalModeContent.style.display = tabIndex == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (globalBrushContent != null)
                globalBrushContent.style.display = tabIndex == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (globalPaintContent != null)
                globalPaintContent.style.display = tabIndex == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            if (globalDetailContent != null)
                globalDetailContent.style.display = tabIndex == 3 ? DisplayStyle.Flex : DisplayStyle.None;
            if (globalTreeContent != null)
                globalTreeContent.style.display = tabIndex == 4 ? DisplayStyle.Flex : DisplayStyle.None;

            // Mark as dirty for serialization
            EditorUtility.SetDirty(this);
        }

        void OnFindRefsClicked()
        {
            if (TryAutoDetectTerrainAndSplineGroup(out var detectedTerrainGroup, out var detectedSplineGroup))
            {
                ApplyTargetReferences(detectedTerrainGroup, detectedSplineGroup, updatePreviewPipeline: false);
            }
        }

        void OnSetupGroupClicked()
        {
            if (targetTerrainGroup == null || splineGroup == null)
                return;

            var splineGroupObject = splineGroup.gameObject;
            if (splineGroupObject == null)
                return;

            var group = splineGroupObject.GetComponent<TerrainSplinesGroup>();
            if (group == null)
            {
                group = Undo.AddComponent<TerrainSplinesGroup>(splineGroupObject);
            }
            else
            {
                Undo.RecordObject(group, "Setup Terrain Splines Group");
            }

            group.SetReferences(targetTerrainGroup, splineGroup);
            EditorUtility.SetDirty(group);
        }
    }
}
