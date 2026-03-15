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
        [SerializeField, Min(10f)] private float maxHealth = 100f;
        [SerializeField] LayerMask groundLayer;
        
        [HideInInspector][SerializeField] CarController carController;
        [HideInInspector][SerializeField] RobotController robotController;
        
        private float _currentHealth;
        private PlayerManagerInput _input;
        private Rigidbody _rigidbody;
        private Transform _cameraTransform;
        
        private PlayerControllerType _playerControllerTypeEnum;
        private PlayerControllerType _lastFramePlayerControllerTypeEnum;
        
        private IPlayerController _currentController;

        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public bool CanBuild => _currentController.canBuild;
        public bool CanShoot => _currentController.canShoot;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        
        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;
        public event Action<PlayerControllerType> OnControllerChanged;
        public event Action<float, float> OnHealthChanged;

        private void OnValidate()
        {
            if (carController == null)
                carController = GetComponentInChildren<CarController>();
            if(robotController == null)
                robotController = GetComponentInChildren<RobotController>();
        }

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _input = GetComponent<PlayerManagerInput>();
            _rigidbody = GetComponent<Rigidbody>();
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            _playerControllerTypeEnum = PlayerControllerType.Robot;

            PlayerReferences playerReferences = new PlayerReferences()
            {
                cameraTransform = _cameraTransform,
                rigidBody = _rigidbody,
                groundLayer = groundLayer
            };

            carController.Initialize(playerReferences);
            robotController.Initialize(playerReferences);
            
            _currentHealth = maxHealth;
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
            _currentController = isRobot ? robotController : carController;
            robotController.gameObject.SetActive(isRobot);
            carController.gameObject.SetActive(!isRobot);
            
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
            if (damage <= 0 || _currentHealth <= 0) return;
            
            _currentHealth -= damage;
            
            OnDamaged?.Invoke(damage);
            
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                OnDeath?.Invoke(attacker);
            }
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Push(Vector3 direction, float force)
        {
            
        }
    }
}
