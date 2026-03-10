using System;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;

namespace ProjectWallE
{
    public class PlayerEffects : MonoBehaviour
    {
        
        [Header("Particle")]
        [SerializeField] private ParticleSystem[] carBoostParticles;
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
            ToggleEffect(carBoostParticles, true);
        }

        
        private void OnBoostEnd()
        {
            ToggleEffect(carBoostParticles,false);
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            if (!animator) return;
            
            switch (type)
            {
                case PlayerControllerType.Car:
                    animator.Play(switchToCar.StateName);
                    ToggleEffect(robotDirtParticles, false);
                    break;
                case PlayerControllerType.Robot:
                    animator.Play(switchToRobot.StateName);
                    ToggleEffect(carDirtParticles, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            ToggleEffect(carBoostParticles, false);
        }

        private void ToggleEffect(ParticleSystem[] particleSystems, bool state)
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
        
    }
}
