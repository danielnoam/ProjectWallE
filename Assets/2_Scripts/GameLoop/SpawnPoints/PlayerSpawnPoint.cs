using DNExtensions.Utilities.Button;
using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    
    public class PlayerSpawnPoint : BaseSpawnPoint
    {
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
            LevelManager.Instance?.Player?.Teleport(this);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2);

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