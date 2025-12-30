


using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, IDamageable attacker = null);
    Transform Transform();
}