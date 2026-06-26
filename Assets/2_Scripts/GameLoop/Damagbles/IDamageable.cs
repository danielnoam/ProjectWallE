using System;
using UnityEngine;

public interface IDamageable
{
    Team Team { get; }
    public Transform transform { get; }
    public GameObject gameObject { get; }
    bool IsAlive { get; }
    
    public event Action<IDamageable> OnDeath;
    public event Action<DamageInfo> OnDamaged;
    
    public void TakeDamage(float damage, IDamageable attacker = null);
}