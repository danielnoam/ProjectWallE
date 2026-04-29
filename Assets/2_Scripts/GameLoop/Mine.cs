using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using UnityEngine;
using DNExtensions.Utilities.AutoGet;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(Collider))]
    public class Mine : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float detonateRadius = 5f;
        [SerializeField] private float detonateDelay = 0.5f;
        [SerializeField, SOSelector("Assets/6_Data")] private SOLayerMask detectionLayers;
        [SerializeField, MinMaxRange(0f, 500f)] private RangedFloat damageRange = new RangedFloat(50f, 100f);
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat pushStrength = new RangedFloat(3f, 15f);

        [Header("Effects")]
        [SerializeField] private VisualEffectAction detonateEffect;
        [SerializeField] private DamageEffects damageEffects;
        [SerializeField] private DamageEffects triggeredEffect;
        [SerializeField, AutoGetChildren, HideInInspector] private Renderer rend;
        

        private Coroutine _detonateCoroutine;
        private float _currentHealth;
        private Material _material;
        private bool _detonated;
        private Team _impactTeam = Team.Neutral;

        public bool IsAlive => _currentHealth > 0;
        public Team Team => Team.Neutral;

        public event Action<IDamageable> OnDeath;
        public event Action<float> OnDamaged;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _material = rend ? rend.material : null;
        }

        public void Initialize(Team impactTeam)
        {
            _impactTeam = impactTeam;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !_impactTeam.CanDamage(damageable.Team)) return;
            _detonateCoroutine ??= StartCoroutine(DetonateDelayed());
            triggeredEffect?.Play(transform.position, _material);
        }
        
        private IEnumerator DetonateDelayed()
        {
            yield return new WaitForSeconds(detonateDelay);
            Detonate();
        }

        private void Detonate()
        {
            if (_detonated) return;
            _detonated = true;
            
            detonateEffect?.Play(transform.position);

            var hit = new HashSet<IDamageable>();
            var colliders = Physics.OverlapSphere(transform.position, detonateRadius, detectionLayers.Value);
            foreach (var col in colliders)
            {
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !hit.Add(damageable)) continue;
                if (!_impactTeam.CanDamage(damageable.Team)) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damage = damageRange.Lerp(1f - distance / detonateRadius);
                damageable.TakeDamage(damage);

                IPushable pushable = col.GetComponentInParent<IPushable>();
                if (pushable == null) continue;

                float push = pushStrength.Lerp(1f - distance / detonateRadius);
                Vector3 direction = (col.transform.position - transform.position).normalized;
                pushable.Push(direction, push);
            }

            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
        
        public void TakeDamage(float damage, IDamageable attacker = null)
        {
            if (!IsAlive) return;
            _currentHealth -= damage;
            damageEffects?.Play(transform.position, _material);
            OnDamaged?.Invoke(damage);
            if (_currentHealth <= 0)
            {
                if (_detonateCoroutine != null) StopCoroutine(_detonateCoroutine);
                Detonate();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detonateRadius);
        }
#endif
    }
}