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
        [SerializeField] private VisualEffectAction shoot1Effect;
        [SerializeField] private VisualEffectAction shoot2Effect;
        [SerializeField] private VisualEffectAction carBoostEffect;
        [SerializeField] private VisualEffectAction wheelsAirReleaseEffect;
        [SerializeField] private DamageEffects damageEffects;

        [Header("Audio")]
        [SerializeField, AudioLibraryID] private string changeStateSoundId;
        [SerializeField, AudioLibraryID] private string robotMoveSoundId;
        [SerializeField, AudioLibraryID] private string carMoveSoundId;
        [SerializeField, MinMaxRange(0f, 30f)] private RangedFloat robotMoveVolumeSpeedRange = new RangedFloat(0.5f, 10f);
        [SerializeField, MinMaxRange(0f, 30f)] private RangedFloat carMoveVolumeSpeedRange = new RangedFloat(0.5f, 10f);

        [Header("Tire Dirt")]
        [SerializeField] private float tireEffectRobotSpeedThreshold = 5f;
        [SerializeField] private ParticleSystem[] robotTireEffects;
        [SerializeField] private float tireEffectCarSpeedThreshold = 8f;
        [SerializeField] private ParticleSystem[] carTireEffects;

        [Header("References")]
        [SerializeField] private AudioSource airReleaseAudioSource;
        [SerializeField] private AudioSource changeStateAudioSource;
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField] private AudioSource robotMoveAudioSource;
        [SerializeField] private AudioSource carMoveAudioSource;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        private Material[] _materials;
        private bool[] _tireEffectsPlaying;
        private ParticleSystem[] _activeTireEffects;
        private AudioSource _activeMoveAudioSource;
        private AudioSource _inactiveMoveAudioSource;

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

            if (robotMoveAudioSource) AudioLibrary.PlayOnSource(robotMoveSoundId, robotMoveAudioSource);
            if (carMoveAudioSource) AudioLibrary.PlayOnSource(carMoveSoundId, carMoveAudioSource);
        }

        private void OnEnable()
        {
            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnDamaged += OnDamaged;
                if (player.CarController.CarBoost)
                {
                    player.CarController.CarBoost.OnBoostStart += OnBoostStart;
                    player.CarController.CarBoost.OnBoostEnd += OnBoostEnd;
                }
                player.Shooter.OnAttack1 += OnAttack1;
                player.Shooter.OnAttack2 += OnAttack2;
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
            }
        }

        private void Update()
        {
            UpdateTireEffects();
            UpdateMovementAudio();
        }

        private void OnDamaged(float damage)
        {
            if (damage <= 0) return;
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

        private void OnAttack1()
        {
            shoot1Effect?.Play(transform.position);
        }

        private void OnAttack2()
        {
            shoot2Effect?.Play(transform.position);
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            carBoostEffect?.Stop(boostAudioSource);
            AudioLibrary.PlayOnSource(changeStateSoundId, changeStateAudioSource);
            StopAllTireEffects();
            _activeTireEffects = type == PlayerControllerType.Robot ? robotTireEffects : carTireEffects;
            _tireEffectsPlaying = new bool[_activeTireEffects.Length];

            _activeMoveAudioSource = type == PlayerControllerType.Robot ? robotMoveAudioSource : carMoveAudioSource;
            _inactiveMoveAudioSource = type == PlayerControllerType.Robot ? carMoveAudioSource : robotMoveAudioSource;
            if (_inactiveMoveAudioSource) _inactiveMoveAudioSource.volume = 0f;
        }

        private void UpdateMovementAudio()
        {
            if (!_activeMoveAudioSource) return;
            float speed = player.Velocity.magnitude;
            bool isRobot = player.PlayerControllerType == PlayerControllerType.Robot;
            var range = isRobot ? robotMoveVolumeSpeedRange : carMoveVolumeSpeedRange;
            _activeMoveAudioSource.volume = Mathf.Lerp(0.1f, 0.4f, Mathf.InverseLerp(range.minValue, range.maxValue, speed));
        }

        private bool IsTireGrounded(int index)
        {
            return player.PlayerControllerType == PlayerControllerType.Robot
                ? player.RobotController.IsGrounded()
                : player.CarController.IsTireGrounded(index);
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