using UnityEngine;

public class MortarTurret : Turret
{
    protected override Quaternion GetAimRotation(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - headTransform.position).normalized;
        direction = (direction + Vector3.up).normalized;
        return Quaternion.LookRotation(direction);
    }

    protected override bool CanFire(Vector3 targetPosition)
    {
        return true;
    }
}