using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class EnemyBaseCannon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private VisualEffectAction cannonEffect;
        [SerializeField] private ImpulseSettings impulseSettings;
        [SerializeField, AutoGetParent] private EnemyBase ownerBase;
        [SerializeField, AutoGetSelf] private CinemachineImpulseSource impulseSource;
        [SerializeField] private Transform effectPosition;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Start()
        {
            DeploymentManager.Instance?.RegisterBaseCannon(ownerBase, this);
        }

        private void OnDestroy()
        {
            DeploymentManager.Instance?.UnregisterBaseCannon(ownerBase, this);
        }

        public void PlayEffects()
        {
            impulseSource?.GenerateImpulse(impulseSettings);
            cannonEffect?.Play(effectPosition ? effectPosition.position : transform.position);
        }
    }
}