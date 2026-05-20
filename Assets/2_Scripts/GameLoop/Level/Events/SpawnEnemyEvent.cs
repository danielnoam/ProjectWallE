using System;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    [Header("Spawn")]
    public float spawnInterval = 1f;
    [SerializeField] private EnemySpawnParams spawnParams;

    public override void Execute(IExposedPropertyTable resolver = null)
    {
        spawnParams.Initialize(resolver);
        spawnParams.Spawn();
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