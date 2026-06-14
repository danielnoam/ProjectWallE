using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public class SetShipSplineMarker : BaseLevelEventMarker
{
    public enum TransitionMode { Teleport, OverTime }

    [Header("Ship Spline")]
    [SerializeField, ScenePicker] private ExposedReference<SplineContainer> spline;
    [SerializeField, Min(0f)] private float speed = 15f;
    [SerializeField] private ShipMovement.FollowMode followMode = ShipMovement.FollowMode.Loop;

    [Header("Transition")]
    [SerializeField] private TransitionMode transition = TransitionMode.OverTime;
    [SerializeField] private ShipMovement.EntryPoint entryPoint = ShipMovement.EntryPoint.ClosestPoint;
    [Tooltip("Seconds the ship takes to slowly ease from its current position onto the new spline")]
    [SerializeField, Min(0f), ShowIf("transition", TransitionMode.OverTime)] private float transitionDuration = 3f;

    public TransitionMode Transition => transition;
    public ShipMovement.FollowMode FollowMode => followMode;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        if (!ShipMovement.Instance) return;

        SplineContainer target = spline.Resolve(resolver);
        if (!target) return;

        float duration = transition == TransitionMode.Teleport ? 0f : transitionDuration;
        ShipMovement.Instance.SetSpline(target, speed, duration, followMode, entryPoint);
    }
}
