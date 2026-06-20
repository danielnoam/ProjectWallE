using System;
using ProjectWallE;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PlayCinematicMarker : BaseLevelEventMarker
{
    [Header("Cinematic")]
    [SerializeField, ScenePicker] private ExposedReference<PlayableDirector> cinematic;
    [SerializeField] private bool pauseLevelTimeline = true;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var director = cinematic.Resolve(resolver);
        if (!director) return;

        CinematicManager.Instance?.Play(director, pauseLevelTimeline);
    }
}
