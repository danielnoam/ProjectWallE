using System;
using UnityEngine;

namespace GruelTerraSplines
{

    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class TerrainSplinesGroup : MonoBehaviour
    {
#if UNITY_EDITOR
        public static event Action GroupsChanged;
#endif

        [SerializeField] GameObject terrainGroup;
        [SerializeField] Transform splineGroup;

        public GameObject TerrainGroup => terrainGroup;
        public Transform SplineGroup => splineGroup;

        public bool IsValid => terrainGroup != null && splineGroup != null;

        public void SetReferences(GameObject terrainGroupReference, Transform splineGroupReference)
        {
            terrainGroup = terrainGroupReference;
            splineGroup = splineGroupReference;
#if UNITY_EDITOR
            NotifyGroupsChanged();
#endif
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            NotifyGroupsChanged();
        }

        void OnValidate()
        {
            NotifyGroupsChanged();
        }

        void OnDestroy()
        {
            NotifyGroupsChanged();
        }

        static void NotifyGroupsChanged()
        {
            if (Application.isPlaying)
                return;

            GroupsChanged?.Invoke();
        }
#endif
    }

}