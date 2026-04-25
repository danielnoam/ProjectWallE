using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class EnemySpawnPoint : BaseSpawnPoint
    {
        [Header("Settings")] 
        [SerializeField] private bool isActive = true;
        [SerializeField] private float spawnPointRange = 10f;
        [SerializeField] private float spawnHeight = 5f;

        public float SpawnPointRange => spawnPointRange;
        public float SpawnHeight => spawnHeight;

        private void OnValidate()
        {
            if (Application.isPlaying || gameObject.scene.name == null) return;

            gameObject.name = $"EnemySpawnPoint({(isActive ? "Active" : "Not Active")})";
        }


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
            Handles.color = isActive ? Color.red : Color.gray;
            Handles.DrawWireDisc(transform.position, Vector3.up, spawnPointRange);

            if (spawnHeight != 0)
            {
                Vector3 topPoint = transform.position + Vector3.up * spawnHeight;
                Handles.DrawLine(transform.position, topPoint);
                Handles.DrawWireDisc(topPoint, Vector3.up, 0.5f);
            }

            var enemyString = $"Enemy Spawn Point: {(isActive ? "Active" : "Not Active")}";
            Handles.Label(
                transform.position + Vector3.up * 10,
                enemyString,
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.red },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }

#endif
    }
}