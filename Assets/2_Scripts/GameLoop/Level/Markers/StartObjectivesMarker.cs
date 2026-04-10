using System;
using System.Collections.Generic;
using DNExtensions.Utilities.SerializableSelector;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class StartObjectivesMarker : BaseLevelEventMarker
{
    [SerializeReference, SerializableSelector(Foldout = false)] public List<BaseLevelObjective> objectives = new();

    public override void Execute(IExposedPropertyTable resolver = null)
    {
        LevelManager.Instance?.StartObjectives(objectives, resolver);
    }
}