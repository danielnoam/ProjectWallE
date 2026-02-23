using UnityEngine;

public static class ProjectileUtility
{
    public static Vector3 GetArcLaunchVelocity(Vector3 from, Vector3 to, float speed)
    {
        Vector3 toTarget = to - from;
        float travelTime = toTarget.magnitude / speed;
        return toTarget / travelTime - Physics.gravity * travelTime / 2f;
    }
}