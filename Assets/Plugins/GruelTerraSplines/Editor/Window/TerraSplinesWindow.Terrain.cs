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
        /// <summary>
        /// Generate combined baseline texture from all terrains
        /// </summary>
        void UpdateCombinedBaselineTexture()
        {
            // Skip if preview is disabled
            if (!heightmapPreviewEnabled)
            {
                return;
            }

            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                if (baselineTex != null)
                {
                    DestroyImmediate(baselineTex);
                    baselineTex = null;
                }

                return;
            }

            var terrainData = new List<(Terrain terrain, float[,] heights)>();
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state != null && state.BaselineHeights != null)
                {
                    terrainData.Add((terrain, state.BaselineHeights));
                }
            }

            if (terrainData.Count > 0)
            {
                if (baselineTex != null)
                {
                    DestroyImmediate(baselineTex);
                }

                baselineTex = TerraSplinesTool.HeightsToCombinedTexture(terrainData, 256);
            }
        }

        void EnsureBaseline()
        {
            // Use TerrainStateManager to ensure baseline for all terrains
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;

            terrainStateManager.SetTerrains(terrains);

            // Sync to window fields for backward compatibility (use first terrain)
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager.TerrainStates.GetValueOrDefault(firstTerrain);
            if (firstState != null)
            {
                baselineHeights = firstState.BaselineHeights;
                workingHeights = firstState.WorkingHeights;
                baselineHoles = firstState.BaselineHoles;
                workingHoles = firstState.WorkingHoles;
                baselineAlphamaps = firstState.BaselineAlphamaps;
                workingAlphamaps = firstState.WorkingAlphamaps;
                originalHeights = firstState.OriginalHeights;
                originalHoles = firstState.OriginalHoles;
                originalAlphamaps = firstState.OriginalAlphamaps;

                // Generate combined baseline texture from all terrains
                var terrainData = new List<(Terrain terrain, float[,] heights)>();
                foreach (var terrain in terrains)
                {
                    var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                    if (state != null && state.BaselineHeights != null)
                    {
                        terrainData.Add((terrain, state.BaselineHeights));
                    }
                }

                if (terrainData.Count > 0)
                {
                    if (baselineTex != null)
                    {
                        DestroyImmediate(baselineTex);
                    }

                    baselineTex = TerraSplinesTool.HeightsToCombinedTexture(terrainData, 256);
                }
                else
                {
                    baselineTex = firstState.BaselineTexture;
                }

                // Initialize preview texture if needed (fixed 256x256 for combined multi-terrain preview)
                if (previewTex == null || previewTex.width != 256 || previewTex.height != 256)
                {
                    previewTex = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
                    previewTex.wrapMode = TextureWrapMode.Clamp;
                }

                heightRangeNeedsUpdate = true;
            }
        }


        void RebuildPreview(bool applyToTerrain)
        {
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;

            // Use first terrain for backward compatibility checks
            var firstTerrain = terrains[0];
            var firstState = terrainStateManager?.TerrainStates?.GetValueOrDefault(firstTerrain);
            if (firstState == null || firstState.BaselineHeights == null) return;

            var td = firstTerrain.terrainData;

            // Build ordered list first to check if there's any work to do
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

            // Early exit if no active splines - just restore baseline to avoid expensive operations
            if (ordered.Count == 0)
            {
                if (applyToTerrain)
                {
                    // Restore baseline for all terrains
                    foreach (var terrain in terrains)
                    {
                        var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                        if (state != null && state.BaselineHeights != null)
                        {
                            TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights, state.BaselineHoles, state.BaselineAlphamaps);
                        }
                    }
                }

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

            // Time terrain operations
            var terrainSw = System.Diagnostics.Stopwatch.StartNew();

            // Process each terrain separately
            foreach (var terrain in terrains)
            {
                var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                if (state == null || state.BaselineHeights == null) continue;

                var terrainTd = terrain.terrainData;
                int res = terrainTd.heightmapResolution;

                // Always reset working arrays to baseline at the start of rebuild
                // This ensures we're applying splines on top of baseline, not on top of previous spline results
                if (state.WorkingHeights == null || state.WorkingHeights.GetLength(0) != res)
                {
                    state.WorkingHeights = TerraSplinesTool.CopyHeights(state.BaselineHeights);
                }
                else
                {
                    // Reset existing array to baseline
                    TerraSplinesTool.CopyHeightsTo(state.BaselineHeights, state.WorkingHeights);
                }

                if (featureHoleEnabled)
                {
                    if (state.WorkingHoles == null || state.WorkingHoles.GetLength(0) != res)
                    {
                        state.WorkingHoles = TerraSplinesTool.CopyHoles(state.BaselineHoles);
                    }
                    else
                    {
                        TerraSplinesTool.CopyHolesTo(state.BaselineHoles, state.WorkingHoles);
                    }
                }

                if (featurePaintEnabled && terrainTd.alphamapLayers > 0)
                {
                    // Ensure baseline alphamaps match terrain's current layer count
                    if (state.BaselineAlphamaps == null
                        || state.BaselineAlphamaps.GetLength(0) != terrainTd.alphamapHeight
                        || state.BaselineAlphamaps.GetLength(1) != terrainTd.alphamapWidth
                        || state.BaselineAlphamaps.GetLength(2) != terrainTd.alphamapLayers)
                    {
                        // Refresh baseline from terrain if dimensions don't match
                        state.BaselineAlphamaps = terrainTd.GetAlphamaps(0, 0, terrainTd.alphamapWidth, terrainTd.alphamapHeight);
                    }

                    // Ensure working alphamaps match terrain's current layer count
                    if (state.WorkingAlphamaps == null
                        || state.WorkingAlphamaps.GetLength(0) != terrainTd.alphamapHeight
                        || state.WorkingAlphamaps.GetLength(1) != terrainTd.alphamapWidth
                        || state.WorkingAlphamaps.GetLength(2) != terrainTd.alphamapLayers)
                    {
                        state.WorkingAlphamaps = TerraSplinesTool.CopyAlphamaps(state.BaselineAlphamaps);
                    }
                    else
                    {
                        TerraSplinesTool.CopyAlphamapsTo(state.BaselineAlphamaps, state.WorkingAlphamaps);
                    }
                }

                // Initialize masks if needed (always reset to zero/false)
                if (featurePaintEnabled)
                {
                    if (state.PaintSplineAlphaMask == null || state.PaintSplineAlphaMask.GetLength(0) != res)
                    {
                        state.PaintSplineAlphaMask = new float[res, res];
                    }
                    else
                    {
                        // Reset mask to zero
                        for (int z = 0; z < res; z++)
                        {
                            for (int x = 0; x < res; x++)
                            {
                                state.PaintSplineAlphaMask[z, x] = 0f;
                            }
                        }
                    }
                }

                if (featureHoleEnabled)
                {
                    if (state.HoleSplineMask == null || state.HoleSplineMask.GetLength(0) != res)
                    {
                        state.HoleSplineMask = new bool[res, res];
                    }
                    else
                    {
                        // Reset mask to false
                        for (int z = 0; z < res; z++)
                        {
                            for (int x = 0; x < res; x++)
                            {
                                state.HoleSplineMask[z, x] = false;
                            }
                        }
                    }
                }

                // Filter splines to only those that overlap this terrain
                var overlappingSplines = new List<(SplineContainer container, SplineStrokeSettings settings, int priority)>();
                foreach (var entry in ordered)
                {
                    if (entry.container == null) continue;

                    // Calculate spline bounds using cached data if available, or accurate calculation
                    var splineBounds = CalculateSplineBounds(terrain, entry.container, entry.settings, globals);
                    var terrainBounds = TerrainCoordinates.GetTerrainBounds(terrain);

                    if (splineBounds.Intersects(terrainBounds))
                    {
                        overlappingSplines.Add(entry);
                    }
                }

                if (overlappingSplines.Count == 0) continue;

                // Apply splines on top of baseline for this terrain
                TerraSplinesTool.RasterizeSplines(
                    terrain, overlappingSplines, globals, state.BaselineHeights, state.WorkingHeights,
                    state.BaselineHoles, state.WorkingHoles, state.BaselineAlphamaps, state.WorkingAlphamaps, splineGroup,
                    featureHoleEnabled, featurePaintEnabled, featureDetailEnabled, featureTreeEnabled,
                    state.PaintSplineAlphaMask, state.HoleSplineMask,
                    state.BaselineDetailLayers, state.WorkingDetailLayers,
                    state.BaselineTreeInstances,
                    trees => state.WorkingTreeInstances = trees
                );

                // Apply alphamaps to terrain if they exist and paint feature is enabled
                if (state.WorkingAlphamaps != null && featurePaintEnabled)
                {
                    TerraSplinesTool.ApplyAlphamapsToTerrain(terrain, state.WorkingAlphamaps);
                }

                // Apply detail layers to terrain if they exist and grass feature is enabled
                if (state.WorkingDetailLayers != null && featureDetailEnabled)
                {
                    TerraSplinesTool.ApplyDetailLayersToTerrain(terrain, state.WorkingDetailLayers);
                }

                if (state.WorkingTreeInstances != null && featureTreeEnabled)
                {
                    TerraSplinesTool.ApplyTreeInstancesToTerrain(terrain, state.WorkingTreeInstances);
                }

                if (applyToTerrain)
                {
                    TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.WorkingHeights, state.WorkingHoles, state.WorkingAlphamaps);
                }
            }

            // Sync first terrain's state to window fields for backward compatibility
            if (firstState != null)
            {
                workingHeights = firstState.WorkingHeights;
                workingHoles = firstState.WorkingHoles;
                workingAlphamaps = firstState.WorkingAlphamaps;
                paintSplineAlphaMask = firstState.PaintSplineAlphaMask;
                holeSplineMask = firstState.HoleSplineMask;
            }

            // Stop terrain operation timing
            terrainSw.Stop();
            terrainOperationTimeMs = terrainSw.Elapsed.TotalMilliseconds;

            // Update preview texture (combine all terrains) - only if enabled
            if (heightmapPreviewEnabled && previewTex != null)
            {
                var previewSw = System.Diagnostics.Stopwatch.StartNew();

                if (previewTex.format != TextureFormat.RGBA32 || previewTex.width != 256 || previewTex.height != 256)
                {
                    DestroyImmediate(previewTex);
                    previewTex = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
                    previewTex.wrapMode = TextureWrapMode.Clamp;
                }

                // Collect all terrains and their working heights
                var terrainData = new List<(Terrain terrain, float[,] heights)>();
                foreach (var terrain in terrains)
                {
                    var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                    if (state != null && state.WorkingHeights != null)
                    {
                        terrainData.Add((terrain, state.WorkingHeights));
                    }
                }

                if (terrainData.Count > 0)
                {
                    var combinedTex = TerraSplinesTool.HeightsToCombinedTexture(terrainData, 256);
                    var colors = combinedTex.GetPixels32();
                    previewTex.SetPixels32(colors);
                    previewTex.Apply(false, false);
                    DestroyImmediate(combinedTex);
                }

                previewSw.Stop();
                previewGenerationTimeMs = previewSw.Elapsed.TotalMilliseconds;
            }
            else
            {
                previewGenerationTimeMs = 0;
            }

            // Mark that preview textures need updating
            previewTexturesNeedUpdate = true;
            heightRangeNeedsUpdate = true;

            // Update preview texture UI elements
            UpdatePreviewTexturesUI();
        }

        /// <summary>
        /// Calculate world-space bounds of a spline including brush radius.
        /// Uses cached bounds if available (most accurate), otherwise calculates from spline points.
        /// </summary>
        Bounds CalculateSplineBounds(Terrain terrain, SplineContainer container, SplineStrokeSettings settings, TerraSplinesTool.GlobalSettings globals)
        {
            if (container == null || terrain == null) return new Bounds();

            // Try to get cached bounds first (most accurate - already accounts for brush size multipliers)
            var mode = settings.overrideMode ? settings.mode : globals.mode;
            var brushSize = settings.overrideBrush ? settings.sizeMeters : globals.brushSizeMeters;
            var brushHardness = settings.overrideBrush ? settings.hardness : globals.brushHardness;
            var sampleStep = settings.overrideBrush ? settings.sampleStep : globals.sampleStepMeters;
            var brushSizeMultiplier = (settings.overrideBrush && settings.overrideSizeMultiplier) ? settings.sizeMultiplier : AnimationCurve.Constant(0f, 1f, 1f);

            // Try to get cache from strategy
            if (TerraSplinesTool.strategies.TryGetValue(mode, out var strategy))
            {
                try
                {
                    var cache = strategy.RasterizeToCache(terrain, container, brushHardness, brushSize, sampleStep, mode, brushSizeMultiplier);
                    if (cache != null && cache.isValid)
                    {
                        // Convert cached heightmap bounds to world-space bounds
                        var minWorld = TerrainCoordinates.HeightmapToWorld(terrain, cache.minX, cache.minZ);
                        var maxWorld = TerrainCoordinates.HeightmapToWorld(terrain, cache.maxX, cache.maxZ);

                        // Create bounds from the corners
                        var center = (minWorld + maxWorld) * 0.5f;
                        var size = maxWorld - minWorld;
                        // Ensure size is positive and add some padding for safety
                        size.x = Mathf.Abs(size.x);
                        size.z = Mathf.Abs(size.z);
                        size.y = terrain.terrainData.size.y; // Use full terrain height

                        return new Bounds(center, size);
                    }
                }
                catch
                {
                    // Fall through to calculation method if cache fails
                }
            }

            // Fallback: Calculate bounds from spline points with accurate brush size tracking
            var bounds = new Bounds();
            bool first = true;
            float maxBrushSize = brushSize;

            foreach (var spline in container.Splines)
            {
                float length = SplineUtility.CalculateLength(spline, container.transform.localToWorldMatrix);
                if (length <= 0.0001f) continue;

                // Sample more densely to catch all points
                int steps = Mathf.Max(10, Mathf.CeilToInt(length / Mathf.Max(0.1f, sampleStep)));
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    var worldPos = container.transform.TransformPoint(SplineUtility.EvaluatePosition(spline, t));

                    // Track maximum brush size including multiplier
                    float sizeMultiplier = brushSizeMultiplier != null ? brushSizeMultiplier.Evaluate(t) : 1f;
                    float actualBrushSize = brushSize * sizeMultiplier;
                    maxBrushSize = Mathf.Max(maxBrushSize, actualBrushSize);

                    if (first)
                    {
                        bounds = new Bounds(worldPos, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(worldPos);
                    }
                }
            }

            // Expand bounds by maximum brush radius (accounting for multipliers)
            bounds.Expand(maxBrushSize * 2f);

            return bounds;
        }

        void UpdatePreviewTextures()
        {
            var terrains = GetAllTerrains();
            if (terrains.Count == 0) return;

            try
            {
                var allSplines = GetAllSplineItems();
                if (allSplines.Count == 0) return;

                // Early exit optimization: Check if global brush settings changed
                // CheckForSplinePropertyChanges() already detects spline changes and clears previewTextureCacheKeys,
                // so we can rely on missing cache keys to detect changes without calling GetSplineVersion()
                int globalPreviewSettingsHash = GetGlobalPreviewSettingsHash();
                bool globalSettingsChanged = globalPreviewSettingsHash != lastGlobalPreviewSettingsHash;

                // If global settings changed, clear all cache keys so previews regenerate with new settings
                if (globalSettingsChanged)
                {
                    previewTextureCacheKeys.Clear();
                }

                if (!globalSettingsChanged)
                {
                    // Check if all splines have cached previews AND their settings haven't changed
                    // We can check settings without calling GetSplineVersion() by comparing stored settings
                    bool allCached = true;
                    foreach (var item in allSplines)
                    {
                        if (item.container == null) continue;
                        int currentPreviewSettingsHash = GetEffectivePreviewSettingsHash(item);
                        
                        // Check if we have a cached key AND a preview texture
                        if (!previewTextureCacheKeys.ContainsKey(item.container) || item.previewTexture == null)
                        {
                            allCached = false;
                            break;
                        }

                        if (lastSplinePreviewSettingsHashes.TryGetValue(item.container, out var lastSettingsHash))
                        {
                            if (lastSettingsHash != currentPreviewSettingsHash)
                            {
                                previewTextureCacheKeys.Remove(item.container);
                                allCached = false;
                                break;
                            }
                        }
                        else
                        {
                            // First time seeing this spline - not fully cached yet
                            allCached = false;
                            break;
                        }
                    }

                    if (allCached)
                    {
                        // All previews are cached, global settings haven't changed, and no spline settings changed
                        // Skip expensive GetSplineVersion calls
                        return;
                    }
                }

                lastGlobalPreviewSettingsHash = globalPreviewSettingsHash;

                bool anyPreviewReferenceChanged = false;
                foreach (var item in allSplines)
                {
                    if (item.container == null) continue;

                    // Get current spline version to detect changes
                    int currentVersion = TerraSplinesTool.GetSplineVersion(item.container);
                    
                    var mode = item.settings.overrideMode ? item.settings.mode : globalMode;
                    int previewSettingsHash = GetEffectivePreviewSettingsHash(item);

                    lastSplinePreviewSettingsHashes[item.container] = previewSettingsHash;

                    int cacheKey = currentVersion;
                    cacheKey = cacheKey * 31 + previewSettingsHash;
                    
                    // Check if we already have a cached preview for this version, mode, and brush settings
                    // The cache key already includes mode and brush settings, so if those change, the key will be different
                    bool needsRegeneration = true;
                    if (item.previewTexture != null && previewTextureCacheKeys.TryGetValue(item.container, out int lastCachedKey))
                    {
                        if (cacheKey == lastCachedKey)
                        {
                            // Preview is still valid (geometry, mode, and brush settings haven't changed), skip regeneration
                            needsRegeneration = false;
                        }
                    }

                    // Only regenerate if version changed or preview doesn't exist
                    if (needsRegeneration)
                    {
                        var oldTexture = item.previewTexture;
                        Texture2D updatedTexture = null;

                        if (mode == SplineApplyMode.Shape)
                        {
                            // Update preview in-place (reuses existing texture when possible)
                            updatedTexture = TerraSplinesTool.GetShapeHeightMaskTexture(item.container, 128, oldTexture);
                        }
                        else if (mode == SplineApplyMode.Path)
                        {
                            // Update preview in-place (reuses existing texture when possible)
                            updatedTexture = TerraSplinesTool.GetPathHeightMaskTexture(item.container, 128, oldTexture);
                        }

                        if (updatedTexture == null)
                        {
                            if (oldTexture != null)
                            {
                                DestroyImmediate(oldTexture);
                                anyPreviewReferenceChanged = true;
                            }
                            item.previewTexture = null;
                            previewTextureCacheKeys.Remove(item.container);
                            continue;
                        }

                        if (updatedTexture != oldTexture)
                        {
                            if (oldTexture != null)
                            {
                                DestroyImmediate(oldTexture);
                            }
                            anyPreviewReferenceChanged = true;
                        }

                        item.previewTexture = updatedTexture;

                        // Cache the combined key (version + mode + brush settings) after successful regeneration
                        previewTextureCacheKeys[item.container] = cacheKey;
                    }
                }

                // Update ListView to show new previews
                if (splinesListView != null && anyPreviewReferenceChanged)
                {
                    if (updatesPaused)
                    {
                        splinesListView.RefreshItems();
                    }
                    else
                    {
                        // Manually update preview images without triggering full ListView refresh
                        UpdatePreviewImagesManually();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to update preview textures: {e.Message}");
            }
        }

        int GetGlobalPreviewSettingsHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + globalMode.GetHashCode();
                hash = hash * 31 + globalBrushSize.GetHashCode();
                hash = hash * 31 + globalBrushHardness.GetHashCode();
                hash = hash * 31 + globalStrength.GetHashCode();
                hash = hash * 31 + globalSampleStep.GetHashCode();
                hash = hash * 31 + (globalBrushNoise != null ? globalBrushNoise.GetHashCode() : 0);
                hash = hash * 31 + globalOpPaint.GetHashCode();
                hash = hash * 31 + globalSelectedLayerIndex.GetHashCode();
                hash = hash * 31 + globalPaintStrength.GetHashCode();
                hash = hash * 31 + globalPaintBlend.GetHashCode();
                hash = hash * 31 + globalPaintThreshold.GetHashCode();
                hash = hash * 31 + GetPaintNoiseLayersHash(globalPaintNoiseLayers);
                return hash;
            }
        }

        int GetEffectivePreviewSettingsHash(SplineItemData item)
        {
            if (item == null || item.settings == null)
                return GetGlobalPreviewSettingsHash();

            unchecked
            {
                int hash = 17;
                var settings = item.settings;
                bool useBrushOverride = settings.overrideBrush;
                var mode = settings.overrideMode ? settings.mode : globalMode;
                float brushSize = useBrushOverride ? settings.sizeMeters : globalBrushSize;
                float brushHardness = useBrushOverride ? settings.hardness : globalBrushHardness;
                float brushStrength = useBrushOverride ? settings.strength : globalStrength;
                float sampleStep = useBrushOverride ? settings.sampleStep : globalSampleStep;
                bool useSizeMultiplier = useBrushOverride && settings.overrideSizeMultiplier;
                int sizeMultiplierHash = useSizeMultiplier && settings.sizeMultiplier != null
                    ? settings.sizeMultiplier.GetAnimationCurveHash()
                    : 0;
                int brushNoiseHash = useBrushOverride
                    ? (settings.brushNoise != null ? settings.brushNoise.GetHashCode() : 0)
                    : (globalBrushNoise != null ? globalBrushNoise.GetHashCode() : 0);
                bool usePaintOverride = settings.overridePaint;
                bool paintOperationEnabled = settings.overrideMode ? settings.operationPaint : globalOpPaint;
                int selectedPaintLayerIndex = usePaintOverride ? settings.selectedLayerIndex : globalSelectedLayerIndex;
                float paintStrength = usePaintOverride ? settings.paintStrength : globalPaintStrength;
                float paintBlend = usePaintOverride ? settings.paintBlend : globalPaintBlend;
                float paintThreshold = usePaintOverride ? settings.paintThreshold : globalPaintThreshold;
                int paintNoiseHash = usePaintOverride
                    ? GetPaintNoiseLayersHash(settings.paintNoiseLayers)
                    : GetPaintNoiseLayersHash(globalPaintNoiseLayers);

                hash = hash * 31 + mode.GetHashCode();
                hash = hash * 31 + useBrushOverride.GetHashCode();
                hash = hash * 31 + brushSize.GetHashCode();
                hash = hash * 31 + brushHardness.GetHashCode();
                hash = hash * 31 + brushStrength.GetHashCode();
                hash = hash * 31 + sampleStep.GetHashCode();
                hash = hash * 31 + useSizeMultiplier.GetHashCode();
                hash = hash * 31 + sizeMultiplierHash;
                hash = hash * 31 + brushNoiseHash;
                hash = hash * 31 + paintOperationEnabled.GetHashCode();
                hash = hash * 31 + usePaintOverride.GetHashCode();
                hash = hash * 31 + selectedPaintLayerIndex.GetHashCode();
                hash = hash * 31 + paintStrength.GetHashCode();
                hash = hash * 31 + paintBlend.GetHashCode();
                hash = hash * 31 + paintThreshold.GetHashCode();
                hash = hash * 31 + paintNoiseHash;
                return hash;
            }
        }

        static int GetPaintNoiseLayersHash(System.Collections.Generic.IEnumerable<PaintNoiseLayerSettings> layers)
        {
            if (layers == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                foreach (var layer in layers)
                {
                    hash = hash * 31 + (layer != null ? layer.GetHashCode() : 0);
                }

                return hash;
            }
        }

        void UpdatePreviewImagesManually()
        {
            if (splinesListView == null) return;

            try
            {
                // Get all visible ListView items and update their preview images
                var items = splinesListView.Query<VisualElement>(className: "spline-item-container").ToList();

                foreach (var item in items)
                {
                    // Find the preview thumbnail image in this item
                    var previewThumbnail = item.Q<Image>("preview-thumbnail");
                    var noPreviewLabel = item.Q<Label>("no-preview-label");

                    if (previewThumbnail != null)
                    {
                        // Find the corresponding spline data for this item
                        var splineData = FindSplineItemDataForElement(item);
                        if (splineData != null && splineData.previewTexture != null)
                        {
                            previewThumbnail.image = splineData.previewTexture;
                            previewThumbnail.style.display = DisplayStyle.Flex;
                            if (noPreviewLabel != null)
                                noPreviewLabel.style.display = DisplayStyle.None;
                        }
                        else
                        {
                            previewThumbnail.style.display = DisplayStyle.None;
                            if (noPreviewLabel != null)
                                noPreviewLabel.style.display = DisplayStyle.Flex;
                        }
                    }
                }

                // Also update group items
                var groupItems = splinesListView.Query<VisualElement>(className: "group-item-container").ToList();
                foreach (var groupItem in groupItems)
                {
                    // Find spline items within this group
                    var nestedSplines = groupItem.Query<VisualElement>(className: "spline-item-container").ToList();
                    foreach (var nestedSpline in nestedSplines)
                    {
                        var previewThumbnail = nestedSpline.Q<Image>("preview-thumbnail");
                        var noPreviewLabel = nestedSpline.Q<Label>("no-preview-label");

                        if (previewThumbnail != null)
                        {
                            var splineData = FindSplineItemDataForElement(nestedSpline);
                            if (splineData != null && splineData.previewTexture != null)
                            {
                                previewThumbnail.image = splineData.previewTexture;
                                previewThumbnail.style.display = DisplayStyle.Flex;
                                if (noPreviewLabel != null)
                                    noPreviewLabel.style.display = DisplayStyle.None;
                            }
                            else
                            {
                                previewThumbnail.style.display = DisplayStyle.None;
                                if (noPreviewLabel != null)
                                    noPreviewLabel.style.display = DisplayStyle.Flex;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to manually update preview images: {e.Message}");
            }
        }

        bool HasActiveSplines()
        {
            // Validate references first
            ValidateAndResetReferences();

            bool hasActive = false;

            if (splineGroup != null && splineItems.Count > 0)
            {
                var allSplines = GetAllSplineItems();
                for (int i = 0; i < allSplines.Count; i++)
                {
                    var item = allSplines[i];
                    if (item.container != null && item.container.gameObject.activeInHierarchy)
                    {
                        hasActive = true;
                        break;
                    }
                }
            }

            // Only clean up caches when state changes from active to inactive
            if (lastHadActiveSplines && !hasActive)
            {
                TerraSplinesTool.ClearInactiveSplineCaches();
            }

            lastHadActiveSplines = hasActive;
            return hasActive;
        }

        void RefreshPreview()
        {
            if (suppressRefreshPreviewRequestCount > 0)
                return;

            ResumeAutoPipeline();

            // Trigger preview refresh (event-driven pipeline)
            MarkPipelineDirty(preview: true, apply: true, previewTextures: true, heightRange: true, allowWhileAutoPipelineSuspended: true);

            if (updatesPaused || IsInteractiveRefreshDeferred())
                return;

            ProcessPipelineUpdate();
        }

        void UpdateBrushPreview()
        {
            if (globalBrushPreviewImage != null)
            {
                globalBrushPreviewImage.image = BrushFalloffUtils.GenerateBrushPreviewTexture(64, globalBrushHardness);
            }
        }

        // OPTIMIZATION #1: BuildCombinedSplineMask() removed - masks are now built during RasterizeSplines() 
        // to eliminate redundant rasterization. The method was re-rasterizing all splines just to build masks,
        // but we already have the cache data from the main RasterizeSplines() pass. Now masks are accumulated
        // directly in the operation appliers (PaintOperationApplier, HoleOperationApplier, HeightAndPaintOperationApplier)
        // as they process pixels, eliminating the entire duplicate rasterization pass.
    }
}
