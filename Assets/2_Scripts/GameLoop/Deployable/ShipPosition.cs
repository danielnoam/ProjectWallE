using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class ShipPosition : MonoBehaviour
    {
        private void Start()
        {
            DeploymentManager.Instance?.RegisterShipPosition(transform);
        }

        private void OnDestroy()
        {
            DeploymentManager.Instance?.UnregisterShipPosition(transform);
        }
    }
}