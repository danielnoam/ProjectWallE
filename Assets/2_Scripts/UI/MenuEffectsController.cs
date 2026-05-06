using System;
using DNExtensions.Systems.VFXManager;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities;
using PrimeTween;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class MenuEffectsController : MonoBehaviour
    {
        [Header("TimeScale")]
        [SerializeField] private float slowScale = 0.05f;
        [SerializeField] private float slowDuration = 0.5f;
        [SerializeField] private Ease slowEase = Ease.Linear;
        [SerializeField] private float resumeDuration = 0.5f;
        [SerializeField] private Ease resumeEase = Ease.OutBack;

        [Header("Radial Layout")]
        [SerializeField] private float angleAnimDuration = 0.3f;
        [SerializeField] private Ease angleAnimEase = Ease.OutBack;
        [SerializeField, AutoGetScene] private RadialLayoutGroup radialLayout;

        [Header("Element Animations")]
        [SerializeField] private float hoverScale = 1.2f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private Ease hoverEase = Ease.OutBack;
        [SerializeField] private float selectPunchScale = 0.3f;
        [SerializeField] private float selectPunchDuration = 0.2f;

        [Header("VFX")]
        [SerializeField] private EffectSequence fadeInSequence;
        [SerializeField] private EffectSequence fadeOutSequence;

        [Header("References")]
        [SerializeField, AutoGetScene, HideInInspector] private StructureBuildMenu buildMenu;
        [SerializeField, AutoGetScene, HideInInspector] private StructureActionsMenu actionsMenu;
        [SerializeField, AutoGetScene, HideInInspector] private SupportActionsMenu supportMenu;

        private float _savedStartAngle;
        private float _savedEndAngle;
        private int _openMenuCount;
        private Sequence _timeSequence;
        private RadialMenuElement _lastHoveredElement;

        private Action<RadialMenuElement, Structure> _onBuildHover;
        private Action<RadialMenuElement, Structure> _onBuildSelected;
        private Action<RadialMenuElement, StructureAction> _onActionsHover;
        private Action<RadialMenuElement, StructureAction> _onActionsSelected;
        private Action<RadialMenuElement, SOSupportActionData> _onSupportHover;
        private Action<RadialMenuElement, SOSupportActionData> _onSupportSelected;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Awake()
        {
            _savedStartAngle = radialLayout.StartAngle;
            _savedEndAngle = radialLayout.EndAngle;

            _onBuildHover = (e, _) => OnElementHoverChanged(e);
            _onBuildSelected = (e, _) => OnElementSelected(e);
            _onActionsHover = (e, _) => OnElementHoverChanged(e);
            _onActionsSelected = (e, _) => OnElementSelected(e);
            _onSupportHover = (e, _) => OnElementHoverChanged(e);
            _onSupportSelected = (e, _) => OnElementSelected(e);
        }

        private void OnEnable()
        {
            if (buildMenu)
            {
                buildMenu.OnMenuOpened += OnMenuOpened;
                buildMenu.OnMenuClosed += OnMenuClosed;
                buildMenu.OnElementHoverChanged += _onBuildHover;
                buildMenu.OnElementSelected += _onBuildSelected;
            }
            if (actionsMenu)
            {
                actionsMenu.OnMenuOpened += OnMenuOpened;
                actionsMenu.OnMenuClosed += OnMenuClosed;
                actionsMenu.OnElementHoverChanged += _onActionsHover;
                actionsMenu.OnElementSelected += _onActionsSelected;
            }
            if (supportMenu)
            {
                supportMenu.OnMenuOpened += OnMenuOpened;
                supportMenu.OnMenuClosed += OnMenuClosed;
                supportMenu.OnElementHoverChanged += _onSupportHover;
                supportMenu.OnElementSelected += _onSupportSelected;
            }
        }

        private void OnDisable()
        {
            if (buildMenu)
            {
                buildMenu.OnMenuOpened -= OnMenuOpened;
                buildMenu.OnMenuClosed -= OnMenuClosed;
                buildMenu.OnElementHoverChanged -= _onBuildHover;
                buildMenu.OnElementSelected -= _onBuildSelected;
            }
            if (actionsMenu)
            {
                actionsMenu.OnMenuOpened -= OnMenuOpened;
                actionsMenu.OnMenuClosed -= OnMenuClosed;
                actionsMenu.OnElementHoverChanged -= _onActionsHover;
                actionsMenu.OnElementSelected -= _onActionsSelected;
            }
            if (supportMenu)
            {
                supportMenu.OnMenuOpened -= OnMenuOpened;
                supportMenu.OnMenuClosed -= OnMenuClosed;
                supportMenu.OnElementHoverChanged -= _onSupportHover;
                supportMenu.OnElementSelected -= _onSupportSelected;
            }
        }

        private void OnElementHoverChanged(RadialMenuElement element)
        {
            if (_lastHoveredElement)
                Tween.Scale(_lastHoveredElement.transform, Vector3.one, hoverDuration, hoverEase, useUnscaledTime: true);

            if (element)
                Tween.Scale(element.transform, Vector3.one * hoverScale, hoverDuration, hoverEase, useUnscaledTime: true);

            _lastHoveredElement = element;
        }

        private void OnElementSelected(RadialMenuElement element)
        {
            if (!element) return;
            Tween.PunchScale(element.transform, Vector3.one * selectPunchScale, selectPunchDuration, useUnscaledTime: true);
        }

        private void OnMenuOpened()
        {
            _openMenuCount++;
            if (_openMenuCount > 1) return;

            if (ActiveChildCount() > 2)
            {
                radialLayout.StartAngle = 0f;
                radialLayout.EndAngle = 0f;
                Sequence.Create(useUnscaledTime: true)
                    .Group(Tween.Custom(radialLayout, 0f, _savedStartAngle, angleAnimDuration, (lg, v) => lg.StartAngle = v, angleAnimEase))
                    .Group(Tween.Custom(radialLayout, 0f, _savedEndAngle, angleAnimDuration, (lg, v) => lg.EndAngle = v, angleAnimEase));
            }

            VFXManager.Instance?.PlaySequence(fadeInSequence);
            SetTimeScale(slowScale, slowDuration, slowEase);
        }

        private void OnMenuClosed()
        {
            _openMenuCount = Mathf.Max(0, _openMenuCount - 1);
            if (_openMenuCount > 0) return;

            VFXManager.Instance?.PlaySequence(fadeOutSequence);
            SetTimeScale(1f, resumeDuration, resumeEase);
        }

        private int ActiveChildCount()
        {
            int count = 0;
            Transform t = radialLayout.transform;
            for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).gameObject.activeInHierarchy) count++;
            return count;
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