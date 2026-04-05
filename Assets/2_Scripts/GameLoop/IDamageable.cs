using System;
using UnityEngine;

public interface IDamageable
{
    public Transform transform { get; }
    public GameObject gameObject { get; }
    bool IsAlive { get; }
    
    public event Action<IDamageable> OnDeath;
    public event Action<float> OnDamaged;
    
    public void TakeDamage(float damage, IDamageable attacker = null);
}