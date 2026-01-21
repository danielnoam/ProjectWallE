using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// A utility class providing methods to handle and manage spline-to-terrain bridge operations.
    /// Initializes event listeners on load to monitor and respond to object changes, duplication, and paste operations specific to splines.
    /// </summary>
    [InitializeOnLoad]
    public static class SplineToTerrainUtility
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
        static void OnPasteOrDuplicated(GameObject[] duplicates)
        {
            foreach (var duplicate in duplicates)
                CheckForSplineToTerrainCreatedOrModified(duplicate);
        }

        static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                    
#pragma warning disable CS0618 // Type or member is obsolete
                    if (EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) is GameObject go)
#pragma warning restore CS0618 // Type or member is obsolete
                        CheckForSplineToTerrainAdded(go);
                }
            }
        }
#else
        static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
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

        static void GameObjectCreatedOrStructureModified(int instanceId)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is GameObject go)
                CheckForSplineToTerrainCreatedOrModified(go);
        }
#endif

        static void CheckForSplineToTerrainAdded(GameObject go)
        {
            if (go.TryGetComponent<SplineToTerrain>(out var splineToTerrain))
                splineToTerrain.SetSplineContainerOnGO();

            var childCount = go.transform.childCount;
            if (childCount > 0)
            {
                for (int childIndex = 0; childIndex < childCount; ++childIndex)
                    CheckForSplineToTerrainAdded(go.transform.GetChild(childIndex).gameObject);
            }
        }

        static void CheckForSplineToTerrainCreatedOrModified(GameObject go)
        {
            if (go.TryGetComponent<SplineToTerrain>(out var component))
                component.Reset();

            var childCount = go.transform.childCount;
            if (childCount > 0)
            {
                for (int childIndex = 0; childIndex < childCount; ++childIndex)
                    CheckForSplineToTerrainCreatedOrModified(go.transform.GetChild(childIndex).gameObject);
            }
        }
    }
}