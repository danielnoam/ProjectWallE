using PrimeTween;
using UnityEngine;

namespace ProjectWallE.UI
{
    [AddComponentMenu("DNExtensions/UI/UI Manager")]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private float fadeDuration = 0.35f;

        private Tween _fadeTween;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetHudVisible(bool visible)
        {
            if (!hudCanvasGroup) return;

            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(hudCanvasGroup, visible ? 1f : 0f, fadeDuration);
            hudCanvasGroup.interactable = visible;
            hudCanvasGroup.blocksRaycasts = visible;
        }
    }
}
