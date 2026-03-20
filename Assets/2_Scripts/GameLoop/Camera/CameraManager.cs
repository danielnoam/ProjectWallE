using System;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayerManagerInput))]
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraLookSensitivity;
        [SerializeField, Range(1, 89)] private float cameraVerticalClamp;
        
        private CinemachineInputAxisController _robotInputAxisController;
        private CinemachineInputAxisController _carInputAxisController;
        
        private PlayerManager _playerManager;
        private PlayerManagerInput _input;

        private void Awake()
        {
            _robotInputAxisController = robotCamera.GetComponent<CinemachineInputAxisController>();
            _carInputAxisController = carCamera.GetComponent<CinemachineInputAxisController>();
            
            _playerManager = FindFirstObjectByType<PlayerManager>();
            _input = GetComponent<PlayerManagerInput>();
        }

        private void OnEnable()
        {
            if (_playerManager == null) return;
            _playerManager.OnControllerChanged += OnControllerSwitch;
            _playerManager.StructureBuilder.ActionsMenuRequested += OnMenuActionRequested;
            _playerManager.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
            _playerManager.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
        }
        
        private void OnDisable()
        {           
            if (_playerManager == null) return;
            _playerManager.OnControllerChanged -= OnControllerSwitch;
        }

        private void LateUpdate()
        {
            UpdateCameraMovement();
        }

        private void OnControllerSwitch(PlayerControllerType controllerType)
        {
            bool isRobot = controllerType == PlayerControllerType.Robot;
            
            robotCamera.Priority.Value = isRobot ? 1 : 0;
            carCamera.Priority.Value = isRobot ? 0 : 1;
        }
        
        private void OnMenuActionRequested(Structure obj)
        {
            
        }
        
        private void OnBuildMenuRequested(Structure[] obj)
        {
            
        }
        private void OnMenuCloseRequested()
        {
            
        }

        private void UpdateCameraMovement()
        {
            cameraTarget.position = _playerManager.transform.position;
            
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

        private void ToggleCameraMouseInput(bool toggle)
        {
            _robotInputAxisController.enabled = toggle;
            _carInputAxisController.enabled = toggle;
        }
    }
}
