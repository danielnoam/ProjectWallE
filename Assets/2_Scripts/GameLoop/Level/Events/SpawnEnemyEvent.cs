using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    public int enemiesPerWave = 3;
    public float spawnInterval = 1f;
    
    public override void Execute(IExposedPropertyTable resolver = null)
    {
        EnemyManager.Instance?.TrySpawnEnemyWave(enemiesPerWave);
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