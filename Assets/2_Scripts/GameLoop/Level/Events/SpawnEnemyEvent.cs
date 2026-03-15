using System;
using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    [Header("Spawn")]
    public int enemiesPerWave = 3;
    public float spawnInterval = 1f;
    
    [Header("Position")]
    public SpawnPosition spawnPosition = SpawnPosition.Random;
    [ShowIf("spawnPosition", SpawnPosition.Specific)] public ExposedReference<EnemySpawnPoint> spawnPoint;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        switch (spawnPosition)
        {
            case SpawnPosition.Random:
                EnemyManager.Instance?.SpawnEnemyWaveRandomPosition(enemiesPerWave);
                break;
            case SpawnPosition.Specific:
                EnemyManager.Instance?.SpawnEnemyWaveSpecificPosition(enemiesPerWave, spawnPoint.Resolve(resolver));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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