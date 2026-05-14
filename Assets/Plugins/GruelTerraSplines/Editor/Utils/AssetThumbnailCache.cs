using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GruelTerraSplines
{
    static class AssetThumbnailCache
    {
        public static event System.Action PrefabThumbnailsChanged;

        sealed class CachedThumbnail
        {
            public Texture texture;
            public bool isFallback;
            public double nextRetryTime;
        }

        const double FallbackRetryIntervalSeconds = 0.5;

        static readonly Dictionary<EntityId, CachedThumbnail> PrefabThumbnails = new Dictionary<EntityId, CachedThumbnail>();
        static readonly Dictionary<EntityId, GameObject> PendingPrefabPreviews = new Dictionary<EntityId, GameObject>();

        static void EnsurePreviewPollingRegistered()
        {
            EditorApplication.update -= UpdatePendingPrefabPreviews;
            EditorApplication.update += UpdatePendingPrefabPreviews;
        }

        static void StopPreviewPollingIfIdle()
        {
            if (PendingPrefabPreviews.Count == 0)
            {
                EditorApplication.update -= UpdatePendingPrefabPreviews;
            }
        }

        static void TrackPendingPrefabPreview(GameObject prefab)
        {
            if (prefab == null)
                return;

            PendingPrefabPreviews[UnityObjectIdUtility.GetEntityId(prefab)] = prefab;
            EnsurePreviewPollingRegistered();
        }

        static void UpdatePendingPrefabPreviews()
        {
            if (PendingPrefabPreviews.Count == 0)
            {
                StopPreviewPollingIfIdle();
                return;
            }

            var completedIds = new List<EntityId>();
            bool changed = false;

            foreach (var entry in PendingPrefabPreviews)
            {
                EntityId instanceId = entry.Key;
                var prefab = entry.Value;
                if (prefab == null)
                {
                    completedIds.Add(instanceId);
                    continue;
                }

                var preview = AssetPreview.GetAssetPreview(prefab);
                if (preview != null)
                {
                    PrefabThumbnails[instanceId] = new CachedThumbnail
                    {
                        texture = preview,
                        isFallback = false,
                        nextRetryTime = 0d
                    };
                    completedIds.Add(instanceId);
                    changed = true;
                    continue;
                }

                if (!AssetPreview.IsLoadingAssetPreview(instanceId))
                {
                    completedIds.Add(instanceId);
                }
            }

            for (int i = 0; i < completedIds.Count; i++)
            {
                PendingPrefabPreviews.Remove(completedIds[i]);
            }

            StopPreviewPollingIfIdle();

            if (changed)
            {
                PrefabThumbnailsChanged?.Invoke();
            }
        }

        public static Texture GetPrefabThumbnail(GameObject prefab)
        {
            if (prefab == null)
                return Texture2D.grayTexture;

            EntityId instanceId = UnityObjectIdUtility.GetEntityId(prefab);
            double now = EditorApplication.timeSinceStartup;

            if (PrefabThumbnails.TryGetValue(instanceId, out var cached))
            {
                if (!cached.isFallback)
                    return cached.texture != null ? cached.texture : Texture2D.grayTexture;

                if (now < cached.nextRetryTime)
                    return cached.texture != null ? cached.texture : Texture2D.grayTexture;
            }

            var preview = AssetPreview.GetAssetPreview(prefab);
            if (preview != null)
            {
                PendingPrefabPreviews.Remove(instanceId);
                PrefabThumbnails[instanceId] = new CachedThumbnail
                {
                    texture = preview,
                    isFallback = false,
                    nextRetryTime = 0d
                };
                return preview;
            }

            var fallback = AssetPreview.GetMiniThumbnail(prefab);
            PrefabThumbnails[instanceId] = new CachedThumbnail
            {
                texture = fallback != null ? fallback : Texture2D.grayTexture,
                isFallback = true,
                nextRetryTime = now + FallbackRetryIntervalSeconds
            };

            TrackPendingPrefabPreview(prefab);

            return fallback != null ? fallback : Texture2D.grayTexture;
        }
    }
}