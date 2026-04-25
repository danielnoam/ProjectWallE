using System;
using System.Collections;
using _2_Scripts;
using ProjectWallE.GameLoop;
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
        [Header("Settings")]
        [SerializeField, Min(10f)] private float maxHealth = 100f;
        [SerializeField] private float timeBeforeHealthRegen = 1.5f;
        [SerializeField] private float healthRegenRate = 5f;
        [SerializeField] private float switchCooldown = 1f;
        
        [Header("Collision")]
        [SerializeField] private float robotHeightCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask switchBlockLayer;
        
        [HideInInspector, SerializeField] private CarController carController;
        [HideInInspector, SerializeField] private RobotController robotController;
        [HideInInspector, SerializeField] private PlayerStructureBuilder structureBuilder;
        [HideInInspector, SerializeField] private PlayerShooter shooter;
        [HideInInspector, SerializeField] private PlayerAimer aimer;
        

        private PlayerManagerInput _input;
        private Rigidbody _rigidbody;
        private Transform _cameraTransform;
        
        private PlayerControllerType _playerControllerTypeEnum;
        private PlayerControllerType _lastFramePlayerControllerTypeEnum;
        
        private IPlayerController _currentController;
        
        private Coroutine _lateFixedUpdateCoroutine;
        
        private float _currentHealth;
        private float _switchTimer;
        private float _lastDamageTime;

        public PlayerControllerType PlayerControllerType => _playerControllerTypeEnum;
        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public PlayerStructureBuilder StructureBuilder => structureBuilder;
        public PlayerShooter Shooter => shooter;
        public PlayerAimer Aimer => aimer;
        public bool CanBuild => _currentController.canBuild;
        public bool CanShoot => _currentController.canShoot;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Player;

        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;
        public event Action<PlayerControllerType> OnControllerChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action OnControllerSwitchFailed;

        private void OnValidate()
        {
            if (carController == null) carController = GetComponentInChildren<CarController>();
            if(robotController == null) robotController = GetComponentInChildren<RobotController>();
            if(structureBuilder == null) structureBuilder = GetComponentInChildren<PlayerStructureBuilder>();
            if (shooter == null) shooter = GetComponentInChildren<PlayerShooter>();
            if (aimer == null) aimer = GetComponentInChildren<PlayerAimer>();
        }

        private void Awake()
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

        private void OnEnable()
        {
            _lateFixedUpdateCoroutine = StartCoroutine(RunLateFixedUpdate());
        }

        private void OnDisable()
        {
            StopCoroutine(_lateFixedUpdateCoroutine);
        }

        private void Update()
        {
            _currentController.ApplyUpdate();
            
            if (_input.SwitchPressed && Time.time > _switchTimer + switchCooldown) SwitchControllerEnum();
            
            SwitchBehavior();
            RegenerateHealth();
        }

        private void FixedUpdate()
        {
            _currentController?.ApplyFixedUpdate();
        }

        private void LateUpdate()
        {
            _currentController?.ApplyLateUpdate();
        }

        private void LateFixedUpdate()
        {
            _currentController?.ApplyLateFixedUpdate();
        }
        


        #region Controller Switch

        private void SwitchControllerEnum()
        {
            if (_playerControllerTypeEnum == PlayerControllerType.Car && !CanSwitchToRobot())
            {
                OnControllerSwitchFailed?.Invoke();
                return;
            }
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

        #region Health

        private void Die(IDamageable attacker = null)
        {
            _currentHealth = 0;
            OnDeath?.Invoke(attacker);
            if (LevelManager.Instance) Teleport(LevelManager.Instance.ActivePlayerSpawnPoint);
        }

        private void RegenerateHealth()
        {
            if (_currentHealth >= maxHealth || Time.time < _lastDamageTime + timeBeforeHealthRegen) return;

            _currentHealth += healthRegenRate * Time.deltaTime;
            _currentHealth = Mathf.Min(_currentHealth, maxHealth);
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
        
        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (damage <= 0 || _currentHealth <= 0) return;
            
            _currentHealth -= damage;
            _lastDamageTime = Time.time;
            
            OnDamaged?.Invoke(damage);
            
            if (_currentHealth <= 0)
            {
                Die(attacker);
            }
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
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

        private bool CanSwitchToRobot()
        {
            bool isClear = !Physics.Raycast(transform.position, Vector3.up, robotHeightCheck, switchBlockLayer);
            return isClear;
        }

        private void ResetRbRotation()
        {
            Vector3 rot = _rigidbody.rotation.eulerAngles;
            rot.y = _cameraTransform.rotation.eulerAngles.y;
            _rigidbody.rotation = Quaternion.Euler(rot);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private IEnumerator RunLateFixedUpdate()
        {
            while (true)
            {
                LateFixedUpdate();
                yield return new WaitForFixedUpdate();
            }
        }

        #endregion


        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        public void Teleport(PlayerSpawnPoint spawnPoint)
        {
            if (!spawnPoint) return;
            
            _rigidbody.position = spawnPoint.transform.position;
            _rigidbody.rotation = spawnPoint.transform.rotation;
        }

        public void Push(Vector3 direction, float force)
        {
            _rigidbody.AddForce(direction * force, ForceMode.Impulse);
        }

        private void OnDrawGizmosSelected()
        {
            if(_rigidbody != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position + _rigidbody.centerOfMass, .1f);
            }
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.up * robotHeightCheck);
        }
        
    }
}
