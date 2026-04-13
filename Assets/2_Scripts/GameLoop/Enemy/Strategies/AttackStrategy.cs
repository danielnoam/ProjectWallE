using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public struct AttackContext
    {
        public Vector3 Position;
        public Vector3 TargetPosition;
        public IDamageable Owner;
        public LayerMask HitLayers;
    }

    [Serializable]
    public abstract class AttackStrategy
    {
        [SerializeField] private float attackRange = 10f;

        public float AttackRange => attackRange;
        public virtual bool ShouldDestroySelf => false;
        public virtual bool RequiresDirectApproach => false;
        public virtual void SetUp() { }
        
        public abstract void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime);
        public abstract void Reset();
    }

    [Serializable]
    [SerializableSelectorName("Projectile")]
    public class ProjectileAttack : AttackStrategy
    {
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private Transform firePoint;
        [SerializeField, SOSelector("Assets/6_Data")] private SOProjectileData soProjectileData;
        [SerializeField] private VisualEffectAction shootEffect;

        private float _attackTimer;

        public override void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime)
        {
            if (!isAligned || distanceToTarget > AttackRange)
            {
                _attackTimer = 0f;
                return;
            }

            _attackTimer += deltaTime;
            if (!(_attackTimer < attackCooldown))
            {
                var direction = (context.TargetPosition - context.Position).normalized;
                soProjectileData?.Spawn(firePoint.position, direction, context.TargetPosition);
                shootEffect?.Play(firePoint.position);
                _attackTimer = 0f;
            }
        }

        public override void Reset() => _attackTimer = 0f;
    }

    [Serializable]
    [SerializableSelectorName("Suicide")]
    public class SuicideAttack : AttackStrategy
    {
        [SerializeField] private float armedDuration = 1.5f;
        [SerializeField] private float aoeRadius = 5f;
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat damageRange = new RangedFloat(10f, 25f);
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat pushStrengthRange = new RangedFloat(25f, 50f);
        [SerializeField] private ParticleEffectAction detonateEffect;
        [SerializeField] private ColorPunchEffect armedPulseEffect;
        [SerializeField, AudioLibraryID] private string armedSoundId;
        [SerializeField] private float pulseIntervalStart = 0.5f;
        [SerializeField] private float pulseIntervalEnd = 0.08f;
        [SerializeField] private Renderer screenRenderer;
        
        private Material _material;
        private bool _shouldDestroySelf;
        private bool _isArmed;
        private float _armedTimer;
        private float _pulseTimer;
        private readonly HashSet<IDamageable> _alreadyHit = new();

        public override bool ShouldDestroySelf => _shouldDestroySelf;
        public override bool RequiresDirectApproach => true;

        public override void SetUp()
        {
            if (!screenRenderer) return;
            _material = screenRenderer.material;
        }

        public override void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime)
        {
            if (!(distanceToTarget > AttackRange) && !_isArmed)
            {
                _isArmed = true;
                _armedTimer = armedDuration;
                PlayArmedEffect(context.Position);
            }
            
            if (_isArmed)
            {
                _armedTimer -= deltaTime;
                _pulseTimer -= deltaTime;

                if (_pulseTimer <= 0f)
                {
                    PlayArmedEffect(context.Position);
                }

                if (_armedTimer <= 0f)
                {
                    Detonate(context);
                }
            }
        }

        private void PlayArmedEffect(Vector3 position)
        {
            float t = 1f - (_armedTimer / armedDuration);
            t = t * t * t;
            _pulseTimer = Mathf.Lerp(pulseIntervalStart, pulseIntervalEnd, t);
            armedPulseEffect?.Play(_material);
            AudioLibrary.PlayAtPosition(armedSoundId, position);
        }

        private void Detonate(AttackContext context)
        {
            Collider[] hits = Physics.OverlapSphere(context.Position, aoeRadius, context.HitLayers);
            _alreadyHit.Clear();

            foreach (var hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                if (target is EnemyDamageRelay relay) target = relay.Parent;
                if (target == null || !_alreadyHit.Add(target)) continue;

                float distance = Vector3.Distance(context.Position, hit.transform.position);
                float falloff = 1f - distance / aoeRadius;
                float damage = Mathf.Lerp(damageRange.minValue, damageRange.maxValue, falloff);
                float push = Mathf.Lerp(pushStrengthRange.minValue, pushStrengthRange.maxValue, falloff);

                target.TakeDamage(damage, context.Owner);

                var pushable = hit.GetComponentInParent<IPushable>();
                if (pushable != null)
                {
                    Vector3 pushDirection = (hit.transform.position - context.Position).normalized;
                    pushable.Push(pushDirection, push);
                }
            }
            
            detonateEffect?.Play(context.Position);
            _shouldDestroySelf = true;
        }

        public override void Reset()
        {
            _shouldDestroySelf = false;
            _alreadyHit.Clear();
        }
    }
}