

using System;
using UnityEngine;

public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("General Enemy Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected AudioClip damagedSfx;
    [SerializeField] protected AudioClip deathSfx;
    [SerializeField] protected AudioSource audioSource;

    protected float CurrentHealth;
    protected float AttackTimer;
    protected IDamageable MainTarget;
    protected IDamageable SecondaryTarget;
    protected IDamageable CurrentTarget => SecondaryTarget ?? MainTarget;
    
    public event Action<IDamageable> OnDeath;

    
    
    protected abstract void UpdateBehavior();
    protected abstract void PerformAttack();
    protected abstract void MoveToTarget();
    
    
    
    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
        EnemyManager.Instance.RegisterEnemy(this);
    }

    protected virtual void Update()
    {
        UpdateBehavior();
    }

    

    public void SetMainTarget(IDamageable target)
    {
        MainTarget = target;
        OnMainTargetSet();
    }
    
    protected virtual void OnMainTargetSet()
    {
        MoveToTarget();
    }

    protected void UpdateSecondaryTarget(IDamageable newTarget)
    {
        if (newTarget == null) return;
        
        if (SecondaryTarget != null)
        {
            SecondaryTarget.OnDeath -= HandleAttackerDeath;
        }
        
        SecondaryTarget = newTarget;
        SecondaryTarget.OnDeath += HandleAttackerDeath;
        MoveToTarget();
    }

    private void HandleAttackerDeath(IDamageable deadAttacker)
    {
        if (deadAttacker == SecondaryTarget)
        {
            deadAttacker.OnDeath -= HandleAttackerDeath;
            SecondaryTarget = null;
            MoveToTarget();
        }
    }

    public virtual void TakeDamage(float damage, IDamageable attacker = null)
    {
        CurrentHealth -= damage;
        UpdateSecondaryTarget(attacker);
        
        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }
        
        if (damagedSfx)
        {
            audioSource?.PlayOneShot(damagedSfx);
        }
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke(this);
        
        if (deathSfx)
        {
            audioSource?.PlayOneShot(deathSfx);
        }
        
        Destroy(gameObject, deathSfx ? deathSfx.length : 0f);
    }

    public Transform Transform() => transform;

    protected virtual void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        
        if (SecondaryTarget != null)
        {
            SecondaryTarget.OnDeath -= HandleAttackerDeath;
        }
    }
}