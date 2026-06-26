using System;
using System.Collections.Generic;
using DNExtensions.Utilities.SerializableSelector;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class StartObjectivesEvent : BaseLevelEventAsset
{
    [Space(10)]
    [SerializeReference, SerializableSelector(Foldout = false)] private List<BaseLevelObjective> objectives = new();

    public List<BaseLevelObjective> Objectives => objectives;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<StartObjectivesBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventAsset = this;
        behaviour.Resolver = graph.GetResolver();
        return playable;
    }
}
