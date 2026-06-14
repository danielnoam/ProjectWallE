using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using sc.terrain.vegetationspawner;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Systems.PrefabScatter
{
    /// <summary>
    /// Scatters prefab GameObjects (rocks, bushes, props) across terrains based on terrain layer masks,
    /// density, height, slope and curvature. Each item spawns real prefab instances under its own parent.
    /// </summary>
    [AddComponentMenu("DNExtensions/Terrain/Prefab Scatter")]
    public class PrefabScatter : MonoBehaviour
    {
        [Serializable]
        public class ScatterLayerMask
        {
            public int layerIndex;
            [Range(0f, 1f), Tooltip("Minimum painted strength the terrain layer must have for this item to spawn")]
            public float threshold = 0.5f;
        }

        [Serializable]
        public class ScatterPrefab
        {
            public GameObject prefab;

            [MinMaxRange(0f, 5f), Tooltip("Random horizontal scale (applied to X and Z)")]
            public RangedFloat width = new RangedFloat(1f, 1f);
            [MinMaxRange(0f, 5f), Tooltip("Random vertical scale (applied to Y)")]
            public RangedFloat length = new RangedFloat(1f, 1f);
            [MinMaxRange(-360f, 360f), Tooltip("Random rotation range in degrees around the X axis (pitch)")]
            public RangedFloat rotationX = new RangedFloat(0f, 0f);
            [MinMaxRange(-360f, 360f), Tooltip("Random rotation range in degrees around the Y axis (yaw)")]
            public RangedFloat rotationY = new RangedFloat(0f, 360f);
            [MinMaxRange(-360f, 360f), Tooltip("Random rotation range in degrees around the Z axis (roll)")]
            public RangedFloat rotationZ = new RangedFloat(0f, 0f);
            [Range(0f, 1f), Tooltip("Blends the up axis from world-up (0) towards the terrain normal (1)")]
            public float alignToGround;
            [Tooltip("Vertical offset from the terrain surface, useful for sinking or raising instances")]
            public float surfaceOffset;

            [NonSerialized] public bool foldout = true;

            public static ScatterPrefab Clone(ScatterPrefab source)
            {
                return new ScatterPrefab
                {
                    prefab = source.prefab,
                    width = source.width,
                    length = source.length,
                    rotationX = source.rotationX,
                    rotationY = source.rotationY,
                    rotationZ = source.rotationZ,
                    alignToGround = source.alignToGround,
                    surfaceOffset = source.surfaceOffset
                };
            }
        }

        [Serializable]
        public class ScatterItem
        {
            public bool enabled = true;
            public string name = "Scatter Item";
            [Tooltip("Prefabs to scatter. One is picked at random for each spawn point, using its own transform settings")]
            public List<ScatterPrefab> prefabs = new List<ScatterPrefab>();
            [Tooltip("Parent that spawned instances are placed under. Its existing children are cleared on respawn. If empty, a child object is created on the spawner")]
            public Transform parent;

            public int seed;
            [Range(0f, 100f), Tooltip("Per-point chance to spawn, used to thin out the distribution")]
            public float spawnChance = 100f;
            [Min(0.5f), Tooltip("Minimum distance between instances. Lower values spawn denser")]
            public float density = 5f;
            [Min(0), Tooltip("Maximum number of instances to spawn across all terrains (0 = unlimited)")]
            public int maxInstances = 1000;

            [MinMaxRange(-100f, 2000f), Tooltip("World height range the item can spawn within")]
            public RangedFloat heightRange = new RangedFloat(-100f, 2000f);
            [MinMaxRange(0f, 90f), Tooltip("Slope range in degrees the item can spawn within")]
            public RangedFloat slopeRange = new RangedFloat(0f, 45f);
            [MinMaxRange(0f, 1f), Tooltip("0 = concave (bowl), 0.5 = flat, 1 = convex (edge)")]
            public RangedFloat curvatureRange = new RangedFloat(0f, 1f);

            [Tooltip("Skip points that overlap a collider on the selected layers")]
            public bool collisionCheck;
            [Tooltip("Layers treated as obstacles. Do not include the terrain's own layer")]
            public LayerMask collisionLayers;
            [Min(0f), Tooltip("Overlap sphere radius used for the collision check")]
            public float collisionRadius = 1f;

            public List<ScatterLayerMask> layerMasks = new List<ScatterLayerMask>();

            [NonSerialized] public int instanceCount;
            [NonSerialized] public bool foldout = true;

            public static ScatterItem Duplicate(ScatterItem source)
            {
                ScatterItem copy = new ScatterItem
                {
                    enabled = source.enabled,
                    name = source.name + " Copy",
                    parent = source.parent,
                    seed = source.seed,
                    spawnChance = source.spawnChance,
                    density = source.density,
                    maxInstances = source.maxInstances,
                    heightRange = source.heightRange,
                    slopeRange = source.slopeRange,
                    curvatureRange = source.curvatureRange,
                    collisionCheck = source.collisionCheck,
                    collisionLayers = source.collisionLayers,
                    collisionRadius = source.collisionRadius
                };

                foreach (ScatterPrefab prefab in source.prefabs)
                {
                    copy.prefabs.Add(ScatterPrefab.Clone(prefab));
                }

                foreach (ScatterLayerMask mask in source.layerMasks)
                {
                    copy.layerMasks.Add(new ScatterLayerMask { layerIndex = mask.layerIndex, threshold = mask.threshold });
                }

                return copy;
            }
        }

        public List<Terrain> terrains = new List<Terrain>();
        public List<ScatterItem> items = new List<ScatterItem>();

        public void SpawnAll()
        {
            if (terrains == null || terrains.Count == 0) return;

            foreach (ScatterItem item in items)
            {
                SpawnItem(item);
            }
        }

        public void ClearAll()
        {
            foreach (ScatterItem item in items)
            {
                ClearItem(item);
            }
        }

        public void SpawnItem(ScatterItem item)
        {
            ClearItem(item);

            item.instanceCount = 0;

            if (!item.enabled) return;
            if (terrains == null || terrains.Count == 0) return;

            List<ScatterPrefab> validPrefabs = item.prefabs.FindAll(p => p != null && p.prefab);
            if (validPrefabs.Count == 0) return;

            Transform container = GetContainer(item);

            foreach (Terrain terrain in terrains)
            {
                if (!terrain) continue;
                if (item.maxInstances > 0 && item.instanceCount >= item.maxInstances) break;

                SpawnItemOnTerrain(terrain, item, container, validPrefabs);
            }

            MarkDirty();
        }

        public void ClearItem(ScatterItem item)
        {
            Transform container = item.parent ? item.parent : transform.Find(item.name);
            if (!container) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            item.instanceCount = 0;
            MarkDirty();
        }

        private void SpawnItemOnTerrain(Terrain terrain, ScatterItem item, Transform container, List<ScatterPrefab> validPrefabs)
        {
            int itemSeed = item.seed;
            List<Vector3> spawnPoints = PoissonDisc.GetSpawnpoints(terrain, Mathf.Max(0.5f, item.density), itemSeed);

            foreach (Vector3 point in spawnPoints)
            {
                if (item.maxInstances > 0 && item.instanceCount >= item.maxInstances) return;

                Vector2 normalizedPos = terrain.GetNormalizedPosition(point);

                int pointSeed = itemSeed ^ (Mathf.RoundToInt(point.x * 100f) * 73856093) ^ (Mathf.RoundToInt(point.z * 100f) * 19349663);
                Random.InitState(pointSeed);

                if (Random.value * 100f > item.spawnChance) continue;

                terrain.SampleHeight(normalizedPos, out _, out float worldHeight, out _);
                if (worldHeight < item.heightRange.minValue || worldHeight > item.heightRange.maxValue) continue;

                float slope = terrain.GetSlope(normalizedPos);
                if (slope < item.slopeRange.minValue || slope > item.slopeRange.maxValue) continue;

                if (item.curvatureRange.minValue > 0f || item.curvatureRange.maxValue < 1f)
                {
                    float curvature = TerrainSampler.ConvexityToCurvature(terrain.SampleConvexity(normalizedPos));
                    if (curvature < item.curvatureRange.minValue || curvature > item.curvatureRange.maxValue) continue;
                }

                if (!PassesLayerMask(terrain, normalizedPos, item)) continue;

                ScatterPrefab chosen = validPrefabs[Random.Range(0, validPrefabs.Count)];

                Vector3 position = point;
                position.y = worldHeight + chosen.surfaceOffset;

                if (item.collisionCheck && Physics.CheckSphere(position, item.collisionRadius, item.collisionLayers, QueryTriggerInteraction.Ignore)) continue;

                Vector3 up = Vector3.up;
                if (chosen.alignToGround > 0f)
                {
                    Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y);
                    up = Vector3.Slerp(Vector3.up, normal, chosen.alignToGround);
                }

                Vector3 randomEuler = new Vector3(chosen.rotationX.RandomValue, chosen.rotationY.RandomValue, chosen.rotationZ.RandomValue);
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(randomEuler);

                float horizontal = chosen.width.RandomValue;
                float vertical = chosen.length.RandomValue;

                GameObject instance = InstantiatePrefab(chosen.prefab, container);
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.transform.localScale = new Vector3(horizontal, vertical, horizontal);

                item.instanceCount++;
            }
        }

        private static bool PassesLayerMask(Terrain terrain, Vector2 normalizedPos, ScatterItem item)
        {
            if (item.layerMasks.Count == 0) return true;

            Vector2Int texel = terrain.SplatmapTexelIndex(normalizedPos);
            int alphamapCount = terrain.terrainData.alphamapTextureCount;

            float strength = 0f;
            foreach (ScatterLayerMask mask in item.layerMasks)
            {
                int mapIndex = mask.layerIndex / 4;
                if (mapIndex >= alphamapCount) continue;

                Color splat = terrain.terrainData.GetAlphamapTexture(mapIndex).GetPixel(texel.x, texel.y);
                float value = splat[mask.layerIndex % 4];

                if (value > 0f) strength += Mathf.Clamp01(value - mask.threshold);
            }

            return strength > 0f;
        }

        private Transform GetContainer(ScatterItem item)
        {
            if (item.parent) return item.parent;

            Transform container = transform.Find(item.name);
            if (container) return container;

            container = new GameObject(item.name).transform;
            container.SetParent(transform, false);
            return container;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
#endif
            return Instantiate(prefab, parent);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}
