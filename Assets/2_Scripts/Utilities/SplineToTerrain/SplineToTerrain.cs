using UnityEngine;
using UnityEngine.Splines;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Bridges a SplineContainer with a Terrain — snapping spline knots to terrain height,
    /// deforming terrain to match the spline path, and painting terrain layers along it.
    /// </summary>
    [AddComponentMenu("Splines/Spline To Terrain")]
    [ExecuteAlways]
    public class SplineToTerrain : MonoBehaviour
    {
        [SerializeField, Tooltip("The Spline to bridge with terrain.")]
        private SplineContainer container;

        [SerializeField, Tooltip("The terrain to interact with. If null, will search for nearest terrain.")]
        private Terrain terrain;

        [SerializeField, Tooltip("Enable to automatically update when the spline is modified.")]
        private bool rebuildOnSplineChange = true;

        [SerializeField, Tooltip("The maximum number of times per-second that updates will occur.")]
        private int rebuildFrequency = 30;

        [Header("Spline to Terrain")]
        [SerializeField, Tooltip("Snap spline knot positions to terrain height.")]
        private bool snapSplineToTerrain = false;

        [SerializeField, Tooltip("Additional vertical offset applied to snapped spline points.")]
        private float splineHeightOffset = 0f;

        [Header("Terrain to Spline")]
        [SerializeField, Tooltip("Deform terrain to match the spline path.")]
        private bool deformTerrainToSpline = false;

        [SerializeField, Tooltip("The width (in world units) where terrain fully matches spline height.")]
        private float deformWidth = 5f;

        [SerializeField, Tooltip("Additional distance (in world units) for smooth falloff beyond the width.")]
        private float smoothingDistance = 3f;

        [SerializeField, Tooltip("Offset applied to the target terrain height (X=along spline, Y=vertical, Z=perpendicular to spline).")]
        private Vector3 terrainOffset = Vector3.zero;

        [SerializeField, Tooltip("How many terrain samples per world unit along the spline.")]
        private float samplesPerUnit = 2f;

        [SerializeField, Range(0f, 1f), Tooltip("Blend factor between original terrain and target height. 0 = no change, 1 = full deformation.")]
        private float deformStrength = 1f;

        [Header("Paint Terrain Layer")]
        [SerializeField, Tooltip("Paint a terrain layer along the spline path.")]
        private bool paintTerrainLayer = false;

        [SerializeField, Tooltip("The terrain layer to paint along the spline.")]
        private TerrainLayer terrainLayer = null;

        [SerializeField, Tooltip("The width (in world units) where the layer is painted at full strength.")]
        private float paintWidth = 5f;

        [SerializeField, Tooltip("Additional distance (in world units) for smooth falloff beyond the paint width.")]
        private float paintSmoothingDistance = 3f;

        [SerializeField, Range(0f, 1f), Tooltip("Blend factor between the current layer weights and full paint. 0 = no change, 1 = full paint.")]
        private float paintStrength = 1f;

        private float _nextScheduledRebuild;
        private bool _rebuildRequested;

        public SplineContainer Container
        {
            get => container;
            set => container = value;
        }

        public Terrain Terrain
        {
            get => terrain;
            set => terrain = value;
        }

        public bool RebuildOnSplineChange
        {
            get => rebuildOnSplineChange;
            set
            {
                rebuildOnSplineChange = value;
                if (!value)
                    _rebuildRequested = false;
            }
        }

        public int RebuildFrequency
        {
            get => rebuildFrequency;
            set => rebuildFrequency = Mathf.Max(value, 1);
        }

        public bool SnapSplineToTerrain
        {
            get => snapSplineToTerrain;
            set => snapSplineToTerrain = value;
        }

        public float SplineHeightOffset
        {
            get => splineHeightOffset;
            set => splineHeightOffset = value;
        }

        public bool DeformTerrainToSpline
        {
            get => deformTerrainToSpline;
            set => deformTerrainToSpline = value;
        }

        public float DeformWidth
        {
            get => deformWidth;
            set => deformWidth = Mathf.Max(value, 0.1f);
        }

        public float SmoothingDistance
        {
            get => smoothingDistance;
            set => smoothingDistance = Mathf.Max(value, 0f);
        }

        public Vector3 TerrainOffset
        {
            get => terrainOffset;
            set => terrainOffset = value;
        }

        public float SamplesPerUnit
        {
            get => samplesPerUnit;
            set => samplesPerUnit = Mathf.Max(value, 0.1f);
        }

        public float DeformStrength
        {
            get => deformStrength;
            set => deformStrength = Mathf.Clamp01(value);
        }

        public bool PaintTerrainLayer
        {
            get => paintTerrainLayer;
            set => paintTerrainLayer = value;
        }

        public TerrainLayer TerrainLayer
        {
            get => terrainLayer;
            set => terrainLayer = value;
        }

        public float PaintWidth
        {
            get => paintWidth;
            set => paintWidth = Mathf.Max(value, 0.1f);
        }

        public float PaintSmoothingDistance
        {
            get => paintSmoothingDistance;
            set => paintSmoothingDistance = Mathf.Max(value, 0f);
        }

        public float PaintStrength
        {
            get => paintStrength;
            set => paintStrength = Mathf.Clamp01(value);
        }

        public Spline Spline => container?.Spline;

        private void OnEnable()
        {
            if (terrain == null)
                terrain = FindNearestTerrain();

            Spline.Changed += OnSplineChanged;
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        private void Update()
        {
            if (_rebuildRequested && rebuildOnSplineChange && Time.time > _nextScheduledRebuild)
                Rebuild();
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (rebuildOnSplineChange)
                _rebuildRequested = true;
        }

        private Terrain FindNearestTerrain()
        {
            var terrains = Terrain.activeTerrains;
            if (terrains.Length == 0)
                return null;

            Terrain nearest = terrains[0];
            float minDist = Vector3.Distance(transform.position, nearest.transform.position);

            for (int i = 1; i < terrains.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, terrains[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = terrains[i];
                }
            }

            return nearest;
        }

        private bool IsNullOrEmptyContainer()
        {
            return container == null || container.Splines == null || container.Splines.Count == 0;
        }

        public void Rebuild()
        {
            if (IsNullOrEmptyContainer())
                return;

            if (terrain == null)
            {
                terrain = FindNearestTerrain();
                if (terrain == null)
                {
                    Debug.LogWarning("SplineToTerrain: No terrain found.", this);
                    return;
                }
            }

            if (snapSplineToTerrain)
                SnapSplinePointsToTerrain();

            if (deformTerrainToSpline)
                DeformTerrainAlongSpline();

            if (paintTerrainLayer)
                PaintTerrainLayerAlongSpline();

            _nextScheduledRebuild = Time.time + 1f / rebuildFrequency;
            _rebuildRequested = false;
        }

        private void SnapSplinePointsToTerrain()
        {
            var spline = Spline;
            if (spline == null)
                return;

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                var worldPos = container.transform.TransformPoint(knot.Position);

                float terrainHeight = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
                worldPos.y = terrainHeight + splineHeightOffset;

                var localPos = container.transform.InverseTransformPoint(worldPos);
                knot.Position = localPos;
                spline[i] = knot;
            }
        }

        private void DeformTerrainAlongSpline()
        {
            var spline = Spline;
            if (spline == null)
                return;

            var terrainData = terrain.terrainData;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(terrainData, "Deform Terrain to Spline");
#endif

            int heightmapWidth = terrainData.heightmapResolution;
            int heightmapHeight = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

            float splineLength = spline.GetLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(splineLength * samplesPerUnit));
            float totalInfluenceDistance = deformWidth + smoothingDistance;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                var worldSplinePoint = container.transform.TransformPoint(spline.EvaluatePosition(t));

                var worldTangent = container.transform.TransformDirection(spline.EvaluateTangent(t)).normalized;
                var worldRight = Vector3.Cross(Vector3.up, worldTangent).normalized;

                worldSplinePoint += worldTangent * terrainOffset.x +
                                    Vector3.up * terrainOffset.y +
                                    worldRight * terrainOffset.z;

                Vector3 terrainLocalPos = worldSplinePoint - terrain.transform.position;
                float xNormalized = terrainLocalPos.x / terrainData.size.x;
                float zNormalized = terrainLocalPos.z / terrainData.size.z;

                int centerX = Mathf.RoundToInt(xNormalized * (heightmapWidth - 1));
                int centerZ = Mathf.RoundToInt(zNormalized * (heightmapHeight - 1));

                int radius = Mathf.CeilToInt((totalInfluenceDistance / terrainData.size.x) * heightmapWidth);
                float targetHeight = terrainLocalPos.y / terrainData.size.y;

                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int heightmapX = centerX + x;
                        int heightmapZ = centerZ + z;

                        if (heightmapX < 0 || heightmapX >= heightmapWidth ||
                            heightmapZ < 0 || heightmapZ >= heightmapHeight)
                            continue;

                        float xWorldDist = (x / (float)heightmapWidth) * terrainData.size.x;
                        float zWorldDist = (z / (float)heightmapHeight) * terrainData.size.z;
                        float worldDistance = Mathf.Sqrt(xWorldDist * xWorldDist + zWorldDist * zWorldDist);

                        if (worldDistance > totalInfluenceDistance)
                            continue;

                        float falloff;
                        if (worldDistance <= deformWidth)
                        {
                            falloff = 1f;
                        }
                        else
                        {
                            // Smoothstep falloff in the blending zone
                            float t2 = (worldDistance - deformWidth) / smoothingDistance;
                            falloff = 1f - (t2 * t2 * (3f - 2f * t2));
                        }

                        float currentHeight = heights[heightmapZ, heightmapX];
                        heights[heightmapZ, heightmapX] = Mathf.Lerp(currentHeight, targetHeight, falloff * deformStrength);
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private void PaintTerrainLayerAlongSpline()
        {
            var spline = Spline;
            if (spline == null)
                return;

            if (terrainLayer == null)
            {
                Debug.LogWarning("SplineToTerrain: No terrain layer assigned.", this);
                return;
            }

            var terrainData = terrain.terrainData;
            var terrainLayers = terrainData.terrainLayers;

            int layerIndex = -1;
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                if (terrainLayers[i] == terrainLayer)
                {
                    layerIndex = i;
                    break;
                }
            }

            if (layerIndex == -1)
            {
                Debug.LogWarning($"SplineToTerrain: Layer '{terrainLayer.name}' is not present on the target terrain.", this);
                return;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(terrainData, "Paint Terrain Layer Along Spline");
#endif

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
            int layerCount = terrainData.alphamapLayers;

            float splineLength = spline.GetLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(splineLength * samplesPerUnit));
            float totalInfluenceDistance = paintWidth + paintSmoothingDistance;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                var worldPoint = container.transform.TransformPoint(spline.EvaluatePosition(t));

                Vector3 terrainLocalPos = worldPoint - terrain.transform.position;
                float xNorm = terrainLocalPos.x / terrainData.size.x;
                float zNorm = terrainLocalPos.z / terrainData.size.z;

                int centerX = Mathf.RoundToInt(xNorm * (alphamapWidth - 1));
                int centerZ = Mathf.RoundToInt(zNorm * (alphamapHeight - 1));

                int radius = Mathf.CeilToInt((totalInfluenceDistance / terrainData.size.x) * alphamapWidth);

                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int ax = centerX + x;
                        int az = centerZ + z;

                        if (ax < 0 || ax >= alphamapWidth || az < 0 || az >= alphamapHeight)
                            continue;

                        float xWorldDist = (x / (float)alphamapWidth) * terrainData.size.x;
                        float zWorldDist = (z / (float)alphamapHeight) * terrainData.size.z;
                        float worldDistance = Mathf.Sqrt(xWorldDist * xWorldDist + zWorldDist * zWorldDist);

                        if (worldDistance > totalInfluenceDistance)
                            continue;

                        float falloff;
                        if (worldDistance <= paintWidth)
                        {
                            falloff = 1f;
                        }
                        else
                        {
                            float t2 = (worldDistance - paintWidth) / paintSmoothingDistance;
                            falloff = 1f - (t2 * t2 * (3f - 2f * t2));
                        }

                        float oldValue = alphamaps[az, ax, layerIndex];
                        float newValue = Mathf.Lerp(oldValue, 1f, falloff * paintStrength);
                        float remaining = 1f - newValue;
                        float previousOthers = 1f - oldValue;

                        alphamaps[az, ax, layerIndex] = newValue;

                        for (int l = 0; l < layerCount; l++)
                        {
                            if (l == layerIndex) continue;
                            alphamaps[az, ax, l] = previousOthers > 0f
                                ? alphamaps[az, ax, l] * (remaining / previousOthers)
                                : 0f;
                        }
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

#if UNITY_EDITOR

        public void SetSplineContainerOnGO()
        {
            if (this.container == null && TryGetComponent<SplineContainer>(out var container))
                this.container = container;
        }

        public void Reset()
        {
            SetSplineContainerOnGO();
            if (terrain == null)
                terrain = FindNearestTerrain();
            Rebuild();
        }
#endif
    }
}