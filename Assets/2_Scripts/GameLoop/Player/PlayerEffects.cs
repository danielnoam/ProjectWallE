using System;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;
using UnityEngine.VFX;

namespace ProjectWallE
{
    public class PlayerEffects : MonoBehaviour
    {
        
        [Header("Particle")]
        [SerializeField] private VisualEffect[] carBoostEffects;
        [SerializeField] private ParticleSystem[] carDirtParticles;
        [SerializeField] private ParticleSystem[] robotDirtParticles;
        [SerializeField] private VisualEffect[] wheelsAirReleaseEffects;
        [SerializeField] private VisualEffect[] muzzleFlashEffects;
        
        [Header("Animations")]
        [SerializeField] private AnimatorStateField switchToCar;
        [SerializeField] private AnimatorStateField switchToRobot;
        
        [Header("SpeedLines")]
        [SerializeField] private float heighSpeedMagnitude = 20f;
        
        [Header("References")]
        [SerializeField] private MaterialPropertyTweener lowHealthEffect;
        [SerializeField] private MaterialPropertyTweener speedLinesEffect;
        [SerializeField, AutoGetSelf, HideInInspector] private Animator animator;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerShooter shooter;
        [SerializeField, AutoGetScene, HideInInspector] private CarBoost boost;


        private bool _speedLinesActive;
        
        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }


        private void OnEnable()
        {
            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnDamaged += OnDamaged;
                if (boost)
                {
                    boost.OnBoostStart += OnBoostStart;
                    boost.OnBoostEnd += OnBoostEnd;
                }
                if (shooter) shooter.OnShoot += PlayMuzzleFlash;
            }


        }
        
        private void OnDisable()
        {
            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.OnDamaged -= OnDamaged;
                if (boost)
                {
                    boost.OnBoostStart -= OnBoostStart;
                    boost.OnBoostEnd -= OnBoostEnd;
                }
                
                if (shooter) shooter.OnShoot -= PlayMuzzleFlash;
            }
        }
        

        private void Update()
        {
            if (player && speedLinesEffect)
            {
                if (player.velocity.magnitude > heighSpeedMagnitude && !_speedLinesActive)
                {
                    speedLinesEffect.Show();
                    _speedLinesActive = true;
                }
                else if (player.velocity.magnitude <= heighSpeedMagnitude && _speedLinesActive)
                {
                    speedLinesEffect.Hide();
                    _speedLinesActive = false;
                }
            }
        }


        private void OnBoostStart()
        {
            ToggleEffect(carBoostEffects, true);
        }

        
        private void OnBoostEnd()
        {
            ToggleEffect(carBoostEffects,false);
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            if (!animator) return;
            
            switch (type)
            {
                case PlayerControllerType.Car:
                    animator.Play(switchToCar.StateName);
                    ToggleParticle(robotDirtParticles, false);
                    break;
                case PlayerControllerType.Robot:
                    animator.Play(switchToRobot.StateName);
                    ToggleParticle(carDirtParticles, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            ToggleEffect(carBoostEffects, false);
        }
        
        private void OnDamaged(float damage)
        {
            lowHealthEffect?.Punch();
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
        }
        
        public void EnableWheelsAirRelease()
        {
           ToggleEffect(wheelsAirReleaseEffects, true);
        }
        
        public void DisableWheelsAirRelease()
        {
            ToggleEffect(wheelsAirReleaseEffects, false);
        }

        public void PlaySound(string id)
        {
            AudioLibrary.PlayAtPosition(id, transform);
        }
        
        
    }
}
