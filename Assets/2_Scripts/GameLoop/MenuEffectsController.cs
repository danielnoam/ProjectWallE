using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class MenuEffectsController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float slowScale = 0.05f;
        [SerializeField] private float slowDuration = 0.5f;
        [SerializeField] private float resumeDuration = 0.5f;
        [SerializeField] private EffectSequence fadeInSequence;
        [SerializeField] private EffectSequence fadeOutSequence;
        [SerializeField, AutoGetScene, HideInInspector] private StructureBuildMenu buildMenu;
        [SerializeField, AutoGetScene, HideInInspector] private StructureActionsMenu actionsMenu;
        [SerializeField, AutoGetScene, HideInInspector] private SupportActionsMenu supportMenu;

        private int _openMenuCount;
        private Sequence _timeSequence;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void OnEnable()
        {
            if (buildMenu) 
            { 
                buildMenu.OnMenuOpened += OnMenuOpened; 
                buildMenu.OnMenuClosed += OnMenuClosed; 
            }
            if (actionsMenu)
            {
                actionsMenu.OnMenuOpened += OnMenuOpened;
                actionsMenu.OnMenuClosed += OnMenuClosed;
            }
            if (supportMenu)
            {
                supportMenu.OnMenuOpened += OnMenuOpened; 
                supportMenu.OnMenuClosed += OnMenuClosed;
            }
        }

        private void OnDisable()
        {
            if (buildMenu) 
            { 
                buildMenu.OnMenuOpened -= OnMenuOpened; 
                buildMenu.OnMenuClosed -= OnMenuClosed; 
            }
            if (actionsMenu) 
            { 
                actionsMenu.OnMenuOpened -= OnMenuOpened;
                actionsMenu.OnMenuClosed -= OnMenuClosed; 
            }
            if (supportMenu)
            {
                supportMenu.OnMenuOpened -= OnMenuOpened;
                supportMenu.OnMenuClosed -= OnMenuClosed; 
            }
        }

        private void OnMenuOpened()
        {
            _openMenuCount++;
            if (_openMenuCount == 1)
            {
                VFXManager.Instance?.PlaySequence(fadeInSequence);
                SetTimeScale(slowScale, slowDuration, Ease.Linear);
            }
        }

        private void OnMenuClosed()
        {
            _openMenuCount = Mathf.Max(0, _openMenuCount - 1);
            if (_openMenuCount == 0)
            {
                VFXManager.Instance?.PlaySequence(fadeOutSequence);
                SetTimeScale(1f, resumeDuration, Ease.OutBack);
            }
        }

        private void SetTimeScale(float target, float duration, Ease ease)
        {
            if (Mathf.Approximately(Time.timeScale, target)) return;
            if (_timeSequence.isAlive) _timeSequence.Stop();
            _timeSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.GlobalTimeScale(target, duration, ease));
        }
    }
}