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

        [Header("GFX")]
        [SerializeField] private Transform gfx;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobSpeed = 2f;

        [Header("Effects")]
        [SerializeField] private ParticleEffectAction pickupEffect;

        [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;

        public static event Action<UpgradePickup> OnPickedUp;

        private bool _collected;
        private Vector3 _gfxLocalStart;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Start()
        {
            if (gfx) _gfxLocalStart = gfx.localPosition;

            Vector2 random = Random.insideUnitCircle * horizontalForce;
            Vector3 force = Vector3.up * upForce + new Vector3(random.x, 0f, random.y);
            rigidBody.AddForce(force, ForceMode.Impulse);
        }

        private void Update()
        {
            if (!gfx) return;

            gfx.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            gfx.localPosition = _gfxLocalStart + Vector3.up * bob;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (!player) return;

            _collected = true;
            player.Upgrades.ApplyUpgrade(upgradeType, amount);
            pickupEffect?.Play(transform.position);
            OnPickedUp?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
