using System;
using System.Collections.Generic;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
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
    public abstract class Enemy : MonoBehaviour, IDamageable, IPushable, IPoolable
    {
        [Header("Settings")] 
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float targetFindRange = 20f;
        [SerializeField] private AttackerResponse attackerResponse = AttackerResponse.RetaliateIfPlayer;
        [SerializeReference, SerializableSelector] private TargetingStrategy[] targetingStrategies;
        [SerializeField] private EnemyEffects effects;
        
        [Header("Head")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private float headRotationSpeed = 180f;
        [SerializeField] private float fireAngleThreshold = 15f;
        [SerializeField] private bool yawOnly = true;

        [Header("Attack")] 
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private Transform firePoint;
        [SerializeField, SOSelector("Assets/Data")] private ProjectileData projectileData;
        [SerializeField, SOSelector("Assets/Data")] private SOLayerMask hitLayers;

        
        private readonly List<EnemyDamageRelay> _relays = new();
        private float _currentHealth;
        private float _attackTimer;

        protected EnemyState State;
        protected IDamageable CurrentTarget;
        protected const float RandomRange = 20f;
        protected const float RetargetThreshold = 25f;

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
            RotateHead();
            TryAttack();
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
            _attackTimer = 0f;
            State = EnemyState.Idle;
            CheckForTarget();
            EnemyManager.Instance?.RegisterEnemy(this);
        }
        
        protected virtual void OnFixedUpdate()
        {
            
        }

        protected virtual void OnUpdate()
        {
            
        }
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
        
        private void RotateHead()
        {
            if (!headTransform || !IsTargetValid(CurrentTarget)) return;

            Vector3 direction = CurrentTarget.transform.position - headTransform.position;
    
            if (yawOnly) direction.y = 0;
    
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            headTransform.rotation = Quaternion.RotateTowards(
                headTransform.rotation, targetRotation, headRotationSpeed * Time.deltaTime);
        }


        private void Die()
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        private void TryAttack()
        {
            if (!IsInAttackRange() || !IsHeadAligned())
            {
                _attackTimer = 0f;
                return;
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCooldown)
            {
                Attack();
                _attackTimer = 0f;
            }
        }

        private void Attack()
        {
            if (!IsTargetValid(CurrentTarget)) return;

            var direction = (CurrentTarget.transform.position - transform.position).normalized;
            projectileData?.Spawn(hitLayers.Value, firePoint.position, direction, CurrentTarget.transform.position);
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
        
        private bool IsHeadAligned()
        {
            if (!headTransform || !IsTargetValid(CurrentTarget)) return true;

            Vector3 direction = CurrentTarget.transform.position - headTransform.position;
            if (yawOnly) direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return false;

            return Vector3.Angle(headTransform.forward, direction) <= fireAngleThreshold;
        }

        private bool IsInAttackRange()
        {
            return IsTargetValid(CurrentTarget) && Vector3.Distance(transform.position, CurrentTarget.transform.position) <= attackRange;
        }
        
        private void ApplyDamage(float damage, IDamageable attacker)
        {
            _currentHealth -= damage;
            OnDamaged?.Invoke(damage);

            if (attacker != null && attacker != CurrentTarget && ShouldRetaliate(attacker))
                SetTarget(attacker);

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

            effects.PlayHit(transform.position, relay);
            ApplyDamage(damage * multiplier, attacker);
        }

        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (!IsAlive) return;
            
            effects.PlayHitAll(transform.position, _relays);
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
        }

        public void OnPoolRecycle()
        {

        }

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
        }
#endif
    }
}