using System;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public class CarPusher : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float pushPower = 35f;
        [SerializeField] private float pushDamage = 15f;
        [SerializeField, Min(0f)] private float speedThreshold = 14f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private CollisionRelay pushableCollider;
        [SerializeField, AutoGetParent, HideInInspector] private PlayerManager playerManager;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered += OnColliderEntered;
        }

        private void OnDisable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered -= OnColliderEntered;
        }
        

        private void OnColliderEntered(Collider other)
        {
            if ((enemyLayer & (1 << other.gameObject.layer)) == 0) return;
            
            var velocity = playerManager.Velocity.SetY(0f).magnitude;
            
            if (velocity < speedThreshold)  return;
            
            var pushable = other.GetComponentInParent<IPushable>();
            if (pushable != null)
            {
                var direction = (other.transform.position - transform.position).normalized;
                pushable.Push(direction, pushPower);
            }
            
            var damageable = other.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(pushDamage);
        }
    }
}