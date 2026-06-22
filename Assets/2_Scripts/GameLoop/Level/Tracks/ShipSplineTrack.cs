using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.85f, 0.6f, 0.2f)]
[TrackClipType(typeof(ShipSplineAsset))]
[TrackBindingType(typeof(ShipMovement))]
public class ShipSplineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<ShipSplineMixerBehaviour>.Create(graph, inputCount);
    }

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        if (director.GetGenericBinding(this) is ShipMovement ship)
        {
            GameObject go = ship.gameObject;
            driver.AddFromName<Transform>(go, "m_LocalPosition.x");
            driver.AddFromName<Transform>(go, "m_LocalPosition.y");
            driver.AddFromName<Transform>(go, "m_LocalPosition.z");
            driver.AddFromName<Transform>(go, "m_LocalRotation.x");
            driver.AddFromName<Transform>(go, "m_LocalRotation.y");
            driver.AddFromName<Transform>(go, "m_LocalRotation.z");
            driver.AddFromName<Transform>(go, "m_LocalRotation.w");
        }

        base.GatherProperties(director, driver);
    }
}
