using UnityEngine;
using System.Collections.Generic;

namespace GruelTerraSplines
{
    public class DetailOperationApplier : OperationApplierBase
    {
        const int DefaultDetailDensityBudget = 2048;

        static float RemapDetailStrength(float detailStrength)
        {
            return TerraSplinesCurves.EvaluateDetailStrengthResponse(detailStrength);
        }

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

        public override void Apply(
            Terrain terrain,
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            OperationContext context)
        {
            if (!ValidateCache(cache) || context == null) return;

            var detailLayers = (context.detailLayers != null && context.detailLayers.Length > 0)
                ? context.detailLayers
                : (context.detailLayer != null ? new[] { context.detailLayer } : null);
            if (detailLayers == null || detailLayers.Length == 0) return;

            int heightmapWidth = terrain != null && terrain.terrainData != null
                ? terrain.terrainData.heightmapResolution
                : writePriority.GetLength(1);
            int heightmapHeight = terrain != null && terrain.terrainData != null
                ? terrain.terrainData.heightmapResolution
                : writePriority.GetLength(0);
            int detailHeight = detailLayers[0].GetLength(0);
            int detailWidth = detailLayers[0].GetLength(1);

            if (context.detailLayer == null && (context.detailLayers == null || context.detailLayers.Length == 0)) return;

            var activeLayers = new List<int[,]>(detailLayers.Length);
            var activeLayerIndices = new List<int>(detailLayers.Length);
            for (int i = 0; i < detailLayers.Length; i++)
            {
                if (detailLayers[i] == null)
                {
                    continue;
                }

                activeLayers.Add(detailLayers[i]);
                activeLayerIndices.Add((context.detailLayerIndices != null && i < context.detailLayerIndices.Length)
                    ? context.detailLayerIndices[i]
                    : context.detailLayerIndex);
            }

            int selectedLayerCount = activeLayers.Count;
            if (selectedLayerCount == 0) return;

            Dictionary<int, DetailNoiseLayerSettings> noiseByLayer = null;
            HashSet<EntityId> unreadableNoiseTextureWarned = null;
            if (context.detailNoiseLayers != null && context.detailNoiseLayers.Length > 0)
            {
                noiseByLayer = new Dictionary<int, DetailNoiseLayerSettings>(context.detailNoiseLayers.Length);
                for (int i = 0; i < context.detailNoiseLayers.Length; i++)
                {
                    var entry = context.detailNoiseLayers[i];
                    if (entry == null) continue;
                    noiseByLayer[entry.detailLayerIndex] = entry;
                }

                unreadableNoiseTextureWarned = new HashSet<EntityId>();
            }

            float terrainPosX = 0f;
            float terrainPosZ = 0f;
            float terrainSizeX = 1f;
            float terrainSizeZ = 1f;
            if (terrain != null && terrain.terrainData != null)
            {
                terrainPosX = terrain.transform.position.x;
                terrainPosZ = terrain.transform.position.z;
                terrainSizeX = Mathf.Max(0.0001f, terrain.terrainData.size.x);
                terrainSizeZ = Mathf.Max(0.0001f, terrain.terrainData.size.z);
            }

            float detailStrength = RemapDetailStrength(Mathf.Clamp01(context.strength) * Mathf.Clamp01(context.detailStrength));
            float densityBudget = DefaultDetailDensityBudget;
            float widthMinusOne = Mathf.Max(1, detailWidth - 1);
            float heightMinusOne = Mathf.Max(1, detailHeight - 1);

            float Wrap01(float t) => t - Mathf.Floor(t);

            float SampleLayerNoise(int layerArrayIndex, int targetX, int targetZ, float alpha)
            {
                if (noiseByLayer == null)
                {
                    return alpha;
                }

                if (!noiseByLayer.TryGetValue(activeLayerIndices[layerArrayIndex], out var noise) || noise == null)
                {
                    return alpha;
                }

                var noiseTex = noise.noiseTexture;
                if (noiseTex == null)
                {
                    return alpha;
                }

                if (!noiseTex.isReadable && unreadableNoiseTextureWarned != null)
                {
                    EntityId id = UnityObjectIdUtility.GetEntityId(noiseTex);
                    if (unreadableNoiseTextureWarned.Add(id))
                    {
                        Debug.LogWarning($"Detail noise texture '{noiseTex.name}' is not readable; using a temporary GPU-readback copy. Enable 'Read/Write' in import settings for better performance.");
                    }
                }

                var readableNoise = GetReadableTexture(noiseTex);
                if (readableNoise == null)
                {
                    return alpha;
                }

                bool useAlpha = ShouldUseAlpha(readableNoise);
                float nx = targetX / widthMinusOne;
                float nz = targetZ / heightMinusOne;
                float worldX = terrainPosX + nx * terrainSizeX;
                float worldZ = terrainPosZ + nz * terrainSizeZ;

                float worldSize = Mathf.Max(0.001f, noise.noiseWorldSizeMeters);
                float u = Wrap01((worldX / worldSize) + noise.noiseOffset.x);
                float v = Wrap01((worldZ / worldSize) + noise.noiseOffset.y);
                Color sampleColor = readableNoise.GetPixelBilinear(u, v);
                float sample = useAlpha ? sampleColor.a : sampleColor.grayscale;
                if (noise.noiseInvert) sample = 1f - sample;
                if (noise.noiseThreshold > 0f)
                {
                    sample = Mathf.Clamp01(Mathf.InverseLerp(noise.noiseThreshold, 1f, sample));
                }

                return alpha * Mathf.Lerp(1f, sample, Mathf.Clamp01(noise.noiseStrength));
            }

            ProcessCacheRegion(cache, childPriority, writePriority, terrain, context, (x, z, localX, localZ, cachedAlpha) =>
            {
                if (terrain != null && context.detailSlopeLimitDegrees < 90f)
                {
                    var td = terrain.terrainData;
                    if (td != null && td.heightmapResolution > 1)
                    {
                        float nx = Mathf.Clamp01((float)x / (td.heightmapResolution - 1));
                        float nz = Mathf.Clamp01((float)z / (td.heightmapResolution - 1));
                        float slope = td.GetSteepness(nx, nz);
                        if (slope > context.detailSlopeLimitDegrees)
                        {
                            return;
                        }
                    }
                }

                float detailAlpha = cachedAlpha * detailStrength;
                float falloffPower = Mathf.Max(0.1f, context.detailFalloffPower);
                detailAlpha = Mathf.Pow(detailAlpha, falloffPower);
                if (detailAlpha <= 0.0001f) return;

                writePriority[z, x] = childPriority;

                int radius = Mathf.Max(0, context.detailSpreadRadius);
                int radiusSquared = radius * radius;

                GetMappedIndexRange(x, heightmapWidth, detailWidth, out int detailMinX, out int detailMaxX);
                GetMappedIndexRange(z, heightmapHeight, detailHeight, out int detailMinZ, out int detailMaxZ);

                for (int detailZ = detailMinZ; detailZ <= detailMaxZ; detailZ++)
                {
                    for (int detailX = detailMinX; detailX <= detailMaxX; detailX++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            for (int dx = -radius; dx <= radius; dx++)
                            {
                                int distanceSquared = (dx * dx) + (dz * dz);
                                if (distanceSquared > radiusSquared) continue;

                                float spreadAlpha = detailAlpha;
                                if (radius > 0)
                                {
                                    float distance = Mathf.Sqrt(distanceSquared);
                                    float spreadFalloff = Mathf.Clamp01(1f - (distance / (radius + 1f)));
                                    spreadAlpha *= spreadFalloff;
                                    if (spreadAlpha <= 0.0001f) continue;
                                }

                                int targetX = Mathf.Clamp(detailX + dx, 0, detailWidth - 1);
                                int targetZ = Mathf.Clamp(detailZ + dz, 0, detailHeight - 1);

                                if (context.detailMode == DetailOperationMode.Add)
                                {
                                    int chosenLayer = selectedLayerCount == 1
                                        ? 0
                                        : PositiveModulo(StableHash(targetX, targetZ, childPriority), selectedLayerCount);

                                    float layerAlpha = SampleLayerNoise(chosenLayer, targetX, targetZ, spreadAlpha);
                                    if (layerAlpha <= 0.0001f)
                                    {
                                        continue;
                                    }

                                    float baseDensity = layerAlpha * densityBudget;
                                    if (densityBudget > 0f)
                                    {
                                        float variation = (Mathf.PerlinNoise(targetX * 0.1f, targetZ * 0.1f) - 0.5f) * 0.2f * densityBudget;
                                        baseDensity += variation;
                                    }

                                    int targetDensity = Mathf.Clamp(Mathf.FloorToInt(baseDensity), 0, DefaultDetailDensityBudget);
                                    var detailLayer = activeLayers[chosenLayer];
                                    int currentDensity = detailLayer[targetZ, targetX];
                                    if (targetDensity > currentDensity)
                                    {
                                        detailLayer[targetZ, targetX] = targetDensity;
                                    }
                                }
                                else if (context.detailMode == DetailOperationMode.Remove)
                                {
                                    for (int layer = 0; layer < selectedLayerCount; layer++)
                                    {
                                        float layerAlpha = SampleLayerNoise(layer, targetX, targetZ, spreadAlpha);
                                        if (layerAlpha <= 0.0001f)
                                        {
                                            continue;
                                        }

                                        var detailLayer = activeLayers[layer];
                                        if (detailLayer[targetZ, targetX] <= 0)
                                        {
                                            continue;
                                        }

                                        if (layerAlpha >= Mathf.Clamp01(context.detailRemoveThreshold))
                                        {
                                            detailLayer[targetZ, targetX] = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
    }
}