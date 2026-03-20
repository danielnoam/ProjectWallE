using System;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEditor;
using UnityEngine;  

namespace ProjectWallE.GameLoop
{
    public enum AttackResponse
    {
        None,
        RetaliatePlayer,
        RetaliateTurret,
        RetaliateAll,
    }

    public enum EnemyState
    {
        MovingToTarget,
        Idle
    }

    [SelectionBase]
    public abstract class Enemy : MonoBehaviour, IDamageable, IPushable, IPoolable
    {
        [Header("Settings")] 
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float targetFindRange = 20f;
        [SerializeField] private AttackResponse attackResponse = AttackResponse.RetaliatePlayer;
        [SerializeReference, SerializableSelector] private TargetingStrategy[] targetingStrategies;

        [Header("Attack")] 
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackRange = 10f;
        [SerializeField] private Transform firePoint;
        [SerializeField, SOSelector("Assets/Data")] private ProjectileData projectileData;
        [SerializeField, SOSelector("Assets/Data")] private SOLayerMask hitLayers;

        private float _currentHealth;
        private float _attackTimer;

        protected EnemyState State = EnemyState.MovingToTarget;
        protected IDamageable CurrentTarget;
        protected const float RandomRange = 20f;
        protected const float RetargetThreshold = 25f;

        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;
        public event Action OnAttack;


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
            UpdateMovement();
            TryAttack();
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
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        private void TryAttack()
        {
            if (!IsInAttackRange())
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
            OnAttack?.Invoke();
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
            switch (attackResponse)
            {
                case AttackResponse.RetaliatePlayer:
                    return attacker is PlayerManager;
                case AttackResponse.RetaliateTurret:
                    return attacker is Turret;
                case AttackResponse.RetaliateAll:
                    return true;
                case AttackResponse.None:
                default:
                    return false;
            }
        }

        private bool IsInAttackRange()
        {
            return IsTargetValid(CurrentTarget) && Vector3.Distance(transform.position, CurrentTarget.transform.position) <= attackRange;
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

        protected abstract void UpdateMovement();
        protected abstract void SetDestination();
        protected abstract void OnPush(Vector3 direction, float force);

        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (_currentHealth <= 0) return;

            _currentHealth -= damage;
            OnDamaged?.Invoke(damage);

            if (attacker != null && attacker != CurrentTarget && ShouldRetaliate(attacker))
            {
                SetTarget(attacker);
            }

            if (_currentHealth <= 0) Die();
        }

        public void Push(Vector3 direction, float force)
        {
            OnPush(direction, force);
        }

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