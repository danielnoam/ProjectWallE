using System;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DeathScreenManager : MonoBehaviour
    {
        public static event Action SkipButtonClicked;
        
        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private string skipButtonTextPrefix = "Skip: ";
        [SerializeField] private string respawnTextPrefix = "Respawn in T-";
        
        [Header("References")]
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI skipCostText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SOFontStyle respawnTimeStyle;
        [SerializeField, AutoGetScene] private PlayerManager player;
        [SerializeField, AutoGetScene] private ResourceManager resourceManager;

        private Tween _fadeTween;

        private void OnValidate()
        {
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void Start()
        {
            if (skipButton)
            {
                skipButton.gameObject.SetActive(resourceManager);
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            }
        }

        private void OnEnable()
        {
            if (!player) return;
            player.OnDeath += OnPlayerDeath;
            player.OnRespawnTick += OnRespawnTick;
            player.OnSpawn += FadeOut;
            player.OnRespawnPodCalled += FadeOut;
        }

        private void OnDisable()
        {
            if (!player) return;
            player.OnDeath -= OnPlayerDeath;
            player.OnRespawnTick -= OnRespawnTick;
            player.OnSpawn -= FadeOut;
            player.OnRespawnPodCalled -= FadeOut;
        }

        private void OnPlayerDeath(IDamageable attacker)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (!Mathf.Approximately(canvasGroup.alpha, 1f))
            {
                _fadeTween.Stop();
                _fadeTween = Tween.Alpha(canvasGroup, 1f, fadeDuration);
                _fadeTween.OnComplete(() => skipButton.SetSelected());
            }
        }

        private void OnRespawnTick(float timeRemaining, int currentCost)
        {
            countdownText.text = $"{respawnTextPrefix}{respawnTimeStyle.ApplyStyle(Mathf.CeilToInt(timeRemaining).ToString())}";
            if (skipCostText) skipCostText.text = $"{skipButtonTextPrefix}{currentCost}";
        }

        private void FadeOut()
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (!Mathf.Approximately(canvasGroup.alpha, 0f))
            {
                _fadeTween.Stop();
                _fadeTween = Tween.Alpha(canvasGroup, 0f, fadeDuration);
            }
        }

        private void OnSkipButtonClicked()
        {
            SkipButtonClicked?.Invoke();
        }
    }
}