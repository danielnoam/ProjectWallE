using System;
using DNExtensions.Systems.AudioTrack;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities;
using ProjectWallE;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PresentationMarker : BaseLevelEventMarker
{
    private enum UIAction { None, Show, Hide }

    [Header("UI")]
    [SerializeField] private UIAction ui = UIAction.None;

    [Header("Music")]
    [SerializeField] private MusicManager.MusicAction music = MusicManager.MusicAction.None;
    [SerializeField, ShowIf("music", MusicManager.MusicAction.PlaySpecific), AudioTrackID] private string musicTrackId;

    [Header("Effects")]
    [SerializeField] private EffectSequence effectSequence;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        if (ui != UIAction.None) UIManager.Instance?.SetHudVisible(ui == UIAction.Show);
        MusicManager.Instance?.Execute(music, musicTrackId);
        if (effectSequence) effectSequence.PlaySequence();
    }
}
