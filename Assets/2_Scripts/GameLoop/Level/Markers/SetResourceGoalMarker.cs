using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SetResourceGoalMarker : BaseLevelEventMarker
{
    [Header("Resource Goal")]
    [Tooltip("The level completes once the player reaches this resource amount")]
    [SerializeField, Min(1)] private int targetAmount = 5000;

    [Header("Loop Timeline")]
    [Tooltip("Optional timeline that loops until the resource goal is reached")]
    [SerializeField, ScenePicker] private ExposedReference<PlayableDirector> loopTimeline;

    public int TargetAmount => targetAmount;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        PlayableDirector loop = loopTimeline.Resolve(resolver);
        LevelManager.Instance?.SetResourceGoal(targetAmount, loop);
    }
}
