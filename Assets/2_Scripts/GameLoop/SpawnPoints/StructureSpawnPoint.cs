using DNExtensions.Utilities;
using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class StructureSpawnPoint : BaseSpawnPoint
    {
        [Header("Settings")]
        [Tooltip("Whether the spawn should happen at the start of the level or be triggered by an event.")]
        [SerializeField] private bool spawnAtStart = true;
        [SerializeField, EnableIf("spawnAtStart")] private bool enablePodCamera;
        [SerializeField, EnableIf("spawnAtStart"), PrefabSelector("Assets/Prefabs/Structures")] private Structure structurePrefab;

        private bool _hasSpawned;

        private void OnValidate()
        {
            if (Application.isPlaying || gameObject.scene.name == null) return;

            if (structurePrefab && spawnAtStart)
            {
                gameObject.name = $"StructureSpawnPointAtStart({structurePrefab.StructureUIData.Label})";
            }
            else
            {
                gameObject.name = "StructureSpawnPoint";
            }
        }

        private void SpawnStartStructure() => SpawnStructure(structurePrefab, enablePodCamera);

        protected override void OnLevelStarted()
        {
            if (spawnAtStart)
            {
                SpawnStartStructure();
            }
        }

        
        public void SpawnStructure(Structure structure, bool enablePodCamera)
        {
            if (!structure || _hasSpawned) return;

            _hasSpawned = true;
            StructureManager.Instance?.DeployStructureOnGround(structure, transform.position, transform.forward, transform.up, enablePodCamera);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_hasSpawned) return;

            if (spawnAtStart)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 2);

            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(transform.position, 2);
            }

            Handles.Label(
                transform.position + Vector3.up * (2 + 0.5f),
                structurePrefab && spawnAtStart
                    ? $"Start Structure Spawn Point: {structurePrefab.StructureUIData.Label}"
                    : $"Structure Spawn Point",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }

#endif
    }
}