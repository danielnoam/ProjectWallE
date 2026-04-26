using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class PlayerFullScreenEffects : MonoBehaviour
    {
        [Header("Speed Lines")]
        [SerializeField, MinMaxRange(0f, 50f)] private RangedFloat heighSpeedMagnitudeRange = new RangedFloat(15f, 30f);
        [SerializeField, MinMaxRange(0f, 1f)] private Vector2 speedLinesSizeRange = new Vector2(1f, 0.9f);

        [Header("Low Health")]
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;

        [Header("References")]
        [SerializeField] private MaterialPropertyTweener lowHealthEffect;
        [SerializeField] private MaterialPropertyTweener speedLinesVisibility;
        [SerializeField] private MaterialPropertyTweener speedLinesSize;
        [SerializeField, AutoGetScene] private PlayerManager player;

        private bool _speedLinesActive;
        private bool _lowHealthActive;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            if (player)
            {
                player.OnHealthChanged += OnHealthChanged;
                player.OnDeath += OnDeath;
            }
        }

        private void OnDestroy()
        {
            if (player)
            {
                player.OnHealthChanged -= OnHealthChanged;
                player.OnDeath -= OnDeath;
            }
        }

        private void Update()
        {
            UpdateSpeedLines();
        }
        
        private void OnDeath(IDamageable attacker)
        {
            lowHealthEffect?.Hide();
            _lowHealthActive = false;
            
            speedLinesVisibility.Hide();
            _speedLinesActive = false;
        }

        private void UpdateSpeedLines()
        {
            if (!player || !speedLinesSize) return;

            var horizontalVelocity = player.Velocity.SetY(0f);
            float sqrSpeed = horizontalVelocity.sqrMagnitude;
            float minThresholdSqr = heighSpeedMagnitudeRange.minValue * heighSpeedMagnitudeRange.minValue;

            if (sqrSpeed > minThresholdSqr && !_speedLinesActive)
            {
                speedLinesVisibility.Show();
                _speedLinesActive = true;
            }
            else if (sqrSpeed <= minThresholdSqr && _speedLinesActive)
            {
                speedLinesVisibility.Hide();
                _speedLinesActive = false;
            }

            if (_speedLinesActive)
            {
                var t = Mathf.InverseLerp(heighSpeedMagnitudeRange.minValue, heighSpeedMagnitudeRange.maxValue, horizontalVelocity.magnitude);
                var size = Mathf.Lerp(speedLinesSizeRange.x, speedLinesSizeRange.y, t);
                speedLinesSize.SetValue(size);
            }
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            if (currentHealth <= 0) return;
            
            var percentage = currentHealth / maxHealth;

            if (!_lowHealthActive && percentage < lowHealthThreshold)
            {
                lowHealthEffect?.Show();
                _lowHealthActive = true;
            }
            else if (_lowHealthActive && percentage >= lowHealthThreshold)
            {
                lowHealthEffect?.Hide();
                _lowHealthActive = false;
            }
        }
    }
}