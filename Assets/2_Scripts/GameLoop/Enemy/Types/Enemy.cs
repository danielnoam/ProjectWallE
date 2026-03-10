using System;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using UnityEngine;

public enum TargetPriority
{
    Player, PlayerInRange, NearestBase, NearestTurretInRange,
    NearestGeneratorInRange, WeakestStructureInRange, NearestStructure,
}

public enum EnemyState { MovingToTarget, Idle }

[SelectionBase]
public abstract class Enemy : MonoBehaviour, IDamageable, IPushable
{
    [Header("Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float targetFindRange = 20f;
    [SerializeField] protected TargetPriority[] targetPriorities = new[] { TargetPriority.NearestBase };

    [Header("Attack")]
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField, SOSelector("Assets/Data")] protected ProjectileData projectileData;
    [SerializeField, SOSelector("Assets/Data")] protected SOLayerMask hitLayers;

    private float _currentHealth;
    private float _attackTimer;

    protected EnemyState State = EnemyState.MovingToTarget;
    protected IDamageable CurrentTarget;

    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;

    protected abstract void Initialize();
    protected abstract void UpdateMovement();
    protected abstract void SetDestination();
    protected abstract void OnPush(Vector3 direction, float force);

    
    private void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        if (CurrentTarget != null) CurrentTarget.OnDeath -= OnTargetDeath;
    }
    
    private void Start()
    {
        _currentHealth = maxHealth;
        UpdateTarget();
        EnemyManager.Instance.RegisterEnemy(this);
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
    
    private void OnTargetChanged()
    {
        SetDestination();
    }

    private void OnTargetDeath(IDamageable deadTarget)
    {
        CurrentTarget.OnDeath -= OnTargetDeath;
        UpdateTarget();
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
            AttackTarget();
            _attackTimer = 0f;
        }
    }
    

    private void UpdateTarget()
    {
        if (CurrentTarget != null)
            CurrentTarget.OnDeath -= OnTargetDeath;

        foreach (var priority in targetPriorities)
        {
            IDamageable target = FindTargetByPriority(priority);
            if (IsTargetValid(target, out _))
            {
                CurrentTarget = target;
                CurrentTarget.OnDeath += OnTargetDeath;
                OnTargetChanged();
                return;
            }
        }

        CurrentTarget = null;
    }

    private IDamageable FindTargetByPriority(TargetPriority priority)
    {
        switch (priority)
        {
            case TargetPriority.NearestBase:
                return StructureManager.Instance?.GetNearestBase(transform.position);
            case TargetPriority.Player:
                return LevelManager.Instance?.Player;
            case TargetPriority.NearestTurretInRange:
                return StructureManager.Instance?.GetNearestTurretInRange(transform.position, targetFindRange);
            case TargetPriority.WeakestStructureInRange:
                return StructureManager.Instance?.GetWeakestStructureInRange(transform.position, targetFindRange);
            case TargetPriority.PlayerInRange:
                var player = LevelManager.Instance?.Player;
                if (!player) return null;
                return Vector3.Distance(transform.position, player.transform.position) <= targetFindRange ? player as IDamageable : null;
            case TargetPriority.NearestGeneratorInRange:
                return StructureManager.Instance?.GetNearestGeneratorInRange(transform.position, targetFindRange);
            case TargetPriority.NearestStructure:
                return StructureManager.Instance?.GetNearestStructure(transform.position);
            default:
                return null;
        }
    }
    

    private void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }

    private void AttackTarget()
    {
        if (IsTargetValid(CurrentTarget, out var targetComponent))
        {
            var direction = (targetComponent.transform.position - transform.position).normalized;
            projectileData?.Spawn(hitLayers.Value, transform.position, direction, targetComponent.transform.position);
        }
    }
    
    private bool IsInAttackRange() 
    {
        return IsTargetValid(CurrentTarget, out var component) && Vector3.Distance(transform.position, component.transform.position) <= targetFindRange;
    }

    
    protected bool IsTargetValid(IDamageable target, out Component targetComponent)
    {
        targetComponent = target as Component;
        return targetComponent && targetComponent.gameObject.activeInHierarchy;
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        if (_currentHealth <= 0) return;
        
        _currentHealth -= damage;
        OnDamaged?.Invoke(damage);
        
        if (_currentHealth <= 0) Die();
    }

    public void Push(Vector3 direction, float force)
    {
        OnPush(direction, force);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        string targetName = (CurrentTarget is Component t && t) ? t.name : "None";
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {_currentHealth}/{maxHealth}\nState: {State}\nTarget: {targetName}",
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