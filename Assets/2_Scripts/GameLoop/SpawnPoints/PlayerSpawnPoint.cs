using DNExtensions.Utilities.Button;
using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class PlayerSpawnPoint : BaseSpawnPoint
    {
        [Header("Settings")] 
        [SerializeField] private float spawnHeight = 1f;
        [SerializeField] private float spawnRotationY;

        public Vector3 SpawnPosition => transform.position + Vector3.up * spawnHeight;
        public Quaternion SpawnRotation => Quaternion.Euler(0, spawnRotationY, 0); 
        
        private void OnValidate()
        {
            if (Application.isPlaying || gameObject.scene.name == null) return;
            gameObject.name = $"PlayerSpawnPoint";
        }

        [Button]
        public void SetAsActiveSpawnPoint()
        {
            LevelManager.Instance?.SetPlayerSpawnPoint(this);
        }

        [Button]
        public void TeleportPlayer()
        {
            LevelManager.Instance?.Player?.Teleport(SpawnPosition, SpawnRotation);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2);
            
            Vector3 topPoint = SpawnPosition;
            
            if (spawnHeight != 0)
            {
                Handles.DrawLine(transform.position, topPoint);
                Handles.DrawWireDisc(topPoint, Vector3.up, 0.5f);
            }
            
            Handles.ArrowHandleCap(0, topPoint, SpawnRotation, 1.5f, EventType.Repaint);

            Handles.Label(
                transform.position + Vector3.up * (2 + 0.5f),
                "Player Spawn Point",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.yellow },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }
#endif
    }
}