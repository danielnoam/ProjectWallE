using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    [DefaultExecutionOrder(201)]
    public class InGameCrosshair : MonoBehaviour
    {
        [Header("Position")]
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private float distanceSmoothTime = 0.04f;
        [SerializeField] private bool lerpPosition;
        [SerializeField, Range(5f, 50f), EnableIf("lerpPosition")] private float positionLerpSpeed = 25f;

        [Header("Scale")]
        [SerializeField] private bool scaleWithDistance = true;
        [SerializeField, MinMaxRange(0.1f, 5f), EnableIf("scaleWithDistance")]
        private RangedFloat minMaxScaleMultiplier = new RangedFloat(1f, 2f);
        [SerializeField, MinMaxRange(0f, 100f), EnableIf("scaleWithDistance")]
        private RangedFloat minMaxDistance = new RangedFloat(5f, 30f);

        [Header("Reticle Punch")]
        [SerializeField] private float reticlePunchScale = 1.3f;
        [SerializeField] private float reticlePunchDuration = 0.15f;

        [Header("Hit Marker")]
        [SerializeField] private float hitMarkerPunchScale = 1.8f;
        [SerializeField] private float hitMarkerDuration = 0.25f;

        [Header("References")]
        [SerializeField] private Transform reticle;
        [SerializeField] private GameObject visual;
        [SerializeField] private Transform hitMarker;
        [SerializeField] private Image hitMarkerImage;
        [SerializeField, AutoGetScene] private PlayerManager player;

        private Vector3 _currentPosition;
        private Vector3 _reticleBaseScale;
        private Tween _reticlePunchTween;
        private Vector3 _hitMarkerBaseScale;
        private Tween _hitMarkerScaleTween;
        private Tween _hitMarkerAlphaTween;
        private Camera _cam;
        private Vector3 _baseScale;
        private bool _isActive;
        private bool _inMenu;
        private float _currentDistance;
        private float _distanceVelocity;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Awake()
        {
            _cam = Camera.main;
            if (visual)
            {
                _baseScale = visual.transform.localScale;
                visual.SetActive(false);
            }

            if (reticle) _reticleBaseScale = reticle.localScale;

            if (hitMarkerImage)
            {
                _hitMarkerBaseScale = hitMarkerImage.transform.localScale;
                Color c = hitMarkerImage.color;
                c.a = 0f;
                hitMarkerImage.color = c;
            }
        }

        private void OnEnable()
        {
            if (!player) return;
            player.OnControllerChanged += OnControllerChanged;
            player.Shooter.OnAttack1 += OnAttack;
            player.Shooter.OnAttack2 += OnAttack;
            player.StructureBuilder.BuildMenuRequested += OnMenuRequested;
            player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
            player.StructureBuilder.MenuCloseRequested += OnMenuClosed;

            RefreshActiveState();

            Enemy.OnEnemyDamaged += OnEnemyDamaged;
        }

        private void OnDisable()
        {
            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.Shooter.OnAttack1 -= OnAttack;
                player.Shooter.OnAttack2 -= OnAttack;
                player.StructureBuilder.BuildMenuRequested -= OnMenuRequested;
                player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= OnMenuClosed;
            }
            Enemy.OnEnemyDamaged -= OnEnemyDamaged;
        }

        private void LateUpdate()
        {
            if (!_isActive || !player || !_cam) return;

            var aimer = player.Aimer;
            float targetDistance = aimer.HasHit
                ? Mathf.Min(aimer.LastHit.distance, maxDistance)
                : maxDistance;

            _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVelocity, distanceSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            Vector3 targetPosition = aimer.CameraRay.GetPoint(_currentDistance);

            if (lerpPosition)
            {
                _currentPosition = Vector3.Lerp(_currentPosition, targetPosition, positionLerpSpeed * Time.unscaledDeltaTime);
                transform.position = _currentPosition;
            }
            else
            {
                transform.position = targetPosition;
            }

            transform.rotation = _cam.transform.rotation;

            if (scaleWithDistance && visual)
            {
                float t = Mathf.InverseLerp(minMaxDistance.minValue, minMaxDistance.maxValue, _currentDistance);
                float mult = Mathf.Lerp(minMaxScaleMultiplier.minValue, minMaxScaleMultiplier.maxValue, t);
                visual.transform.localScale = _baseScale * mult;
            }
        }

        private void OnEnemyDamaged(IDamageable attacker)
        {
            if (attacker is PlayerManager pm && pm == player)
            {
                PlayHitMarker();
            }
        }

        private void OnAttack()
        {
            PlayReticlePunch();
        }

        private void OnMenuRequested(Structure[] _) => SetMenuOpen(true);
        private void OnActionsMenuRequested(Structure _) => SetMenuOpen(true);
        private void OnMenuClosed() => SetMenuOpen(false);

        private void SetMenuOpen(bool value)
        {
            _inMenu = value;
            RefreshActiveState();
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            RefreshActiveState();
        }

        private void RefreshActiveState()
        {
            bool shouldBeActive = player
                && player.PlayerControllerType == PlayerControllerType.Robot
                && !_inMenu;
            SetActive(shouldBeActive);
        }

        private void SetActive(bool value)
        {
            _isActive = value;
            if (visual) visual.SetActive(value);
            
            if (value && player)
            {
                var aimer = player.Aimer;
                _currentDistance = aimer.HasHit ? Mathf.Min(aimer.LastHit.distance, maxDistance) : maxDistance;
                _distanceVelocity = 0f;
                _currentPosition = aimer.CameraRay.GetPoint(_currentDistance);
            }
        }

        private void PlayReticlePunch()
        {
            if (!reticle) return;

            _reticlePunchTween.Stop();
            reticle.localScale = _reticleBaseScale * reticlePunchScale;
            _reticlePunchTween = Tween.Scale(reticle, _reticleBaseScale, reticlePunchDuration, Ease.OutQuad);
        }

        private void PlayHitMarker()
        {
            if (!hitMarkerImage) return;

            _hitMarkerScaleTween.Stop();
            _hitMarkerAlphaTween.Stop();

            Transform t = hitMarkerImage.transform;
            t.localScale = _hitMarkerBaseScale * hitMarkerPunchScale;

            Color c = hitMarkerImage.color;
            c.a = 1f;
            hitMarkerImage.color = c;

            t.localScale = _hitMarkerBaseScale;
            _hitMarkerScaleTween = Tween.Scale(t, _hitMarkerBaseScale * hitMarkerPunchScale, hitMarkerDuration, Ease.OutQuad);
            _hitMarkerAlphaTween = Tween.Alpha(hitMarkerImage, 0f, hitMarkerDuration, Ease.OutQuad);
        }
    }
}