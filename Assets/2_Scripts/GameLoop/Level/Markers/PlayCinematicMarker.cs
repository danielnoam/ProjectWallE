using System;
using ProjectWallE;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PlayCinematicMarker : BaseLevelEventMarker
{
    [Header("Cinematic")]
    [SerializeField, ScenePicker] private ExposedReference<CinematicController> cinematic;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var controller = cinematic.Resolve(resolver);
        if (!controller) return;

        CinematicManager.Instance?.Play(controller);
    }
}
