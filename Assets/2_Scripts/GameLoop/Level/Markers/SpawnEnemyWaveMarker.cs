using System;
using DNExtensions.Utilities;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveMarker : BaseLevelEventMarker
{
    [Header("Spawn")]
    [Min(1)] public int enemyCount = 5;
    
    [Header("Position")]
    public SpawnPosition spawnPosition = SpawnPosition.Random;
    [ShowIf("spawnPosition", SpawnPosition.Specific)] public ExposedReference<EnemySpawnPoint> spawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        switch (spawnPosition)
        {
            case SpawnPosition.Random:
                EnemyManager.Instance?.SpawnEnemyWaveRandomPosition(enemyCount);
                break;
            case SpawnPosition.Specific:
                EnemyManager.Instance?.SpawnEnemyWaveSpecificPosition(enemyCount, spawnPoint.Resolve(resolver));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}