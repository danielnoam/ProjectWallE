using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public interface IDeployable
    {
        void Deploy(Vector3 position, Vector3 surfaceNormal, Vector3 forward);
    }
}