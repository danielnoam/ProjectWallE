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
        [InfoBox("Leave the enemy field empty to allow any enemy to count towards the objective. (Works by type of class not prefab)")]
        [PrefabSelector("Assets/5_Prefabs/Enemies")] public Enemy enemyPrefab;
        [SerializeField, Min(1)] private int killCount = 10;

        private int _currentKills;

        public override string Description => enemyPrefab ? $"Kill {killCount} {enemyPrefab.name}" : $"Kill {killCount} enemies";

        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentKills}/{killCount}";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentKills = 0;
            Enemy.OnEnemyKilled += OnEnemyKilled;
        }

        public override void Dispose()
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
}