using System;
using DNExtensions.Utilities;
using ProjectWallE;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PlayCinematicMarker : BaseLevelEventMarker
{
    [Header("Cinematic")]
    [SerializeField, ScenePicker] private ExposedReference<CinematicController> cinematic;

    [Header("Settings")]
    [SerializeField] private bool pauseLevelTimeline = true;
    [SerializeField] private bool hideUI = true;
    [SerializeField, ShowIf("hideUI"), Tooltip("If true, the UI is shown again when the cinematic ends")] private bool showUIAfter = true;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var controller = cinematic.Resolve(resolver);
        if (!controller) return;

        CinematicManager.Instance?.Play(controller, pauseLevelTimeline, hideUI, showUIAfter);
    }
}
