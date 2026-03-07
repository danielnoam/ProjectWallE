using System;
using UnityEngine;

public interface IDamageable
{
    event Action<IDamageable> OnDeath;
    void TakeDamage(float damage, IDamageable attacker = null);
}