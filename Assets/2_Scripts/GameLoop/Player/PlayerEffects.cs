using System;
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
        
        [Header("Animations")]
        [SerializeField] private AnimatorStateField switchToCar;
        [SerializeField] private AnimatorStateField switchToRobot;
        
        [SerializeField, AutoGetSelf, HideInInspector] private Animator animator;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;


        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }


        private void OnEnable()
        {
            PlayerManager.OnControllerChanged += OnControllerChanged;
            PlayerManager.OnBoostStart += OnBoostStart;
            PlayerManager.OnBoostEnd += OnBoostEnd;
        }
        

        private void OnDisable()
        {
            PlayerManager.OnControllerChanged -= OnControllerChanged;
            PlayerManager.OnBoostStart -= OnBoostStart;
            PlayerManager.OnBoostEnd -= OnBoostEnd;
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
        
    }
}
