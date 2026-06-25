using System;
using DNExtensions.Systems.VFXManager;
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

    [Header("Music")]
    [SerializeField] private MusicManager.MusicAction music = MusicManager.MusicAction.None;
    [SerializeField, ShowIf("music", MusicManager.MusicAction.PlaySpecific)] private AudioClip musicClip;
    [SerializeField, Tooltip("Music action applied when the cinematic ends")] private MusicManager.MusicAction musicAfter = MusicManager.MusicAction.None;
    [SerializeField, ShowIf("musicAfter", MusicManager.MusicAction.PlaySpecific)] private AudioClip musicAfterClip;

    [Header("Effects")]
    [SerializeField, Tooltip("Effect sequence played when the cinematic starts")] private EffectSequence effectSequenceBefore;
    [SerializeField, Tooltip("Effect sequence played when the cinematic ends")] private EffectSequence effectSequenceAfter;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        var controller = cinematic.Resolve(resolver);
        if (!controller) return;

        MusicManager.Instance?.Execute(music, musicClip);
        if (effectSequenceBefore) effectSequenceBefore.PlaySequence();

        CinematicManager.Instance?.Play(controller, pauseLevelTimeline, hideUI, showUIAfter, musicAfter, musicAfterClip, effectSequenceAfter);
    }
}
