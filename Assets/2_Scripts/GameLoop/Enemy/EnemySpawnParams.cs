using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum EnemySourceType { Random, Specific, ByType }
    public enum SpawnPositionType { Random, Specific }
    
    [Serializable]
    public class EnemySpawnEntry
    {
        public Enemy enemy;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class EnemySpawnParams
    {
        [Header("Enemy")]
        [SerializeField] private EnemySourceType enemyType = EnemySourceType.Random;
        [SerializeField, Min(1), EnableIf("ShowCount")] private int count = 3;
        [SerializeField, ShowIf("enemyType", EnemySourceType.Specific)] private ChanceList<Enemy> enemies;
        [SerializeField, ShowIf("enemyType", EnemySourceType.ByType)] private List<EnemySpawnEntry> enemyEntries;

        [Header("Position")]
        [SerializeField] private SpawnPositionType spawnPosition = SpawnPositionType.Random;
        [SerializeField, ShowIf("spawnPosition", SpawnPositionType.Specific), ScenePicker] private ChanceList<ExposedReference<EnemySpawnPoint>> spawnPoints;
        
        private ChanceList<EnemySpawnPoint> _resolvedSpawnPoints;
        
        private bool ShowCount => enemyType is EnemySourceType.Random or EnemySourceType.Specific;
        
        public void Initialize(IExposedPropertyTable resolver = null)
        {
            if (spawnPosition != SpawnPositionType.Specific) return;
    
            _resolvedSpawnPoints = new ChanceList<EnemySpawnPoint>();
            var rawList = spawnPoints.ToList();
            for (int i = 0; i < rawList.Count; i++)
            {
                var resolved = rawList[i].Resolve(resolver);
                if (resolved) _resolvedSpawnPoints.AddItem(resolved, spawnPoints.GetChance(i));
            }
        }

        public void Spawn()
        {
            EnemySpawnPoint point = spawnPosition switch
            {
                SpawnPositionType.Specific => _resolvedSpawnPoints?.GetRandomItem(),
                _ => null
            };

            if (enemyType == EnemySourceType.ByType)
            {
                if (enemyEntries == null) return;
                foreach (var entry in enemyEntries)
                {
                    if (!entry.enemy) continue;
                    var single = new ChanceList<Enemy>();
                    single.AddItem(entry.enemy);
                    EnemyManager.Instance?.SpawnEnemyWave(entry.count, single, point);
                }
                return;
            }

            var source = enemyType == EnemySourceType.Specific ? enemies : null;
            EnemyManager.Instance?.SpawnEnemyWave(count, source, point);
        }
    }
}