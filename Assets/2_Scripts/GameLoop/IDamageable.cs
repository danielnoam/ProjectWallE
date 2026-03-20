using System;
using UnityEngine;

public interface IDamageable
{
    Transform transform { get; }
    GameObject gameObject { get; }
    event Action<IDamageable> OnDeath;
    event Action<float> OnDamaged;
    void TakeDamage(float damage, IDamageable attacker = null);
}