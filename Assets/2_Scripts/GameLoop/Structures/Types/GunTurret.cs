using UnityEngine;

public class GunTurret : Turret
{
    [SerializeField] private float angleThreshold = 10f;

    protected override Quaternion GetAimRotation(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - headTransform.position).normalized;
        return Quaternion.LookRotation(direction);
    }

    protected override bool CanFire(Vector3 targetPosition)
    {
        Quaternion targetRotation = GetAimRotation(targetPosition);
        return Quaternion.Angle(headTransform.rotation, targetRotation) <= angleThreshold;
    }
}