using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class ShipCannon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private VisualEffectAction cannonEffect;
        
        
        private void Start()
        {
            DeploymentManager.Instance?.RegisterShipCannon(this);
        }

        private void OnDestroy()
        {
            DeploymentManager.Instance?.UnregisterShipCannon(this);
        }

        public void PlayEffects()
        {
           cannonEffect?.Play(transform.position);
        }
    }
}