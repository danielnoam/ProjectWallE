using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerPusher : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float pushPower = 35f;
        [SerializeField] private float pushDamage = 15f;
        [SerializeField, Min(0f)] private float speedThreshold = 14f;
        [SerializeField] private SOLayerMask enemyLayer;
        [SerializeField] private CollisionRelay pushableCollider;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;

        private bool _isCar;

        private void OnEnable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered += OnColliderEntered;
           if (playerManager) playerManager.OnControllerChanged += OnControllerChanged;
        }

        private void OnDisable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered -= OnColliderEntered;
           if (playerManager) playerManager.OnControllerChanged -= OnControllerChanged;
        }

        private void OnControllerChanged(PlayerControllerType controllerType)
        {
            _isCar = controllerType == PlayerControllerType.Car;
        }
        

        private void OnColliderEntered(Collider other)
        {
            if (!_isCar || (enemyLayer.Value & (1 << other.gameObject.layer)) == 0) return;
            
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