using ProjectWallE.GameLoop;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayerManagerInput))]
    public class CameraManager : MonoBehaviour
    {
        [Header("Pod")]
        [SerializeField] private CinemachineCamera podCamera;
        
        [Header("Player")]
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraLookSensitivity;
        [SerializeField, Range(1, 89)] private float cameraVerticalClamp;
        
        private PlayerManager _playerManager;
        private PlayerManagerInput _input;
        private Pod _activePod;
        
        private bool _cameraLocked;

        private void Awake()
        {
            _playerManager = FindFirstObjectByType<PlayerManager>();
            _input = GetComponent<PlayerManagerInput>();
        }

        private void OnEnable()
        {
            DeploymentManager.OnPodDeployed += OnPodDeployed;
            
            if (_playerManager != null)
            {
                _playerManager.OnControllerChanged += OnControllerSwitch;
                _playerManager.StructureBuilder.ActionsMenuRequested += OnMenuActionRequested;
                _playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                _playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
            }
        }
        

        private void OnDisable()
        {
            DeploymentManager.OnPodDeployed -= OnPodDeployed;
            
            if (_playerManager != null) _playerManager.OnControllerChanged -= OnControllerSwitch;
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
                Vector3 direction = _activePod.transform.position - _playerManager.transform.position;
                if (direction.sqrMagnitude > 0.001f)
                    cameraTarget.rotation = Quaternion.LookRotation(direction);
                podCamera.LookAt = null;
                _activePod.OnLand -= OnLand;
            }
            _activePod = null;
            SwitchActiveCamera(_playerManager.PlayerControllerType == PlayerControllerType.Robot ? robotCamera : carCamera);
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
            if (_cameraLocked) return;
            
            cameraTarget.rotation *= Quaternion.Euler(-_input.MouseDelta.y * cameraLookSensitivity, _input.MouseDelta.x * cameraLookSensitivity, 0);
            Vector3 targetRotation = cameraTarget.rotation.eulerAngles;
            
            float x = targetRotation.x > cameraVerticalClamp && targetRotation.x < 180 
                    ? Mathf.Clamp(targetRotation.x, 0, cameraVerticalClamp) 
                    : targetRotation.x < 360 -cameraVerticalClamp && targetRotation.x > 180 
                    ? Mathf.Clamp(targetRotation.x, 360 -cameraVerticalClamp, 360) 
                    : targetRotation.x;
            
            targetRotation = new Vector3(x, targetRotation.y, 0);
            cameraTarget.rotation = Quaternion.Euler(targetRotation);
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
