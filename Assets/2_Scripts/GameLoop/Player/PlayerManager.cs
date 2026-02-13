using System;
using _2_Scripts;
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
        [SerializeField] private CarController carController;
        [SerializeField] private RobotController robotController;
        
        private PlayerManagerInput _input;
        
        private CurrentController _currentControllerEnum;
        
        private IPlayerController _currentController;
        private IPlayerController _carController;
        private IPlayerController _robotController;
        

        void Awake()
        {
            _input = GetComponent<PlayerManagerInput>();
            
            _carController = carController.GetComponent<CarController>();
            _robotController = robotController.GetComponent<RobotController>();
            _currentControllerEnum = CurrentController.Robot;
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
            if (_currentControllerEnum == CurrentController.Car && _currentController != _carController)
            {
                _currentController = _carController;
                robotController.gameObject.SetActive(false);
                carController.gameObject.SetActive(true);
            }
            else if (_currentControllerEnum == CurrentController.Robot && _currentController != _robotController)
            {
                _currentController = _robotController;
                robotController.gameObject.SetActive(true);
                carController.gameObject.SetActive(false);
            }
        }
    }
}
