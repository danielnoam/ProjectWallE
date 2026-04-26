using System;
using System.Collections;
using _2_Scripts;
using ProjectWallE.GameLoop;
using ProjectWallE.GameLoop.Player;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE
{
    public enum PlayerControllerType
    {
        Robot, Car
    }
    
    public class PlayerManager : MonoBehaviour, IDamageable, IPushable, IDeployable
    {
        [Header("Settings")]
        [SerializeField, Min(10f)] private float maxHealth = 100f;
        [SerializeField] private float timeBeforeHealthRegen = 1.5f;
        [SerializeField] private float healthRegenRate = 5f;
        [SerializeField] private float respawnTime = 5f;
        [SerializeField] private int skipRespawnBaseCost = 250;
        [SerializeField] private float switchCooldown = 0.5f;
        
        [Header("Collision")]
        [SerializeField] private float robotHeightCheck = 2;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask switchBlockLayer;
        
        [Header("References")]
        [SerializeField] private GameObject gfx;
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
        
        private Coroutine _respawnCoroutine;
        private Coroutine _lateFixedUpdateCoroutine;
        
        private float _currentHealth;
        private float _switchTimer;
        private float _lastDamageTime;
        private int _currentSkipCost;
        private bool _podInFlight;

        public PlayerControllerType PlayerControllerType => _playerControllerTypeEnum;
        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public PlayerStructureBuilder StructureBuilder => structureBuilder;
        public PlayerShooter Shooter => shooter;
        public PlayerAimer Aimer => aimer;
        public bool CanBuild => _currentController.canBuild && IsAlive;
        public bool CanShoot => _currentController.canShoot && IsAlive;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Player;

        public event Action<IDamageable> OnDeath;
        public event Action OnSpawn;
        public event Action OnRespawnPodCalled;
        public event Action<float> OnDamaged;
        public event Action<PlayerControllerType> OnControllerChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action OnControllerSwitchFailed;
        public event Action<float, int> OnRespawnTick;
        public event Action<float, float> OnSwitchCooldownUpdated;
        
        

        private void OnValidate()
        {
            if (!carController) carController = GetComponentInChildren<CarController>();
            if (!robotController) robotController = GetComponentInChildren<RobotController>();
            if (!structureBuilder) structureBuilder = GetComponentInChildren<PlayerStructureBuilder>();
            if (!shooter) shooter = GetComponentInChildren<PlayerShooter>();
            if (!aimer) aimer = GetComponentInChildren<PlayerAimer>();
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
            OnSpawn?.Invoke();
        }

        private void OnEnable()
        {
            _lateFixedUpdateCoroutine = StartCoroutine(RunLateFixedUpdate());
            DeathScreenManager.SkipButtonClicked += OnSkipRespawnRequested;
        }

        private void OnDisable()
        {
            StopCoroutine(_lateFixedUpdateCoroutine);
            DeathScreenManager.SkipButtonClicked -= OnSkipRespawnRequested;
        }

        private void Update()
        {
            _currentController.ApplyUpdate();
            
            if (_input.SwitchPressed) SwitchControllerEnum();
            
            UpdateSwitchCooldown();
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

        #region Controller

        private void SwitchControllerEnum()
        {
            if (!IsAlive) return;
            
            if (_playerControllerTypeEnum == PlayerControllerType.Car && !CanSwitchToRobot())
            {
                OnControllerSwitchFailed?.Invoke();
                return;
            }
            
            if (Time.time < _switchTimer + switchCooldown) return;

            _playerControllerTypeEnum = _playerControllerTypeEnum == PlayerControllerType.Robot 
                ? PlayerControllerType.Car 
                : PlayerControllerType.Robot;
        }
        
        private void UpdateSwitchCooldown()
        {
            float elapsed = Time.time - _switchTimer;
            float current = Mathf.Clamp(switchCooldown - elapsed, 0, switchCooldown);
            OnSwitchCooldownUpdated?.Invoke(current, switchCooldown);
        }

        private void SwitchBehavior()
        {
            if (_playerControllerTypeEnum == _lastFramePlayerControllerTypeEnum) return;
            EnableController(_playerControllerTypeEnum);
            ResetRbRotation();
            _lastFramePlayerControllerTypeEnum = _playerControllerTypeEnum;
            _switchTimer = Time.time;
        }

        #endregion

        #region LifeCycle
                
        private void Die(IDamageable attacker = null)
        {
            _currentHealth = 0;
            gfx.SetActive(false);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnDeath?.Invoke(attacker);
            _respawnCoroutine = StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _currentSkipCost = skipRespawnBaseCost;

            float timeRemaining = respawnTime;
            while (timeRemaining > 0f)
            {
                float progress = timeRemaining / respawnTime;
                _currentSkipCost = Mathf.CeilToInt(skipRespawnBaseCost * progress);
                OnRespawnTick?.Invoke(timeRemaining, _currentSkipCost);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            if (DeploymentManager.Instance) CallRespawnPod();
            else if (LevelManager.Instance) Respawn(LevelManager.Instance.ActivePlayerSpawnPoint);
            else Respawn(transform.position, transform.rotation);
        }
        
        private void OnSkipRespawnRequested()
        {
            if (IsAlive || _podInFlight) return;
    
            if (ResourceManager.Instance && ResourceManager.Instance.TrySpendResources(_currentSkipCost))
            {
                if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
                
                if (DeploymentManager.Instance) CallRespawnPod();
                else if (LevelManager.Instance) Respawn(LevelManager.Instance.ActivePlayerSpawnPoint);
                else Respawn(transform.position, transform.rotation);
            }
        }

        private void RegenerateHealth()
        {
            if (_currentHealth >= maxHealth || Time.time < _lastDamageTime + timeBeforeHealthRegen || !IsAlive) return;

            _currentHealth += healthRegenRate * Time.deltaTime;
            _currentHealth = Mathf.Min(_currentHealth, maxHealth);
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
        
        private void Respawn(PlayerSpawnPoint spawnPoint)
        {
            Teleport(spawnPoint);
            FinishRespawn();
        }

        private void Respawn(Vector3 position, Quaternion rotation)
        {
            Teleport(position, rotation);
            FinishRespawn();
        }

        private void FinishRespawn()
        {
            _podInFlight = false;
            gfx?.SetActive(true);
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnSpawn?.Invoke();
            _respawnCoroutine = null;
        }
        
        private void CallRespawnPod()
        {
            _podInFlight = true;

            Vector3 spawnPosition = LevelManager.Instance 
                ? LevelManager.Instance.ActivePlayerSpawnPoint.SpawnPosition 
                : transform.position;
            Quaternion spawnRotation = LevelManager.Instance 
                ? LevelManager.Instance.ActivePlayerSpawnPoint.SpawnRotation 
                : transform.rotation;

            var request = new DeploymentRequest(spawnPosition, spawnRotation * Vector3.forward, null)
            {
                InstantiateOnLand = false,
                DamageOnImpact = false,
                CameraMode = PodCameraMode.Follow
            };

            OnRespawnPodCalled?.Invoke();
            DeploymentManager.Instance.DeployFromShip(this, request);
        }

        #endregion
        
        #region Helpers

        private void EnableController(PlayerControllerType playerControllerType)
        {
            bool isRobot = playerControllerType == PlayerControllerType.Robot;
            
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
            return !Physics.Raycast(transform.position, Vector3.up, robotHeightCheck, switchBlockLayer);
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

        #region Public API
        
        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (damage <= 0 || _currentHealth <= 0) return;
            
            _currentHealth -= damage;
            _lastDamageTime = Time.time;
            
            OnDamaged?.Invoke(damage);
            
            if (_currentHealth <= 0) Die(attacker);
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Heal(float healAmount)
        {
            if (healAmount <= 0 || _currentHealth <= 0 || _currentHealth >= maxHealth) return;
            
            _currentHealth += healAmount;
            if (_currentHealth > maxHealth) _currentHealth = maxHealth;

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
        
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        public void Teleport(PlayerSpawnPoint spawnPoint)
        {
            if (!spawnPoint) return;
            Teleport(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);
        }

        public void Push(Vector3 direction, float force)
        {
            _rigidbody.AddForce(direction * force, ForceMode.Impulse);
        }
        
        public void Deploy(DeploymentRequest request)
        {
            Respawn(request.TargetPosition, Quaternion.LookRotation(request.TargetForward));
        }

        #endregion
        

        private void OnDrawGizmosSelected()
        {
            if (_rigidbody)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position + _rigidbody.centerOfMass, .1f);
            }
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.up * robotHeightCheck);
        }
    }
}