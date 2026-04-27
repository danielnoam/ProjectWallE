using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class ShipPosition : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ParticleEffectAction cannonEffect;
        
        
        private void Start()
        {
            DeploymentManager.Instance?.RegisterShipPosition(this);
        }

        private void OnDestroy()
        {
            DeploymentManager.Instance?.UnregisterShipPosition(this);
        }

        public void PlayEffects()
        {
           cannonEffect?.Play(transform.position);
        }
    }
}