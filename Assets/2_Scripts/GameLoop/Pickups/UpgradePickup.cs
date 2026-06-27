using System;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(Rigidbody))]
    public class UpgradePickup : MonoBehaviour
    {
        [Header("Upgrade")]
        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private float amount = 0.1f;

        [Header("Pop")]
        [SerializeField] private float upForce = 5f;
        [SerializeField] private float horizontalForce = 2f;

        [Header("Spin")]
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Effects")]
        [SerializeField] private ParticleEffectAction pickupEffect;

        [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;

        public static event Action<UpgradePickup> OnPickedUp;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Start()
        {
            Vector2 random = Random.insideUnitCircle * horizontalForce;
            Vector3 force = Vector3.up * upForce + new Vector3(random.x, 0f, random.y);
            rigidBody.AddForce(force, ForceMode.Impulse);
        }

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (!player) return;

            player.Upgrades.ApplyUpgrade(upgradeType, amount);
            pickupEffect?.Play(transform.position);
            OnPickedUp?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
