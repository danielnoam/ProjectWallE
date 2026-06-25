using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerEffects : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private VisualEffectAction shoot1Effect;
        [SerializeField] private VisualEffectAction shoot2Effect;
        [SerializeField] private VisualEffectAction carBoostEffect;
        [SerializeField] private VisualEffectAction wheelsAirReleaseEffect;
        [SerializeField] private DamageEffects damageEffects;
        [SerializeField] private ParticleEffectAction deathEffects;
        [SerializeField] private VisualEffectAction explodeEffect;
        [SerializeField, AudioLibraryID] private string changeStateSoundId;
        [SerializeField, AudioLibraryID] private string jumpSoundId;

        [Header("Tire Dirt")]
        [SerializeField] private float tireEffectRobotSpeedThreshold = 5f;
        [SerializeField] private ParticleSystem[] robotTireEffects;
        [SerializeField] private float tireEffectCarSpeedThreshold = 8f;
        [SerializeField] private ParticleSystem[] carTireEffects;

        [Header("References")]
        [SerializeField] private AudioSource airReleaseAudioSource;
        [SerializeField] private AudioSource changeStateAudioSource;
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField] private AudioSource jumpAudioSource;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        private ControllerType _lastControllerType;
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
            if (player) _lastControllerType = player.ControllerType;
        }

        private void OnEnable()
        {
            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnDamaged += OnDamaged;
                player.OnDeath += OnDeath;
                if (player.CarController.CarBoost)
                {
                    player.CarController.CarBoost.OnBoostStart += OnBoostStart;
                    player.CarController.CarBoost.OnBoostEnd += OnBoostEnd;
                }
                player.Shooter.OnAttack1 += OnAttack1;
                player.Shooter.OnAttack2 += OnAttack2;
                player.RobotController.OnJumped += OnJump;
            }
        }

        private void OnDisable()
        {
            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.OnDamaged -= OnDamaged;
                if (player.CarController.CarBoost)
                {
                    player.CarController.CarBoost.OnBoostStart -= OnBoostStart;
                    player.CarController.CarBoost.OnBoostEnd -= OnBoostEnd;
                }
                player.Shooter.OnAttack1 -= OnAttack1;
                player.Shooter.OnAttack2 -= OnAttack2;
                player.RobotController.OnJumped -= OnJump;
            }
        }

        private void Update()
        {
            UpdateTireEffects();
        }
        
        
        private void OnDeath(IDamageable attacker)
        {
            explodeEffect?.Play(transform.position);
            deathEffects?.Play(transform.position);
            carBoostEffect?.Stop(boostAudioSource);
            StopAllTireEffects();
        }

        private void OnDamaged(float damage)
        {
            if (damage <= 0) return;
            damageEffects?.Play(transform.position, _materials);
        }

        private void OnBoostStart()
        {
            if (player.ControllerType == ControllerType.Robot) return;
            carBoostEffect?.Play(transform.position, boostAudioSource);
        }

        private void OnBoostEnd()
        {
            carBoostEffect?.Stop(boostAudioSource);
        }

        private void OnAttack1()
        {
            shoot1Effect?.Play(transform.position);
        }

        private void OnAttack2()
        {
            shoot2Effect?.Play(transform.position);
        }

        private void OnControllerChanged(ControllerType type)
        {
            carBoostEffect?.Stop(boostAudioSource);
            if (_lastControllerType != type) AudioLibrary.PlayOnSource(changeStateSoundId, changeStateAudioSource);
            _lastControllerType = type;
            StopAllTireEffects();
            _activeTireEffects = type == ControllerType.Robot ? robotTireEffects : carTireEffects;
            _tireEffectsPlaying = new bool[_activeTireEffects.Length];
        }
        
        private void OnJump()
        {
            AudioLibrary.PlayOnSource(jumpSoundId, jumpAudioSource);
        }

        private bool IsTireGrounded(int index)
        {
            return player.ControllerType == ControllerType.Robot
                ? player.RobotController.IsGrounded()
                : player.CarController.IsTireGrounded(index);
        }

        private void UpdateTireEffects()
        {
            if (_activeTireEffects == null) return;

            float threshold = player.ControllerType == ControllerType.Robot
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

        private void StopAllTireEffects()
        {
            if (_activeTireEffects == null) return;
            for (int i = 0; i < _activeTireEffects.Length; i++)
            {
                _activeTireEffects[i]?.Stop();
                if (_tireEffectsPlaying != null) _tireEffectsPlaying[i] = false;
            }
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