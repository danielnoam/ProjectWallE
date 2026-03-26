using UnityEngine;

public class MortarTurret : Turret
{
    protected override Quaternion GetAimRotation(Vector3 targetPosition)
    {
        Vector3 launchVelocity = ProjectileUtility.GetArcLaunchVelocity(headTransform.position, targetPosition, CurrentTurretLevelData.soProjectileData.speed);
        return Quaternion.LookRotation(launchVelocity.normalized);
    }

    protected override bool CanFire(Vector3 targetPosition)
    {
        Vector3 launchVelocity = ProjectileUtility.GetArcLaunchVelocity(headTransform.position, targetPosition, CurrentTurretLevelData.soProjectileData.speed);
        return Quaternion.Angle(headTransform.rotation, Quaternion.LookRotation(launchVelocity.normalized)) < 5f;
    }
}