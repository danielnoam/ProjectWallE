using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerEffects : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private VisualEffectAction shootEffect;
        [SerializeField] private VisualEffectAction carBoostEffect;
        [SerializeField] private VisualEffectAction wheelsAirReleaseEffect;
        [SerializeField] private DamageEffects damageEffects;
        [SerializeField, AudioLibraryID] private string changeStateSoundId;
        
        [Header("Tire Dirt")]
        [SerializeField] private float tireEffectRobotSpeedThreshold = 3f;
        [SerializeField] private ParticleSystem[] robotTireEffects;
        [SerializeField] private float tireEffectCarSpeedThreshold = 6f;
        [SerializeField] private ParticleSystem[] carTireEffects;
        
        [Header("FullScreen")]
        [SerializeField, MinMaxRange(0f, 50f)] private RangedFloat heighSpeedMagnitudeRange = new RangedFloat(20f,30f);
        [SerializeField, MinMaxRange(0f, 1f)] private Vector2 speedLinesSizeRange = new Vector2(1f, 0.8f);
        [SerializeField, Range(0f,1f)] private float lowHealthThreshold = 0.3f;
        
        [Header("References")]
        [SerializeField] private MaterialPropertyTweener lowHealthEffect;
        [SerializeField] private MaterialPropertyTweener speedLinesVisibility;
        [SerializeField] private MaterialPropertyTweener speedLinesSize;
        [SerializeField] private AudioSource airReleaseAudioSource;
        [SerializeField] private AudioSource changeStateAudioSource;
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;


        private bool _speedLinesActive;
        private bool _lowHealthActive;
        private Material[] _materials;
        private bool[] _tireEffectsPlaying;
        private ParticleSystem[] _activeTireEffects;
        
        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var mats = new List<Material>();
            foreach (var rend in renderers)
            {
                if (rend is UnityEngine.VFX.VFXRenderer) continue;
                foreach (var mat in rend.materials)
                {
                    if (mat.HasProperty(DamageEffects.EmissionStrength)) mats.Add(mat);
                }
            }
            _materials = mats.ToArray();
        }

        private void OnEnable()
        {
            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnHealthChanged += OnHealthChanged;
                player.OnDamaged += OnDamaged;
                if (player.CarController.CarBoost)
                {
                    player.CarController.CarBoost.OnBoostStart += OnBoostStart;
                    player.CarController.CarBoost.OnBoostEnd += OnBoostEnd;
                }
                player.Shooter.OnAttack1 += PlayMuzzleFlash;
                player.Shooter.OnAttack2 += PlayMuzzleFlash;
            }
        }
        
        private void OnDisable()
        {
            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.OnHealthChanged -= OnHealthChanged;
                player.OnDamaged -= OnDamaged;
                if (player.CarController.CarBoost)
                {
                    player.CarController.CarBoost.OnBoostStart -= OnBoostStart;
                    player.CarController.CarBoost.OnBoostEnd -= OnBoostEnd;
                }
                
                player.Shooter.OnAttack1 -= PlayMuzzleFlash;
                player.Shooter.OnAttack2 -= PlayMuzzleFlash;
            }
        }
        

        private void Update()
        {
            UpdateSpeedLines();
            UpdateTireEffects();
        }

        private void UpdateSpeedLines()
        {
            if (player && speedLinesSize)
            {
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
        }
        
        private void OnDamaged(float damage)
        {
            damageEffects?.Play(transform.position, _materials);
        }
        
        private void OnBoostStart()
        {
            if (player.PlayerControllerType == PlayerControllerType.Robot) return;
            
            carBoostEffect?.Play(transform.position, boostAudioSource);
        }
        
        private void OnBoostEnd()
        {
            carBoostEffect?.Stop(boostAudioSource);
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            carBoostEffect?.Stop(boostAudioSource);
            AudioLibrary.PlayOnSource(changeStateSoundId, changeStateAudioSource);
            StopAllTireEffects();
            _activeTireEffects = type == PlayerControllerType.Robot ? robotTireEffects : carTireEffects;
            _tireEffectsPlaying = new bool[_activeTireEffects.Length];
        }
        
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
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
        
        private void UpdateTireEffects()
        {
            if (_activeTireEffects == null) return;

            float threshold = player.PlayerControllerType == PlayerControllerType.Robot
                ? tireEffectRobotSpeedThreshold
                : tireEffectCarSpeedThreshold;
            
            bool isMoving = player.Velocity.sqrMagnitude > threshold * threshold;

            for (int i = 0; i < _activeTireEffects.Length; i++)
            {
                if (!_activeTireEffects[i]) continue;

                bool shouldPlay = isMoving && IsTireGrounded(i);

                if (shouldPlay && !_tireEffectsPlaying[i])
                {
                    _activeTireEffects[i].Play();
                    _tireEffectsPlaying[i] = true;
                }
                else if (!shouldPlay && _tireEffectsPlaying[i])
                {
                    _activeTireEffects[i].Stop();
                    _tireEffectsPlaying[i] = false;
                }
            }
        }
        
        private bool IsTireGrounded(int index)
        {
            return player.PlayerControllerType == PlayerControllerType.Robot ? player.RobotController.IsGrounded() : player.CarController.IsTireGrounded(index);
        }

        private void StopAllTireEffects()
        {
            if (_activeTireEffects == null) return;
            for (int i = 0; i < _activeTireEffects.Length; i++)
            {
                _activeTireEffects[i]?.Stop();
                if (_tireEffectsPlaying != null) _tireEffectsPlaying[i] = false;
            }
        }
        
        private void PlayMuzzleFlash()
        {
            shootEffect?.Play(transform.position);
        }
        
        public void EnableWheelsAirRelease()
        {
            wheelsAirReleaseEffect?.Play(transform.position, airReleaseAudioSource);
        }
        
        public void DisableWheelsAirRelease()
        {
            wheelsAirReleaseEffect?.Stop(airReleaseAudioSource);
        }
    }
}
