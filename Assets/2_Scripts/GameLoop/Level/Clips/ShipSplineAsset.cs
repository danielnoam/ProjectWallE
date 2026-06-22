using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using UnityEngine.Timeline;

[Serializable]
public class ShipSplineAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField, ScenePicker] private ExposedReference<SplineContainer> spline;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ShipSplineBehaviour>.Create(graph);
        playable.GetBehaviour().Spline = spline.Resolve(graph.GetResolver());
        return playable;
    }
}
