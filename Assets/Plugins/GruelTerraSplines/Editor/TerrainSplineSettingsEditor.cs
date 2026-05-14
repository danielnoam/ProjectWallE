using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace GruelTerraSplines
{
    [CustomEditor(typeof(TerrainSplineSettings))]
    [CanEditMultipleObjects]
    public class TerrainSplineSettingsEditor : Editor
    {
        const string EditorPrefsPrefix = "GruelTerraSplines";
        const string LegacyEditorPrefsPrefix = "DKSplineTerrain";

        struct LineSegment
        {
            public Vector3 from;
            public Vector3 to;

            public LineSegment(Vector3 from, Vector3 to)
            {
                this.from = from;
                this.to = to;
            }
        }

        sealed class ContourCacheEntry
        {
            public int splineId;
            public int splineVersion;
            public int previewSettingsHash;
            public int contourGridSize;
            public List<Vector3[]> polylines;
        }

        sealed class LivePreviewState
        {
            public int splineId;
            public int lastComputedVersion;
            public int lastPreviewSettingsHash;
            public double lastInteractionTime;
            public List<Vector3[]> polylines;
        }

        sealed class HighQualityBuildRequest
        {
            public int splineId;
            public int splineVersion;
            public int previewSettingsHash;
            public int contourGridSize;
            public bool isClosed;
            public int generation;
            public int priority;
            public long sequence;
            public List<Vector3> centerPoints;
            public List<float> halfWidths;
        }

        sealed class HighQualityBuildResult
        {
            public HighQualityBuildRequest request;
            public List<Vector3[]> polylines;
        }

        static Material lineMaterial;
        static readonly Color RailColor = new Color(0f, 0.9f, 0.85f, 1f);
        static readonly Color RailOccludedColor = new Color(0f, 0.9f, 0.85f, 0.22f);
        static readonly object highQualityBuildLock = new object();
        static readonly Dictionary<int, ContourCacheEntry> contourCache = new Dictionary<int, ContourCacheEntry>();
        static readonly Dictionary<int, LivePreviewState> livePreviewStates = new Dictionary<int, LivePreviewState>();
        static readonly Dictionary<int, HighQualityBuildRequest> pendingHighQualityBuilds = new Dictionary<int, HighQualityBuildRequest>();
        static readonly List<HighQualityBuildResult> completedHighQualityBuilds = new List<HighQualityBuildResult>();
        static Task[] highQualityBuildWorkers;
        static long buildRequestSequence;
        static int highQualityBuildGeneration;
        static bool stopHighQualityBuildWorkers;
        const float MinSplineLength = 0.0001f;
        const float MinBrushWidth = 0.0001f;
        const float SampleSpacing = 1f;
        const float InteractiveSampleSpacing = 2.5f;
        const float MinSegmentCount = 24f;
        const float InteractiveMinSegmentCount = 10f;
        const float LineThickness = 3f;
        const float MinSegmentLength2D = 0.0001f;
        const int HighQualityContourGridSize = 112;
        const int InteractiveContourGridSize = 20;
        const int MaxCachedContours = 128;
        const float PolylineJoinTolerance = 0.01f;
        const double HighQualityIdleDelaySeconds = 0.2d;
        const int FastPreviewContourGridSize = 10;
        const int MaxFastPreviewSamples = 12;
        const float NarrowWidthFastRailThreshold = 30f;
        const float MaxRailMiterScale = 3f;
        const int MaxConcurrentHighQualityBuildWorkers = 2;
        static readonly int HighQualityBuildWorkerCount = Math.Max(1, Math.Min(MaxConcurrentHighQualityBuildWorkers, Math.Max(1, Environment.ProcessorCount - 1)));

        static TerrainSplineSettingsEditor()
        {
            EditorApplication.update -= ProcessPendingHighQualityBuilds;
            EditorApplication.update += ProcessPendingHighQualityBuilds;
            AssemblyReloadEvents.beforeAssemblyReload -= StopHighQualityBuildWorkers;
            AssemblyReloadEvents.beforeAssemblyReload += StopHighQualityBuildWorkers;
            StartHighQualityBuildWorkers();
        }

        static string BuildEditorPrefsKey(string suffix) => $"{EditorPrefsPrefix}.{suffix}";
        static string BuildLegacyEditorPrefsKey(string suffix) => $"{LegacyEditorPrefsPrefix}.{suffix}";

        static bool LoadBoolPref(string suffix, bool defaultValue)
        {
            string key = BuildEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(key))
                return EditorPrefs.GetBool(key, defaultValue);

            string legacyKey = BuildLegacyEditorPrefsKey(suffix);
            if (EditorPrefs.HasKey(legacyKey))
                return EditorPrefs.GetBool(legacyKey, defaultValue);

            return defaultValue;
        }

        static bool IsGizmosFeatureEnabled()
        {
            return LoadBoolPref("FeatureGizmosEnabled", true);
        }

        public static void SetGizmosFeatureEnabled(bool enabled)
        {
            if (!enabled)
            {
                ClearQueuedHighQualityBuilds();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        void OnSceneGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            if (!IsGizmosFeatureEnabled())
                return;

            var terrainSettings = (TerrainSplineSettings)target;
            if (terrainSettings == null || terrainSettings.settings == null || !terrainSettings.settings.enabled)
                return;

            var container = terrainSettings.GetComponent<SplineContainer>();
            if (container == null)
                return;

            bool isInteractive = GUIUtility.hotControl != 0;
            int splineCount = container.Splines.Count;
            for (int splineIndex = 0; splineIndex < splineCount; splineIndex++)
            {
                var spline = container.Splines[splineIndex];
                int splineVersion = ComputePreviewSplineVersion(container, spline);
                DrawSplineRails(container, terrainSettings.settings, spline, splineIndex, splineVersion, isInteractive);
            }
        }

        static void DrawSplineRails(SplineContainer container, SplineStrokeSettings settings, Spline spline, int splineIndex, int splineVersion, bool isInteractive)
        {
            if (spline == null)
                return;

            bool isClosed = spline.Closed;

            float baseWidth = Mathf.Max(MinBrushWidth, ResolveBrushWidth(settings));
            float length = SplineUtility.CalculateLength(spline, container.transform.localToWorldMatrix);
            if (length <= MinSplineLength)
                return;

            float contourSampleSpacing = isInteractive
                ? Mathf.Clamp(baseWidth * 0.4f, 0.75f, InteractiveSampleSpacing)
                : Mathf.Clamp(baseWidth * 0.15f, 0.25f, SampleSpacing);
            int minSegmentCount = isInteractive ? (int)InteractiveMinSegmentCount : (int)MinSegmentCount;
            int segmentCount = Mathf.Max(minSegmentCount, Mathf.CeilToInt(length / contourSampleSpacing));
            int pointCount = isClosed ? segmentCount : segmentCount + 1;

            var centerPoints = new List<Vector3>(pointCount);
            var halfWidths = new List<float>(pointCount);

            for (int i = 0; i < pointCount; i++)
            {
                float t = segmentCount == 0 ? 0f : (float)i / segmentCount;
                Vector3 center = container.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(spline, t));

                float widthMultiplier = 1f;
                if (settings.overrideSizeMultiplier && settings.sizeMultiplier != null)
                    widthMultiplier = Mathf.Max(0f, settings.sizeMultiplier.Evaluate(t));

                centerPoints.Add(center);
                halfWidths.Add(baseWidth * widthMultiplier * 0.5f);
            }

            if (centerPoints.Count < 2)
                return;

            int splineId = ComputeSplinePreviewId(container, splineIndex);
            int contourGridSize = isInteractive ? InteractiveContourGridSize : HighQualityContourGridSize;
            int previewSettingsHash = ComputePreviewSettingsHash(settings, baseWidth);

            if (isInteractive || ShouldUseInteractivePreview(splineId))
            {
                DrawInteractivePreview(splineId, splineVersion, centerPoints, halfWidths, isClosed, isInteractive,
                    previewSettingsHash);
                return;
            }

            if (TryDrawCachedContour(splineId, splineVersion, previewSettingsHash, contourGridSize))
                return;

            EnqueueHighQualityBuild(container, splineId, splineVersion, previewSettingsHash, contourGridSize, isClosed, centerPoints, halfWidths);
            DrawInteractivePreview(splineId, splineVersion, centerPoints, halfWidths, isClosed, false, previewSettingsHash);
        }

        static float ResolveBrushWidth(SplineStrokeSettings settings)
        {
            if (settings.overrideBrush)
                return settings.sizeMeters;

            var openWindow = TerraSplinesWindow.GetOpenWindow();
            if (openWindow != null)
                return openWindow.CurrentGlobalBrushSize;

            return settings.sizeMeters;
        }

        static int ComputeSplinePreviewId(SplineContainer container, int splineIndex)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + UnityObjectIdUtility.GetObjectHash(container);
                hash = hash * 31 + splineIndex;
                return hash;
            }
        }

        static int ComputePreviewSplineVersion(SplineContainer container, Spline spline)
        {
            if (container == null || spline == null)
                return 0;

            unchecked
            {
                int hash = 17;
                var transform = container.transform;
                if (transform != null)
                {
                    hash = hash * 31 + transform.position.GetHashCode();
                    hash = hash * 31 + transform.rotation.GetHashCode();
                    hash = hash * 31 + transform.lossyScale.GetHashCode();
                }

                hash = hash * 31 + spline.Count;
                hash = hash * 31 + spline.Closed.GetHashCode();

                for (int i = 0; i < spline.Count; i++)
                {
                    var knot = spline[i];
                    hash = hash * 31 + knot.Position.GetHashCode();
                    hash = hash * 31 + knot.TangentIn.GetHashCode();
                    hash = hash * 31 + knot.TangentOut.GetHashCode();
                    hash = hash * 31 + knot.Rotation.GetHashCode();
                }

                return hash;
            }
        }

        static int ComputePreviewSettingsHash(SplineStrokeSettings settings, float baseWidth)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + baseWidth.GetHashCode();

                bool usesSizeMultiplier = settings.overrideSizeMultiplier && settings.sizeMultiplier != null;
                hash = hash * 31 + usesSizeMultiplier.GetHashCode();
                if (usesSizeMultiplier)
                    hash = hash * 31 + settings.sizeMultiplier.GetAnimationCurveHash();

                return hash;
            }
        }

        static bool ShouldUseInteractivePreview(int splineId)
        {
            if (!livePreviewStates.TryGetValue(splineId, out var liveState) || liveState?.polylines == null)
                return false;

            return EditorApplication.timeSinceStartup - liveState.lastInteractionTime < HighQualityIdleDelaySeconds;
        }

        static void DrawInteractivePreview(int splineId, int splineVersion, List<Vector3> centerPoints, List<float> halfWidths, bool isClosed, bool isInteractive,
            int previewSettingsHash)
        {
            if (!livePreviewStates.TryGetValue(splineId, out var liveState))
            {
                liveState = new LivePreviewState { splineId = splineId };
                livePreviewStates[splineId] = liveState;
            }

            if (isInteractive)
                liveState.lastInteractionTime = EditorApplication.timeSinceStartup;

            bool shouldRebuild = liveState.polylines == null ||
                liveState.lastComputedVersion != splineVersion ||
                liveState.lastPreviewSettingsHash != previewSettingsHash;

            if (shouldRebuild)
            {
                liveState.polylines = BuildFastPreviewPolylines(centerPoints, halfWidths, isClosed);
                liveState.lastComputedVersion = splineVersion;
                liveState.lastPreviewSettingsHash = previewSettingsHash;
            }

            DrawPolylines(liveState.polylines);
        }

        static void EnqueueHighQualityBuild(SplineContainer container, int splineId, int splineVersion, int previewSettingsHash, int contourGridSize, bool isClosed,
            List<Vector3> centerPoints, List<float> halfWidths)
        {
            int priority = 0;
            if (Selection.activeGameObject == container.gameObject)
                priority = 2;
            else if (Selection.Contains(container.gameObject))
                priority = 1;

            lock (highQualityBuildLock)
            {
                pendingHighQualityBuilds[splineId] = new HighQualityBuildRequest
                {
                    splineId = splineId,
                    splineVersion = splineVersion,
                    previewSettingsHash = previewSettingsHash,
                    contourGridSize = contourGridSize,
                    isClosed = isClosed,
                    generation = highQualityBuildGeneration,
                    priority = priority,
                    sequence = ++buildRequestSequence,
                    centerPoints = new List<Vector3>(centerPoints),
                    halfWidths = new List<float>(halfWidths)
                };

                Monitor.PulseAll(highQualityBuildLock);
            }
        }

        static void ProcessPendingHighQualityBuilds()
        {
            if (!IsGizmosFeatureEnabled())
            {
                lock (highQualityBuildLock)
                {
                    pendingHighQualityBuilds.Clear();
                    completedHighQualityBuilds.Clear();
                }
                return;
            }

            StartHighQualityBuildWorkers();

            List<HighQualityBuildResult> completedBuilds = null;
            lock (highQualityBuildLock)
            {
                if (completedHighQualityBuilds.Count > 0)
                {
                    completedBuilds = new List<HighQualityBuildResult>(completedHighQualityBuilds);
                    completedHighQualityBuilds.Clear();
                }
            }

            if (completedBuilds == null || completedBuilds.Count == 0)
                return;

            bool shouldRepaint = false;
            for (int i = 0; i < completedBuilds.Count; i++)
            {
                var completedBuild = completedBuilds[i];
                if (completedBuild?.request == null || completedBuild.request.generation != highQualityBuildGeneration)
                    continue;

                if (IsBuildRequestSuperseded(completedBuild.request))
                    continue;

                if (HasMatchingCachedContour(completedBuild.request.splineId, completedBuild.request.splineVersion,
                    completedBuild.request.previewSettingsHash, completedBuild.request.contourGridSize))
                    continue;

                StoreContour(
                    completedBuild.request.splineId,
                    completedBuild.request.splineVersion,
                    completedBuild.request.previewSettingsHash,
                    completedBuild.request.contourGridSize,
                    completedBuild.polylines);
                shouldRepaint = true;
            }

            if (shouldRepaint)
                SceneView.RepaintAll();
        }

        static bool HasMatchingCachedContour(int splineId, int splineVersion, int previewSettingsHash, int contourGridSize)
        {
            if (!contourCache.TryGetValue(splineId, out var cacheEntry) || cacheEntry?.polylines == null)
                return false;

            return cacheEntry.splineVersion == splineVersion &&
                cacheEntry.previewSettingsHash == previewSettingsHash &&
                cacheEntry.contourGridSize == contourGridSize;
        }

        static bool IsBuildRequestSuperseded(HighQualityBuildRequest request)
        {
            lock (highQualityBuildLock)
            {
                if (pendingHighQualityBuilds.TryGetValue(request.splineId, out var pendingRequest))
                    return pendingRequest.sequence > request.sequence;
            }

            return false;
        }

        static void StartHighQualityBuildWorkers()
        {
            lock (highQualityBuildLock)
            {
                if (highQualityBuildWorkers != null)
                    return;

                stopHighQualityBuildWorkers = false;
                highQualityBuildWorkers = new Task[HighQualityBuildWorkerCount];
                for (int i = 0; i < highQualityBuildWorkers.Length; i++)
                {
                    highQualityBuildWorkers[i] = Task.Factory.StartNew(
                        HighQualityBuildWorkerLoop,
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                }
            }
        }

        static void StopHighQualityBuildWorkers()
        {
            Task[] workersToWait = null;

            lock (highQualityBuildLock)
            {
                if (highQualityBuildWorkers == null)
                    return;

                stopHighQualityBuildWorkers = true;
                pendingHighQualityBuilds.Clear();
                completedHighQualityBuilds.Clear();
                highQualityBuildGeneration++;
                workersToWait = highQualityBuildWorkers;
                highQualityBuildWorkers = null;
                Monitor.PulseAll(highQualityBuildLock);
            }

            try
            {
                Task.WaitAll(workersToWait, 250);
            }
            catch
            {
            }
        }

        static void ClearQueuedHighQualityBuilds()
        {
            lock (highQualityBuildLock)
            {
                highQualityBuildGeneration++;
                pendingHighQualityBuilds.Clear();
                completedHighQualityBuilds.Clear();
            }
        }

        static void HighQualityBuildWorkerLoop()
        {
            while (true)
            {
                HighQualityBuildRequest request = null;
                lock (highQualityBuildLock)
                {
                    while (!stopHighQualityBuildWorkers && !TryDequeueNextHighQualityBuildRequest(out request))
                    {
                        Monitor.Wait(highQualityBuildLock);
                    }

                    if (stopHighQualityBuildWorkers)
                        return;
                }

                if (request == null)
                    continue;

                List<Vector3[]> polylines;
                try
                {
                    polylines = BuildContourPolylines(request.centerPoints, request.halfWidths, request.isClosed, request.contourGridSize);
                }
                catch
                {
                    continue;
                }

                lock (highQualityBuildLock)
                {
                    if (stopHighQualityBuildWorkers)
                        return;

                    completedHighQualityBuilds.Add(new HighQualityBuildResult
                    {
                        request = request,
                        polylines = polylines
                    });
                }
            }
        }

        static bool TryDequeueNextHighQualityBuildRequest(out HighQualityBuildRequest nextRequest)
        {
            nextRequest = null;
            foreach (var request in pendingHighQualityBuilds.Values)
            {
                if (nextRequest == null || request.priority > nextRequest.priority ||
                    (request.priority == nextRequest.priority && request.sequence < nextRequest.sequence))
                {
                    nextRequest = request;
                }
            }

            if (nextRequest == null)
                return false;

            pendingHighQualityBuilds.Remove(nextRequest.splineId);
            return true;
        }

        static List<Vector3[]> BuildFastPreviewPolylines(List<Vector3> centerPoints, List<float> halfWidths, bool isClosed)
        {
            int count = centerPoints.Count;
            if (count < 2)
                return new List<Vector3[]>();

            float maxWidth = 0f;
            for (int i = 0; i < halfWidths.Count; i++)
                maxWidth = Mathf.Max(maxWidth, halfWidths[i] * 2f);

            if (maxWidth <= NarrowWidthFastRailThreshold)
                return BuildOffsetRailPolylines(centerPoints, halfWidths, isClosed);

            int targetSamples = Mathf.Min(MaxFastPreviewSamples, count);
            var reducedCenters = new List<Vector3>(targetSamples + 1);
            var reducedHalfWidths = new List<float>(targetSamples + 1);
            var selectedIndices = SelectFastPreviewSampleIndices(centerPoints, halfWidths, targetSamples, isClosed);

            for (int i = 0; i < selectedIndices.Count; i++)
            {
                int index = selectedIndices[i];
                reducedCenters.Add(centerPoints[index]);
                reducedHalfWidths.Add(halfWidths[index]);
            }

            if (reducedCenters.Count < 2)
                return new List<Vector3[]>();

            return BuildContourPolylines(reducedCenters, reducedHalfWidths, isClosed, FastPreviewContourGridSize);
        }

        static List<Vector3[]> BuildOffsetRailPolylines(List<Vector3> centerPoints, List<float> halfWidths, bool isClosed)
        {
            int count = centerPoints.Count;
            if (count < 2)
                return new List<Vector3[]>();

            int outputCount = isClosed ? count + 1 : count;
            var leftRail = new Vector3[outputCount];
            var rightRail = new Vector3[outputCount];

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = ComputeOffsetDirection(centerPoints, halfWidths, i, isClosed);
                Vector3 center = centerPoints[i];
                Vector3 worldOffset = new Vector3(offset.x, 0f, offset.y);

                leftRail[i] = center + worldOffset;
                rightRail[i] = center - worldOffset;
            }

            if (isClosed)
            {
                leftRail[count] = leftRail[0];
                rightRail[count] = rightRail[0];
            }

            return new List<Vector3[]>
            {
                leftRail,
                rightRail
            };
        }

        static Vector2 ComputeOffsetDirection(List<Vector3> centerPoints, List<float> halfWidths, int index, bool isClosed)
        {
            int count = centerPoints.Count;
            float halfWidth = halfWidths[index];

            if (!isClosed && index == 0)
                return BuildSegmentNormal(centerPoints[0], centerPoints[1]) * halfWidth;

            if (!isClosed && index == count - 1)
                return BuildSegmentNormal(centerPoints[count - 2], centerPoints[count - 1]) * halfWidth;

            Vector2 incomingNormal = BuildSegmentNormal(
                centerPoints[GetWrappedIndex(index - 1, count, isClosed)],
                centerPoints[index]);
            Vector2 outgoingNormal = BuildSegmentNormal(
                centerPoints[index],
                centerPoints[GetWrappedIndex(index + 1, count, isClosed)]);

            Vector2 joinNormal = incomingNormal + outgoingNormal;
            if (joinNormal.sqrMagnitude <= MinSegmentLength2D)
                return outgoingNormal * halfWidth;

            joinNormal.Normalize();
            float denominator = Mathf.Abs(Vector2.Dot(joinNormal, outgoingNormal));
            float miterScale = denominator <= 0.001f ? 1f : Mathf.Min(MaxRailMiterScale, 1f / denominator);
            return joinNormal * halfWidth * miterScale;
        }

        static Vector2 BuildSegmentNormal(Vector3 from, Vector3 to)
        {
            Vector2 direction = new Vector2(to.x - from.x, to.z - from.z);
            float magnitude = direction.magnitude;
            if (magnitude <= MinSegmentLength2D)
                return Vector2.right;

            direction /= magnitude;
            return new Vector2(-direction.y, direction.x);
        }

        static List<int> SelectFastPreviewSampleIndices(List<Vector3> centerPoints, List<float> halfWidths, int targetSamples, bool isClosed)
        {
            int count = centerPoints.Count;
            if (count <= targetSamples)
            {
                var allIndices = new List<int>(count);
                for (int i = 0; i < count; i++)
                    allIndices.Add(i);

                return allIndices;
            }

            var selected = new HashSet<int>();
            if (!isClosed)
            {
                selected.Add(0);
                selected.Add(count - 1);
            }
            else
            {
                selected.Add(0);
            }

            int maxSamples = Mathf.Min(targetSamples, count);
            int remainingBudget = Mathf.Max(0, maxSamples - selected.Count);

            int backboneTarget = Mathf.Clamp(Mathf.CeilToInt(maxSamples * 0.4f), 2, maxSamples);
            AddUniformBackboneIndices(selected, count, backboneTarget, isClosed);

            remainingBudget = Mathf.Max(0, maxSamples - selected.Count);
            int cornerBudget = Mathf.CeilToInt(remainingBudget * 0.6f);
            int widthBudget = remainingBudget - cornerBudget;

            var rankedCorners = RankCornerIndices(centerPoints, isClosed);
            AddTopRankedIndices(selected, rankedCorners, maxSamples, cornerBudget);

            var rankedWidthFeatures = RankWidthFeatureIndices(halfWidths, isClosed);
            AddTopRankedIndices(selected, rankedWidthFeatures, maxSamples, widthBudget);

            if (selected.Count < maxSamples)
            {
                AddUniformBackboneIndices(selected, count, maxSamples, isClosed);
            }

            var ordered = selected.ToList();
            ordered.Sort();
            return ordered;
        }

        static void AddUniformBackboneIndices(HashSet<int> selected, int count, int targetCount, bool isClosed)
        {
            if (targetCount <= 0 || count <= 0)
                return;

            if (!isClosed)
            {
                if (targetCount == 1)
                {
                    selected.Add(0);
                    return;
                }

                float denominator = Mathf.Max(1, targetCount - 1);
                for (int i = 0; i < targetCount; i++)
                {
                    int index = Mathf.RoundToInt(i * (count - 1) / denominator);
                    selected.Add(Mathf.Clamp(index, 0, count - 1));
                }

                return;
            }

            for (int i = 0; i < targetCount; i++)
            {
                int index = Mathf.FloorToInt(i * count / (float)targetCount) % count;
                selected.Add(index);
            }
        }

        static List<(int index, float score)> RankCornerIndices(List<Vector3> centerPoints, bool isClosed)
        {
            int count = centerPoints.Count;
            var ranked = new List<(int index, float score)>(count);
            for (int i = 0; i < count; i++)
            {
                if (!isClosed && (i == 0 || i == count - 1))
                    continue;

                Vector3 previous = centerPoints[GetWrappedIndex(i - 1, count, isClosed)];
                Vector3 current = centerPoints[i];
                Vector3 next = centerPoints[GetWrappedIndex(i + 1, count, isClosed)];

                Vector2 incoming = new Vector2(current.x - previous.x, current.z - previous.z);
                Vector2 outgoing = new Vector2(next.x - current.x, next.z - current.z);
                float incomingMagnitude = incoming.magnitude;
                float outgoingMagnitude = outgoing.magnitude;
                if (incomingMagnitude <= MinSegmentLength2D || outgoingMagnitude <= MinSegmentLength2D)
                    continue;

                incoming /= incomingMagnitude;
                outgoing /= outgoingMagnitude;
                float turnScore = 1f - Mathf.Clamp01(Vector2.Dot(incoming, outgoing));
                ranked.Add((i, turnScore));
            }

            ranked.Sort((a, b) => b.score.CompareTo(a.score));
            return ranked;
        }

        static List<(int index, float score)> RankWidthFeatureIndices(List<float> halfWidths, bool isClosed)
        {
            int count = halfWidths.Count;
            var ranked = new List<(int index, float score)>(count);
            for (int i = 0; i < count; i++)
            {
                if (!isClosed && (i == 0 || i == count - 1))
                    continue;

                int previousIndex = GetWrappedIndex(i - 1, count, isClosed);
                int nextIndex = GetWrappedIndex(i + 1, count, isClosed);
                float previousWidth = halfWidths[previousIndex];
                float currentWidth = halfWidths[i];
                float nextWidth = halfWidths[nextIndex];

                float gradientScore = Mathf.Abs(currentWidth - previousWidth) + Mathf.Abs(nextWidth - currentWidth);
                bool isExtremum = (currentWidth >= previousWidth && currentWidth >= nextWidth) ||
                    (currentWidth <= previousWidth && currentWidth <= nextWidth);
                float extremumBonus = isExtremum ? Mathf.Abs((currentWidth * 2f) - previousWidth - nextWidth) : 0f;
                float score = gradientScore + extremumBonus;
                ranked.Add((i, score));
            }

            ranked.Sort((a, b) => b.score.CompareTo(a.score));
            return ranked;
        }

        static void AddTopRankedIndices(HashSet<int> selected, List<(int index, float score)> rankedIndices, int maxSamples, int budget)
        {
            if (budget <= 0)
                return;

            int added = 0;
            for (int i = 0; i < rankedIndices.Count && selected.Count < maxSamples && added < budget; i++)
            {
                if (selected.Add(rankedIndices[i].index))
                    added++;
            }
        }

        static int GetWrappedIndex(int index, int count, bool isClosed)
        {
            if (isClosed)
            {
                index %= count;
                if (index < 0)
                    index += count;
                return index;
            }

            return Mathf.Clamp(index, 0, count - 1);
        }

        static Vector3[] BuildCirclePolyline(Vector3 center, float radius, int sideCount)
        {
            int pointCount = Mathf.Max(6, sideCount) + 1;
            var points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (i / (float)(pointCount - 1)) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                points[i] = new Vector3(center.x + x, center.y, center.z + z);
            }

            return points;
        }

        static bool TryDrawCachedContour(int splineId, int splineVersion, int previewSettingsHash, int contourGridSize)
        {
            if (!contourCache.TryGetValue(splineId, out var cacheEntry) || cacheEntry?.polylines == null)
                return false;

            if (cacheEntry.splineVersion != splineVersion ||
                cacheEntry.previewSettingsHash != previewSettingsHash ||
                cacheEntry.contourGridSize != contourGridSize)
                return false;

            DrawPolylines(cacheEntry.polylines);
            return true;
        }

        static void StoreContour(int splineId, int splineVersion, int previewSettingsHash, int contourGridSize, List<Vector3[]> polylines)
        {
            if (contourCache.Count >= MaxCachedContours)
                contourCache.Clear();

            contourCache[splineId] = new ContourCacheEntry
            {
                splineId = splineId,
                splineVersion = splineVersion,
                previewSettingsHash = previewSettingsHash,
                contourGridSize = contourGridSize,
                polylines = polylines
            };
        }

        static List<Vector3[]> BuildContourPolylines(List<Vector3> centerPoints, List<float> halfWidths, bool isClosed, int contourGridSize)
        {
            int count = centerPoints.Count;
            var segments = new List<LineSegment>();
            if (count < 2)
                return new List<Vector3[]>();

            Bounds bounds = BuildContourBounds(centerPoints, halfWidths);
            float sizeX = Mathf.Max(bounds.size.x, 0.01f);
            float sizeZ = Mathf.Max(bounds.size.z, 0.01f);
            float baseCellSize = Mathf.Clamp(Mathf.Max(sizeX, sizeZ) / contourGridSize, 0.1f, 2f);

            int cellsX = Mathf.Clamp(Mathf.CeilToInt(sizeX / baseCellSize), 2, contourGridSize);
            int cellsZ = Mathf.Clamp(Mathf.CeilToInt(sizeZ / baseCellSize), 2, contourGridSize);
            float cellSizeX = sizeX / cellsX;
            float cellSizeZ = sizeZ / cellsZ;

            var field = new float[cellsX + 1, cellsZ + 1];
            for (int z = 0; z <= cellsZ; z++)
            {
                float worldZ = bounds.min.z + z * cellSizeZ;
                for (int x = 0; x <= cellsX; x++)
                {
                    float worldX = bounds.min.x + x * cellSizeX;
                    field[x, z] = EvaluateDistanceField(worldX, worldZ, centerPoints, halfWidths, isClosed);
                }
            }

            for (int z = 0; z < cellsZ; z++)
            {
                float z0 = bounds.min.z + z * cellSizeZ;
                float z1 = z0 + cellSizeZ;
                for (int x = 0; x < cellsX; x++)
                {
                    float x0 = bounds.min.x + x * cellSizeX;
                    float x1 = x0 + cellSizeX;
                    AddMarchingSquaresCellSegments(segments, x0, x1, z0, z1, centerPoints, isClosed,
                        field[x, z],
                        field[x + 1, z],
                        field[x + 1, z + 1],
                        field[x, z + 1]);
                }
            }

            var polylines = StitchSegmentsToPolylines(segments);
            return polylines;
        }

        static void DrawPolylines(List<Vector3[]> polylines)
        {
            DrawPolylinesPass(polylines, RailOccludedColor, UnityEngine.Rendering.CompareFunction.Greater);
            DrawPolylinesPass(polylines, RailColor, UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        static void DrawPolylinesPass(List<Vector3[]> polylines, Color color, UnityEngine.Rendering.CompareFunction zTest)
        {
            if (polylines == null)
                return;

            Material material = GetOrCreateLineMaterial();
            if (material == null)
                return;

            material.SetInt("_ZTest", (int)zTest);
            material.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < polylines.Count; i++)
            {
                var polyline = polylines[i];
                if (polyline == null || polyline.Length < 2)
                    continue;

                for (int pointIndex = 1; pointIndex < polyline.Length; pointIndex++)
                {
                    GL.Vertex(polyline[pointIndex - 1]);
                    GL.Vertex(polyline[pointIndex]);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        static Material GetOrCreateLineMaterial()
        {
            if (lineMaterial != null)
                return lineMaterial;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                return null;

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

            return lineMaterial;
        }

        static List<Vector3[]> StitchSegmentsToPolylines(List<LineSegment> segments)
        {
            var remaining = new List<LineSegment>(segments);
            var polylines = new List<Vector3[]>();

            while (remaining.Count > 0)
            {
                var firstSegment = remaining[remaining.Count - 1];
                remaining.RemoveAt(remaining.Count - 1);

                var points = new List<Vector3>
                {
                    firstSegment.from,
                    firstSegment.to
                };

                bool extended;
                do
                {
                    extended = false;
                    for (int i = remaining.Count - 1; i >= 0; i--)
                    {
                        var segment = remaining[i];
                        Vector3 start = points[0];
                        Vector3 end = points[points.Count - 1];

                        if (ArePointsNear(end, segment.from))
                        {
                            AppendPoint(points, segment.to);
                        }
                        else if (ArePointsNear(end, segment.to))
                        {
                            AppendPoint(points, segment.from);
                        }
                        else if (ArePointsNear(start, segment.to))
                        {
                            PrependPoint(points, segment.from);
                        }
                        else if (ArePointsNear(start, segment.from))
                        {
                            PrependPoint(points, segment.to);
                        }
                        else
                        {
                            continue;
                        }

                        remaining.RemoveAt(i);
                        extended = true;
                    }
                }
                while (extended);

                if (points.Count > 2 && ArePointsNear(points[0], points[points.Count - 1]))
                {
                    points[points.Count - 1] = points[0];
                }

                polylines.Add(points.ToArray());
            }

            return polylines;
        }

        static bool ArePointsNear(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude <= PolylineJoinTolerance * PolylineJoinTolerance;
        }

        static void AppendPoint(List<Vector3> points, Vector3 point)
        {
            if (!ArePointsNear(points[points.Count - 1], point))
                points.Add(point);
        }

        static void PrependPoint(List<Vector3> points, Vector3 point)
        {
            if (!ArePointsNear(points[0], point))
                points.Insert(0, point);
        }

        static Bounds BuildContourBounds(List<Vector3> centerPoints, List<float> halfWidths)
        {
            float maxHalfWidth = 0f;
            var bounds = new Bounds(centerPoints[0], Vector3.zero);
            for (int i = 0; i < centerPoints.Count; i++)
            {
                bounds.Encapsulate(centerPoints[i]);
                maxHalfWidth = Mathf.Max(maxHalfWidth, halfWidths[i]);
            }

            bounds.Expand(new Vector3(maxHalfWidth * 2f + 1f, 0f, maxHalfWidth * 2f + 1f));
            return bounds;
        }

        static float ComputeAverageHeight(List<Vector3> centerPoints)
        {
            float sum = 0f;
            for (int i = 0; i < centerPoints.Count; i++)
                sum += centerPoints[i].y;

            return sum / centerPoints.Count;
        }

        static float EvaluateDistanceField(float x, float z, List<Vector3> centerPoints, List<float> halfWidths, bool isClosed)
        {
            float best = float.PositiveInfinity;
            int segmentCount = isClosed ? centerPoints.Count : centerPoints.Count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % centerPoints.Count;
                float candidate = DistanceToVariableRadiusSegment(
                    new Vector2(x, z),
                    new Vector2(centerPoints[i].x, centerPoints[i].z),
                    new Vector2(centerPoints[nextIndex].x, centerPoints[nextIndex].z),
                    halfWidths[i],
                    halfWidths[nextIndex]);

                if (candidate < best)
                    best = candidate;
            }

            return best;
        }

        static float DistanceToVariableRadiusSegment(Vector2 point, Vector2 a, Vector2 b, float radiusA, float radiusB)
        {
            Vector2 ab = b - a;
            float abLengthSq = ab.sqrMagnitude;
            if (abLengthSq <= MinSegmentLength2D)
                return Vector2.Distance(point, a) - radiusA;

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / abLengthSq);
            Vector2 closest = a + ab * t;
            float radius = Mathf.Lerp(radiusA, radiusB, t);
            return Vector2.Distance(point, closest) - radius;
        }

        static void AddMarchingSquaresCellSegments(List<LineSegment> segments, float x0, float x1, float z0, float z1,
            List<Vector3> centerPoints, bool isClosed, float bottomLeft, float bottomRight, float topRight, float topLeft)
        {
            var intersections = new List<Vector3>(4);

            TryAddIntersection(intersections, new Vector2(x0, z0), new Vector2(x1, z0), bottomLeft, bottomRight, centerPoints, isClosed);
            TryAddIntersection(intersections, new Vector2(x1, z0), new Vector2(x1, z1), bottomRight, topRight, centerPoints, isClosed);
            TryAddIntersection(intersections, new Vector2(x1, z1), new Vector2(x0, z1), topRight, topLeft, centerPoints, isClosed);
            TryAddIntersection(intersections, new Vector2(x0, z1), new Vector2(x0, z0), topLeft, bottomLeft, centerPoints, isClosed);

            if (intersections.Count == 2)
            {
                segments.Add(new LineSegment(intersections[0], intersections[1]));
                return;
            }

            if (intersections.Count == 4)
            {
                segments.Add(new LineSegment(intersections[0], intersections[1]));
                segments.Add(new LineSegment(intersections[2], intersections[3]));
            }
        }

        static void TryAddIntersection(List<Vector3> intersections, Vector2 a, Vector2 b, float valueA, float valueB,
            List<Vector3> centerPoints, bool isClosed)
        {
            bool isInsideA = valueA <= 0f;
            bool isInsideB = valueB <= 0f;
            if (isInsideA == isInsideB)
                return;

            float denominator = valueA - valueB;
            float t = Mathf.Abs(denominator) <= 0.00001f ? 0.5f : valueA / denominator;
            Vector2 pointXZ = Vector2.Lerp(a, b, Mathf.Clamp01(t));
            float y = SampleSplineHeightAtXZ(pointXZ, centerPoints, isClosed);
            Vector3 point = new Vector3(pointXZ.x, y, pointXZ.y);
            if (intersections.Count == 0 || Vector3.Distance(intersections[intersections.Count - 1], point) > 0.001f)
            {
                intersections.Add(point);
            }
        }

        static float SampleSplineHeightAtXZ(Vector2 pointXZ, List<Vector3> centerPoints, bool isClosed)
        {
            float bestDistanceSq = float.PositiveInfinity;
            float bestHeight = centerPoints[0].y;
            int segmentCount = isClosed ? centerPoints.Count : centerPoints.Count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % centerPoints.Count;
                Vector3 start = centerPoints[i];
                Vector3 end = centerPoints[nextIndex];
                Vector2 startXZ = new Vector2(start.x, start.z);
                Vector2 endXZ = new Vector2(end.x, end.z);
                Vector2 segment = endXZ - startXZ;
                float segmentLengthSq = segment.sqrMagnitude;

                float t = segmentLengthSq <= MinSegmentLength2D
                    ? 0f
                    : Mathf.Clamp01(Vector2.Dot(pointXZ - startXZ, segment) / segmentLengthSq);

                Vector2 closestXZ = startXZ + segment * t;
                float distanceSq = (pointXZ - closestXZ).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                bestHeight = Mathf.Lerp(start.y, end.y, t);
            }

            return bestHeight;
        }
    }
}