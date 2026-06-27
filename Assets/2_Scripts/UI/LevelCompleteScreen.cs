using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    [AddComponentMenu("DNExtensions/UI/Level Complete Screen")]
    [RequireComponent(typeof(CanvasGroup))]
    public class LevelCompleteScreen : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("References")]
        [SerializeField] private Button keepPlayingButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Quit")]
        [SerializeField] private SceneField menuScene;
        [SerializeField] private EffectSequence fadeOutSequence;

        private Tween _fadeTween;

        private void OnValidate()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            keepPlayingButton.EnableOnPointerEnterSelection();
            keepPlayingButton.EnableOnPointerExitDeselection();
            quitButton.EnableOnPointerEnterSelection();
            quitButton.EnableOnPointerExitDeselection();
        }

        private void OnEnable()
        {
            LevelManager.OnLevelCompleted += Show;
            keepPlayingButton.onClick.AddListener(Hide);
            quitButton.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            LevelManager.OnLevelCompleted -= Show;
            keepPlayingButton.onClick.RemoveListener(Hide);
            quitButton.onClick.RemoveListener(Quit);
        }

        private void Show()
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (PlayerManager.Instance) PlayerManager.Instance.SetCinematicMode(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Mathf.Approximately(canvasGroup.alpha, 1f)) return;

            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 1f, fadeDuration);
            _fadeTween.OnComplete(() => keepPlayingButton.SetSelected());
        }

        private void Hide()
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (PlayerManager.Instance) PlayerManager.Instance.SetCinematicMode(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

            if (Mathf.Approximately(canvasGroup.alpha, 0f)) return;

            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 0f, fadeDuration);
        }

        private void Quit()
        {
            TransitionManager.TransitionToScene(menuScene, fadeOutSequence);
        }
    }
}
