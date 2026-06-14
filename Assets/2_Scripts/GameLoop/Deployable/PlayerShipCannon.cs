using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class PlayerShipCannon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform effectPosition;
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
            cannonEffect?.Play(effectPosition? effectPosition.position : transform.position);
        }
    }
}