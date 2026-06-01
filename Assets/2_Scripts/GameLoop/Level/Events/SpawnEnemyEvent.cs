using System;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    [SerializeField] private EnemySpawnerConfig spawner;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ContinuousEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventAsset = this;
        behaviour.Resolver = graph.GetResolver();
        behaviour.Interval = spawner.spawnInterval;
        spawner.Initialize(graph.GetResolver());
        return playable;
    }

    public override void Execute(IExposedPropertyTable resolver = null) => spawner.Spawn();
}