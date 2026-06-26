using System;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities;
using ProjectWallE;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class PresentationMarker : BaseLevelEventMarker
{
    public enum UIAction { None, Show, Hide }

    [Header("UI")]
    [SerializeField] private UIAction ui = UIAction.None;

    [Header("Music")]
    [SerializeField] private MusicManager.MusicAction music = MusicManager.MusicAction.None;
    [SerializeField, ShowIf("music", MusicManager.MusicAction.PlaySpecific)] private AudioClip musicClip;

    [Header("Effects")]
    [SerializeField] private EffectSequence effectSequence;

    public UIAction UI => ui;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        if (ui != UIAction.None) UIManager.Instance?.SetHudVisible(ui == UIAction.Show);
        MusicManager.Instance?.Execute(music, musicClip);
        if (effectSequence) effectSequence.PlaySequence();
    }
}
