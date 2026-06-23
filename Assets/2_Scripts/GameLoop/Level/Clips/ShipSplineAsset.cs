using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using UnityEngine.Timeline;

[Serializable]
public class ShipSplineAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField, ScenePicker] private ExposedReference<SplineContainer> spline;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ShipSplineBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.Spline = spline.Resolve(graph.GetResolver());
        behaviour.SpeedCurve = speedCurve;
        return playable;
    }
}
