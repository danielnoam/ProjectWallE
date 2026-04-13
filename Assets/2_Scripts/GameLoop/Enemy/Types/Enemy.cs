using System;
using System.Collections.Generic;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.SerializableSelector;
using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum EnemyState
    {
        MovingToTarget,
        Idle
    }

    public enum EnemyHitZone
    {
        Normal,
        Critical
    }

    [SelectionBase]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Enemy : MonoBehaviour, IDamageable, IPushable, IPoolable, IDeployable
    {
        public static event Action<Enemy> OnEnemyKilled;
        
        
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool canBePushed = true;
        [SerializeField, SOSelector("Assets/6_Data")] private SOLayerMask hitLayers;

        [Header("Targeting")]
        [SerializeField] private float targetFindRange = 75f;
        [SerializeField] protected float targetRandomOffset = 20f;
        [SerializeField] protected float retargetDestinationThreshold = 25f;
        [SerializeReference, SerializableSelector(Foldout = false)] private TargetingStrategy[] targetingStrategies;

        [Header("Behavior")]
        [SerializeReference, SerializableSelector] private RetaliationStrategy retaliationStrategy = new NoRetaliation();
        [SerializeReference, SerializableSelector] private AimingStrategy aimingStrategy;
        [SerializeReference, SerializableSelector] private AttackStrategy attackStrategy;

        [Header("Effects")]
        [SerializeField] private DamageEffects normalDamageEffects;
        [SerializeField] private DamageEffects criticalDamageEffects;
        [SerializeField] private ParticleEffectAction deathEffect;
        [SerializeReference, SerializableSelector(Foldout = false)] private EnemyEffect[] effects;
        [SerializeField] private Renderer visibilityRenderer;
        
        
        [SerializeField, AutoGetSelf, HideInInspector] protected Rigidbody rigidBody;
        private readonly List<EnemyDamageRelay> _relays = new();
        private float _currentHealth;

        protected EnemyState State;
        protected IDamageable CurrentTarget;
        protected bool AimingControlsBodyRotation => aimingStrategy?.ControlsBodyRotation ?? false;
        protected bool RequiresDirectApproach => attackStrategy?.RequiresDirectApproach ?? false;
        protected virtual Vector3 Velocity => rigidBody.linearVelocity;


        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Enemy;

        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;

        private void OnValidate() => AutoGetSystem.Process(this);

        private void Awake()
        {
            attackStrategy?.SetUp();
        }

        private void OnDestroy()
        {
            EnemyManager.Instance?.UnregisterEnemy(this);
            if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            Vector3? targetPos = IsTargetValid(CurrentTarget) ? CurrentTarget.transform.position : null;
            bool isVisible = visibilityRenderer && visibilityRenderer.isVisible;

            var context = new EffectContext
            {
                DeltaTime = Time.deltaTime,
                Position = transform.position,
                Velocity = Velocity,
                TargetPosition = targetPos,
                IsVisible = isVisible
            };

            foreach (var effect in effects)
            {
                effect?.Tick(context);
            }

            if (targetPos.HasValue)
            {
                aimingStrategy?.Aim(transform, targetPos.Value, Time.deltaTime);

                bool isAligned = aimingStrategy?.IsAligned(transform, targetPos.Value) ?? true;
                float distance = Vector3.Distance(transform.position, targetPos.Value);

                attackStrategy?.Tick(isAligned, distance, new AttackContext
                {
                    Position = transform.position,
                    TargetPosition = targetPos.Value,
                    Owner = this,
                    HitLayers = hitLayers.Value
                }, Time.deltaTime);

                if (attackStrategy is { ShouldDestroySelf: true }) Die();
            }

            OnUpdate();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate();
        }
        

        private void OnTargetDeath(IDamageable deadTarget)
        {
            CurrentTarget.OnDeath -= OnTargetDeath;
            CheckForTarget();
        }

        protected bool IsTargetValid(IDamageable target)
        {
            if (target == null) return false;
            
            return target.gameObject.activeInHierarchy && target.IsAlive;
        }

        protected virtual void Initialize()
        {
            _currentHealth = maxHealth;
            State = EnemyState.Idle;
            attackStrategy?.Reset();
            CheckForTarget();
            EnemyManager.Instance?.RegisterEnemy(this);
        }

        protected virtual void OnFixedUpdate() { }
        protected virtual void OnUpdate() { }
        protected abstract void SetDestination();
        protected abstract void OnPush(Vector3 direction, float force);

        private void CheckForTarget()
        {
            if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;

            foreach (var strategy in targetingStrategies)
            {
                IDamageable target = strategy.GetTarget(transform.position, targetFindRange);
                if (IsTargetValid(target))
                {
                    SetTarget(target);
                    return;
                }
            }

            CurrentTarget = null;
        }

        private void Die()
        {
            deathEffect?.Play(transform.position);
            OnDeath?.Invoke(this);
            OnEnemyKilled?.Invoke(this);
            Destroy(gameObject);
        }

        private void SetTarget(IDamageable target)
        {
            if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;
            CurrentTarget = target;
            CurrentTarget.OnDeath += OnTargetDeath;
            SetDestination();
        }

        private bool ShouldRetaliate(IDamageable attacker)
        {
            return retaliationStrategy?.ShouldRetarget(CurrentTarget, attacker) ?? false;
        }

        private void ApplyDamage(float damage, IDamageable attacker)
        {
            _currentHealth -= damage;
            OnDamaged?.Invoke(damage);

            if (attacker != null && attacker != CurrentTarget && ShouldRetaliate(attacker))
            {
                SetTarget(attacker);
            }
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        public void TakeHitZoneDamage(float damage, IDamageable attacker, EnemyDamageRelay relay)
        {
            if (!IsAlive) return;

            bool isCritical = relay.HitZone == EnemyHitZone.Critical;
            var damageEffects = isCritical ? criticalDamageEffects : normalDamageEffects;
            float multiplier = isCritical ? 2f : 1f;

            damageEffects?.Play(transform.position, relay.Materials);
            ApplyDamage(damage * multiplier, attacker);
        }

        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (!IsAlive) return;

            foreach (var relay in _relays)
            {
                normalDamageEffects?.PunchOnly(relay.Materials);
            }
            normalDamageEffects?.PlayHitSound(transform.position);
            
            ApplyDamage(damage, attacker);
        }
        public void Push(Vector3 direction, float force)
        {
            if (!canBePushed) return;
            OnPush(direction, force);
        }

        public void RegisterRelay(EnemyDamageRelay relay) => _relays.Add(relay);

        public void OnPoolGet() => Initialize();

        public void OnPoolReturn()
        {
            EnemyManager.Instance?.UnregisterEnemy(this);
            if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;
            CurrentTarget = null;
            attackStrategy?.Reset();
        }
        
        public void Deploy(DeploymentRequest request)
        {

        }

        public void OnPoolRecycle() { }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            string targetName = (CurrentTarget is Component t && t) ? t.name : "None";
            Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"Health: {_currentHealth:N1}/{maxHealth}\nState: {State}\nTarget: {targetName}",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.red },
                    fontSize = 8,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter
                });
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, targetFindRange);

            foreach (var effect in effects)
                effect?.OnDrawGizmos(transform);
        }
#endif
        
    }
}