
using UnityEngine;

public interface IDeployable
{
    void Deploy(Vector3 impactPoint, Vector3 surfaceNormal, Vector3 forward);
}