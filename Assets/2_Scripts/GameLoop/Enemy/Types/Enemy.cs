using System;
using System.Collections.Generic;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.SerializableSelector;
using UnityEditor;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum AttackerResponse
    {
        None,
        RetaliateIfPlayer,
        RetaliateIfTurret,
        RetaliateAll,
    }

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
    public abstract class Enemy : MonoBehaviour, IDamageable, IPushable, IPoolable
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float targetFindRange = 20f;
        [SerializeField] private AttackerResponse attackerResponse = AttackerResponse.RetaliateIfPlayer;
        [SerializeReference, SerializableSelector] private TargetingStrategy[] targetingStrategies;
        [SerializeReference, SerializableSelector] private EnemyEffect[] effects;

        [Header("Behavior")]
        [SerializeReference, SerializableSelector] private AimingStrategy aimingStrategy;
        [SerializeReference, SerializableSelector] private AttackStrategy attackStrategy;

        [Header("References")]
        [SerializeField] private Renderer visibilityRenderer;
        [SerializeField, AutoGetSelf, HideInInspector] private protected Rigidbody rigidBody;

        private readonly List<EnemyDamageRelay> _relays = new();
        private float _currentHealth;

        protected EnemyState State;
        protected IDamageable CurrentTarget;
        protected const float RandomRange = 20f;
        protected const float RetargetThreshold = 25f;
        protected bool AimingControlsBodyRotation => aimingStrategy?.ControlsBodyRotation ?? false;
        protected bool RequiresDirectApproach => attackStrategy?.RequiresDirectApproach ?? false;
        protected virtual Vector3 Velocity => rigidBody.linearVelocity;

        public bool IsAlive => _currentHealth > 0;

        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;

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
                    Owner = this
                }, Time.deltaTime);

                if (attackStrategy is { ShouldDestroySelf: true }) Die();
            }

            OnUpdate();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Pod _))
            {
                Die();
            }
        }

        private void OnTargetDeath(IDamageable deadTarget)
        {
            CurrentTarget.OnDeath -= OnTargetDeath;
            CheckForTarget();
        }

        protected bool IsTargetValid(IDamageable target)
        {
            return target is Component component && component && component.gameObject.activeInHierarchy;
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
                IDamageable target = strategy.FindTarget(transform.position, targetFindRange);
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
            foreach (var effect in effects)
            {
                effect?.OnDeath(transform.position);
            }

            OnDeath?.Invoke(this);
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
            switch (attackerResponse)
            {
                case AttackerResponse.RetaliateIfPlayer:
                    return attacker is PlayerManager;
                case AttackerResponse.RetaliateIfTurret:
                    return attacker is Turret;
                case AttackerResponse.RetaliateAll:
                    return true;
                case AttackerResponse.None:
                default:
                    return false;
            }
        }

        private void ApplyDamage(float damage, IDamageable attacker)
        {
            _currentHealth -= damage;
            OnDamaged?.Invoke(damage);

            if (attacker != null && attacker != CurrentTarget && ShouldRetaliate(attacker)) SetTarget(attacker);

            if (_currentHealth <= 0) Die();
        }

        public void TakeHitZoneDamage(float damage, IDamageable attacker, EnemyDamageRelay relay)
        {
            if (!IsAlive) return;

            float multiplier = relay.HitZone switch
            {
                EnemyHitZone.Critical => 2f,
                _ => 1f
            };

            foreach (var effect in effects)
            {
                effect?.OnHit(transform.position, relay);
            }
            ApplyDamage(damage * multiplier, attacker);
        }

        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (!IsAlive) return;

            foreach (var effect in effects)
            {
                effect?.OnHitAll(transform.position, _relays);
            }
            ApplyDamage(damage, attacker);
        }

        public void Push(Vector3 direction, float force)
        {
            OnPush(direction, force);
        }

        public void RegisterRelay(EnemyDamageRelay relay) => _relays.Add(relay);

        public void OnPoolGet()
        {
            Initialize();
        }

        public void OnPoolReturn()
        {
            EnemyManager.Instance?.UnregisterEnemy(this);
            if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;
            CurrentTarget = null;
            attackStrategy?.Reset();
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
            {
                effect?.OnDrawGizmos(transform);
            }
        }
#endif
    }
}