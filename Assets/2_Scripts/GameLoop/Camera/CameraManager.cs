using DNExtensions.Utilities.CinemachineExtensions;
using ProjectWallE.GameLoop;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayerManagerInput))]
    public class CameraManager : MonoBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float mouseLookSensitivity = 0.08f;
        [SerializeField] private float gamepadLookSensitivity = 180f;
        [SerializeField, Range(1, 89)] private float cameraVerticalClamp = 80f;
        
        [Header("Shake Settings")]
        [SerializeField] private ImpulseSettings damageImpulseSettings;
        [SerializeField] private RotationShakeSettings attack1ShakeSettings;
        [SerializeField] private RotationShakeSettings attack2ShakeSettings;
        
        [Header("References")]
        [SerializeField] private CinemachineCamera podCamera;
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private CinemachineRotationShake rotationShake;
        [SerializeField] private Transform cameraTarget;

        private PlayerManager _playerManager;
        private PlayerManagerInput _input;
        private Pod _activePod;
        
        private bool _cameraLocked;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            _playerManager = FindFirstObjectByType<PlayerManager>();
            _input = GetComponent<PlayerManagerInput>();

            Vector3 euler = cameraTarget.rotation.eulerAngles;
            _yaw = euler.y;

            _pitch = euler.x;
            if (_pitch > 180f)
                _pitch -= 360f;
        }

        private void OnEnable()
        {
            DeploymentManager.OnPodDeployed += OnPodDeployed;
            
            if (_playerManager != null)
            {
                _playerManager.OnControllerChanged += OnControllerSwitch;
                _playerManager.OnDamaged += OnDamaged;
                _playerManager.StructureBuilder.ActionsMenuRequested += OnMenuActionRequested;
                _playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                _playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
                _playerManager.Shooter.OnAttack1 += OnAttack1;
                _playerManager.Shooter.OnAttack2 += OnAttack2;
            }
        }
        

        private void OnDisable()
        {
            DeploymentManager.OnPodDeployed -= OnPodDeployed;
            
            if (_playerManager != null)
            {
                _playerManager.OnControllerChanged -= OnControllerSwitch;
                _playerManager.OnDamaged -= OnDamaged;
                _playerManager.StructureBuilder.ActionsMenuRequested -= OnMenuActionRequested;
                _playerManager.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                _playerManager.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
                _playerManager.Shooter.OnAttack1 -= OnAttack1;
                _playerManager.Shooter.OnAttack2 -= OnAttack2;
            }
        }

        private void LateUpdate()
        {
            UpdateCameraMovement();
        }
        
        private void OnPodDeployed(DeploymentRequest request, Pod pod)
        {
            if (!request.UseCamera) return;

            _activePod = pod;
            _activePod.OnLand += OnLand;
    
            podCamera.LookAt = pod.transform;
    
            SwitchActiveCamera(podCamera);
        }

        private void OnLand()
        {
            if (_activePod)
            {
                Vector3 dir = (_activePod.transform.position - _playerManager.transform.position).normalized;
                _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
                _pitch = Mathf.Clamp(_pitch, -cameraVerticalClamp, cameraVerticalClamp);

                podCamera.LookAt = null;
                _activePod.OnLand -= OnLand;
            }
            _activePod = null;
            SwitchActiveCamera(_playerManager.PlayerControllerType == PlayerControllerType.Robot ? robotCamera : carCamera);
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

        private void OnControllerSwitch(PlayerControllerType controllerType)
        {
            if (_activePod) return;
            
            bool isRobot = controllerType == PlayerControllerType.Robot;
            
            SwitchActiveCamera(isRobot ? robotCamera : carCamera);
        }

        private void OnMenuActionRequested(Structure obj)
        {
            _cameraLocked = true;
        }

        private void OnBuildMenuRequested(Structure[] obj)
        {
            _cameraLocked = true;
        }

        private void OnMenuCloseRequested()
        {
            _cameraLocked = false;
        }

        private void UpdateCameraMovement()
        {
            cameraTarget.position = _playerManager.transform.position;

            if (_cameraLocked || _activePod) return;

            Vector2 lookDelta = GetScaledLookDelta();

            _yaw += lookDelta.x;
            _pitch -= lookDelta.y;
            _pitch = Mathf.Clamp(_pitch, -cameraVerticalClamp, cameraVerticalClamp);

            cameraTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
        
        private Vector2 GetScaledLookDelta()
        {
            Vector2 rawLook = _input.LookInput;

            if (_input.IsGamepadLook)
                return rawLook * (gamepadLookSensitivity * Time.deltaTime);

            return rawLook * mouseLookSensitivity;
        }

        private void SwitchActiveCamera(CinemachineCamera cam)
        {
            podCamera.Priority.Value = 0;
            carCamera.Priority.Value = 0;
            robotCamera.Priority.Value = 0;
            
            cam.Priority.Value = 1;
        }
    }
}