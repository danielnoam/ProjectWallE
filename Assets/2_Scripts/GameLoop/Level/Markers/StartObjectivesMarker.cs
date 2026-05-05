using System;
using System.Collections.Generic;
using DNExtensions.Utilities.SerializableSelector;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class StartObjectivesMarker : BaseLevelEventMarker
{
    [Space(10)]
    [SerializeReference, SerializableSelector(Foldout = false)] private List<BaseLevelObjective> objectives = new();
    
    public List<BaseLevelObjective> Objectives => objectives;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        LevelManager.Instance?.StartObjectives(objectives, resolver);
    }
}