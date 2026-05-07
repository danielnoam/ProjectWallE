using System;
using System.Collections;
using _2_Scripts;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop;
using ProjectWallE.GameLoop.Player;
using ProjectWallE.UI;
using UnityEngine;

namespace ProjectWallE
{
    public enum ControllerType
    {
        Robot, Car
    }
    
    public class PlayerManager : MonoBehaviour, IDamageable, IPushable, IDeployable
    {
        public static PlayerManager Instance  { get; private set; }
        
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
        [HideInInspector, SerializeField, AutoGetChildren] private CarController carController;
        [HideInInspector, SerializeField, AutoGetChildren] private RobotController robotController;
        [HideInInspector, SerializeField, AutoGetChildren] private PlayerStructureBuilder structureBuilder;
        [HideInInspector, SerializeField, AutoGetChildren] private PlayerSupportCaller supportCaller;
        [HideInInspector, SerializeField, AutoGetChildren] private PlayerShooter shooter;
        [HideInInspector, SerializeField, AutoGetChildren] private PlayerAimer aimer;

        private PlayerManagerInput _input;
        private Rigidbody _rigidbody;
        private Transform _cameraTransform;
        
        private ControllerType _controllerTypeEnum;
        private ControllerType _lastFrameControllerTypeEnum;
        
        private IPlayerController _currentController;
        
        private Coroutine _respawnCoroutine;
        private Coroutine _lateFixedUpdateCoroutine;
        
        private float _currentHealth;
        private float _switchTimer;
        private float _lastDamageTime;
        private int _currentSkipCost;
        private bool _podInFlight;

        public ControllerType ControllerType => _controllerTypeEnum;
        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public PlayerStructureBuilder StructureBuilder => structureBuilder;
        public PlayerShooter Shooter => shooter;
        public PlayerAimer Aimer => aimer;
        public PlayerSupportCaller SupportCaller => supportCaller;
        public bool CanBuild => _currentController.canBuild && IsAlive;
        public bool CanShoot => _currentController.canShoot && IsAlive;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Player;


        public event Action<IDamageable> OnDeath;
        public event Action OnSpawn;
        public event Action OnRespawnPodCalled;
        public event Action<float> OnDamaged;
        public event Action<ControllerType> OnControllerChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action OnControllerSwitchFailed;
        public event Action<float, int> OnRespawnTick;
        public event Action<float, float> OnSwitchCooldownUpdated;
        
        

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
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
            EnableController(ControllerType.Robot);
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
            if (!IsAlive) return;
            
            _currentController.ApplyUpdate();
            if (_input.SwitchPressed) SwitchControllerEnum();
            UpdateSwitchCooldown();
            SwitchBehavior();
            RegenerateHealth();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            _currentController?.ApplyFixedUpdate();
        }

        private void LateUpdate()
        {
            if (!IsAlive) return;
            _currentController?.ApplyLateUpdate();
        }

        private void LateFixedUpdate()
        {
            if (!IsAlive) return;
            _currentController?.ApplyLateFixedUpdate();
        }

        #region Controller

        private void SwitchControllerEnum()
        {
            if (!IsAlive) return;
            
            if (_controllerTypeEnum == ControllerType.Car && !CanSwitchToRobot())
            {
                OnControllerSwitchFailed?.Invoke();
                return;
            }
            
            if (Time.time < _switchTimer + switchCooldown) return;

            _controllerTypeEnum = _controllerTypeEnum == ControllerType.Robot 
                ? ControllerType.Car 
                : ControllerType.Robot;
        }
        
        private void UpdateSwitchCooldown()
        {
            float elapsed = Time.time - _switchTimer;
            float current = Mathf.Clamp(switchCooldown - elapsed, 0, switchCooldown);
            OnSwitchCooldownUpdated?.Invoke(current, switchCooldown);
        }

        private void SwitchBehavior()
        {
            if (_controllerTypeEnum == _lastFrameControllerTypeEnum) return;
            EnableController(_controllerTypeEnum);
            ResetRbRotation();
            _lastFrameControllerTypeEnum = _controllerTypeEnum;
            _switchTimer = Time.time;
        }

        #endregion

        #region LifeCycle
                
        private void Die(IDamageable attacker = null)
        {
            ResetRbVelocity();
            _rigidbody.isKinematic = true;
            _currentHealth = 0;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnDeath?.Invoke(attacker);
            gfx.SetActive(false);
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
            _rigidbody.isKinematic = false;
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

            Vector3 spawnPosition = LevelManager.Instance && LevelManager.Instance.ActivePlayerSpawnPoint
                ? LevelManager.Instance.ActivePlayerSpawnPoint.SpawnPosition 
                : transform.position;
            Quaternion spawnRotation = LevelManager.Instance && LevelManager.Instance.ActivePlayerSpawnPoint
                ? LevelManager.Instance.ActivePlayerSpawnPoint.SpawnRotation 
                : transform.rotation;

            var request = new DeploymentRequest(spawnPosition, spawnRotation * Vector3.forward, null)
            {
                InstantiateOnLand = false,
                Team = Team.Player,
                CameraMode = PodCameraMode.Follow
            };

            OnRespawnPodCalled?.Invoke();
            DeploymentManager.Instance.LaunchFromShip(this, request);
        }

        #endregion
        
        #region Helpers

        private void EnableController(ControllerType controllerType)
        {
            bool isRobot = controllerType == ControllerType.Robot;
            
            _currentController?.OnExit();
            _currentController?.gameObject.SetActive(false);
            _currentController = isRobot ? robotController : carController;
            _currentController.gameObject.SetActive(true);
            _currentController.OnEnter();

            _rigidbody.centerOfMass = _currentController.CenterOfMassOffset;
            
            OnControllerChanged?.Invoke(controllerType);
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

        private void ResetRbVelocity()
        {
            if (_rigidbody.isKinematic) return;
            _rigidbody.linearVelocity = Vector3.zero;
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
            ResetRbVelocity();
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
            if (!IsAlive) return;
            _rigidbody.AddForce(direction * force, ForceMode.Impulse);
        }
        
        public void Deploy(DeploymentRequest deploymentRequest, BehaviorRequest behaviorRequest)
        {
            Respawn(deploymentRequest.TargetPosition, Quaternion.LookRotation(deploymentRequest.TargetForward));
        }
        
        public void CallPodTo(PlayerSpawnPoint spawnPoint)
        {
            if (!DeploymentManager.Instance || !spawnPoint) return;

            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }
            
            if (IsAlive)
            {
                ResetRbVelocity();
                _currentHealth = 0;
                _rigidbody.isKinematic = true;
                gfx.SetActive(false);
            }

            _podInFlight = true;

            var request = new DeploymentRequest(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation * Vector3.forward, null)
            {
                InstantiateOnLand = false,
                Team = Team.Player,
                CameraMode = PodCameraMode.Follow
            };

            DeploymentManager.Instance.LaunchFromShip(this, request);
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