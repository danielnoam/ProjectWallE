using UnityEngine;

namespace GruelTerraSplines
{
    public abstract class OperationApplierBase : IOperationApplier
    {
        public static event System.Action NoiseTextureCacheInvalidated;

        static readonly System.Collections.Generic.Dictionary<EntityId, Texture2D> readableNoiseCache = new System.Collections.Generic.Dictionary<EntityId, Texture2D>();
        static readonly System.Collections.Generic.Dictionary<EntityId, bool> noiseUseAlphaCache = new System.Collections.Generic.Dictionary<EntityId, bool>();
        static readonly System.Collections.Generic.Dictionary<int, float[]> noiseResponseLutCache = new System.Collections.Generic.Dictionary<int, float[]>();
        const int NoiseResponseLutSize = 256;
        static readonly int identityNoiseResponseCurveHash = AnimationCurve.Linear(0f, 0f, 1f, 1f).GetAnimationCurveHash();

        public static bool InvalidateNoiseTexture(Texture2D texture, bool notifyListeners = true)
        {
            if (texture == null)
                return false;

            bool changed = false;
            EntityId sourceId = UnityObjectIdUtility.GetEntityId(texture);

            if (readableNoiseCache.TryGetValue(sourceId, out var cachedReadable))
            {
                readableNoiseCache.Remove(sourceId);
                changed = true;

                if (cachedReadable != null)
                {
                    noiseUseAlphaCache.Remove(UnityObjectIdUtility.GetEntityId(cachedReadable));
                    Object.DestroyImmediate(cachedReadable);
                }
            }

            changed |= noiseUseAlphaCache.Remove(sourceId);

            if (changed && notifyListeners)
            {
                NoiseTextureCacheInvalidated?.Invoke();
            }

            return changed;
        }

        public static bool InvalidateNoiseTextures(System.Collections.Generic.IEnumerable<Texture2D> textures, bool notifyListeners = true)
        {
            if (textures == null)
                return false;

            bool changed = false;
            foreach (var texture in textures)
            {
                changed |= InvalidateNoiseTexture(texture, notifyListeners: false);
            }

            if (changed && notifyListeners)
            {
                NoiseTextureCacheInvalidated?.Invoke();
            }

            return changed;
        }

        public static void NotifyNoiseTextureCacheInvalidated()
        {
            NoiseTextureCacheInvalidated?.Invoke();
        }

        public static bool InvalidateAllNoiseTextures(bool notifyListeners = true)
        {
            bool changed = readableNoiseCache.Count > 0 || noiseUseAlphaCache.Count > 0;

            foreach (var entry in readableNoiseCache)
            {
                if (entry.Value != null)
                {
                    Object.DestroyImmediate(entry.Value);
                }
            }

            readableNoiseCache.Clear();
            noiseUseAlphaCache.Clear();
            noiseResponseLutCache.Clear();

            if (changed && notifyListeners)
            {
                NoiseTextureCacheInvalidated?.Invoke();
            }

            return changed;
        }

        protected static Texture2D GetReadableTexture(Texture2D src)
        {
            if (src == null) return null;
            if (src.isReadable) return src;

            EntityId id = UnityObjectIdUtility.GetEntityId(src);
            if (readableNoiseCache.TryGetValue(id, out var cached) && cached != null)
            {
                return cached;
            }

            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prev = RenderTexture.active;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;

                var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true)
                {
                    name = $"{src.name}_ReadableCopy",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = src.filterMode,
                    hideFlags = HideFlags.HideAndDontSave
                };

                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply(false, false);

                readableNoiseCache[id] = tex;
                return tex;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        protected static bool ShouldUseAlpha(Texture2D readableNoise)
        {
            if (readableNoise == null) return false;

            EntityId id = UnityObjectIdUtility.GetEntityId(readableNoise);
            if (noiseUseAlphaCache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            float minA = 1f;
            float maxA = 0f;
            void Accumulate(Texture2D t, float u, float v, ref float min, ref float max)
            {
                float a = t.GetPixelBilinear(u, v).a;
                min = Mathf.Min(min, a);
                max = Mathf.Max(max, a);
            }

            Accumulate(readableNoise, 0.13f, 0.13f, ref minA, ref maxA);
            Accumulate(readableNoise, 0.87f, 0.13f, ref minA, ref maxA);
            Accumulate(readableNoise, 0.13f, 0.87f, ref minA, ref maxA);
            Accumulate(readableNoise, 0.87f, 0.87f, ref minA, ref maxA);
            Accumulate(readableNoise, 0.50f, 0.50f, ref minA, ref maxA);

            bool useAlpha = (maxA - minA) > 0.01f && minA < 0.999f;
            noiseUseAlphaCache[id] = useAlpha;
            return useAlpha;
        }

        protected static bool IsIdentityNoiseResponseCurve(AnimationCurve curve)
        {
            return curve == null || curve.GetAnimationCurveHash() == identityNoiseResponseCurveHash;
        }

        protected static float[] GetNoiseResponseLut(AnimationCurve curve)
        {
            if (IsIdentityNoiseResponseCurve(curve))
            {
                return null;
            }

            int hash = curve.GetAnimationCurveHash();
            if (noiseResponseLutCache.TryGetValue(hash, out var lut) && lut != null)
            {
                return lut;
            }

            lut = new float[NoiseResponseLutSize];
            for (int i = 0; i < NoiseResponseLutSize; i++)
            {
                float t = i / (float)(NoiseResponseLutSize - 1);
                lut[i] = Mathf.Clamp01(curve.Evaluate(t));
            }

            noiseResponseLutCache[hash] = lut;
            return lut;
        }

        protected static float ApplyNoiseResponse(float sample, float[] lut)
        {
            sample = Mathf.Clamp01(sample);
            if (lut == null || lut.Length == 0)
            {
                return sample;
            }

            float scaled = sample * (lut.Length - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, lut.Length - 1);
            int nextIndex = Mathf.Min(index + 1, lut.Length - 1);
            float fraction = scaled - index;
            return Mathf.Lerp(lut[index], lut[nextIndex], fraction);
        }

        public abstract void Apply(
            Terrain terrain,
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            OperationContext context
        );

        protected bool ValidateCache(ISplineHeightmapCache cache)
        {
            return cache != null && cache.isValid && cache.cachedAlpha != null;
        }

        protected static void GetMappedIndexRange(int sourceIndex, int sourceResolution, int targetResolution, out int targetMin, out int targetMax)
        {
            if (targetResolution <= 0)
            {
                targetMin = 0;
                targetMax = -1;
                return;
            }

            if (targetResolution == 1 || sourceResolution <= 1)
            {
                targetMin = 0;
                targetMax = 0;
                return;
            }

            float sourceDenominator = sourceResolution - 1f;
            float targetDenominator = targetResolution - 1f;

            float start = sourceIndex <= 0 ? 0f : (sourceIndex - 0.5f) / sourceDenominator;
            float end = sourceIndex >= sourceResolution - 1 ? 1f : (sourceIndex + 0.5f) / sourceDenominator;

            targetMin = Mathf.Clamp(Mathf.CeilToInt(start * targetDenominator), 0, targetResolution - 1);
            targetMax = Mathf.Clamp(Mathf.FloorToInt(end * targetDenominator), 0, targetResolution - 1);

            if (targetMax < targetMin)
            {
                int mappedIndex = Mathf.Clamp(
                    Mathf.RoundToInt(sourceIndex * (targetDenominator / sourceDenominator)),
                    0,
                    targetResolution - 1);
                targetMin = mappedIndex;
                targetMax = mappedIndex;
            }
        }

        protected void ProcessCacheRegion(
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            System.Action<int, int, int, int, float> processPixel)
        {
            ProcessCacheRegion(cache, childPriority, writePriority, null, null, processPixel);
        }

        protected void ProcessCacheRegion(
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            Terrain terrain,
            System.Action<int, int, int, int, float> processPixel)
        {
            ProcessCacheRegion(cache, childPriority, writePriority, terrain, null, processPixel);
        }

        protected void ProcessCacheRegion(
            ISplineHeightmapCache cache,
            int childPriority,
            int[,] writePriority,
            Terrain terrain,
            OperationContext context,
            System.Action<int, int, int, int, float> processPixel)
        {
            if (!ValidateCache(cache)) return;

            // Get terrain bounds if provided
            int terrainRes = 0;
            float terrainPosX = 0f;
            float terrainPosZ = 0f;
            float terrainSizeX = 1f;
            float terrainSizeZ = 1f;
            if (terrain != null && terrain.terrainData != null)
            {
                terrainRes = terrain.terrainData.heightmapResolution;
                terrainPosX = terrain.transform.position.x;
                terrainPosZ = terrain.transform.position.z;
                terrainSizeX = Mathf.Max(0.0001f, terrain.terrainData.size.x);
                terrainSizeZ = Mathf.Max(0.0001f, terrain.terrainData.size.z);
            }

            var brushNoise = context?.brushNoise;
            var noiseTexture = brushNoise?.noiseTexture;
            Texture2D readableNoise = null;
            bool useAlpha = false;
            bool warnedNoise = false;

            float worldSize = 1f;
            float noiseStrength = 1f;
            float noiseEdge = 0f;
            Vector2 noiseOffset = Vector2.zero;
            bool noiseInvert = false;
            float[] noiseResponseLut = null;
            if (noiseTexture != null)
            {
                readableNoise = GetReadableTexture(noiseTexture);
                useAlpha = readableNoise != null && ShouldUseAlpha(readableNoise);
                worldSize = Mathf.Max(0.001f, brushNoise.noiseWorldSizeMeters);
                noiseStrength = Mathf.Clamp01(brushNoise.noiseStrength);
                noiseEdge = Mathf.Clamp(brushNoise.noiseEdge, -1f, 1f);

                noiseOffset = brushNoise.noiseOffset;
                noiseInvert = brushNoise.noiseInvert;
                noiseResponseLut = GetNoiseResponseLut(brushNoise.noiseResponse);
            }

            float Wrap01(float t) => t - Mathf.Floor(t);

            for (int z = cache.minZ; z <= cache.maxZ; z++)
            {
                for (int x = cache.minX; x <= cache.maxX; x++)
                {
                    // Convert to local cache coordinates
                    int localX = x - cache.minX;
                    int localZ = z - cache.minZ;

                    // Bounds check: skip if outside terrain bounds (for seamless gradient across terrain boundaries)
                    if (terrain != null && (x < 0 || x >= terrainRes || z < 0 || z >= terrainRes))
                    {
                        continue;
                    }

                    float cachedAlpha = cache.cachedAlpha[localZ, localX];
                    if (cachedAlpha <= 0.0001f) continue;

                    // Check priority (only if within bounds)
                    if (x >= 0 && x < writePriority.GetLength(1) && z >= 0 && z < writePriority.GetLength(0))
                    {
                        if (childPriority < writePriority[z, x]) continue;
                    }

                    if (readableNoise != null)
                    {
                        if (!noiseTexture.isReadable && !warnedNoise)
                        {
                            warnedNoise = true;
                            Debug.LogWarning($"Brush noise texture '{noiseTexture.name}' is not readable; using a temporary GPU-readback copy. Enable 'Read/Write' in import settings for better performance.");
                        }

                        float resMinusOne = Mathf.Max(1f, (terrainRes > 0 ? terrainRes - 1f : 1f));
                        float nx = terrainRes > 1 ? x / resMinusOne : 0f;
                        float nz = terrainRes > 1 ? z / resMinusOne : 0f;
                        float worldX = terrainPosX + nx * terrainSizeX;
                        float worldZ = terrainPosZ + nz * terrainSizeZ;

                        float u = Wrap01((worldX / worldSize) + noiseOffset.x);
                        float v = Wrap01((worldZ / worldSize) + noiseOffset.y);
                        Color c = readableNoise.GetPixelBilinear(u, v);
                        float sample = useAlpha ? c.a : c.grayscale;
                        sample = ApplyNoiseResponse(sample, noiseResponseLut);
                        if (noiseInvert) sample = 1f - sample;

                        float alpha = Mathf.Clamp01(cachedAlpha);
                        float edgeFactor = noiseEdge >= 0f
                            ? 1f - (noiseEdge * alpha)
                            : 1f - (-noiseEdge * (1f - alpha));

                        sample = Mathf.Lerp(1f, sample, noiseStrength * edgeFactor);
                        cachedAlpha *= sample;
                        if (cachedAlpha <= 0.0001f) continue;
                    }

                    processPixel(x, z, localX, localZ, cachedAlpha);
                }
            }
        }
    }
}