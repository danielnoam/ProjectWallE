using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class PlayerShipCannon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ImpulseSettings impulseSettings;
        [SerializeField] private VisualEffectAction cannonEffect;
        [SerializeField] private Transform effectPosition;
        [SerializeField, AutoGetSelf] private CinemachineImpulseSource impulseSource;
        
        
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
            impulseSource?.GenerateImpulse(impulseSettings);
            cannonEffect?.Play(effectPosition? effectPosition.position : transform.position);
        }
    }
}