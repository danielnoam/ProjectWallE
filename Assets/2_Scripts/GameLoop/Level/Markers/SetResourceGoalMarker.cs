using System;
using UnityEngine;

[Serializable]
public class SetResourceGoalMarker : BaseLevelEventMarker
{
    [Header("Resource Goal")]
    [Tooltip("The level completes once the player reaches this resource amount")]
    [SerializeField, Min(1)] private int targetAmount = 5000;

    public int TargetAmount => targetAmount;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        LevelManager.Instance?.SetResourceGoal(targetAmount);
    }
}
