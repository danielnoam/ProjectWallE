using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveMarker : BaseLevelEventMarker
{
    [Header("Spawn")]
    [Min(1)] public int enemyCount = 5;
    public SpawnType enemyType = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the full enemy pool \n Specific - Will spawn from the enemies chance list")]
    [ShowIf("enemyType", SpawnType.Specific)] public ChanceList<Enemy> enemies;
    
    [Header("Position")]
    public SpawnType spawnType = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the active spawners pool \n Specific - Will spawn in a specific spawn point")]
    [ShowIf("spawnPosition", SpawnType.Specific)] public ExposedReference<EnemySpawnPoint> spawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        var enemySource = enemyType == SpawnType.Specific ? enemies : null;
        var point = spawnType == SpawnType.Specific ? spawnPoint.Resolve(resolver) : null;

        EnemyManager.Instance?.SpawnEnemyWave(enemyCount, enemySource, point);
    }
}