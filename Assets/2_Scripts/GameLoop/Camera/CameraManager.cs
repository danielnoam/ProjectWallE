using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using ProjectWallE.GameLoop;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayerManagerInput))]
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        public static event Action<bool> OnCameraChanged;
        
        [Header("Look Settings")]
        [SerializeField] private float mouseLookSensitivity = 0.08f;
        [SerializeField] private float gamepadLookSensitivity = 180f;
        [SerializeField, Range(1, 89)] private float cameraVerticalClamp = 80f;
        
        [Header("Car FOV Settings")]
        [SerializeField, MinMaxRange(0f, 50f)] private RangedFloat speedMagnitudeRange = new RangedFloat(15f, 30f);
        [SerializeField, MinMaxRange(0f, 150)] private RangedFloat fovRange = new RangedFloat(75f, 90f);
        
        [Header("Shake Settings")]
        [SerializeField] private ImpulseSettings damageImpulseSettings;
        [SerializeField] private ImpulseSettings deathImpulseSettings;
        [SerializeField] private RotationShakeSettings attack1ShakeSettings;
        [SerializeField] private RotationShakeSettings attack2ShakeSettings;
        
        [Header("References")]
        [SerializeField] private CinemachineCamera lookAtPodCamera;
        [SerializeField] private CinemachineCamera podCamera;
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private CinemachineRotationShake rotationShake;
        [SerializeField] private Transform cameraTarget;
        [SerializeField, AutoGetScene] private PlayerManager playerManager;
        [SerializeField, AutoGetSelf] private PlayerManagerInput input;
        
        private Transform _podLookTarget;
        
        private CameraAutoAlign _cameraAutoAlign;
        private Pod _activePod;
        
        private bool _cameraLocked;
        private float _yaw;
        private float _pitch;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            } 
            Instance = this;

            Vector3 euler = cameraTarget.rotation.eulerAngles;
            _yaw = euler.y;

            _pitch = euler.x;
            if (_pitch > 180f) _pitch -= 360f;
            
            _podLookTarget = new GameObject("PodLookTarget").transform;
        }

        private void OnEnable()
        {
            DeploymentManager.OnPodLaunched += OnPodLaunched;
            
            if (playerManager != null)
            {
                playerManager.OnControllerChanged += OnControllerSwitch;
                playerManager.OnDamaged += OnDamaged;
                playerManager.OnDeath += OnDeath;
                playerManager.StructureBuilder.ActionsMenuRequested += OnMenuActionRequested;
                playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
                playerManager.SupportCaller.SupportMenuRequested += SupportCallerOnSupportMenuRequested;
                playerManager.SupportCaller.MenuCloseRequested += OnMenuCloseRequested;

                playerManager.Shooter.OnAttack1 += OnAttack1;
                playerManager.Shooter.OnAttack2 += OnAttack2;
            }
        }

        private void SupportCallerOnSupportMenuRequested(SOSupportActionData[] obj)
        {
            _cameraLocked = true;
        }


        private void OnDisable()
        {
            DeploymentManager.OnPodLaunched -= OnPodLaunched;
            
            if (playerManager != null)
            {
                playerManager.OnControllerChanged -= OnControllerSwitch;
                playerManager.OnDamaged -= OnDamaged;
                playerManager.OnDeath -= OnDeath;
                playerManager.StructureBuilder.ActionsMenuRequested -= OnMenuActionRequested;
                playerManager.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                playerManager.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
                playerManager.SupportCaller.SupportMenuRequested -= SupportCallerOnSupportMenuRequested;
                playerManager.SupportCaller.MenuCloseRequested -= OnMenuCloseRequested;
                playerManager.Shooter.OnAttack1 -= OnAttack1;
                playerManager.Shooter.OnAttack2 -= OnAttack2;
            }
        }

        private void LateUpdate()
        {
            if (_activePod) _podLookTarget.position = _activePod.transform.position;
            UpdateCameraMovement();
            UpdateCarFOV();
        }
        
        private void OnDeath(IDamageable attacker)
        {
            impulseSource?.GenerateImpulse(deathImpulseSettings);
            cameraTarget.eulerAngles = Vector3.zero;
        }

        private void OnPodLaunched(DeploymentRequest request, Pod pod)
        {
            if (request.CameraMode == PodCameraMode.None) return;

            if (_activePod)
            {
                _activePod.OnLand -= OnPodLand;
            }

            _activePod = pod;
            _activePod.OnLand += OnPodLand;

            switch (request.CameraMode)
            {
                case PodCameraMode.LookAt:
                    lookAtPodCamera.LookAt = _podLookTarget;
                    SwitchActiveCamera(lookAtPodCamera);
                    break;
                case PodCameraMode.Follow:
                    podCamera.Follow = _podLookTarget;
                    podCamera.LookAt = _podLookTarget;
                    SwitchActiveCamera(podCamera);
                    break;
            }
        }

        private void OnPodLand()
        {
            if (_activePod)
            {
                Vector3 dir = (_activePod.transform.position - playerManager.transform.position).normalized;
                _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
                _pitch = Mathf.Clamp(_pitch, -cameraVerticalClamp, cameraVerticalClamp);

                lookAtPodCamera.LookAt = null;
                podCamera.Follow = null;
                podCamera.LookAt = null;
                _activePod.OnLand -= OnPodLand;
            }

            _activePod = null;
            SwitchActiveCamera(playerManager.ControllerType == ControllerType.Robot ? robotCamera : carCamera);
        }
        
        private void OnDamaged(float damage)
        {
            if (damage <= 0) return;
            impulseSource?.GenerateImpulse(damageImpulseSettings);
        }
        
        private void OnAttack1()
        {
            rotationShake.Shake(attack1ShakeSettings);
        }
        
        private void OnAttack2()
        {
            rotationShake.Shake(attack2ShakeSettings);
        }

        private void OnControllerSwitch(ControllerType controllerType)
        {
            if (_activePod) return;
            
            bool isRobot = controllerType == ControllerType.Robot;
            SwitchActiveCamera(isRobot ? robotCamera : carCamera);
        }

        private void OnMenuActionRequested(Structure obj)
        {
            _cameraLocked = true;
        }

        private void OnBuildMenuRequested(Structure[] obj, bool canBuild)
        {
            _cameraLocked = true;
        }

        private void OnMenuCloseRequested()
        {
            _cameraLocked = false;
        }

        private void UpdateCameraMovement()
        {
            cameraTarget.position = playerManager.transform.position;

            if (_cameraLocked || _activePod) return;

            Vector2 lookDelta = GetScaledLookDelta();

            if (lookDelta != Vector2.zero)
            {
                _cameraAutoAlign?.NotifyInput();

                _yaw += lookDelta.x;
                _pitch -= lookDelta.y;
                _pitch = Mathf.Clamp(_pitch, -cameraVerticalClamp, cameraVerticalClamp);
            }
            else if (_cameraAutoAlign != null && _cameraAutoAlign.CanAlign(out float xOffset))
            {
                Quaternion target = Quaternion.Euler(playerManager.transform.eulerAngles.x + xOffset, playerManager.transform.eulerAngles.y, 0f);
                cameraTarget.rotation = Quaternion.Slerp(cameraTarget.rotation, target, Time.deltaTime * _cameraAutoAlign.Strength);

                // Sync yaw/pitch so there's no snap when the player looks again
                _yaw = cameraTarget.eulerAngles.y;
                _pitch = cameraTarget.eulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;

                return;
            }

            cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
        
        private Vector2 GetScaledLookDelta()
        {
            Vector2 rawLook = input.LookInput;

            if (input.IsGamepadLook)
                return rawLook * (gamepadLookSensitivity * Time.deltaTime);

            return rawLook * mouseLookSensitivity;
        }
        
        private void UpdateCarFOV()
        {
            var horizontalVelocity = playerManager.Velocity.SetY(0f);
            float t = Mathf.InverseLerp(speedMagnitudeRange.minValue, speedMagnitudeRange.maxValue, horizontalVelocity.magnitude);
            carCamera.Lens.FieldOfView = Mathf.Lerp(fovRange.minValue, fovRange.maxValue, t);
        }

        private void SwitchActiveCamera(CinemachineCamera cam)
        {
            bool nonePlayerCamera = cam == lookAtPodCamera || cam == podCamera;
            
            lookAtPodCamera.Priority.Value = 0;
            podCamera.Priority.Value = 0;
            carCamera.Priority.Value = 0;
            robotCamera.Priority.Value = 0;

            cam.Priority.Value = 1;
            cam.TryGetComponent(out _cameraAutoAlign);
            OnCameraChanged?.Invoke(nonePlayerCamera);
        }
    }
}