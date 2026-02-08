using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public abstract class BaseLevelEventAsset : PlayableAsset
{
    public virtual void Execute(IExposedPropertyTable resolver = null) { }
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<LevelEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventAsset = this;
        behaviour.Resolver = graph.GetResolver();
        return playable;
    }
}