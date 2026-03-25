using System;
using System.Collections.Generic;
using DNExtensions.Systems.Scriptables;
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
    }

    [Serializable]
    public abstract class AttackStrategy
    {
        [SerializeField] private float attackRange = 10f;

        public float AttackRange => attackRange;
        public virtual bool ShouldDestroySelf => false;
        public virtual bool RequiresDirectApproach => false;

        public abstract void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime);
        public abstract void Reset();
    }

    [Serializable]
    [SerializableSelectorName("Projectile")]
    public class ProjectileAttack : AttackStrategy
    {
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private Transform firePoint;
        [SerializeField, SOSelector("Assets/Data")] private ProjectileData projectileData;
        [SerializeField, SOSelector("Assets/Data")] private SOLayerMask hitLayers;

        private float _attackTimer;

        public override void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime)
        {
            if (!isAligned || distanceToTarget > AttackRange)
            {
                _attackTimer = 0f;
                return;
            }

            _attackTimer += deltaTime;
            if (_attackTimer < attackCooldown) return;

            var direction = (context.TargetPosition - context.Position).normalized;
            projectileData?.Spawn(hitLayers.Value, firePoint.position, direction, context.TargetPosition);
            _attackTimer = 0f;
        }

        public override void Reset() => _attackTimer = 0f;
    }

    [Serializable]
    [SerializableSelectorName("Suicide")]
    public class SuicideAttack : AttackStrategy
    {
        [SerializeField] private float aoeRadius = 5f;
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat damageRange = new RangedFloat(10f, 25f);
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat pushStrengthRange = new RangedFloat(25f, 50f);
        [SerializeField, SOSelector("Assets/Data")] private SOLayerMask hitLayers;

        private bool _shouldDestroySelf;
        private readonly HashSet<IDamageable> _alreadyHit = new();

        public override bool ShouldDestroySelf => _shouldDestroySelf;
        public override bool RequiresDirectApproach => true;

        public override void Tick(bool isAligned, float distanceToTarget, AttackContext context, float deltaTime)
        {
            if (distanceToTarget > AttackRange) return;

            Detonate(context);
            _shouldDestroySelf = true;
        }

        private void Detonate(AttackContext context)
        {
            Collider[] hits = Physics.OverlapSphere(context.Position, aoeRadius, hitLayers.Value);
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
        }

        public override void Reset()
        {
            _shouldDestroySelf = false;
            _alreadyHit.Clear();
        }
    }
}