using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;


namespace ProjectWallE.GameLoop.Player
{
    public class PlayerPusher : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float pushPower = 1500f;
        [SerializeField] private float pushDamage = 35f;
        [SerializeField] private bool pushOnlyWhenBoosting = true;
        [SerializeField] private SOLayerMask enemyLayer;
        [SerializeField] private CollisionRelay pushableCollider;
        [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;

        private bool _isCar;
        private bool _isBoosting;

        private void OnEnable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered += OnColliderEntered;
           if (playerManager) playerManager.OnControllerChanged += OnControllerChanged;
           if (playerManager.CarController.CarBoost)
           {
               playerManager.CarController.CarBoost.OnBoostStart += OnBoostStart;
               playerManager.CarController.CarBoost.OnBoostEnd += OnBoostEnd;
           }
        }

        private void OnDisable()
        {
           if (pushableCollider) pushableCollider.TriggerEntered -= OnColliderEntered;
           if (playerManager) playerManager.OnControllerChanged -= OnControllerChanged;
           if (playerManager.CarController.CarBoost)
           {
               playerManager.CarController.CarBoost.OnBoostStart -= OnBoostStart;
               playerManager.CarController.CarBoost.OnBoostEnd -= OnBoostEnd;
           }
        }

        private void OnControllerChanged(PlayerControllerType controllerType)
        {
            _isCar = controllerType == PlayerControllerType.Car;
        }
        
        private void OnBoostEnd()
        {
            _isBoosting = false;
        }

        private void OnBoostStart()
        {
            _isBoosting = true;
        }

        private void OnColliderEntered(Collider other)
        {
            if ((pushOnlyWhenBoosting && !_isBoosting) || !_isCar || (enemyLayer.Value & (1 << other.gameObject.layer)) == 0) return;
            
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