using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class EnemySpawnPoint : BaseSpawnPoint
    {
        [Header("Settings")] 
        [SerializeField] private bool isActive = true;
        [SerializeField] private float spawnPointRange = 10f;

        public float SpawnPointRange => spawnPointRange;

        protected override void OnLevelStarted()
        {
            if (isActive)
            {
                EnemyManager.Instance.RegisterSpawnPoint(this);
            }
        }

        protected override void OnLevelFinished()
        {
            EnemyManager.Instance.UnregisterSpawnPoint(this);
        }

        public void SetActiveState(bool state)
        {
            isActive = state;

            if (isActive)
            {
                EnemyManager.Instance.RegisterSpawnPoint(this);
            }
            else
            {
                EnemyManager.Instance.UnregisterSpawnPoint(this);
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Handles.color = !Application.isPlaying || isActive ? Color.red : Color.gray;
            Handles.DrawWireDisc(transform.position, Vector3.up, spawnPointRange);

            Vector3 topPoint = transform.position + Vector3.up * 2;
            Handles.DrawLine(transform.position, topPoint);
            Handles.DrawWireDisc(topPoint, Vector3.up, 0.5f);
            
            Handles.Label(
                transform.position + Vector3.up * (2 + 0.5f),
                "Enemy Spawn Point",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.red },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }

#endif
    }
}