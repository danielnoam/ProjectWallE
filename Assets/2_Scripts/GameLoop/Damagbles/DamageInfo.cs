using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly Vector3 Position;
    public readonly bool IsCritical;
    public readonly IDamageable Attacker;

    public DamageInfo(float amount, Vector3 position, bool isCritical = false, IDamageable attacker = null)
    {
        Amount = amount;
        Position = position;
        IsCritical = isCritical;
        Attacker = attacker;
    }
}
