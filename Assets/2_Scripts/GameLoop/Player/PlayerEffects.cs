using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerEffects : MonoBehaviour
    {
        [Header("Particle")]
        [SerializeField] private VisualEffect[] carBoostEffects;
        [SerializeField] private ParticleSystem[] carDirtParticles;
        [SerializeField] private ParticleSystem[] robotDirtParticles;
        [SerializeField] private VisualEffect[] wheelsAirReleaseEffects;
        [SerializeField] private VisualEffect[] muzzleFlashEffects;
        
        [Header("Screen Effects")]
        [SerializeField, MinMaxRange(0f, 50f)] private RangedFloat heighSpeedMagnitudeRange = new RangedFloat(20f,30f);
        [SerializeField, MinMaxRange(0f, 1f)] private Vector2 speedLinesSizeRange = new Vector2(1f, 0.8f);
        [SerializeField, Range(0f,1f)] private float lowHealthThreshold = 0.3f;
        
        [Header("SFX")]
        [SerializeField, AudioLibraryID] private string boostSoundId;
        [SerializeField, AudioLibraryID] private string airReleaseSoundId;
        [SerializeField, AudioLibraryID] private string changeStateSoundId;
        [SerializeField, AudioLibraryID] private string shootSoundId;
        
        [Header("References")]
        [SerializeField] private MaterialPropertyTweener lowHealthEffect;
        [SerializeField] private MaterialPropertyTweener speedLinesVisibility;
        [SerializeField] private MaterialPropertyTweener speedLinesSize;
        [SerializeField] private MaterialPropertyTweener emission;
        [SerializeField] private AudioSource airReleaseAudioSource;
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField] private AudioSource changeStateAudioSource;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerShooter shooter;


        private bool _speedLinesActive;
        private bool _lowHealthActive;
        
        private void OnValidate()
        {
            AutoGetSystem.Process(this);
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
                if (shooter) shooter.OnShoot += PlayMuzzleFlash;
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
                
                if (shooter) shooter.OnShoot -= PlayMuzzleFlash;
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
                if (player.Velocity.magnitude > heighSpeedMagnitudeRange.minValue && !_speedLinesActive)
                {
                    speedLinesVisibility.Show();
                    _speedLinesActive = true;
                }
                else if (player.Velocity.magnitude <= heighSpeedMagnitudeRange.minValue && _speedLinesActive)
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
            emission?.Punch();
        }

        
        private void OnBoostStart()
        {
            ToggleEffect(carBoostEffects, true);
            AudioLibrary.PlayOnSource(boostSoundId, boostAudioSource);
        }
        
        private void OnBoostEnd()
        {
            ToggleEffect(carBoostEffects,false);
            boostAudioSource?.Stop();
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            ToggleEffect(carBoostEffects, false);
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

        private void ToggleParticle(ParticleSystem[] particleSystems, bool state)
        {
            if (particleSystems == null || particleSystems.Length == 0) return;
            
            if (state)
            {
                foreach (var particle in particleSystems)
                {
                    particle?.Play();
                }
            }
            else
            {
                foreach (var particle in particleSystems)
                {
                    particle?.Stop();
                }
            }

        }
        
        private void ToggleEffect(VisualEffect[] effects, bool state)
        {
            if (effects == null || effects.Length == 0) return;

            if (state)
            {
                foreach (var effect in effects)
                {
                    effect?.Play();
                }
            }
            else
            {
                foreach (var effect in effects)
                {
                    effect?.Stop();
                }
            }
        }
        
        private void PlayMuzzleFlash()
        {
            ToggleEffect(muzzleFlashEffects, true);
            AudioLibrary.PlayAtPosition(shootSoundId, transform.position);
        }
        
        public void EnableWheelsAirRelease()
        {
           ToggleEffect(wheelsAirReleaseEffects, true);
           AudioLibrary.PlayOnSource(airReleaseSoundId, airReleaseAudioSource);
        }
        
        public void DisableWheelsAirRelease()
        {
            ToggleEffect(wheelsAirReleaseEffects, false);
            airReleaseAudioSource?.Stop();
        }
    }
}
