using System;
using System.Collections;
using _2_Scripts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

namespace ProjectWallE
{

    public enum PlayerControllerType
    {
        Robot, Car
    }
    public class PlayerManager : MonoBehaviour, IDamageable, IPushable
    {
        [SerializeField] LayerMask groundLayer;
        
        private PlayerManagerInput _input;
        private Rigidbody _rigidbody;
        private Transform _cameraTransform;
        
        private PlayerControllerType _playerControllerTypeEnum;
        private PlayerControllerType _lastFramePlayerControllerTypeEnum;
        
        private IPlayerController _currentController;
        private IPlayerController _carController;
        private IPlayerController _robotController;
        
        public CarController carController => _carController as CarController;
        public RobotController robotController => _robotController as RobotController;
        public bool canBuild => _currentController.canBuild;
        public bool canShoot => _currentController.canShoot;
        public Vector3 velocity => _rigidbody.linearVelocity;
        
        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;
        public event Action<PlayerControllerType> OnControllerChanged;
        
        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            _input = GetComponent<PlayerManagerInput>();
            _rigidbody = GetComponent<Rigidbody>();
            if (Camera.main != null) _cameraTransform = Camera.main.transform;

            _carController = GetComponentInChildren<CarController>();
            _robotController = GetComponentInChildren<RobotController>();
            _playerControllerTypeEnum = PlayerControllerType.Robot;

            PlayerReferences playerReferences = new PlayerReferences()
            {
                cameraTransform = _cameraTransform,
                rigidBody = _rigidbody,
                groundLayer = groundLayer
            };
            
            _carController.Initialize(playerReferences);
            _robotController.Initialize(playerReferences);
        }

        private void Start()
        {
            EnableController(_playerControllerTypeEnum);
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


        #region Controller Switch

        private void SwitchController()
        {
            _playerControllerTypeEnum = _playerControllerTypeEnum == PlayerControllerType.Robot ? PlayerControllerType.Car : PlayerControllerType.Robot;
        }

        private void SwitchBehavior()
        {
            if(_playerControllerTypeEnum == _lastFramePlayerControllerTypeEnum) return;
            ResetRotation();
            EnableController(_playerControllerTypeEnum);
            _lastFramePlayerControllerTypeEnum = _playerControllerTypeEnum;
        }

        #endregion

        #region Helpers

        private void EnableController(PlayerControllerType playerControllerType)
        {
            bool isRobot = playerControllerType == PlayerControllerType.Robot;
            
            //controller
            _currentController = isRobot ? _robotController : _carController;
            _robotController.gameObject.SetActive(isRobot);
            _carController.gameObject.SetActive(!isRobot);
            
            OnControllerChanged?.Invoke(playerControllerType);
        }

        private void ResetRotation()
        {
            Vector3 rot = _rigidbody.rotation.eulerAngles;
            rot.y = _cameraTransform.rotation.eulerAngles.y;
            _rigidbody.rotation = Quaternion.Euler(rot);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        #endregion

        
        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            
        }

        public void Push(Vector3 direction, float force)
        {
            
        }
    }
}
