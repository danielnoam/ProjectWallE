using System;
using _2_Scripts;
using ProjectWallE.GameLoop.Player;
using UnityEngine;

namespace ProjectWallE
{

    public enum PlayerControllerType
    {
        Robot, Car
    }
    public class PlayerManager : MonoBehaviour, IDamageable, IPushable
    {
        [SerializeField, Min(10f)] private float maxHealth = 100f;
        [SerializeField] private float switchCooldown = 1f;
        [SerializeField] LayerMask groundLayer;
        
        [HideInInspector][SerializeField] CarController carController;
        [HideInInspector][SerializeField] RobotController robotController;
        [HideInInspector][SerializeField] PlayerStructureBuilder structureBuilder;
        
        private PlayerManagerInput _input;
        
        private Rigidbody _rigidbody;
        private Transform _cameraTransform;
        
        private PlayerControllerType _playerControllerTypeEnum;
        private PlayerControllerType _lastFramePlayerControllerTypeEnum;
        
        private IPlayerController _currentController;
        
        private float _currentHealth;
        private float _switchTimer;

        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public PlayerStructureBuilder StructureBuilder => structureBuilder;
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
            if(structureBuilder == null)
                structureBuilder = GetComponentInChildren<PlayerStructureBuilder>();
        }

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _input = GetComponent<PlayerManagerInput>();
            _rigidbody = GetComponent<Rigidbody>();
            if (Camera.main) _cameraTransform = Camera.main.transform;

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
            EnableController(PlayerControllerType.Robot);
        }

        private void Update()
        {
            if (_input.SwitchPressed && Time.time > _switchTimer + switchCooldown) 
                SwitchControllerEnum();
            
            SwitchBehavior();
        }

        private void FixedUpdate()
        {
            _currentController?.ApplyMovement();
        }


        #region Controller Switch

        private void SwitchControllerEnum()
        {
            _playerControllerTypeEnum = _playerControllerTypeEnum == PlayerControllerType.Robot ? PlayerControllerType.Car : PlayerControllerType.Robot;
        }

        private void SwitchBehavior()
        {
            if(_playerControllerTypeEnum == _lastFramePlayerControllerTypeEnum) return;
            EnableController(_playerControllerTypeEnum);
            ResetRbRotation();
            _lastFramePlayerControllerTypeEnum = _playerControllerTypeEnum;
            _switchTimer = Time.time;
        }

        #endregion

        #region Helpers

        private void EnableController(PlayerControllerType playerControllerType)
        {
            bool isRobot = playerControllerType == PlayerControllerType.Robot;
            
            //controller
            _currentController?.OnExit();
            _currentController?.gameObject.SetActive(false);
            _currentController = isRobot ? robotController : carController;
            _currentController.gameObject.SetActive(true);
            _currentController.OnEnter();

            _rigidbody.centerOfMass = _currentController.CenterOfMassOffset;
            
            OnControllerChanged?.Invoke(playerControllerType);
        }

        private void ResetRbRotation()
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

        private void OnDrawGizmosSelected()
        {
            if(_rigidbody == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + _rigidbody.centerOfMass, .1f);
            
        }
    }
}
