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
        void OnEditorUpdate()
        {
            // Don't run updates while in play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            // New update-on-change pipeline: do almost nothing per-frame unless something is actually dirty.
            if (updatesPaused)
            {
                if (!manualUpdateRequested) return;
                manualUpdateRequested = false;

                SyncSplineHierarchyForManualUpdate();

                baselineDirty = true;
                previewDirty = true;
                applyDirty = true;
                previewTexturesNeedUpdate = true;
                heightRangeDirty = true;
                heightRangeNeedsUpdate = true;

                RunPipelineIfDirty();
                SyncObservedTerrainContentHashes();
                return;
            }

            if (updatePendingWhilePaused)
            {
                updatePendingWhilePaused = false;
                RequestPipelineUpdate();
            }

            if (autoPipelineSuspendedForReferenceRefresh)
            {
                if (heightRangeDirty || heightRangeNeedsUpdate)
                {
                    UpdateHeightRangeDisplay(forceRecalculate: true);
                    heightRangeDirty = false;
                }

                return;
            }

            if (hierarchyRefreshPendingWhilePaused)
            {
                hierarchyRefreshPendingWhilePaused = false;
                RequestHierarchyRefresh();
            }

            // Throttled palette check (rare, but must keep UI palettes consistent).
            double now = EditorApplication.timeSinceStartup;
            if (now - lastTerrainPaletteCheckTime >= TERRAIN_PALETTE_CHECK_INTERVAL_SECONDS)
            {
                lastTerrainPaletteCheckTime = now;
                ValidateAndResetReferences();
                var terrains = GetAllTerrains();
                if (terrains.Count > 0)
                {
                    CheckTerrainLayerPaletteUpdates(terrains);
                }
            }

            CheckForSplinePropertyChanges();

            // External edits support: scan terrain content on the existing interval, but only
            // rebuild when the observed terrain data actually changes.
            if (supportOutsideChanges && !updatesPaused && !autoPipelineSuspendedForReferenceRefresh)
            {
                double intervalSeconds = Mathf.Max(0.02f, updateInterval);
                if ((now - lastObservedTerrainScanTime) >= intervalSeconds)
                {
                    lastObservedTerrainScanTime = now;

                    if (HasActiveSplines())
                    {
                        var terrains = GetAllTerrains();
                        if (HaveObservedTerrainContentsChanged(terrains))
                        {
                            MarkPipelineDirty(baseline: true, preview: true, apply: true, heightRange: true);
                        }
                    }
                }
            }

            // Live update while actively editing a selected spline (fallback when Undo callbacks don't fire per-drag).
            if (TerraSplinesTool.IsSplineEditModeActive())
            {
                var selectedGO = UnityEditor.Selection.activeGameObject;
                var selectedContainer = selectedGO != null ? selectedGO.GetComponent<SplineContainer>() : null;
                if (selectedContainer != null &&
                    splineGroup != null &&
                    selectedContainer.transform != null &&
                    selectedContainer.transform.IsChildOf(splineGroup) &&
                    selectedContainer.gameObject.activeInHierarchy)
                {
                    int currentEditHash = TerraSplinesTool.GetSplineVersion(selectedContainer);
                    if (currentEditHash != lastSelectedSplineEditHash)
                    {
                        if (now - lastSelectedSplineEditUpdateTime >= SPLINE_EDIT_MIN_UPDATE_INTERVAL_SECONDS)
                        {
                            lastSelectedSplineEditUpdateTime = now;
                            lastSelectedSplineEditHash = currentEditHash;
                            MarkPipelineDirty(preview: true, apply: true, heightRange: true);
                        }
                    }
                }
            }

            if (baselineDirty || previewDirty || applyDirty || previewTexturesNeedUpdate || heightRangeDirty || heightRangeNeedsUpdate)
            {
                if (ShouldDeferExpensiveSplineOperations())
                {
                    return;
                }

                if (!baselineDirty && !previewDirty && !applyDirty && !previewTexturesNeedUpdate && !heightRangeDirty && heightRangeNeedsUpdate)
                {
                    UpdateHeightRangeDisplay(forceRecalculate: true);
                    return;
                }

                RequestPipelineUpdate();
            }

            return;
        }
        static int CombineTerrainContentHash(int current, int value)
        {
            unchecked
            {
                return (current * 397) ^ value;
            }
        }

        int ComputeTerrainContentHash(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
                return 0;

            var terrainData = terrain.terrainData;

            unchecked
            {
                int hash = 17;
                hash = CombineTerrainContentHash(hash, terrainData.heightmapResolution);
                hash = CombineTerrainContentHash(hash, terrainData.alphamapWidth);
                hash = CombineTerrainContentHash(hash, terrainData.alphamapHeight);
                hash = CombineTerrainContentHash(hash, terrainData.alphamapLayers);
                hash = CombineTerrainContentHash(hash, terrainData.detailWidth);
                hash = CombineTerrainContentHash(hash, terrainData.detailHeight);
                hash = CombineTerrainContentHash(hash, terrainData.detailPrototypes?.Length ?? 0);
                hash = CombineTerrainContentHash(hash, terrainData.treeInstanceCount);

                var heightmapTexture = terrainData.heightmapTexture;
                if (heightmapTexture != null)
                    hash = CombineTerrainContentHash(hash, heightmapTexture.imageContentsHash.GetHashCode());

                var holesTexture = terrainData.holesTexture;
                if (holesTexture != null)
                    hash = CombineTerrainContentHash(hash, holesTexture.imageContentsHash.GetHashCode());

                var alphamapTextures = terrainData.alphamapTextures;
                if (alphamapTextures != null)
                {
                    for (int i = 0; i < alphamapTextures.Length; i++)
                    {
                        var texture = alphamapTextures[i];
                        if (texture != null)
                            hash = CombineTerrainContentHash(hash, texture.imageContentsHash.GetHashCode());
                    }
                }

                return hash;
            }
        }

        bool HaveObservedTerrainContentsChanged(List<Terrain> terrains)
        {
            bool changed = false;

            if (terrains == null || terrains.Count == 0)
            {
                if (observedTerrainContentHashes.Count > 0)
                {
                    observedTerrainContentHashes.Clear();
                    return true;
                }

                return false;
            }

            var activeTerrainIds = new HashSet<EntityId>();
            for (int i = 0; i < terrains.Count; i++)
            {
                var terrain = terrains[i];
                if (terrain == null)
                    continue;

                EntityId terrainId = UnityObjectIdUtility.GetEntityId(terrain);
                activeTerrainIds.Add(terrainId);

                int currentHash = ComputeTerrainContentHash(terrain);
                if (!observedTerrainContentHashes.TryGetValue(terrainId, out int observedHash) || observedHash != currentHash)
                {
                    observedTerrainContentHashes[terrainId] = currentHash;
                    changed = true;
                }
            }

            if (observedTerrainContentHashes.Count != activeTerrainIds.Count)
            {
                var staleIds = observedTerrainContentHashes.Keys.Where(id => !activeTerrainIds.Contains(id)).ToArray();
                for (int i = 0; i < staleIds.Length; i++)
                {
                    observedTerrainContentHashes.Remove(staleIds[i]);
                    changed = true;
                }
            }

            return changed;
        }

        void SyncObservedTerrainContentHashes(List<Terrain> terrains = null)
        {
            terrains ??= GetAllTerrains();
            HaveObservedTerrainContentsChanged(terrains);
        }

        void CheckTerrainLayerPaletteUpdates(List<Terrain> terrains)
        {
            bool paletteChanged = false;

            int paintHash = ComputePaintLayerHash(terrains);
            if (paintHash != lastTerrainLayersHash)
            {
                lastTerrainLayersHash = paintHash;
                PopulateLayerPalette();
                paletteChanged = true;
            }

            int detailHash = ComputeDetailPrototypeHash(terrains);
            if (detailHash != lastDetailPrototypeHash)
            {
                lastDetailPrototypeHash = detailHash;
                PopulateDetailLayerPalette();
                paletteChanged = true;
            }

            if (paletteChanged)
            {
                RequestHierarchyRefresh();
            }
        }

        int ComputePaintLayerHash(List<Terrain> terrains)
        {
            unchecked
            {
                int hash = 17;
                foreach (var terrain in terrains)
                {
                    var layers = terrain?.terrainData?.terrainLayers;
                    if (layers == null)
                    {
                        hash = hash * 31 + 1;
                        continue;
                    }

                    hash = hash * 31 + layers.Length;
                    for (int i = 0; i < layers.Length; i++)
                    {
                        var layer = layers[i];
                        hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(layer);
                        hash = hash * 31 + (layer != null && layer.name != null ? layer.name.GetHashCode() : 0);
                    }
                }

                return hash;
            }
        }

        int ComputeDetailPrototypeHash(List<Terrain> terrains)
        {
            unchecked
            {
                int hash = 23;
                foreach (var terrain in terrains)
                {
                    var detailPrototypes = terrain?.terrainData?.detailPrototypes;
                    if (detailPrototypes == null)
                    {
                        hash = hash * 31 + 1;
                        continue;
                    }

                    hash = hash * 31 + detailPrototypes.Length;
                    for (int i = 0; i < detailPrototypes.Length; i++)
                    {
                        var proto = detailPrototypes[i];
                        hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(proto?.prototype);
                        hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(proto?.prototypeTexture);
                        hash = hash * 31 + (proto?.prototype != null && proto.prototype.name != null ? proto.prototype.name.GetHashCode() : 0);
                    }
                }

                return hash;
            }
        }

        void SyncSplineHierarchyForManualUpdate()
        {
            hierarchyRefreshPendingWhilePaused = false;
            hierarchyRefreshScheduled = false;

            ValidateAndResetReferences();
            UpdateTargetsWarning();

            int currentStructureHash = ComputeSplineGroupStructureHash();
            if (currentStructureHash == lastSplineGroupStructureHash)
                return;

            lastSplineGroupStructureHash = currentStructureHash;
            RefreshChildren();
        }

        void OnHierarchyChanged()
        {
            RequestHierarchyRefresh();
        }

        void RequestHierarchyRefresh()
        {
            lastHierarchyChangedTime = EditorApplication.timeSinceStartup;

            if (updatesPaused)
            {
                hierarchyRefreshPendingWhilePaused = true;
                return;
            }

            if (hierarchyRefreshScheduled) return;
            hierarchyRefreshScheduled = true;
            EditorApplication.delayCall += ProcessHierarchyRefreshDebounced;
        }

        void ProcessHierarchyRefreshDebounced()
        {
            if (this == null)
            {
                hierarchyRefreshScheduled = false;
                return;
            }

            if (IsInteractiveRefreshDeferred())
            {
                EditorApplication.delayCall += ProcessHierarchyRefreshDebounced;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            double requiredDebounce = ShouldUseDeferredHierarchyRefresh() ? HIERARCHY_REFRESH_DEBOUNCE_GIZMOS_OFF_SECONDS : HIERARCHY_REFRESH_DEBOUNCE_SECONDS;
            if (now - lastHierarchyChangedTime < requiredDebounce)
            {
                EditorApplication.delayCall += ProcessHierarchyRefreshDebounced;
                return;
            }

            if (ShouldBypassHierarchyRefreshWhileEditing())
            {
                EditorApplication.delayCall += ProcessHierarchyRefreshDebounced;
                return;
            }

            hierarchyRefreshScheduled = false;

            if (updatesPaused)
            {
                hierarchyRefreshPendingWhilePaused = true;
                return;
            }

            ValidateAndResetReferences();
            UpdateTargetsWarning();

            int currentStructureHash = ComputeSplineGroupStructureHash();
            if (currentStructureHash == lastSplineGroupStructureHash)
            {
                return;
            }

            lastSplineGroupStructureHash = currentStructureHash;
            RefreshChildren();
        }

        bool ShouldBypassHierarchyRefreshWhileEditing()
        {
            if (!ShouldUseDeferredHierarchyRefresh())
                return false;

            return ShouldDeferExpensiveSplineOperations();
        }

        bool ShouldDeferExpensiveSplineOperations()
        {
            if (!TerraSplinesTool.IsSplineEditModeActive())
                return false;

            if (!TryGetSelectedSplineInGroup(out _))
                return false;

            return EditorApplication.timeSinceStartup - lastSelectedSplineEditUpdateTime < SPLINE_EDIT_SETTLE_DELAY_SECONDS;
        }

        bool TryGetSelectedSplineInGroup(out SplineContainer selectedContainer)
        {
            selectedContainer = null;

            if (splineGroup == null)
                return false;

            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
                return false;

            selectedContainer = selectedObject.GetComponent<SplineContainer>();
            if (selectedContainer == null || selectedContainer.transform == null)
            {
                selectedContainer = null;
                return false;
            }

            if (!selectedContainer.transform.IsChildOf(splineGroup))
            {
                selectedContainer = null;
                return false;
            }

            return true;
        }

        bool ShouldUseDeferredHierarchyRefresh()
        {
            if (featureGizmosEnabled)
                return false;

            return TryGetSelectedSplineInGroup(out _);
        }

        int ComputeSplineGroupStructureHash()
        {
            unchecked
            {
                int hash = 17;

                if (splineGroup == null || splineGroup.gameObject == null)
                {
                    return hash;
                }

                hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(splineGroup);
                hash = HashSplineHierarchyRecursive(splineGroup, hash);

                return hash;
            }
        }

        int HashSplineHierarchyRecursive(Transform current, int hash)
        {
            if (current == null)
                return hash;

            unchecked
            {
                hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(current);
                hash = hash * 31 + current.GetSiblingIndex();
                hash = hash * 31 + current.childCount;
                hash = hash * 31 + (current.name != null ? current.name.GetHashCode() : 0);
                hash = hash * 31 + (current.gameObject.activeInHierarchy ? 1 : 0);
                hash = hash * 31 + (current.GetComponent<SplineContainer>() != null ? 1 : 0);

                for (int i = 0; i < current.childCount; i++)
                {
                    hash = HashSplineHierarchyRecursive(current.GetChild(i), hash);
                }

                return hash;
            }
        }

        void OnSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            // Recreate all strategies to ensure fresh state after scene changes
            TerraSplinesTool.RecreateStrategies();

            // Reset window state
            lastHadActiveSplines = false;
            baselineHeights = null;
            needsFreshBaseline = true;
            heightRangeNeedsUpdate = true;

            // Clear spline items to force refresh
            splineItems.Clear();
            RebuildListView();
            UpdateSplineCountLabel();

            // Validate and reset references if they became invalid
            ValidateAndResetReferences();

            // Update warning indicator after scene change
            UpdateTargetsWarning();
            RequestTerrainSplinesGroupsListRefresh();
        }

        void OnTerrainSplinesGroupsChanged()
        {
            RequestTerrainSplinesGroupsListRefresh();
        }

        void ValidateAndResetReferences()
        {
            bool referencesChanged = false;

            // Check if terrain group reference is still valid
            if (targetTerrainGroup != null && targetTerrainGroup.gameObject == null)
            {
                Debug.Log($"ValidateAndResetReferences - Clearing invalid terrain group");
                targetTerrainGroup = null;
                if (terrainField != null)
                {
                    terrainField.value = null;
                    terrainField.MarkDirtyRepaint();
                }

                referencesChanged = true;
            }
            
            // Legacy: Check if legacy terrain reference is still valid
            if (targetTerrain != null && targetTerrain.gameObject == null)
            {
                Debug.Log($"ValidateAndResetReferences - Clearing invalid legacy terrain: {targetTerrain.name}");
                targetTerrain = null;
                referencesChanged = true;
            }

            // Check if spline group reference is still valid
            if (splineGroup != null && splineGroup.gameObject == null)
            {
                splineGroup = null;
                if (splineGroupField != null)
                {
                    splineGroupField.value = null;
                    splineGroupField.MarkDirtyRepaint();
                }

                referencesChanged = true;
            }

            // Reset UI state when references become invalid
            var terrains = GetAllTerrains();
            if (terrains.Count == 0)
            {
                baselineHeights = null;
                workingHeights = null;
                originalHeights = null;
                needsFreshBaseline = true;
                heightRangeNeedsUpdate = true;
                ClearUndoRedoStacks();
            }

            // If references changed, force UI refresh
            if (referencesChanged)
            {
                RefreshUIAfterReferenceChange();
            }

            // Update warning indicator based on current reference state
            UpdateTargetsWarning();
        }

        void RefreshUIAfterReferenceChange()
        {
            // Force refresh of ObjectField elements
            if (terrainField != null)
            {
                terrainField.MarkDirtyRepaint();
            }

            if (splineGroupField != null)
            {
                splineGroupField.MarkDirtyRepaint();
            }

            // Update UI state (but don't call ValidateAndResetReferences here to avoid clearing just-assigned references)
            UpdateUIStateWithoutValidation();
            RefreshTerrainSplinesGroupsList();

            // Force repaint of the entire window
            Repaint();
        }

        void RequestTerrainSplinesGroupsListRefresh()
        {
            if (terrainSplinesGroupsListContainer == null || terrainSplinesGroupsListRefreshScheduled)
                return;

            terrainSplinesGroupsListRefreshScheduled = true;
            EditorApplication.delayCall += PerformTerrainSplinesGroupsListRefresh;
        }

        void PerformTerrainSplinesGroupsListRefresh()
        {
            terrainSplinesGroupsListRefreshScheduled = false;

            if (this == null)
                return;

            RefreshTerrainSplinesGroupsList();
        }

        void RequestListViewRefresh()
        {
            if (splinesListView == null || listRefreshScheduled)
                return;

            listRefreshScheduled = true;
            EditorApplication.delayCall += PerformListViewRefresh;
        }

        void PerformListViewRefresh()
        {
            listRefreshScheduled = false;

            if (this == null || splinesListView == null)
                return;

            splinesListView.RefreshItems();
            splinesListView.MarkDirtyRepaint();
        }

        void RunWithSuppressedTerrainChangeNotifications(System.Action action)
        {
            if (action == null)
                return;

            internalTerrainChangeSuppressionCount++;
            try
            {
                action();
            }
            finally
            {
                internalTerrainChangeSuppressionCount--;
                internalTerrainChangeIgnoreUntil = Math.Max(internalTerrainChangeIgnoreUntil, EditorApplication.timeSinceStartup + INTERNAL_TERRAIN_CHANGE_IGNORE_SECONDS);
            }
        }

        void RunWithSuppressedSplineChangeNotifications(System.Action action)
        {
            if (action == null)
                return;

            internalSplineChangeSuppressionCount++;
            try
            {
                action();
            }
            finally
            {
                internalSplineChangeSuppressionCount--;
            }
        }

        void UpdateVisibleSplineRowFeatureStates()
        {
            if (boundSplineRowElements.Count == 0)
                return;

            var staleElements = new List<VisualElement>();
            foreach (var element in boundSplineRowElements)
            {
                if (element == null || element.panel == null)
                {
                    staleElements.Add(element);
                    continue;
                }

                if (boundSplineRowFeatureRefreshers.TryGetValue(element, out var refresher))
                {
                    refresher?.Invoke();
                }
            }

            for (int i = 0; i < staleElements.Count; i++)
            {
                boundSplineRowElements.Remove(staleElements[i]);
                boundSplineRowFeatureRefreshers.Remove(staleElements[i]);
            }

            splinesListView?.MarkDirtyRepaint();
        }

        void AutoDetectTerrainAndSplineGroup()
        {
            TryAutoDetectTerrainAndSplineGroup(out var detectedTerrainGroup, out var detectedSplineGroup);
            ApplyTargetReferences(detectedTerrainGroup, detectedSplineGroup);
        }

        void RefreshTerrainSplinesGroupsList()
        {
            if (terrainSplinesGroupsListContainer == null)
                return;

            terrainSplinesGroupsListContainer.Clear();

            var groups = GetSceneTerrainSplinesGroups();
            if (terrainSplinesGroupsFoldout != null)
                terrainSplinesGroupsFoldout.style.display = groups.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (groups.Count == 0)
            {
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                bool isActive = IsActiveTerrainSplinesGroup(group);

                var row = new VisualElement();
                row.AddToClassList("targets-group-row");
                if (isActive)
                    row.AddToClassList("targets-group-row-active");
                if (!group.IsValid)
                    row.AddToClassList("targets-group-row-invalid");

                var groupField = new ObjectField
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true,
                    tooltip = BuildTerrainSplinesGroupTooltip(group)
                };
                groupField.AddToClassList("targets-group-object-field");
                groupField.SetValueWithoutNotify(group.gameObject);
                groupField.SetEnabled(false);
                row.Add(groupField);

                var spacer = new VisualElement();
                spacer.AddToClassList("targets-group-row-spacer");
                row.Add(spacer);

                var activateButton = new Button(() => SetActiveTerrainSplinesGroup(group))
                {
                    text = isActive ? "Active" : "Set Active",
                    tooltip = isActive
                        ? "This TerrainSplinesGroup is currently assigned to the window."
                        : "Assign this TerrainSplinesGroup as the active terrain and spline target set."
                };
                activateButton.AddToClassList("targets-group-activate-button");
                if (isActive)
                    activateButton.AddToClassList("targets-group-activate-button-active");
                activateButton.SetEnabled(group.IsValid && !isActive);
                row.Add(activateButton);

                var deleteButton = new Button(() => DeleteTerrainSplinesGroup(group))
                {
                    tooltip = "Delete this TerrainSplinesGroup component."
                };
                deleteButton.AddToClassList("targets-group-delete-button");

                var deleteIcon = new Image
                {
                    image = LoadSplineIcon("delete"),
                    scaleMode = ScaleMode.ScaleToFit
                };
                deleteIcon.AddToClassList("targets-group-delete-icon");
                deleteButton.Add(deleteIcon);
                row.Add(deleteButton);

                terrainSplinesGroupsListContainer.Add(row);
            }
        }

        List<TerrainSplinesGroup> GetSceneTerrainSplinesGroups()
        {
            var allGroups = Resources.FindObjectsOfTypeAll<TerrainSplinesGroup>();
            var sceneGroups = new List<TerrainSplinesGroup>(allGroups.Length);

            for (int i = 0; i < allGroups.Length; i++)
            {
                var group = allGroups[i];
                if (group == null || EditorUtility.IsPersistent(group))
                    continue;

                var groupObject = group.gameObject;
                if (groupObject == null)
                    continue;

                var scene = groupObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                sceneGroups.Add(group);
            }

            sceneGroups.Sort((left, right) => string.CompareOrdinal(GetSceneHierarchyPath(left.transform), GetSceneHierarchyPath(right.transform)));
            return sceneGroups;
        }

        string BuildTerrainSplinesGroupTooltip(TerrainSplinesGroup group)
        {
            if (group == null)
                return "Missing TerrainSplinesGroup reference.";

            string terrainName = group.TerrainGroup != null ? group.TerrainGroup.name : "Missing";
            string splineName = group.SplineGroup != null ? group.SplineGroup.name : "Missing";
            return $"{GetSceneHierarchyPath(group.transform)}\nTerrain Group: {terrainName}\nSpline Group: {splineName}";
        }

        static string GetSceneHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "Unknown";

            var pathParts = new List<string>();
            var current = transform;
            while (current != null)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts);
        }

        bool IsActiveTerrainSplinesGroup(TerrainSplinesGroup group)
        {
            return group != null && group.TerrainGroup == targetTerrainGroup && group.SplineGroup == splineGroup;
        }

        void SetActiveTerrainSplinesGroup(TerrainSplinesGroup group)
        {
            if (group == null || !group.IsValid)
                return;

            ApplyTargetReferences(group.TerrainGroup, group.SplineGroup);
        }

        void DeleteTerrainSplinesGroup(TerrainSplinesGroup group)
        {
            if (group == null)
                return;

            Undo.DestroyObjectImmediate(group);
        }

        bool TryAutoDetectTerrainAndSplineGroup(out GameObject detectedTerrainGroup, out Transform detectedSplineGroup)
        {
            detectedTerrainGroup = targetTerrainGroup;
            detectedSplineGroup = splineGroup;

            if (detectedTerrainGroup == null || detectedSplineGroup == null)
            {
                var firstValidGroup = GetSceneTerrainSplinesGroups().FirstOrDefault(group => group != null && group.IsValid);
                if (firstValidGroup != null)
                {
                    if (detectedTerrainGroup == null)
                    {
                        detectedTerrainGroup = firstValidGroup.TerrainGroup;
                    }

                    if (detectedSplineGroup == null)
                    {
                        detectedSplineGroup = firstValidGroup.SplineGroup;
                    }
                }
            }

            // Auto-detect terrain group if not set
            if (detectedTerrainGroup == null)
            {
                // First try to migrate from legacy targetTerrain
                if (targetTerrain != null)
                {
                    if (targetTerrain.transform.parent != null)
                    {
                        detectedTerrainGroup = targetTerrain.transform.parent.gameObject;
                    }
                    else
                    {
                        var groupGO = new GameObject("Terrain Group");
                        targetTerrain.transform.SetParent(groupGO.transform);
                        detectedTerrainGroup = groupGO;
                    }
                }
                else
                {
                    // Find first Terrain and use its parent or create a group
                    var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
                    if (terrain != null)
                    {
                        if (terrain.transform.parent != null)
                        {
                            detectedTerrainGroup = terrain.transform.parent.gameObject;
                        }
                        else
                        {
                            var groupGO = new GameObject("Terrain Group");
                            terrain.transform.SetParent(groupGO.transform);
                            detectedTerrainGroup = groupGO;
                        }
                    }
                }
            }

            return detectedTerrainGroup != null || detectedSplineGroup != null;
        }

        void ApplyTargetReferences(GameObject newTerrainGroup, Transform newSplineGroup, bool updatePreviewPipeline = true)
        {
            bool terrainChanged = targetTerrainGroup != newTerrainGroup || targetTerrain != null;
            bool splineChanged = splineGroup != newSplineGroup;

            targetTerrainGroup = newTerrainGroup;
            if (targetTerrain != null)
            {
                targetTerrain = null;
            }

            splineGroup = newSplineGroup != null && newSplineGroup.gameObject != null ? newSplineGroup : null;

            if (terrainField != null)
                terrainField.SetValueWithoutNotify(targetTerrainGroup);
            if (splineGroupField != null)
                splineGroupField.SetValueWithoutNotify(splineGroup);

            if (terrainChanged)
            {
                lastHadActiveSplines = false;
                baselineHeights = null;
                needsFreshBaseline = true;
                ClearUndoRedoStacks();
                heightRangeNeedsUpdate = true;

                var terrains = GetAllTerrains();
                if (terrains.Count > 0)
                {
                    terrainStateManager.SetTerrains(terrains);
                    EnsureBaseline();

                    if (terrainStateManager.BaselineHeights != null)
                    {
                        baselineHeights = terrainStateManager.BaselineHeights;
                        workingHeights = terrainStateManager.WorkingHeights;
                    }

                    UpdateCombinedBaselineTexture();
                }
                else
                {
                    terrainStateManager.SetTerrains(new List<Terrain>());
                }

                PopulateLayerPalette();
                PopulateDetailLayerPalette();
                PopulateTreePrototypePalette();
            }

            if (splineChanged)
            {
                if (updatePreviewPipeline)
                {
                    RefreshChildren(true);
                }
                else
                {
                    RunWithSuppressedRefreshPreviewRequests(() => RefreshChildren(false));
                }

                if (splinesListView != null)
                    splinesListView.MarkDirtyRepaint();

                if (updatePreviewPipeline)
                {
                    lastHadActiveSplines = false;
                    var terrains = GetAllTerrains();
                    RunWithSuppressedTerrainChangeNotifications(() =>
                    {
                        foreach (var terrain in terrains)
                        {
                            var state = terrainStateManager?.TerrainStates?.GetValueOrDefault(terrain);
                            if (state != null && state.BaselineHeights != null)
                            {
                                TerraSplinesTool.ApplyPreviewToTerrain(terrain, state.BaselineHeights);
                                isPreviewAppliedToTerrain = false;
                            }
                        }
                    });

                    if (baselineHeights != null)
                    {
                        workingHeights = TerraSplinesTool.CopyHeights(baselineHeights);
                    }

                    heightRangeNeedsUpdate = true;
                }
            }

            if (!updatePreviewPipeline)
            {
                SuspendAutoPipelineForReferenceRefresh();
                UpdateHeightRangeDisplay(forceRecalculate: true);
                SyncObservedTerrainContentHashes();
            }
            else
            {
                ResumeAutoPipeline();
                SyncObservedTerrainContentHashes();
            }

            RefreshUIAfterReferenceChange();
            UpdateTargetActionButtons();
        }

        public Transform GetOrCreateSplineGroup()
        {
            if (splineGroup != null && splineGroup.gameObject != null)
                return splineGroup;

            var groupObject = new GameObject("Spline Group");
            Undo.RegisterCreatedObjectUndo(groupObject, "Create Spline Group");
            splineGroup = groupObject.transform;

            if (splineGroupField != null)
            {
                splineGroupField.value = splineGroup;
                splineGroupField.MarkDirtyRepaint();
            }

            RefreshChildren();
            return splineGroup;
        }

        public Transform GetSplineGroup()
        {
            if (splineGroup == null || splineGroup.gameObject == null)
                return null;

            return splineGroup;
        }

        public void SetSplineGroupExternal(Transform group, bool refreshList = true)
        {
            splineGroup = group != null && group.gameObject != null ? group : null;

            if (splineGroupField != null)
            {
                splineGroupField.value = splineGroup;
                splineGroupField.MarkDirtyRepaint();
            }

            if (refreshList)
                RefreshChildren();
        }

        void RestoreBaselineIfAny()
        {
            // Restore baseline for all terrains
            var terrains = GetAllTerrains();
            foreach (var terrain in terrains)
            {
                if (terrain == null) continue;
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
        }

        string GetHierarchyPath(Transform splineTransform, Transform rootTransform)
        {
            if (splineTransform == null || rootTransform == null) return "Unknown";

            var pathParts = new List<string>();
            Transform current = splineTransform;

            // Walk up the hierarchy until we reach the root spline group
            while (current != null && current != rootTransform)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            // Reverse the list to get the correct order (root to leaf)
            pathParts.Reverse();

            // Join with "/" separator
            return string.Join("/", pathParts);
        }

        (float min, float max) GetHeightmapRange(float[,] heights, Terrain terrain)
        {
            if (heights == null || terrain == null) return (0, 0);

            int res = heights.GetLength(0);
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = heights[z, x];
                    if (h < min) min = h;
                    if (h > max) max = h;
                }
            }

            // Convert normalized heights to meters
            float terrainHeight = terrain.terrainData.size.y;
            return (min * terrainHeight, max * terrainHeight);
        }

        void CheckForSplinePropertyChanges()
        {
            if (updatesPaused) return;

            // Throttle: only check every PROPERTY_CHECK_INTERVAL seconds
            if ((DateTime.Now - lastPropertyCheckTime).TotalSeconds < PROPERTY_CHECK_INTERVAL)
            {
                return;
            }

            lastPropertyCheckTime = DateTime.Now;

            // Check warning indicator periodically (even if splineGroup is null)
            if (splinesListView != null)
            {
                UpdateTargetsWarning();
            }

            // Only check spline properties if ListView and splineGroup exist
            if (splinesListView == null || splineGroup == null) return;

            bool needsRefresh = false;
            var changedTopLevelItemIndices = new HashSet<int>();

            // Get all spline items
            var allSplines = GetAllSplineItems();

            foreach (var splineItem in allSplines)
            {
                if (splineItem.container == null) continue;

                // Track only structural property changes (knot count / closed state), not continuous knot drags.
                int currentVersion = GetSplinePropertyVersion(splineItem.container);

                // Check if we've seen this version before
                if (lastSplineVersions.TryGetValue(splineItem.container, out int lastVersion))
                {
                    if (currentVersion != lastVersion)
                    {
                        // Version changed - spline property was modified
                        needsRefresh = true;
                        lastSplineVersions[splineItem.container] = currentVersion;
                        int itemIndex = GetTopLevelListIndexForSplineItem(splineItem);
                        if (itemIndex >= 0)
                        {
                            changedTopLevelItemIndices.Add(itemIndex);
                        }
                        // Clear preview texture cache key so preview will regenerate for structural changes
                        previewTextureCacheKeys.Remove(splineItem.container);
                    }
                }
                else
                {
                    // First time seeing this spline - record its version
                    lastSplineVersions[splineItem.container] = currentVersion;
                }
            }

            // Remove entries for splines that no longer exist
            var containersToRemove = new List<SplineContainer>();
            foreach (var kvp in lastSplineVersions)
            {
                if (kvp.Key == null || !allSplines.Any(s => s.container == kvp.Key))
                {
                    containersToRemove.Add(kvp.Key);
                }
            }

            foreach (var container in containersToRemove)
            {
                lastSplineVersions.Remove(container);
                // Also clean up preview texture cache and settings tracking
                previewTextureCacheKeys.Remove(container);
                lastSplinePreviewSettingsHashes.Remove(container);
            }

            // Refresh ListView if needed
            if (needsRefresh && splinesListView != null)
            {
                RefreshTopLevelListItems(changedTopLevelItemIndices);
            }
        }

        int GetSplinePropertyVersion(SplineContainer container)
        {
            if (container == null) return 0;

            unchecked
            {
                int version = 17;
                foreach (var spline in container.Splines)
                {
                    version = version * 31 + spline.Count;
                    version = version * 31 + (spline.Closed ? 1 : 0);
                }
                return version;
            }
        }
    }
}
