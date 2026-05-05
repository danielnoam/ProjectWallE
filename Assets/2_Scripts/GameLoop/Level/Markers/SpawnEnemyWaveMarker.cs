using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class SpawnEnemyWaveMarker : BaseLevelEventMarker
{
    [Header("Spawn")]
    [Min(1), SerializeField] private int enemyCount = 5;
    [SerializeField] private SpawnType enemyType = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the full enemy pool \n Specific - Will spawn from the enemies chance list")]
    [ShowIf("enemyType", SpawnType.Specific)] [SerializeField] private ChanceList<Enemy> enemies;
    
    [Header("Position")]
    [SerializeField] private SpawnType spawnType = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the active spawners pool \n Specific - Will spawn in a specific spawn point")]
    [ShowIf("spawnType", SpawnType.Specific)] [SerializeField] private ExposedReference<EnemySpawnPoint> spawnPoint;

    public int EnemyCount => enemyCount;
    
    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var enemySource = enemyType == SpawnType.Specific ? enemies : null;
        var point = spawnType == SpawnType.Specific ? spawnPoint.Resolve(resolver) : null;

        EnemyManager.Instance?.SpawnEnemyWave(enemyCount, enemySource, point);
    }
}