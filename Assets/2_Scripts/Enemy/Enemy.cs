using System;
using DNExtensions;
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

public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("General Enemy Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected float attackRange = 15f;
    [SerializeField] protected float targetRange = 20f;
    [SerializeField] protected AudioClip damagedSfx;
    [SerializeField] protected AudioClip deathSfx;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected TargetPriority[] targetPriorities = new[] { TargetPriority.NearestBase };
    [SerializeField, ReadOnly] protected float currentHealth;

    protected float AttackTimer;
    protected IDamageable CurrentTarget;
    protected IDamageable LastAttacker;
    
    public event Action<IDamageable> OnDeath;
    
    protected abstract void UpdateBehavior();
    protected abstract void PerformAttack();
    protected abstract void MoveToTarget();
    
    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
        UpdateTarget();
    }
    
    private void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
    
        if (CurrentTarget != null)
        {
            CurrentTarget.OnDeath -= HandleTargetDeath;
        }
    }

    private void Update()
    {
        if (CurrentTarget != null && !IsTargetValid(CurrentTarget))
        {
            UpdateTarget();
        }
        
        UpdateBehavior();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out StructurePod pod))
        {
            Die();
        }
    }
    
    private bool IsTargetValid(IDamageable target)
    {
        return target is Component component && component;
    }

    private void UpdateTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.OnDeath -= HandleTargetDeath;
        }
    
        foreach (var priority in targetPriorities)
        {
            IDamageable target = FindTargetByPriority(priority);
            if (target != null && IsTargetValid(target))
            {
                CurrentTarget = target;
                CurrentTarget.OnDeath += HandleTargetDeath;
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
                return StructureManager.Instance?.GetNearestTurretInRange(transform.position, targetRange);
                
            case TargetPriority.WeakestStructureInRange:
                return StructureManager.Instance?.GetWeakestStructureInRange(transform.position, targetRange);
            
            case TargetPriority.DamageGiver:
                return LastAttacker;

            case TargetPriority.NearestGeneratorInRange:
                return StructureManager.Instance?.GetNearestGeneratorInRange(transform.position, targetRange);

            case TargetPriority.NearestStructure:

                return StructureManager.Instance?.GetNearestStructure(transform.position);
            default:
                return null;
        }
    }

    protected virtual void OnTargetChanged()
    {
        MoveToTarget();
    }
    
    private void HandleTargetDeath(IDamageable deadTarget)
    {
        if (deadTarget != CurrentTarget) return;
        CurrentTarget.OnDeath -= HandleTargetDeath;
        UpdateTarget();
    }

    private void Die()
    {
        OnDeath?.Invoke(this);
        
        if (deathSfx)
        {
            audioSource?.PlayOneShot(deathSfx);
        }
        
        Destroy(gameObject);
    }
    
    public void TakeDamage(float damage, IDamageable attacker = null)
    {
        currentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (attacker != null)
        {
            LastAttacker = attacker;
            UpdateTarget();
        }
        
        if (damagedSfx)
        {
            audioSource?.PlayOneShot(damagedSfx);
        }
    }

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}