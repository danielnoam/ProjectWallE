using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    [Header("Interval")]
    public float spawnInterval = 1f;
    public int enemiesPerWave = 3;
    
    [Header("Spawn")]
    public SpawnType enemyType = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the full enemy pool \n Specific - Will spawn from the enemies chance list")]
    [ShowIf("enemyType", SpawnType.Specific)] public ChanceList<Enemy> enemies;
    
    [Header("Position")]
    public SpawnType spawnPosition = SpawnType.Random;
    [InfoBox("Random - Will spawn randomly from the active spawners pool \n Specific - Will spawn in a specific spawn point")]
    [ShowIf("spawnPosition", SpawnType.Specific)] public ExposedReference<EnemySpawnPoint> spawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        var enemySource = enemyType == SpawnType.Specific ? enemies : null;
        var point = spawnPosition == SpawnType.Specific ? spawnPoint.Resolve(resolver) : null;

        EnemyManager.Instance?.SpawnEnemyWave(enemiesPerWave, enemySource, point);
    }
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ContinuousEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventAsset = this;
        behaviour.Resolver = graph.GetResolver();
        behaviour.Interval = spawnInterval;
        return playable;
    }
}