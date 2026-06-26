using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    [SerializableSelectorName("Kill", "Enemy")]
    public class KillEnemiesObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Leave the enemy field empty to allow any enemy to count towards the objective. (Works by type of class not prefab)")]
        [PrefabSelector("Assets/5_Prefabs/Enemies")] public Enemy enemyPrefab;
        [SerializeField, Min(1)] private int killCount = 10;

        private int _currentKills;

        public override string Description => enemyPrefab ? $"Kill {killCount} {enemyPrefab.name}" : $"Kill {killCount} enemies";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentKills}/{killCount}";

        protected override string DefaultTutorialText => "";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentKills = 0;
            Enemy.OnEnemyKilled += OnEnemyKilled;
        }

        protected override void OnDispose()
        {
            Enemy.OnEnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemyPrefab && enemy.GetType() != enemyPrefab.GetType()) return;

            _currentKills++;
            if (_currentKills >= killCount)
            {
                Complete();
            }
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Kill All", "Enemy")]
    public class KillAllEnemiesObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Grace time after all enemies are killed before the objective is marked as complete. This allows for any last second spawns or effects to occur.")]
        [SerializeField, Min(0f)] private float completionDelay = 2f;

        private float _graceTimer;
        private bool _hasSeenEnemy;

        public override string Description => "Kill all enemies";
        public override string ProgressText => IsCompleted ? "Complete" : $"{EnemyManager.Instance.ActiveEnemiesCount + EnemyManager.Instance.PendingEnemySpawns}";

        protected override string DefaultTutorialText => "";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _graceTimer = 0f;
            _hasSeenEnemy = EnemyManager.Instance.HasActiveOrPendingEnemies;
            Enemy.OnEnemyKilled += OnEnemyKilled;
        }

        protected override void OnDispose()
        {
            Enemy.OnEnemyKilled -= OnEnemyKilled;
        }

        public override void Tick(float deltaTime)
        {
            if (!_hasSeenEnemy) return;

            if (!EnemyManager.Instance.HasActiveOrPendingEnemies)
            {
                _graceTimer += deltaTime;
                if (_graceTimer >= completionDelay) Complete();
            }
            else
            {
                _graceTimer = 0f;
            }
        }

        private void OnEnemyKilled(Enemy enemy) => _hasSeenEnemy = true;
    }
}