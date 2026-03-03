using System;
using System.Collections;
using _2_Scripts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

namespace ProjectWallE
{

    public enum CurrentController
    {
        Robot, Car
    }
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] private CarController carController;
        [SerializeField] private RobotController robotController;
        [SerializeField] private CinemachineCamera carCamera;
        [SerializeField] private CinemachineCamera robotCamera;
        
        private PlayerManagerInput _input;
        private Rigidbody _rigidbody;
        
        private CurrentController _currentControllerEnum;
        private CurrentController _lastFrameCurrentControllerEnum;
        
        private IPlayerController _currentController;
        private IPlayerController _carController;
        private IPlayerController _robotController;
        
        

        void Awake()
        {
            _input = GetComponent<PlayerManagerInput>();
            _rigidbody = GetComponent<Rigidbody>();
            
            _carController = carController.GetComponent<CarController>();
            _robotController = robotController.GetComponent<RobotController>();
            _currentControllerEnum = CurrentController.Robot;
        }

        private void Start()
        {
            EnableController(_currentControllerEnum);
        }

        private void Update()
        {
            if (_input.SwitchPressed) SwitchController();
            SwitchBehavior();
        }

        private void FixedUpdate()
        {
            _currentController?.ApplyMovement();
        }
        
        private void SwitchController()
        {
            _currentControllerEnum = _currentControllerEnum == CurrentController.Robot ? CurrentController.Car : CurrentController.Robot;
        }

        private void SwitchBehavior()
        {
            if(_currentControllerEnum == _lastFrameCurrentControllerEnum) return;
            ResetRotation();
            EnableController(_currentControllerEnum);
            _lastFrameCurrentControllerEnum = _currentControllerEnum;
        }


        #region Helpers

        private void EnableController(CurrentController currentController)
        {
            bool isRobot = currentController == CurrentController.Robot;
            
            //controller
            _currentController = isRobot ? _robotController : _carController;
            robotController.gameObject.SetActive(isRobot);
            carController.gameObject.SetActive(!isRobot);
            
            //camera
            robotCamera.Priority.Value = isRobot ? 1 : 0;
            carCamera.Priority.Value = isRobot ? 0 : 1;
        }

        private void ResetRotation()
        {
            Vector3 rot = _rigidbody.rotation.eulerAngles;
            rot.y = cameraTransform.rotation.eulerAngles.y;
            _rigidbody.rotation = Quaternion.Euler(rot);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        #endregion
        
    }
}
