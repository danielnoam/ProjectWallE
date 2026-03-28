using UnityEngine;

public class GunTurret : Turret
{
    protected override bool CanFire(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - firePoint.position).normalized;
        return Vector3.Angle(firePoint.forward, direction) <= angleThreshold;
    }
}