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
        
        [Header("FullScreen Effects")]
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
        }

        private void UpdateSpeedLines()
        {
            if (player && speedLinesSize)
            {
                var velocity = player.Velocity.SetY(0f).magnitude;
                
                if (velocity > heighSpeedMagnitudeRange.minValue && !_speedLinesActive)
                {
                    speedLinesVisibility.Show();
                    _speedLinesActive = true;
                }
                else if (velocity <= heighSpeedMagnitudeRange.minValue && _speedLinesActive)
                {
                    speedLinesVisibility.Hide();
                    _speedLinesActive = false;  
                }

                var t = Mathf.InverseLerp(heighSpeedMagnitudeRange.minValue, heighSpeedMagnitudeRange.maxValue, player.Velocity.magnitude);
                var size = Mathf.Lerp(speedLinesSizeRange.x, speedLinesSizeRange.y, t);
                speedLinesSize.SetValue(size);
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
