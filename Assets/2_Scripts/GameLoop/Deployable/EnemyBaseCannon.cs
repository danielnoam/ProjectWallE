using System;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class EnemyBaseCannon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, AutoGetParent] private EnemyBase ownerBase;
        [SerializeField] private Transform effectPosition;
        [SerializeField] private VisualEffectAction cannonEffect;

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
            cannonEffect?.Play(effectPosition ? effectPosition.position : transform.position);
        }
    }
}