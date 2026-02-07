using System;
using DNExtensions.Utilities;
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

[SelectionBase]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("General Enemy Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float targetRange = 20f;
    [SerializeField] protected AudioClip damagedSfx;
    [SerializeField] protected AudioClip deathSfx;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected TargetPriority[] targetPriorities = new[] { TargetPriority.NearestBase };
    [Separator]
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected float attackRange = 15f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private LayerMask projectileHitLayers;
    [SerializeField] private Projectile projectilePrefab;
    [Separator]
    [SerializeField, ReadOnly] protected float currentHealth;

    protected float AttackTimer;
    protected IDamageable CurrentTarget;
    protected IDamageable LastAttacker;
    
    public event Action<IDamageable> OnDeath;

    protected abstract void OnSetup();
    protected abstract void UpdateBehavior();
    protected abstract void MoveToTarget();
    

    private void Start()
    {
        currentHealth = maxHealth;
        OnSetup();
        EnemyManager.Instance.RegisterEnemy(this);
        UpdateTarget();
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
    
    private bool IsTargetValid(IDamageable target)
    {
        return target is Component component && component;
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