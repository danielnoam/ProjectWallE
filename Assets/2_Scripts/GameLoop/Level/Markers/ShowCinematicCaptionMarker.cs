using System;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class ShowCinematicCaptionMarker : BaseLevelEventMarker
{
    [Header("Caption")]
    [SerializeField, TextArea] private string text;
    [SerializeField, Min(0f)] private float duration = 3f;
    [SerializeField, Min(0f)] private float fadeIn = 0.3f;
    [SerializeField, Min(0f)] private float fadeOut = 0.3f;

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        CinematicCaptionDisplay.Instance?.ShowCaption(text, duration, fadeIn, fadeOut);
    }
}
