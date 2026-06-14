using System;
using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE
{
    public class PlayerEngineSfx : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SOCarControllerSettings carControllerSettings;
        [SerializeField] private SORobotControllerSettings robotControllerSettings;
        [SerializeField] private List<AudioSource> audioSources;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;
        
        [Header("Audio Clip RPM Mapping")]
        [SerializeField] private float clipMinRPM = 1000f;
        [SerializeField] private float clipMaxRPM = 6000f;
        [SerializeField] private float clipRPMStep = 500f;

        [Header("Engine Settings")]
        [SerializeField] private PlayerEngineSfxSettings carEngineSfxSettings;
        [SerializeField] private PlayerEngineSfxSettings robotEngineSfxSettings;

        private ControllerType _currentControllerType;
        private float _currentRPM;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
            
            clipMinRPM = Mathf.Max(1f, clipMinRPM);
            clipMaxRPM = Mathf.Max(clipMinRPM, clipMaxRPM);
            clipRPMStep = Mathf.Max(1f, clipRPMStep);

            carEngineSfxSettings?.Validate();
            robotEngineSfxSettings?.Validate();
        }

        private void Awake()
        {
            _currentRPM = carEngineSfxSettings?.MinRPM ?? robotEngineSfxSettings?.MinRPM ?? 1000f;

            foreach (AudioSource source in audioSources)
            {
                if (source == null) continue;

                source.loop = true;
                source.volume = 0f;
                source.pitch = 1f;

                if (!source.isPlaying)
                    source.Play();
            }
        }

        private void OnEnable()
        {
            if (player != null)
                player.OnControllerChanged += OnControllerSwitch;
        }

        private void OnDisable()
        {
            if (player != null)
                player.OnControllerChanged -= OnControllerSwitch;
        }

        private void Update()
        {
            Engine();
        }

        private void Engine()
        {
            if (audioSources == null || audioSources.Count == 0 || player == null)
                return;

            switch (_currentControllerType)
            {
                case ControllerType.Car:
                    UpdateCarEngine();
                    break;

                case ControllerType.Robot:
                    UpdateRobotEngine();
                    break;

                default:
                    FadeOutAllSources(GetActiveSettings());
                    break;
            }
        }

        private void UpdateCarEngine()
        {
            if (player.CarController == null || carControllerSettings == null || carEngineSfxSettings == null)
            {
                FadeOutAllSources(carEngineSfxSettings);
                return;
            }

            float maxSpeed = carControllerSettings.TopForwardSpeed * carEngineSfxSettings.MaxSpeedMultiplier;
            bool grounded = player.CarController.IsCarGrounded();
            float speed = Mathf.Abs(player.CarController.CarSpeed);
            float airborneInput = Mathf.Clamp01(player.CarController.CarInput.Acceleration);

            UpdateEngineBlend(carEngineSfxSettings, grounded, speed, maxSpeed, airborneInput);
        }

        private void UpdateRobotEngine()
        {
            if (player.RobotController == null || robotControllerSettings == null || robotEngineSfxSettings == null)
            {
                FadeOutAllSources(robotEngineSfxSettings);
                return;
            }

            float maxSpeed = robotControllerSettings.MaxMoveSpeed * robotEngineSfxSettings.MaxSpeedMultiplier;
            bool grounded = player.RobotController.IsGrounded();
            float speed = Mathf.Abs(player.RobotController.RobotSpeed);
            float airborneInput = Mathf.Clamp01(player.RobotController.Input.Movement.magnitude);

            UpdateEngineBlend(robotEngineSfxSettings, grounded, speed, maxSpeed, airborneInput);
        }

        private void UpdateEngineBlend(
            PlayerEngineSfxSettings settings,
            bool grounded,
            float speed,
            float maxSpeed,
            float airborneInput)
        {
            if (settings == null)
                return;

            float maxUsableRPM = Mathf.Min(settings.MaxRPM, clipMaxRPM, GetClipRPM(audioSources.Count - 1));
            maxSpeed = Mathf.Max(0.01f, maxSpeed);

            float targetRPM;

            if (grounded)
            {
                float speedT = Mathf.InverseLerp(0f, maxSpeed, speed);
                targetRPM = Mathf.Lerp(settings.MinRPM, maxUsableRPM, speedT);
            }
            else
            {
                targetRPM = Mathf.Lerp(settings.MinRPM, maxUsableRPM, airborneInput);
            }

            if (_currentRPM <= 0f)
                _currentRPM = settings.MinRPM;

            _currentRPM = SmoothValue(_currentRPM, targetRPM, settings.RPMSmoothSpeed);

            float rpm = Mathf.Clamp(_currentRPM, settings.MinRPM, maxUsableRPM);
            ApplyWeightedEngineBlend(rpm, settings);
        }

        private void ApplyWeightedEngineBlend(float rpm, PlayerEngineSfxSettings settings)
        {
            float totalWeight = 0f;

            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSources[i] == null) continue;

                float clipRPM = GetClipRPM(i);
                totalWeight += GetRPMWeight(rpm, clipRPM, settings);
            }

            if (totalWeight <= 0.0001f)
            {
                FadeOutAllSources(settings);
                return;
            }

            for (int i = 0; i < audioSources.Count; i++)
            {
                AudioSource source = audioSources[i];
                if (source == null) continue;

                float clipRPM = GetClipRPM(i);
                float weight = GetRPMWeight(rpm, clipRPM, settings);

                float normalizedWeight = weight / totalWeight;
                float targetVolume = normalizedWeight * settings.Volume;
                float targetPitch = GetPitchForRPM(rpm, clipRPM, settings);

                source.volume = SmoothValue(source.volume, targetVolume, settings.VolumeSmoothSpeed);
                source.pitch = SmoothValue(source.pitch, targetPitch, settings.PitchSmoothSpeed);
            }
        }

        private void FadeOutAllSources(PlayerEngineSfxSettings settings)
        {
            float volumeSmoothSpeed = settings != null ? settings.VolumeSmoothSpeed : 10f;
            float pitchSmoothSpeed = settings != null ? settings.PitchSmoothSpeed : 12f;

            foreach (AudioSource source in audioSources)
            {
                if (source == null) continue;

                source.volume = SmoothValue(source.volume, 0f, volumeSmoothSpeed);
                source.pitch = SmoothValue(source.pitch, 1f, pitchSmoothSpeed);
            }
        }

        private float GetRPMWeight(float currentRPM, float clipRPM, PlayerEngineSfxSettings settings)
        {
            float distance = Mathf.Abs(currentRPM - clipRPM);
            float weight = 1f - Mathf.Clamp01(distance / settings.BlendRange);

            return weight * weight * (3f - 2f * weight);
        }

        private float GetClipRPM(int index)
        {
            return clipMinRPM + index * clipRPMStep;
        }

        private float GetPitchForRPM(float currentRPM, float clipRPM, PlayerEngineSfxSettings settings)
        {
            if (clipRPM <= 0f)
                return 1f;

            float pitch = currentRPM / clipRPM;
            return Mathf.Clamp(pitch, settings.MinPitch, settings.MaxPitch);
        }

        private float SmoothValue(float current, float target, float speed)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
        }

        private PlayerEngineSfxSettings GetActiveSettings()
        {
            return _currentControllerType == ControllerType.Car
                ? carEngineSfxSettings
                : robotEngineSfxSettings;
        }

        private void OnControllerSwitch(ControllerType type)
        {
            _currentControllerType = type;

            PlayerEngineSfxSettings settings = GetActiveSettings();
            if (settings != null)
                _currentRPM = settings.MinRPM;
        }
    }

    [Serializable]
    public class PlayerEngineSfxSettings
    {
        [Header("Engine Volume")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float volumeSmoothSpeed = 10f;

        [Header("RPM")]
        [SerializeField] private float minRPM = 1000f;
        [SerializeField] private float maxRPM = 6000f;
        [SerializeField] private float rpmSmoothSpeed = 6f;

        [Header("RPM Blend")]
        [SerializeField] private float blendRange = 900f;

        [Header("Pitch")]
        [SerializeField] private float minPitch = 0.75f;
        [SerializeField] private float maxPitch = 1.5f;
        [SerializeField] private float pitchSmoothSpeed = 12f;

        [Header("Speed To RPM")]
        [SerializeField] private float maxSpeedMultiplier = 2f;

        public float Volume => volume;
        public float VolumeSmoothSpeed => volumeSmoothSpeed;

        public float MinRPM => minRPM;
        public float MaxRPM => maxRPM;
        public float RPMSmoothSpeed => rpmSmoothSpeed;

        public float BlendRange => blendRange;

        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float PitchSmoothSpeed => pitchSmoothSpeed;

        public float MaxSpeedMultiplier => maxSpeedMultiplier;

        public void Validate()
        {
            minRPM = Mathf.Max(1f, minRPM);
            maxRPM = Mathf.Max(minRPM, maxRPM);
            blendRange = Mathf.Max(1f, blendRange);

            volumeSmoothSpeed = Mathf.Max(0.01f, volumeSmoothSpeed);
            rpmSmoothSpeed = Mathf.Max(0.01f, rpmSmoothSpeed);
            pitchSmoothSpeed = Mathf.Max(0.01f, pitchSmoothSpeed);

            minPitch = Mathf.Max(0.01f, minPitch);
            maxPitch = Mathf.Max(minPitch, maxPitch);

            maxSpeedMultiplier = Mathf.Max(0.01f, maxSpeedMultiplier);
        }
    }
}