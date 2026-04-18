using System;
using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public class EnemySpawnerConfig
    {
        [Header("Interval")]
        public float spawnInterval = 3f;
        public int enemiesPerWave = 3;
        
        [Header("Enemies Type")]
        public SpawnType enemyType = SpawnType.Random;
        [InfoBox("Random - Will spawn randomly from the full enemy pool \n Specific - Will spawn from the enemies chance list")]
        [ShowIf("enemyType", SpawnType.Specific)] public ChanceList<Enemy> enemies;
        
        [Header("Position")]
        public SpawnType spawnPosition = SpawnType.Random;
        [InfoBox("Random - Will spawn randomly from the active spawners pool \n Specific - Will spawn in a specific spawn point")]
        [ShowIf("spawnPosition", SpawnType.Specific)] public ExposedReference<EnemySpawnPoint> spawnPoint;

        private float _timer;
        private EnemySpawnPoint _resolvedPoint;

        public void Initialize(IExposedPropertyTable resolver)
        {
            _timer = 0f;
            _resolvedPoint = spawnPosition == SpawnType.Specific ? spawnPoint.Resolve(resolver) : null;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < spawnInterval) return;
            _timer -= spawnInterval;

            var source = enemyType == SpawnType.Specific ? enemies : null;
            EnemyManager.Instance?.SpawnEnemyWave(enemiesPerWave, source, _resolvedPoint);
        }
    }
}