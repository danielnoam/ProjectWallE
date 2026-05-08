using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE
{
    public class EngineSfx : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SOCarControllerSettings carControllerSettings;
        [SerializeField] private List<AudioSource> audioSources;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager player;

        [Header("Engine Volume")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private float volumeSmoothSpeed = 10f;

        [Header("RPM")]
        [SerializeField] private float minRPM = 1000f;
        [SerializeField] private float maxRPM = 6000f;
        [SerializeField] private float rpmStep = 500f;
        [SerializeField] private float rpmSmoothSpeed = 6f;

        [Header("RPM Blend")]
        [SerializeField] private float blendRange = 900f;

        [Header("Pitch")]
        [SerializeField] private float minPitch = 0.75f;
        [SerializeField] private float maxPitch = 1.5f;
        [SerializeField] private float pitchSmoothSpeed = 12f;

        [Header("Speed To RPM")]
        [SerializeField] private float maxSpeedMultiplier = 2f;

        private bool _isCar;
        private float _currentRPM;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);

            minRPM = Mathf.Max(1f, minRPM);
            maxRPM = Mathf.Max(minRPM, maxRPM);
            rpmStep = Mathf.Max(1f, rpmStep);
            blendRange = Mathf.Max(rpmStep, blendRange);

            volumeSmoothSpeed = Mathf.Max(0.01f, volumeSmoothSpeed);
            rpmSmoothSpeed = Mathf.Max(0.01f, rpmSmoothSpeed);
            pitchSmoothSpeed = Mathf.Max(0.01f, pitchSmoothSpeed);

            minPitch = Mathf.Max(0.01f, minPitch);
            maxPitch = Mathf.Max(minPitch, maxPitch);
            maxSpeedMultiplier = Mathf.Max(0.01f, maxSpeedMultiplier);
        }

        private void Awake()
        {
            _currentRPM = minRPM;

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
            if (audioSources == null || audioSources.Count == 0)
                return;

            if (player == null || carControllerSettings == null || !_isCar || player.CarController == null)
            {
                FadeOutAllSources();
                _currentRPM = minRPM;
                return;
            }

            float maxUsableRPM = Mathf.Min(maxRPM, GetClipRPM(audioSources.Count - 1));
            float maxSpeed = carControllerSettings.TopForwardSpeed * maxSpeedMultiplier;
            maxSpeed = Mathf.Max(0.01f, maxSpeed);
            float targetRPM;

            if (player.CarController.IsCarGrounded())
            {
                float speed = Mathf.Abs(player.CarController.CarSpeed);
                float speedT = Mathf.InverseLerp(0f, maxSpeed, speed);
                targetRPM = Mathf.Lerp(minRPM, maxUsableRPM, speedT);
            }
            else
            {
                float accelInput = Mathf.Clamp01(player.CarController.CarInput.Acceleration);
                targetRPM = Mathf.Lerp(minRPM, maxUsableRPM, accelInput);
            }

            _currentRPM = SmoothValue(_currentRPM, targetRPM, rpmSmoothSpeed);
            float rpm = Mathf.Clamp(_currentRPM, minRPM, maxUsableRPM);
            ApplyWeightedEngineBlend(rpm);
        }

        private void ApplyWeightedEngineBlend(float rpm)
        {
            float totalWeight = 0f;

            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSources[i] == null) continue;

                float clipRPM = GetClipRPM(i);
                totalWeight += GetRPMWeight(rpm, clipRPM);
            }

            if (totalWeight <= 0.0001f)
            {
                FadeOutAllSources();
                return;
            }

            for (int i = 0; i < audioSources.Count; i++)
            {
                AudioSource source = audioSources[i];
                if (source == null) continue;

                float clipRPM = GetClipRPM(i);
                float weight = GetRPMWeight(rpm, clipRPM);

                float normalizedWeight = weight / totalWeight;
                float targetVolume = normalizedWeight * volume;
                float targetPitch = GetPitchForRPM(rpm, clipRPM);

                source.volume = SmoothValue(source.volume, targetVolume, volumeSmoothSpeed);
                source.pitch = SmoothValue(source.pitch, targetPitch, pitchSmoothSpeed);
            }
        }

        private void FadeOutAllSources()
        {
            foreach (AudioSource source in audioSources)
            {
                if (source == null) continue;

                source.volume = SmoothValue(source.volume, 0f, volumeSmoothSpeed);
                source.pitch = SmoothValue(source.pitch, 1f, pitchSmoothSpeed);
            }
        }

        private float GetRPMWeight(float currentRPM, float clipRPM)
        {
            float distance = Mathf.Abs(currentRPM - clipRPM);

            float weight = 1f - Mathf.Clamp01(distance / blendRange);

            // Smoothstep.
            return weight * weight * (3f - 2f * weight);
        }

        private float GetClipRPM(int index)
        {
            return minRPM + index * rpmStep;
        }

        private float GetPitchForRPM(float currentRPM, float clipRPM)
        {
            if (clipRPM <= 0f)
                return 1f;

            float pitch = currentRPM / clipRPM;
            return Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private float SmoothValue(float current, float target, float speed)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
        }

        private void OnControllerSwitch(ControllerType type)
        {
            _isCar = type == ControllerType.Car;
        }
    }
}