using System.Collections.Generic;
using UnityEngine;

namespace GruelTerraSplines
{
    public class TreeOperationApplier : OperationApplierBase
    {
        static int PositiveModulo(int value, int modulus)
        {
            if (modulus <= 0) return 0;
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        static int StableHash(int a, int b, int c = 0)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + c;
                return hash;
            }
        }

        static float Random01FromHash(int hash)
        {
            return PositiveModulo(hash, 1000000) / 999999f;
        }

        static float Wrap01(float t) => t - Mathf.Floor(t);

        static float RemapTreeStrength(float treeStrength)
        {
            float normalizedStrength = Mathf.Clamp01(treeStrength);
            AnimationCurve responseCurve = TerraSplinesCurves.GetTreeStrengthResponseClone();
            if (responseCurve == null)
            {
                return normalizedStrength;
            }

            return Mathf.Clamp01(responseCurve.Evaluate(normalizedStrength));
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }

        static float SampleValueNoise01(float x, float z, int seed)
        {
            int minX = Mathf.FloorToInt(x);
            int minZ = Mathf.FloorToInt(z);
            float fracX = Smooth01(x - minX);
            float fracZ = Smooth01(z - minZ);

            float v00 = Random01FromHash(StableHash(seed, minX, minZ));
            float v10 = Random01FromHash(StableHash(seed, minX + 1, minZ));
            float v01 = Random01FromHash(StableHash(seed, minX, minZ + 1));
            float v11 = Random01FromHash(StableHash(seed, minX + 1, minZ + 1));

            float row0 = Mathf.Lerp(v00, v10, fracX);
            float row1 = Mathf.Lerp(v01, v11, fracX);
            return Mathf.Lerp(row0, row1, fracZ);
        }

        static float EvaluateClusterFactor(float worldX, float worldZ, float strengthNormalized, int seed)
        {
            float largePatchMeters = Mathf.Lerp(42f, 18f, Mathf.Sqrt(strengthNormalized));
            float mediumPatchMeters = largePatchMeters * 0.42f;

            float large = SampleValueNoise01(worldX / Mathf.Max(1f, largePatchMeters), worldZ / Mathf.Max(1f, largePatchMeters), StableHash(seed, 101, 131));
            float medium = SampleValueNoise01(worldX / Mathf.Max(1f, mediumPatchMeters), worldZ / Mathf.Max(1f, mediumPatchMeters), StableHash(seed, 151, 181));
            float combined = Mathf.Clamp01((large * 0.72f) + (medium * 0.28f));

            float patchBias = Mathf.Lerp(0.6f, 0.45f, Mathf.Sqrt(strengthNormalized));
            float clustered = Mathf.SmoothStep(patchBias, 0.92f, combined);
            return Mathf.Lerp(0.18f, 1.85f, clustered);
        }

        static Vector2 GetWorldXZ(Terrain terrain, float normalizedX, float normalizedZ)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            return new Vector2(
                terrainPosition.x + (normalizedX * terrainSize.x),
                terrainPosition.z + (normalizedZ * terrainSize.z));
        }

        static Vector2Int GetSpatialCell(Vector2 worldXZ, float cellSizeMeters)
        {
            float safeCellSize = Mathf.Max(0.0001f, cellSizeMeters);
            return new Vector2Int(
                Mathf.FloorToInt(worldXZ.x / safeCellSize),
                Mathf.FloorToInt(worldXZ.y / safeCellSize));
        }

        static Dictionary<Vector2Int, List<Vector2>> BuildTreeSpatialHash(List<TreeInstance> treeInstances, Terrain terrain, float cellSizeMeters)
        {
            var spatialHash = new Dictionary<Vector2Int, List<Vector2>>();
            if (treeInstances == null || terrain == null || terrain.terrainData == null)
                return spatialHash;

            for (int i = 0; i < treeInstances.Count; i++)
            {
                AddTreeToSpatialHash(spatialHash, treeInstances[i].position.x, treeInstances[i].position.z, terrain, cellSizeMeters);
            }

            return spatialHash;
        }

        static void AddTreeToSpatialHash(
            Dictionary<Vector2Int, List<Vector2>> spatialHash,
            float normalizedX,
            float normalizedZ,
            Terrain terrain,
            float cellSizeMeters)
        {
            if (spatialHash == null || terrain == null || terrain.terrainData == null)
                return;

            Vector2 worldXZ = GetWorldXZ(terrain, normalizedX, normalizedZ);
            Vector2Int cell = GetSpatialCell(worldXZ, cellSizeMeters);
            if (!spatialHash.TryGetValue(cell, out List<Vector2> bucket))
            {
                bucket = new List<Vector2>();
                spatialHash[cell] = bucket;
            }

            bucket.Add(worldXZ);
        }

        static float EvaluateTreeHeightMultiplier(AnimationCurve curve, int hash)
        {
            float random01 = Random01FromHash(hash);
            if (curve == null)
            {
                return Mathf.Max(0.1f, Mathf.Lerp(0.8f, 1.2f, random01));
            }

            return Mathf.Max(0.1f, curve.Evaluate(random01));
        }

        static float EvaluateTreeRotation(float normalizedX, float normalizedZ, int prototypeIndex, int regionSeed)
        {
            int rotationSeedX = Mathf.RoundToInt(normalizedX * 1000000f);
            int rotationSeedZ = Mathf.RoundToInt(normalizedZ * 1000000f);

            float primary = Random01FromHash(
                StableHash(rotationSeedX, rotationSeedZ, StableHash(regionSeed, prototypeIndex, 53)));

            float secondary = Random01FromHash(
                StableHash(rotationSeedX * 3 + 17, rotationSeedZ * 5 + 29, StableHash(prototypeIndex, regionSeed, 97)));

            float blended = Mathf.Repeat(primary + (secondary * 0.6180339f), 1f);
            return blended * Mathf.PI * 2f;
        }

        static float SampleHeight01(Terrain terrain, float[,] heights, float normalizedX, float normalizedZ)
        {
            if (terrain == null || terrain.terrainData == null)
                return 0f;

            if (heights != null)
            {
                int height = heights.GetLength(0);
                int width = heights.GetLength(1);
                int sampleX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * Mathf.Max(1, width - 1)), 0, Mathf.Max(0, width - 1));
                int sampleZ = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * Mathf.Max(1, height - 1)), 0, Mathf.Max(0, height - 1));
                return Mathf.Clamp01(heights[sampleZ, sampleX]);
            }

            float worldY = terrain.SampleHeight(new Vector3(
                terrain.transform.position.x + normalizedX * terrain.terrainData.size.x,
                0f,
                terrain.transform.position.z + normalizedZ * terrain.terrainData.size.z));

            return terrain.terrainData.size.y > 0f ? Mathf.Clamp01(worldY / terrain.terrainData.size.y) : 0f;
        }

        static float SampleTreeNoise(
            TreeNoisePrototypeSettings noise,
            float normalizedX,
            float normalizedZ,
            Terrain terrain)
        {
            if (noise == null || noise.noiseTexture == null || terrain == null || terrain.terrainData == null)
                return 1f;

            var readableNoise = GetReadableTexture(noise.noiseTexture);
            if (readableNoise == null)
                return 1f;

            bool useAlpha = ShouldUseAlpha(readableNoise);
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedZ = Mathf.Clamp01(normalizedZ);
            float worldX = terrain.transform.position.x + normalizedX * terrain.terrainData.size.x;
            float worldZ = terrain.transform.position.z + normalizedZ * terrain.terrainData.size.z;

            float worldSize = Mathf.Max(0.001f, noise.noiseWorldSizeMeters);
            float u = Wrap01((worldX / worldSize) + noise.noiseOffset.x);
            float v = Wrap01((worldZ / worldSize) + noise.noiseOffset.y);
            Color sample = readableNoise.GetPixelBilinear(u, v);
            float value = useAlpha ? sample.a : sample.grayscale;
            if (noise.noiseInvert) value = 1f - value;
            if (noise.noiseThreshold > 0f)
            {
                value = Mathf.Clamp01(Mathf.InverseLerp(noise.noiseThreshold, 1f, value));
            }

            return Mathf.Lerp(1f, value, Mathf.Clamp01(noise.noiseStrength));
        }

        static bool HasNearbyTree(List<TreeInstance> treeInstances, float normalizedX, float normalizedZ, float minDistanceMeters, Terrain terrain)
        {
            if (treeInstances == null || treeInstances.Count == 0 || terrain == null || terrain.terrainData == null)
                return false;

            float minDistanceSqr = minDistanceMeters * minDistanceMeters;
            float sizeX = Mathf.Max(0.0001f, terrain.terrainData.size.x);
            float sizeZ = Mathf.Max(0.0001f, terrain.terrainData.size.z);
            for (int i = 0; i < treeInstances.Count; i++)
            {
                var tree = treeInstances[i];
                float deltaX = (tree.position.x - normalizedX) * sizeX;
                float deltaZ = (tree.position.z - normalizedZ) * sizeZ;
                if ((deltaX * deltaX) + (deltaZ * deltaZ) <= minDistanceSqr)
                    return true;
            }

            return false;
        }

        static bool HasNearbyTree(
            Dictionary<Vector2Int, List<Vector2>> spatialHash,
            float normalizedX,
            float normalizedZ,
            float minDistanceMeters,
            Terrain terrain,
            float cellSizeMeters)
        {
            if (spatialHash == null || spatialHash.Count == 0 || terrain == null || terrain.terrainData == null)
                return false;

            Vector2 worldXZ = GetWorldXZ(terrain, normalizedX, normalizedZ);
            Vector2Int centerCell = GetSpatialCell(worldXZ, cellSizeMeters);
            float minDistanceSqr = minDistanceMeters * minDistanceMeters;
            int searchRadius = Mathf.Max(1, Mathf.CeilToInt(minDistanceMeters / Mathf.Max(0.0001f, cellSizeMeters)));

            for (int dz = -searchRadius; dz <= searchRadius; dz++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + dx, centerCell.y + dz);
                    if (!spatialHash.TryGetValue(cell, out List<Vector2> bucket))
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        if ((bucket[i] - worldXZ).sqrMagnitude <= minDistanceSqr)
                            return true;
                    }
                }
            }

            return false;
        }

        static float SamplePlacementAlpha(float[,] placementAlphaMap, ISplineHeightmapCache cache, float normalizedX, float normalizedZ, int resolution)
        {
            if (placementAlphaMap == null || cache == null || resolution <= 1)
                return 0f;

            float sampleX = normalizedX * (resolution - 1f);
            float sampleZ = normalizedZ * (resolution - 1f);

            if (sampleX < cache.minX || sampleX > cache.maxX || sampleZ < cache.minZ || sampleZ > cache.maxZ)
                return 0f;

            float localX = sampleX - cache.minX;
            float localZ = sampleZ - cache.minZ;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(localX), 0, placementAlphaMap.GetLength(1) - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(localZ), 0, placementAlphaMap.GetLength(0) - 1);
            int x1 = Mathf.Min(x0 + 1, placementAlphaMap.GetLength(1) - 1);
            int z1 = Mathf.Min(z0 + 1, placementAlphaMap.GetLength(0) - 1);

            float tx = Mathf.Clamp01(localX - x0);
            float tz = Mathf.Clamp01(localZ - z0);

            float a = Mathf.Lerp(placementAlphaMap[z0, x0], placementAlphaMap[z0, x1], tx);
            float b = Mathf.Lerp(placementAlphaMap[z1, x0], placementAlphaMap[z1, x1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        static float SampleCachedAlpha(ISplineHeightmapCache cache, int sampleX, int sampleZ)
        {
            if (cache == null || cache.cachedAlpha == null)
                return 0f;

            if (sampleX < cache.minX || sampleX > cache.maxX || sampleZ < cache.minZ || sampleZ > cache.maxZ)
                return 0f;

            int localX = sampleX - cache.minX;
            int localZ = sampleZ - cache.minZ;
            if (localX < 0 || localZ < 0 || localZ >= cache.cachedAlpha.GetLength(0) || localX >= cache.cachedAlpha.GetLength(1))
                return 0f;

            return cache.cachedAlpha[localZ, localX];
        }

        public override void Apply(
            Terrain terrain,
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            OperationContext context)
        {
            if (!ValidateCache(cache) || terrain == null || terrain.terrainData == null || context?.treeInstances == null)
                return;

            var prototypeIndices = context.treePrototypeIndices;
            if (prototypeIndices == null || prototypeIndices.Length == 0)
                return;

            var treePrototypes = terrain.terrainData.treePrototypes;
            if (treePrototypes == null || treePrototypes.Length == 0)
                return;

            var selectedPrototypeIndices = new List<int>(prototypeIndices.Length);
            for (int i = 0; i < prototypeIndices.Length; i++)
            {
                int clamped = Mathf.Clamp(prototypeIndices[i], 0, treePrototypes.Length - 1);
                if (!selectedPrototypeIndices.Contains(clamped))
                    selectedPrototypeIndices.Add(clamped);
            }

            if (selectedPrototypeIndices.Count == 0)
                return;

            int resolution = terrain.terrainData.heightmapResolution;

            var noiseByPrototype = new Dictionary<int, TreeNoisePrototypeSettings>();
            if (context.treeNoiseLayers != null)
            {
                for (int i = 0; i < context.treeNoiseLayers.Length; i++)
                {
                    var entry = context.treeNoiseLayers[i];
                    if (entry == null) continue;
                    noiseByPrototype[entry.treePrototypeIndex] = entry;
                }
            }

            if (context.treeRemoveMode)
            {
                float removeThreshold = Mathf.Clamp01(context.treeRemoveThreshold);
                float removeStrength = Mathf.Clamp01(context.strength) * Mathf.Clamp01(context.treeStrength);
                float removeFalloffPower = Mathf.Max(0.1f, context.treeFalloffPower);

                for (int i = context.treeInstances.Count - 1; i >= 0; i--)
                {
                    var tree = context.treeInstances[i];
                    if (!selectedPrototypeIndices.Contains(tree.prototypeIndex))
                        continue;

                    int sampleX = Mathf.Clamp(Mathf.RoundToInt(tree.position.x * Mathf.Max(1, resolution - 1)), 0, Mathf.Max(0, resolution - 1));
                    int sampleZ = Mathf.Clamp(Mathf.RoundToInt(tree.position.z * Mathf.Max(1, resolution - 1)), 0, Mathf.Max(0, resolution - 1));
                    float cachedAlpha = SampleCachedAlpha(cache, sampleX, sampleZ);
                    if (cachedAlpha <= 0.0001f)
                        continue;

                    float removalAlpha = Mathf.Pow(Mathf.Clamp01(cachedAlpha * removeStrength), removeFalloffPower);
                    if (noiseByPrototype.TryGetValue(tree.prototypeIndex, out var treeNoise))
                    {
                        removalAlpha *= SampleTreeNoise(treeNoise, tree.position.x, tree.position.z, terrain);
                    }

                    if (removalAlpha >= removeThreshold)
                    {
                        context.treeInstances.RemoveAt(i);
                    }
                }

                return;
            }

            float rawTreeStrength = Mathf.Clamp01(context.strength) * Mathf.Clamp01(context.treeStrength);
            float remappedTreeStrength = RemapTreeStrength(rawTreeStrength);
            float addFalloffPower = Mathf.Max(0.1f, context.treeFalloffPower);
            if (remappedTreeStrength <= 0.0001f)
                return;

            float placementStrength = Mathf.Lerp(0.08f, 1f, remappedTreeStrength);
            float minSpacingMeters = Mathf.Lerp(9f, 1.25f, Mathf.Sqrt(remappedTreeStrength));
            float spatialCellSizeMeters = Mathf.Max(0.5f, minSpacingMeters / Mathf.Sqrt(2f));
            var treeSpatialHash = BuildTreeSpatialHash(context.treeInstances, terrain, spatialCellSizeMeters);
            int regionSeed = StableHash(cache.minX, cache.maxX, StableHash(cache.minZ, cache.maxZ, childPriority));
            float[,] placementAlphaMap = new float[cache.cachedAlpha.GetLength(0), cache.cachedAlpha.GetLength(1)];
            var eligiblePixels = new List<Vector2Int>();

            ProcessCacheRegion(cache, childPriority, writePriority, terrain, context, (x, z, localX, localZ, cachedAlpha) =>
            {
                float placementAlpha = Mathf.Pow(Mathf.Clamp01(cachedAlpha), addFalloffPower) * placementStrength;
                if (placementAlpha <= 0.0001f)
                    return;

                placementAlphaMap[localZ, localX] = placementAlpha;
                eligiblePixels.Add(new Vector2Int(x, z));
            });

            if (eligiblePixels.Count == 0)
                return;

            var random = new System.Random(regionSeed);
            var activeSamples = new List<Vector2>();
            int candidateAttemptsPerActivePoint = Mathf.RoundToInt(Mathf.Lerp(6f, 18f, remappedTreeStrength));
            int seedFailureBudget = Mathf.Min(Mathf.RoundToInt(Mathf.Lerp(24f, 128f, remappedTreeStrength)), eligiblePixels.Count);
            int consecutiveSeedFailures = 0;

            bool TryAddTreeSample(float normalizedX, float normalizedZ, int candidateHash, bool isSeed)
            {
                if (normalizedX < 0f || normalizedX > 1f || normalizedZ < 0f || normalizedZ > 1f)
                    return false;

                float placementAlpha = SamplePlacementAlpha(placementAlphaMap, cache, normalizedX, normalizedZ, resolution);
                if (placementAlpha <= 0.0001f)
                    return false;

                Vector2 worldXZ = GetWorldXZ(terrain, normalizedX, normalizedZ);
                float clusterFactor = EvaluateClusterFactor(worldXZ.x, worldXZ.y, remappedTreeStrength, regionSeed);
                float cluster01 = Mathf.InverseLerp(0.18f, 1.85f, clusterFactor);
                float clusteredPlacementAlpha = Mathf.Clamp01(placementAlpha * Mathf.Lerp(0.45f, 1.15f, cluster01));
                if (!isSeed && Random01FromHash(StableHash(candidateHash, 31, 37)) > clusteredPlacementAlpha)
                    return false;

                int prototypeSeedX = Mathf.RoundToInt(normalizedX * 100000f);
                int prototypeSeedZ = Mathf.RoundToInt(normalizedZ * 100000f);
                int prototypeSelector = PositiveModulo(
                    StableHash(prototypeSeedX, prototypeSeedZ, StableHash(regionSeed, childPriority, selectedPrototypeIndices.Count)),
                    selectedPrototypeIndices.Count);
                int prototypeIndex = selectedPrototypeIndices[prototypeSelector];

                if (noiseByPrototype.TryGetValue(prototypeIndex, out var treeNoise))
                {
                    clusteredPlacementAlpha *= SampleTreeNoise(treeNoise, normalizedX, normalizedZ, terrain);
                }

                if (clusteredPlacementAlpha <= 0.0001f)
                    return false;

                if (Random01FromHash(StableHash(candidateHash, prototypeIndex, 43)) > clusteredPlacementAlpha)
                    return false;

                if (context.treeSlopeLimitDegrees < 90f)
                {
                    float slope = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
                    if (slope > context.treeSlopeLimitDegrees)
                        return false;
                }

                if (HasNearbyTree(treeSpatialHash, normalizedX, normalizedZ, minSpacingMeters, terrain, spatialCellSizeMeters))
                    return false;

                float uniformScale = EvaluateTreeHeightMultiplier(context.treeHeightMultiplier, StableHash(candidateHash, prototypeIndex, 57));

                var tree = new TreeInstance
                {
                    position = new Vector3(
                        normalizedX,
                        SampleHeight01(terrain, context.heights, normalizedX, normalizedZ),
                        normalizedZ),
                    prototypeIndex = prototypeIndex,
                    widthScale = uniformScale,
                    heightScale = uniformScale,
                    rotation = EvaluateTreeRotation(normalizedX, normalizedZ, prototypeIndex, regionSeed),
                    color = Color.white,
                    lightmapColor = Color.white
                };

                context.treeInstances.Add(tree);
                AddTreeToSpatialHash(treeSpatialHash, normalizedX, normalizedZ, terrain, spatialCellSizeMeters);
                activeSamples.Add(worldXZ);
                return true;
            }

            while (activeSamples.Count > 0 || consecutiveSeedFailures < seedFailureBudget)
            {
                if (activeSamples.Count == 0)
                {
                    bool seeded = false;
                    int seedAttempts = 0;
                    while (seedAttempts < seedFailureBudget)
                    {
                        Vector2Int seedPixel = eligiblePixels[random.Next(eligiblePixels.Count)];
                        float normalizedX = resolution > 1
                            ? Mathf.Clamp01((seedPixel.x + ((float)random.NextDouble() - 0.5f)) / (resolution - 1f))
                            : 0f;
                        float normalizedZ = resolution > 1
                            ? Mathf.Clamp01((seedPixel.y + ((float)random.NextDouble() - 0.5f)) / (resolution - 1f))
                            : 0f;

                        int candidateHash = StableHash(regionSeed, seedPixel.x, StableHash(seedPixel.y, seedAttempts + 1));
                        if (TryAddTreeSample(normalizedX, normalizedZ, candidateHash, isSeed: true))
                        {
                            consecutiveSeedFailures = 0;
                            seeded = true;
                            break;
                        }

                        seedAttempts++;
                        consecutiveSeedFailures++;
                    }

                    if (!seeded)
                        break;

                    continue;
                }

                int activeIndex = random.Next(activeSamples.Count);
                Vector2 activeWorldXZ = activeSamples[activeIndex];
                bool acceptedCandidate = false;

                for (int attempt = 0; attempt < candidateAttemptsPerActivePoint; attempt++)
                {
                    float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
                    float radius = minSpacingMeters * (1f + (float)random.NextDouble());
                    float worldX = activeWorldXZ.x + Mathf.Cos(angle) * radius;
                    float worldZ = activeWorldXZ.y + Mathf.Sin(angle) * radius;

                    float normalizedX = (worldX - terrain.transform.position.x) / Mathf.Max(0.0001f, terrain.terrainData.size.x);
                    float normalizedZ = (worldZ - terrain.transform.position.z) / Mathf.Max(0.0001f, terrain.terrainData.size.z);
                    int worldHashX = Mathf.RoundToInt(worldX * 10f);
                    int worldHashZ = Mathf.RoundToInt(worldZ * 10f);
                    int candidateHash = StableHash(regionSeed, worldHashX, StableHash(worldHashZ, attempt + 1));

                    if (TryAddTreeSample(normalizedX, normalizedZ, candidateHash, isSeed: false))
                    {
                        acceptedCandidate = true;
                        break;
                    }
                }

                if (!acceptedCandidate)
                {
                    activeSamples.RemoveAt(activeIndex);
                }
            }
        }
    }
}