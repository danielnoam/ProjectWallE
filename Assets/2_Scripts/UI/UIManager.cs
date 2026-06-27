using PrimeTween;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("World Space")]
        [SerializeField] private CanvasGroup worldSpaceCanvasGroup;
        [SerializeField] private float worldSpaceFadeDuration = 0.35f;

        private Tween _hudFadeTween;
        private Tween _worldSpaceFadeTween;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetHudVisible(bool visible, bool animated = true)
        {
            SetCanvasGroupVisible(hudCanvasGroup, ref _hudFadeTween, visible, fadeDuration, animated);
            SetCanvasGroupVisible(worldSpaceCanvasGroup, ref _worldSpaceFadeTween, visible, worldSpaceFadeDuration, animated);
        }

        private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, ref Tween fadeTween, bool visible, float duration, bool animated)
        {
            if (!canvasGroup) return;
            if (Mathf.Approximately(canvasGroup.alpha, visible ? 1f : 0f)) return;

            if (fadeTween.isAlive) fadeTween.Stop();

            if (animated)
            {
                fadeTween = Tween.Alpha(canvasGroup, visible ? 1f : 0f, duration);
            }
            else
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
