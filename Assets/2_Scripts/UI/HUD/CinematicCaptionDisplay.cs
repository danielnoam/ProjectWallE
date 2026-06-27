using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.Scriptables;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class CinematicCaptionDisplay : MonoBehaviour
    {
        public static CinematicCaptionDisplay Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI captionText;
        [SerializeField] private TMPWriter captionWriter;
        [SerializeField] private SOFontStyle captionFontStyle;

        [Header("SFX")]
        [SerializeField, AudioLibraryID] private string writerSFX;

        private Sequence _sequence;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup) canvasGroup.alpha = 0f;
            captionText.text = string.Empty;

            if (captionWriter)
            {
                captionWriter.OnFinishWriter.AddListener(_ => captionWriter.enabled = false);
                captionWriter.OnCharacterShown.AddListener((_, _) => AudioLibrary.Play(writerSFX));
            }
        }

        private void OnDestroy()
        {
            if (_sequence.isAlive) _sequence.Stop();
        }

        public void ShowCaption(string text, float duration, float fadeIn = 0.3f, float fadeOut = 0.3f)
        {
            if (!canvasGroup || !captionText) return;

            if (_sequence.isAlive) _sequence.Stop();

            captionText.text = captionFontStyle ? captionFontStyle.ApplyStyle(text, '*') : text;

            if (captionWriter)
            {
                captionWriter.enabled = true;
                captionWriter.RestartWriter();
            }

            _sequence = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Alpha(canvasGroup, 1f, fadeIn, useUnscaledTime: true))
                .ChainDelay(duration)
                .Chain(Tween.Alpha(canvasGroup, 0f, fadeOut, useUnscaledTime: true));
        }

        public void HideCaption(float fadeOut = 0.3f)
        {
            if (!canvasGroup) return;

            if (_sequence.isAlive) _sequence.Stop();

            _sequence = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Alpha(canvasGroup, 0f, fadeOut, useUnscaledTime: true));
        }
    }
}
