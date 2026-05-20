using System;
using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum EnemySourceType { Random, Specific }
    public enum SpawnPositionType { Random, Specific, RandomFromSpecific }

    [Serializable]
    public class EnemySpawnParams
    {
        [Header("Enemy")]
        [SerializeField, Min(1)] private int count = 3;
        [SerializeField] private EnemySourceType enemyType = EnemySourceType.Random;
        [SerializeField, ShowIf("enemyType", EnemySourceType.Specific)] private ChanceList<Enemy> enemies;

        [Header("Position")]
        [SerializeField] private SpawnPositionType spawnPosition = SpawnPositionType.Random;
        [SerializeField, ShowIf("spawnPosition", SpawnPositionType.Specific)] private ExposedReference<EnemySpawnPoint> spawnPoint;
        [SerializeField, ShowIf("spawnPosition", SpawnPositionType.RandomFromSpecific)] private ChanceList<ExposedReference<EnemySpawnPoint>> spawnPoints;

        private EnemySpawnPoint _resolvedPoint;
        private ChanceList<EnemySpawnPoint> _resolvedSpawnPoints;

        public void Initialize(IExposedPropertyTable resolver = null)
        {
            _resolvedPoint = spawnPosition == SpawnPositionType.Specific ? spawnPoint.Resolve(resolver) : null;

            if (spawnPosition == SpawnPositionType.RandomFromSpecific)
            {
                _resolvedSpawnPoints = new ChanceList<EnemySpawnPoint>();
                var rawList = spawnPoints.ToList();
                for (int i = 0; i < rawList.Count; i++)
                {
                    var resolved = rawList[i].Resolve(resolver);
                    if (resolved) _resolvedSpawnPoints.AddItem(resolved, spawnPoints.GetChance(i));
                }
            }
        }

        public void Spawn()
        {
            var source = enemyType == EnemySourceType.Specific ? enemies : null;

            EnemySpawnPoint point = spawnPosition switch
            {
                SpawnPositionType.Specific => _resolvedPoint,
                SpawnPositionType.RandomFromSpecific => _resolvedSpawnPoints?.GetRandomItem(),
                _ => null
            };

            EnemyManager.Instance?.SpawnEnemyWave(count, source, point);
        }
    }
}