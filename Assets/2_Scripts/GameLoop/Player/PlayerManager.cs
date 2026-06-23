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

    [Flags]
    public enum PlayerFeature
    {
        None       = 0,
        Shoot      = 1 << 0,
        Build      = 1 << 1,
        AirSupport = 1 << 2,
        Movement = 1 << 3,
        All        = Shoot | Build | AirSupport  | Movement,
    }
    
    public enum ControllerType
    {
        Robot, Car
    }

    public class PlayerManager : MonoBehaviour, IDamageable, IPushable, IDeployableWithPod
    {
        public static PlayerManager Instance { get; private set; }

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
        [SerializeField] private Pod podPrefab;
        [SerializeField, AutoGetChildren] private PlayerManagerInput input;
        [SerializeField, AutoGetChildren] private Rigidbody rigidBody;
        [SerializeField, AutoGetChildren] private CarController carController;
        [SerializeField, AutoGetChildren] private RobotController robotController;
        [SerializeField, AutoGetChildren] private PlayerStructureBuilder structureBuilder;
        [SerializeField, AutoGetChildren] private PlayerSupportCaller supportCaller;
        [SerializeField, AutoGetChildren] private PlayerShooter shooter;
        [SerializeField, AutoGetChildren] private PlayerAimer aimer;


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
        private bool _inCinematic;
        private PlayerFeature _enabledFeatures = PlayerFeature.All;

        public ControllerType ControllerType => _controllerTypeEnum;
        public CarController CarController => carController;
        public RobotController RobotController => robotController;
        public PlayerStructureBuilder StructureBuilder => structureBuilder;
        public PlayerShooter Shooter => shooter;
        public PlayerAimer Aimer => aimer;
        public PlayerSupportCaller SupportCaller => supportCaller;
        public bool CanBuild => !_inCinematic && _currentController.BuildEnabled && IsAlive && _enabledFeatures.HasFlag(PlayerFeature.Build);
        public bool CanShoot => !_inCinematic && _currentController.ShootEnabled && IsAlive && _enabledFeatures.HasFlag(PlayerFeature.Shoot);
        public bool CanSupport => !_inCinematic && _currentController.SupportEnabled && IsAlive && _enabledFeatures.HasFlag(PlayerFeature.AirSupport);
        public bool CanMove => !_inCinematic && IsAlive && _enabledFeatures.HasFlag(PlayerFeature.Movement);
        public Vector3 Velocity => rigidBody.linearVelocity;
        public Quaternion Rotation => rigidBody.rotation;
        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Player;
        public PlayerFeature EnabledFeatures => _enabledFeatures;
        public Pod PodPrefab => podPrefab;

        public event Action<IDamageable> OnDeath;
        public event Action OnSpawn;
        public event Action OnRespawnPodCalled;
        public event Action<float> OnDamaged;
        public event Action<ControllerType> OnControllerChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action OnControllerSwitchFailed;
        public event Action<float, int> OnRespawnTick;
        public event Action<float, float> OnSwitchCooldownUpdated;
        public event Action<PlayerFeature> OnFeaturesChanged;

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
            
            if (Camera.main) _cameraTransform = Camera.main.transform;

            PlayerReferences playerReferences = new PlayerReferences()
            {
                cameraTransform = _cameraTransform,
                rigidBody = rigidBody,
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
            if (!IsAlive || _inCinematic) return;

            _currentController.ApplyUpdate();
            if (input.SwitchPressed) SwitchControllerEnum();
            UpdateSwitchCooldown();
            SwitchBehavior();
            RegenerateHealth();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || _inCinematic) return;
            _currentController?.ApplyFixedUpdate();
        }

        private void LateUpdate()
        {
            if (!IsAlive || _inCinematic) return;
            _currentController?.ApplyLateUpdate();
        }

        private void LateFixedUpdate()
        {
            if (!IsAlive || _inCinematic) return;
            _currentController?.ApplyLateFixedUpdate();
        }

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

        private void Die(IDamageable attacker = null)
        {
            ResetRbVelocity();
            rigidBody.isKinematic = true;
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
            rigidBody.isKinematic = false;
            _podInFlight = false;
            gfx?.SetActive(true);
            _currentHealth = maxHealth;
            _currentController.CanMove = CanMove;
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

        private void EnableController(ControllerType controllerType)
        {
            bool isRobot = controllerType == ControllerType.Robot;

            _currentController?.OnExit();
            _currentController?.gameObject.SetActive(false);
            _currentController = isRobot ? robotController : carController;
            _currentController.gameObject.SetActive(true);
            _currentController.OnEnter();
            _currentController.CanMove = CanMove;
            rigidBody.centerOfMass = _currentController.CenterOfMassOffset;

            OnControllerChanged?.Invoke(controllerType);
        }

        private bool CanSwitchToRobot()
        {
            return !Physics.Raycast(transform.position, Vector3.up, robotHeightCheck, switchBlockLayer);
        }

        private void ResetRbRotation()
        {
            Vector3 rot = rigidBody.rotation.eulerAngles;
            rot.y = _cameraTransform.rotation.eulerAngles.y;
            rigidBody.rotation = Quaternion.Euler(rot);
            rigidBody.angularVelocity = Vector3.zero;
        }

        private void ResetRbVelocity()
        {
            if (rigidBody.isKinematic) return;
            rigidBody.linearVelocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }

        private IEnumerator RunLateFixedUpdate()
        {
            while (true)
            {
                LateFixedUpdate();
                yield return new WaitForFixedUpdate();
            }
        }

        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (damage <= 0 || _currentHealth <= 0 || _inCinematic) return;

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
            rigidBody.position = position;
            rigidBody.rotation = rotation;
        }

        public void Teleport(PlayerSpawnPoint spawnPoint)
        {
            if (!spawnPoint) return;
            Teleport(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);
        }

        public void SetCinematicMode(bool cinematic)
        {
            if (_inCinematic == cinematic) return;
            _inCinematic = cinematic;

            if (cinematic)
            {
                ResetRbVelocity();
                rigidBody.isKinematic = true;
            }
            else
            {
                rigidBody.isKinematic = false;
            }

            _currentController.CanMove = CanMove;
            OnFeaturesChanged?.Invoke(_enabledFeatures);
        }

        public void Push(Vector3 direction, float force)
        {
            if (!IsAlive) return;
            rigidBody.AddForce(direction * force, ForceMode.Impulse);
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
                rigidBody.isKinematic = true;
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

        public void EnableFeatures(PlayerFeature features)
        {
            if (features == PlayerFeature.None) return;
            _enabledFeatures |= features;
            if (IsAlive) _currentController.CanMove = CanMove;
            OnFeaturesChanged?.Invoke(_enabledFeatures);
        }

        public void DisableFeatures(PlayerFeature features)
        {
            if (features == PlayerFeature.None) return;
            _enabledFeatures &= ~features;
            if (IsAlive) _currentController.CanMove = CanMove;
            OnFeaturesChanged?.Invoke(_enabledFeatures);
        }

        private void OnDrawGizmosSelected()
        {
            if (rigidBody)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position + rigidBody.centerOfMass, .1f);
            }
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector3.up * robotHeightCheck);
        }


    }
}