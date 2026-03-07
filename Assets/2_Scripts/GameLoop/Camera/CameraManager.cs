using System;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera robotCamera;
        [SerializeField] private CinemachineCamera carCamera;
        
        private CinemachineInputAxisController _robotInputAxisController;
        private CinemachineInputAxisController _carInputAxisController;

        private void Awake()
        {
            _robotInputAxisController = robotCamera.GetComponent<CinemachineInputAxisController>();
            _carInputAxisController = carCamera.GetComponent<CinemachineInputAxisController>();
        }

        private void OnEnable()
        {
            PlayerManager.OnControllerChanged += OnControllerSwitch;
        }
        
        private void OnDisable()
        {
            PlayerManager.OnControllerChanged -= OnControllerSwitch;
        }
        
        private void OnControllerSwitch(PlayerControllerType controllerType)
        {
            bool isRobot = controllerType == PlayerControllerType.Robot;
            
            robotCamera.Priority.Value = isRobot ? 1 : 0;
            carCamera.Priority.Value = isRobot ? 0 : 1;
        }

        private void ToggleCameraMouseInput(bool toggle)
        {
            _robotInputAxisController.enabled = toggle;
            _carInputAxisController.enabled = toggle;
        }
    }
}
