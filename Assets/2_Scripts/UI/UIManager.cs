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

        public void SetHudVisible(bool visible, bool animated = true)
        {
            if (!hudCanvasGroup) return;
            if (Mathf.Approximately(hudCanvasGroup.alpha, visible ? 1f : 0f)) return;

            if (_fadeTween.isAlive) _fadeTween.Stop();
            
            if (animated)
            {
                _fadeTween = Tween.Alpha(hudCanvasGroup, visible ? 1f : 0f, fadeDuration);
            }
            else
            {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
            }
            hudCanvasGroup.interactable = visible;
            hudCanvasGroup.blocksRaycasts = visible;
        }
    }
}
