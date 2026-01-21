using System;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component for bridging splines with terrain by adjusting spline points to terrain height
    /// and deforming terrain to match the spline path.
    /// </summary>
    [AddComponentMenu("Splines/Spline To Terrain")]
    [ExecuteAlways]
    public class SplineToTerrain : MonoBehaviour
    {
        [SerializeField, Tooltip("The Spline to bridge with terrain.")]
        SplineContainer m_Container;

        [SerializeField, Tooltip("The terrain to interact with. If null, will search for nearest terrain.")]
        Terrain m_Terrain;

        [SerializeField, Tooltip("Enable to automatically update when the spline is modified.")]
        bool m_RebuildOnSplineChange = true;

        [SerializeField, Tooltip("The maximum number of times per-second that updates will occur.")]
        int m_RebuildFrequency = 30;

        [Header("Spline to Terrain")]
        [SerializeField, Tooltip("Snap spline knot positions to terrain height.")]
        bool m_SnapSplineToTerrain = true;

        [SerializeField, Tooltip("Additional vertical offset applied to snapped spline points.")]
        float m_SplineHeightOffset = 0f;

        [Header("Terrain to Spline")]
        [SerializeField, Tooltip("Deform terrain to match the spline path.")]
        bool m_DeformTerrainToSpline = false;

        [SerializeField, Tooltip("The width (in world units) where terrain fully matches spline height.")]
        float m_DeformWidth = 5f;

        [SerializeField, Tooltip("Additional distance (in world units) for smooth falloff beyond the width.")]
        float m_SmoothingDistance = 3f;

        [SerializeField, Tooltip("Offset applied to the target terrain height (X=horizontal offset along spline, Y=vertical, Z=horizontal perpendicular to spline).")]
        Vector3 m_TerrainOffset = Vector3.zero;

        [SerializeField, Tooltip("How many terrain samples per world unit along the spline.")]
        float m_SamplesPerUnit = 2f;

        [SerializeField, Tooltip("Blend factor between original terrain and target height. 0 = no change, 1 = full deformation.")]
        [Range(0f, 1f)]
        float m_DeformStrength = 1f;

        float m_NextScheduledRebuild;
        bool m_RebuildRequested;

        /// <summary>The SplineContainer to bridge with terrain.</summary>
        public SplineContainer Container
        {
            get => m_Container;
            set => m_Container = value;
        }

        /// <summary>The terrain to interact with.</summary>
        public Terrain Terrain
        {
            get => m_Terrain;
            set => m_Terrain = value;
        }

        /// <summary>Enable to automatically update when the spline is modified.</summary>
        public bool RebuildOnSplineChange
        {
            get => m_RebuildOnSplineChange;
            set
            {
                m_RebuildOnSplineChange = value;
                if (!value)
                    m_RebuildRequested = value;
            }
        }

        /// <summary>The maximum number of times per-second that updates will occur.</summary>
        public int RebuildFrequency
        {
            get => m_RebuildFrequency;
            set => m_RebuildFrequency = Mathf.Max(value, 1);
        }

        /// <summary>Snap spline knot positions to terrain height.</summary>
        public bool SnapSplineToTerrain
        {
            get => m_SnapSplineToTerrain;
            set => m_SnapSplineToTerrain = value;
        }

        /// <summary>Additional vertical offset applied to snapped spline points.</summary>
        public float SplineHeightOffset
        {
            get => m_SplineHeightOffset;
            set => m_SplineHeightOffset = value;
        }

        /// <summary>Deform terrain to match the spline path.</summary>
        public bool DeformTerrainToSpline
        {
            get => m_DeformTerrainToSpline;
            set => m_DeformTerrainToSpline = value;
        }

        /// <summary>The width where terrain fully matches spline height.</summary>
        public float DeformWidth
        {
            get => m_DeformWidth;
            set => m_DeformWidth = Mathf.Max(value, 0.1f);
        }

        /// <summary>Additional distance for smooth falloff beyond the width.</summary>
        public float SmoothingDistance
        {
            get => m_SmoothingDistance;
            set => m_SmoothingDistance = Mathf.Max(value, 0f);
        }

        /// <summary>Offset applied to the target terrain height.</summary>
        public Vector3 TerrainOffset
        {
            get => m_TerrainOffset;
            set => m_TerrainOffset = value;
        }

        /// <summary>How many terrain samples per world unit along the spline.</summary>
        public float SamplesPerUnit
        {
            get => m_SamplesPerUnit;
            set => m_SamplesPerUnit = Mathf.Max(value, 0.1f);
        }

        /// <summary>Blend factor between original terrain and target height.</summary>
        public float DeformStrength
        {
            get => m_DeformStrength;
            set => m_DeformStrength = Mathf.Clamp01(value);
        }

        /// <summary>The main Spline to bridge with terrain.</summary>
        public Spline Spline => m_Container?.Spline;

        void OnEnable()
        {
            if (m_Terrain == null)
                m_Terrain = FindNearestTerrain();

            Spline.Changed += OnSplineChanged;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        void Update()
        {
            if (m_RebuildRequested && m_RebuildOnSplineChange && Time.time > m_NextScheduledRebuild)
                Rebuild();
        }

        void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (m_RebuildOnSplineChange)
                m_RebuildRequested = true;
        }

        Terrain FindNearestTerrain()
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

        bool IsNullOrEmptyContainer()
        {
            return m_Container == null || m_Container.Splines == null || m_Container.Splines.Count == 0;
        }

        /// <summary>
        /// Triggers the rebuild of the spline-terrain bridge.
        /// </summary>
        public void Rebuild()
        {
            if (IsNullOrEmptyContainer())
                return;

            if (m_Terrain == null)
            {
                m_Terrain = FindNearestTerrain();
                if (m_Terrain == null)
                {
                    Debug.LogWarning("SplineToTerrain: No terrain found.", this);
                    return;
                }
            }

            if (m_SnapSplineToTerrain)
                SnapSplinePointsToTerrain();

            if (m_DeformTerrainToSpline)
                DeformTerrainAlongSpline();

            m_NextScheduledRebuild = Time.time + 1f / m_RebuildFrequency;
            m_RebuildRequested = false;
        }

        void SnapSplinePointsToTerrain()
        {
            var spline = Spline;
            if (spline == null)
                return;

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                var worldPos = m_Container.transform.TransformPoint(knot.Position);
                
                float terrainHeight = m_Terrain.SampleHeight(worldPos) + m_Terrain.transform.position.y;
                worldPos.y = terrainHeight + m_SplineHeightOffset;

                var localPos = m_Container.transform.InverseTransformPoint(worldPos);
                knot.Position = localPos;
                spline[i] = knot;
            }
        }

        void DeformTerrainAlongSpline()
        {
            var spline = Spline;
            if (spline == null)
                return;

            var terrainData = m_Terrain.terrainData;
            
#if UNITY_EDITOR
            // Record undo for terrain data before modification
            UnityEditor.Undo.RegisterCompleteObjectUndo(terrainData, "Deform Terrain to Spline");
#endif

            int heightmapWidth = terrainData.heightmapResolution;
            int heightmapHeight = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

            float splineLength = spline.GetLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(splineLength * m_SamplesPerUnit));

            // Total influence distance = full deform width + smoothing distance
            float totalInfluenceDistance = m_DeformWidth + m_SmoothingDistance;

            // Sample points along the spline
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                var splinePoint = spline.EvaluatePosition(t);
                var worldSplinePoint = m_Container.transform.TransformPoint(splinePoint);

                // Get spline tangent for offset calculation
                var splineTangent = spline.EvaluateTangent(t);
                var worldTangent = m_Container.transform.TransformDirection(splineTangent).normalized;
                
                // Calculate perpendicular (right) vector for Z offset
                var worldUp = Vector3.up;
                var worldRight = Vector3.Cross(worldUp, worldTangent).normalized;

                // Apply offset: X along spline, Y vertical, Z perpendicular to spline
                Vector3 offsetVector = worldTangent * m_TerrainOffset.x + 
                                      Vector3.up * m_TerrainOffset.y + 
                                      worldRight * m_TerrainOffset.z;
                
                worldSplinePoint += offsetVector;

                // Convert world position to terrain heightmap coordinates
                Vector3 terrainLocalPos = worldSplinePoint - m_Terrain.transform.position;
                float xNormalized = terrainLocalPos.x / terrainData.size.x;
                float zNormalized = terrainLocalPos.z / terrainData.size.z;

                int centerX = Mathf.RoundToInt(xNormalized * (heightmapWidth - 1));
                int centerZ = Mathf.RoundToInt(zNormalized * (heightmapHeight - 1));

                // Calculate influence radius in heightmap coordinates
                float influenceInHeightmapUnits = (totalInfluenceDistance / terrainData.size.x) * heightmapWidth;
                int radius = Mathf.CeilToInt(influenceInHeightmapUnits);

                float targetHeight = terrainLocalPos.y / terrainData.size.y;

                // Deform terrain in a circular area around the point
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int heightmapX = centerX + x;
                        int heightmapZ = centerZ + z;

                        if (heightmapX < 0 || heightmapX >= heightmapWidth || 
                            heightmapZ < 0 || heightmapZ >= heightmapHeight)
                            continue;

                        // Calculate distance in world units
                        float xWorldDist = (x / (float)heightmapWidth) * terrainData.size.x;
                        float zWorldDist = (z / (float)heightmapHeight) * terrainData.size.z;
                        float worldDistance = Mathf.Sqrt(xWorldDist * xWorldDist + zWorldDist * zWorldDist);

                        if (worldDistance > totalInfluenceDistance)
                            continue;

                        float falloff;
                        
                        if (worldDistance <= m_DeformWidth)
                        {
                            // Inside the core width - full deformation
                            falloff = 1f;
                        }
                        else
                        {
                            // In the smoothing zone - gradual falloff
                            float smoothingProgress = (worldDistance - m_DeformWidth) / m_SmoothingDistance;
                            // Use smoothstep for natural falloff
                            falloff = 1f - (smoothingProgress * smoothingProgress * (3f - 2f * smoothingProgress));
                        }

                        float currentHeight = heights[heightmapZ, heightmapX];
                        
                        // Apply deformation bidirectionally (can raise OR lower terrain)
                        float newHeight = Mathf.Lerp(currentHeight, targetHeight, falloff * m_DeformStrength);
                        
                        heights[heightmapZ, heightmapX] = newHeight;
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
                Rebuild();
        }

        internal void SetSplineContainerOnGO()
        {
            if (m_Container == null && TryGetComponent<SplineContainer>(out var container))
                m_Container = container;
        }

        internal void Reset()
        {
            SetSplineContainerOnGO();
            if (m_Terrain == null)
                m_Terrain = FindNearestTerrain();
            Rebuild();
        }
#endif
    }
}