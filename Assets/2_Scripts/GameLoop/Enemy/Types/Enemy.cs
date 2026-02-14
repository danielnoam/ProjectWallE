using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.PrefabSelector;
using UnityEngine;

public enum TargetPriority
{
    Player,
    DamageGiver,
    NearestBase,
    NearestTurretInRange,
    NearestGeneratorInRange,
    WeakestStructureInRange,
    NearestStructure,
}

public enum EnemyState { Attacking, MovingToTarget, Idle }

[SelectionBase]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float targetFindRange = 20f;
    [SerializeField] protected TargetPriority[] targetPriorities = new[] { TargetPriority.NearestBase };
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected float attackRange = 15f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private LayerMask projectileHitLayers;
    [SerializeField, PrefabSelector("Assets/Prefabs")] private Projectile projectilePrefab;

    
    private IDamageable _lastAttacker;
    private float _currentHealth;
    
    
    protected EnemyState State = EnemyState.MovingToTarget;
    protected float AttackTimer;
    protected IDamageable CurrentTarget;
    

    public event Action<IDamageable> OnDeath;
    
    
    
    protected abstract void OnSetup();
    protected abstract void UpdateBehavior();
    protected abstract void MoveToTarget();
    
    

    private void Start()
    {
        _currentHealth = maxHealth;
        UpdateTarget();
        EnemyManager.Instance.RegisterEnemy(this);
        OnSetup();
    }
    
    private void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
    
        if (CurrentTarget != null)
        {
            CurrentTarget.OnDeath -= OnTargetDeath;
        }
    }

    private void Update()
    {
        UpdateBehavior();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out StructurePod pod))
        {
            Die();
        }
    }
    
    private void OnTargetChanged()
    {
        MoveToTarget();
    }
    
    private void OnTargetDeath(IDamageable deadTarget)
    {
        CurrentTarget.OnDeath -= OnTargetDeath;
        UpdateTarget();
    }
    

    private void UpdateTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.OnDeath -= OnTargetDeath;
        }
    
        foreach (var priority in targetPriorities)
        {
            IDamageable target = FindTargetByPriority(priority);
            if (target != null && IsTargetValid(target))
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
                return LevelManager.Instance?.Player as IDamageable;
                
            case TargetPriority.NearestTurretInRange:
                return StructureManager.Instance?.GetNearestTurretInRange(transform.position, targetFindRange);
                
            case TargetPriority.WeakestStructureInRange:
                return StructureManager.Instance?.GetWeakestStructureInRange(transform.position, targetFindRange);
            
            case TargetPriority.DamageGiver:
                return _lastAttacker;

            case TargetPriority.NearestGeneratorInRange:
                return StructureManager.Instance?.GetNearestGeneratorInRange(transform.position, targetFindRange);

            case TargetPriority.NearestStructure:

                return StructureManager.Instance?.GetNearestStructure(transform.position);
            default:
                return null;
        }
    }
    
    private bool IsTargetValid(IDamageable target)
    {
        return target is Component component && component;
    }


    private void Die()
    {
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
    
    protected void AttackTarget()
    {
        if (CurrentTarget is Component targetComponent && targetComponent)
        {
            Vector3 direction = (targetComponent.transform.position - transform.position).normalized;
            Projectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction));
            projectile.Initialize(this, projectileSpeed, attackDamage, direction, projectileHitLayers);
        }
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        if (attacker != null)
        {
            _lastAttacker = attacker;
            UpdateTarget();
        }
    }
    
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        string targetName = (CurrentTarget is Component target && target) ? target.name : "None";
        
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
#endif
}