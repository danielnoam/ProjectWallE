using System;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveMarker : BaseLevelEventMarker
{
    [Min(1)] public int enemyCount = 5;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        EnemyManager.Instance?.TrySpawnEnemyWave(enemyCount);
    }
}