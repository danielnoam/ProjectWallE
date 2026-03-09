using System;
using UnityEngine;

public interface IDamageable
{
    event Action<IDamageable> OnDeath;
    event Action<float> OnDamaged;
    void TakeDamage(float damage, IDamageable attacker = null);
}