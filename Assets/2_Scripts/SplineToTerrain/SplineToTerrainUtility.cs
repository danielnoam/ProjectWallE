using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [InitializeOnLoad]
    internal static class SplineToTerrainUtility
    {
        static SplineToTerrainUtility()
        {
#if UNITY_2022_2_OR_NEWER
            ClipboardUtility.duplicatedGameObjects += OnPasteOrDuplicated;
            ClipboardUtility.pastedGameObjects += OnPasteOrDuplicated;
            ObjectChangeEvents.changesPublished += ObjectEventChangesPublished;
#else
            ObjectChangeEvents.changesPublished += ObjectEventChangesPublished;
#endif
        }

#if UNITY_2022_2_OR_NEWER
        private static void OnPasteOrDuplicated(GameObject[] duplicates)
        {
            foreach (var duplicate in duplicates)
                CheckForSplineToTerrainCreatedOrModified(duplicate);
        }

        private static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);

#pragma warning disable CS0618
                    if (EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) is GameObject go)
#pragma warning restore CS0618
                        CheckForSplineToTerrainAdded(go);
                }
            }
        }
#else
        private static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0, c = stream.length; i < c; ++i)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs data);
                    GameObjectCreatedOrStructureModified(data.instanceId);
                }
                else if (type == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                    if (EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) is GameObject go)
                        CheckForSplineToTerrainAdded(go);
                }
            }
        }

        private static void GameObjectCreatedOrStructureModified(int instanceId)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is GameObject go)
                CheckForSplineToTerrainCreatedOrModified(go);
        }
#endif

        private static void CheckForSplineToTerrainAdded(GameObject go)
        {
            if (go.TryGetComponent<SplineToTerrain>(out var splineToTerrain))
                splineToTerrain.SetSplineContainerOnGO();

            int childCount = go.transform.childCount;
            for (int childIndex = 0; childIndex < childCount; ++childIndex)
                CheckForSplineToTerrainAdded(go.transform.GetChild(childIndex).gameObject);
        }

        private static void CheckForSplineToTerrainCreatedOrModified(GameObject go)
        {
            if (go.TryGetComponent<SplineToTerrain>(out var component))
                component.Reset();

            int childCount = go.transform.childCount;
            for (int childIndex = 0; childIndex < childCount; ++childIndex)
                CheckForSplineToTerrainCreatedOrModified(go.transform.GetChild(childIndex).gameObject);
        }
    }
}