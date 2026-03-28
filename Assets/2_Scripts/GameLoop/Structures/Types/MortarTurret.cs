using UnityEngine;

public class MortarTurret : Turret
{
    protected override void RotateToTarget(Vector3 targetPosition, float rotSpeed)
    {
        Vector3 flatDir = targetPosition - horizontalSwivel.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            horizontalSwivel.rotation = Quaternion.RotateTowards(
                horizontalSwivel.rotation,
                Quaternion.LookRotation(flatDir),
                rotSpeed
            );
        }
        
        Vector3 launchVelocity = ProjectileUtility.GetArcLaunchVelocity(
            firePoint.position, targetPosition, CurrentTurretLevelData.soProjectileData.speed
        );
        Vector3 localLaunch = horizontalSwivel.InverseTransformDirection(launchVelocity);
        float pitch = Mathf.Atan2(localLaunch.y, localLaunch.z) * Mathf.Rad2Deg;

        verticalSwivel.localRotation = Quaternion.RotateTowards(
            verticalSwivel.localRotation,
            Quaternion.Euler(-pitch, 0f, 0f),
            rotSpeed
        );
    }

    protected override bool CanFire(Vector3 targetPosition)
    {
        Vector3 launchVelocity = ProjectileUtility.GetArcLaunchVelocity(
            firePoint.position, targetPosition, CurrentTurretLevelData.soProjectileData.speed
        );
        return Vector3.Angle(firePoint.forward, launchVelocity.normalized) <= angleThreshold;
    }
}