using UnityEngine;

namespace GruelTerraSplines
{
    public class PaintOperationApplier : OperationApplierBase
    {
        const float DefaultShapePaintAlphaThreshold = 0.5f;
        const float MinShapePaintAlphaThreshold = 0.001f;
        const float MaxShapePaintAlphaThreshold = 0.99f;

        static float RemapPaintBlend(float paintBlend)
        {
            float normalizedBlend = Mathf.Clamp01(paintBlend);
            AnimationCurve responseCurve = TerraSplinesCurves.GetPaintBlendResponseClone();
            if (responseCurve == null)
            {
                return normalizedBlend;
            }

            return Mathf.Clamp01(responseCurve.Evaluate(normalizedBlend));
        }

        static float EvaluatePaintBlendExponent(float paintBlend)
        {
            float normalizedBlend = Mathf.Clamp01(paintBlend);
            if (normalizedBlend <= 0.5f)
            {
                float hardBlend = 1f - (normalizedBlend / 0.5f);
                hardBlend *= hardBlend;
                return Mathf.Lerp(2f, 0.05f, hardBlend);
            }

            return Mathf.Lerp(2f, 1f, (normalizedBlend - 0.5f) / 0.5f);
        }

        static float ResolvePaintAlpha(ISplineHeightmapCache cache, float cachedAlpha, float paintThreshold)
        {
            if (cache != null && (cache.lastMode == SplineApplyMode.Shape || cache.lastMode == SplineApplyMode.Path))
            {
                float threshold = Mathf.Clamp(paintThreshold, MinShapePaintAlphaThreshold, MaxShapePaintAlphaThreshold);
                return Mathf.InverseLerp(threshold, 1f, Mathf.Clamp01(cachedAlpha));
            }

            return cachedAlpha;
        }

        static float EvaluateNoiseRegionFactor(float basePaintAlpha, float paintBlend, float noiseEdge)
        {
            float normalizedAlpha = Mathf.Clamp01(basePaintAlpha);
            float normalizedBlend = Mathf.Clamp01(paintBlend);
            float edgeAmount = Mathf.Clamp01(Mathf.Abs(noiseEdge));

            if (edgeAmount <= 0.0001f)
            {
                return 1f;
            }

            float edgeExponent = Mathf.Lerp(1.5f, 4f, 1f - normalizedBlend);
            float centerExponent = Mathf.Lerp(1.25f, 2f, 1f - normalizedBlend);

            float edgeMask = 1f - Mathf.Pow(normalizedAlpha, edgeExponent);
            float centerMask = Mathf.Pow(normalizedAlpha, centerExponent);
            float regionMask = noiseEdge >= 0f ? edgeMask : centerMask;

            // Keep some noise visible everywhere, then bias it toward edge or center.
            float biasedFactor = Mathf.Lerp(0.15f, 1f, regionMask);
            return Mathf.Lerp(1f, biasedFactor, edgeAmount);
        }

        public override void Apply(
            Terrain terrain,
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            OperationContext context)
        {
            if (context.alphamaps == null || !ValidateCache(cache)) return;

            System.Collections.Generic.Dictionary<int, PaintNoiseLayerSettings> noiseByLayer = null;
            System.Collections.Generic.HashSet<EntityId> unreadableNoiseTextureWarned = null;
            if (context.paintNoiseLayers != null && context.paintNoiseLayers.Length > 0)
            {
                noiseByLayer = new System.Collections.Generic.Dictionary<int, PaintNoiseLayerSettings>(context.paintNoiseLayers.Length);
                for (int i = 0; i < context.paintNoiseLayers.Length; i++)
                {
                    var entry = context.paintNoiseLayers[i];
                    if (entry == null) continue;
                    noiseByLayer[entry.paintLayerIndex] = entry;
                }

                unreadableNoiseTextureWarned = new System.Collections.Generic.HashSet<EntityId>();
            }

            int heightmapWidth = terrain != null && terrain.terrainData != null
                ? terrain.terrainData.heightmapResolution
                : writePriority.GetLength(1);
            int heightmapHeight = terrain != null && terrain.terrainData != null
                ? terrain.terrainData.heightmapResolution
                : writePriority.GetLength(0);
            int alphamapWidth = context.alphamaps.GetLength(1);
            int alphamapHeight = context.alphamaps.GetLength(0);

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

            float Wrap01(float t) => t - Mathf.Floor(t);
            float remappedPaintBlend = RemapPaintBlend(context.paintBlend);
            float paintBlendExponent = EvaluatePaintBlendExponent(remappedPaintBlend);
            float paintThreshold = cache != null && (cache.lastMode == SplineApplyMode.Shape || cache.lastMode == SplineApplyMode.Path)
                ? Mathf.Clamp(context.paintThreshold, MinShapePaintAlphaThreshold, MaxShapePaintAlphaThreshold)
                : context.paintThreshold;

            ProcessCacheRegion(cache, childPriority, writePriority, terrain, context, (x, z, localX, localZ, cachedAlpha) =>
            {
                // Accumulate mask for paint operations
                if (context.paintSplineAlphaMask != null)
                {
                    float maskAlpha = ResolvePaintAlpha(cache, cachedAlpha, paintThreshold);
                    context.paintSplineAlphaMask[z, x] = Mathf.Max(context.paintSplineAlphaMask[z, x], maskAlpha);
                }

                // Update write priority
                writePriority[z, x] = childPriority;

                GetMappedIndexRange(x, heightmapWidth, alphamapWidth, out int alphaMinX, out int alphaMaxX);
                GetMappedIndexRange(z, heightmapHeight, alphamapHeight, out int alphaMinZ, out int alphaMaxZ);

                for (int alphaZ = alphaMinZ; alphaZ <= alphaMaxZ; alphaZ++)
                {
                    for (int alphaX = alphaMinX; alphaX <= alphaMaxX; alphaX++)
                    {
                        float basePaintAlpha = ResolvePaintAlpha(cache, cachedAlpha, paintThreshold);
                        float paintAlpha = basePaintAlpha;

                        if (noiseByLayer != null
                            && noiseByLayer.TryGetValue(context.targetLayerIndex, out var noise)
                            && noise != null
                            && noise.noiseTexture != null)
                        {
                            var noiseTex = noise.noiseTexture;
                            if (!noiseTex.isReadable && unreadableNoiseTextureWarned != null)
                            {
                                EntityId id = UnityObjectIdUtility.GetEntityId(noiseTex);
                                if (unreadableNoiseTextureWarned.Add(id))
                                {
                                    Debug.LogWarning($"Paint noise texture '{noiseTex.name}' is not readable; using a temporary GPU-readback copy. Enable 'Read/Write' in import settings for better performance.");
                                }
                            }

                            var readableNoise = GetReadableTexture(noiseTex);
                            if (readableNoise != null)
                            {
                                bool useAlpha = ShouldUseAlpha(readableNoise);
                                float widthMinusOne = Mathf.Max(1, alphamapWidth - 1);
                                float heightMinusOne = Mathf.Max(1, alphamapHeight - 1);
                                float nx = alphaX / widthMinusOne;
                                float nz = alphaZ / heightMinusOne;
                                float worldX = terrainPosX + nx * terrainSizeX;
                                float worldZ = terrainPosZ + nz * terrainSizeZ;

                                float worldSize = Mathf.Max(0.001f, noise.noiseWorldSizeMeters);
                                float u = Wrap01((worldX / worldSize) + noise.noiseOffset.x);
                                float v = Wrap01((worldZ / worldSize) + noise.noiseOffset.y);
                                Color c = readableNoise.GetPixelBilinear(u, v);
                                float sample = useAlpha ? c.a : c.grayscale;
                                sample = ApplyNoiseResponse(sample, GetNoiseResponseLut(noise.noiseResponse));
                                if (noise.noiseInvert) sample = 1f - sample;

                                float noiseStrength = Mathf.Clamp01(noise.noiseStrength);
                                float edge = Mathf.Clamp(noise.noiseEdge, -1f, 1f);
                                float edgeFactor = EvaluateNoiseRegionFactor(basePaintAlpha, remappedPaintBlend, edge);
                                sample = Mathf.Lerp(1f, sample, noiseStrength * edgeFactor);
                                paintAlpha *= sample;
                            }
                        }

                        if (paintAlpha <= 0.0001f) continue;

                        float transitionAlpha = Mathf.Pow(Mathf.Clamp01(paintAlpha), paintBlendExponent) * Mathf.Clamp01(context.paintStrength);
                        if (transitionAlpha <= 0.0001f) continue;

                        float currentTargetValue = context.alphamaps[alphaZ, alphaX, context.targetLayerIndex];
                        float newTargetValue = Mathf.Lerp(currentTargetValue, 1f, transitionAlpha);

                        float reductionFactor = transitionAlpha;

                        for (int l = 0; l < context.alphamaps.GetLength(2); l++)
                        {
                            if (l != context.targetLayerIndex)
                            {
                                context.alphamaps[alphaZ, alphaX, l] *= (1f - reductionFactor);
                            }
                        }

                        context.alphamaps[alphaZ, alphaX, context.targetLayerIndex] = newTargetValue;

                        float otherLayersSum = 0f;
                        for (int l = 0; l < context.alphamaps.GetLength(2); l++)
                        {
                            if (l != context.targetLayerIndex)
                            {
                                otherLayersSum += context.alphamaps[alphaZ, alphaX, l];
                            }
                        }

                        if (otherLayersSum > 0.01f)
                        {
                            if (context.pixelsNeedingNormalization != null)
                            {
                                context.pixelsNeedingNormalization.Add((alphaX, alphaZ));
                            }
                            else
                            {
                                TerraSplinesTool.NormalizeAlphamaps(context.alphamaps, alphaX, alphaZ);
                            }
                        }
                    }
                }
            });
        }
    }
}