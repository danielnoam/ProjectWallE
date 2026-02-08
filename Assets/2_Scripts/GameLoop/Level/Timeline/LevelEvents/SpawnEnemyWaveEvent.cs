using System;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveEventAsset : BaseLevelEventAsset
{
    [Min(1)] public int enemyCount = 5;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        EnemyManager.Instance?.SpawnEnemyWave(enemyCount);
    }
}