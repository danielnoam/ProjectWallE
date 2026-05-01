using System;
using System.Collections.Generic;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class DeployBehavior
    {
        public abstract void Execute(DeploymentRequest request);
    }

    [Serializable]
    [SerializableSelectorName("Damage")]
    public class DamageBehavior : DeployBehavior
    {
        [SerializeField] private float radius = 5f;
        [SerializeField, MinMaxRange(0f, 500f)] private RangedFloat damageRange = new RangedFloat(50f, 100f);
        [SerializeField, SOSelector("Assets/6_Data")] private SOLayerMask hitLayers;

        public override void Execute(DeploymentRequest request)
        {
            var hit = new HashSet<IDamageable>();
            var colliders = Physics.OverlapSphere(request.TargetPosition, radius, hitLayers.Value);
            foreach (var col in colliders)
            {
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !hit.Add(damageable)) continue;
                if (!request.ImpactTeam.CanDamage(damageable.Team)) continue;

                float distance = Vector3.Distance(request.TargetPosition, col.transform.position);
                float damage = damageRange.Lerp(1f - distance / radius);
                damageable.TakeDamage(damage);
            }
        }
    }

    [Serializable]
    [SerializableSelectorName("Push")]
    public class PushBehavior : DeployBehavior
    {
        [SerializeField] private float radius = 10f;
        [SerializeField, MinMaxRange(0f, 100f)] private RangedFloat pushStrength = new RangedFloat(3f, 15f);

        public override void Execute(DeploymentRequest request)
        {
            var hit = new HashSet<IPushable>();
            var colliders = Physics.OverlapSphere(request.TargetPosition, radius);
            foreach (var col in colliders)
            {
                IPushable pushable = col.GetComponentInParent<IPushable>();
                if (pushable == null || !hit.Add(pushable)) continue;

                float distance = Vector3.Distance(request.TargetPosition, col.transform.position);
                float push = pushStrength.Lerp(1f - distance / radius);
                Vector3 direction = (col.transform.position - request.TargetPosition).normalized;
                pushable.Push(direction, push);
            }
        }
    }

    [Serializable]
    [SerializableSelectorName("Spawn Mine")]
    public class SpawnMineBehavior : DeployBehavior
    {
        [SerializeField] private Mine minePrefab;
        [SerializeField] private Team team;

        public override void Execute(DeploymentRequest request)
        {
            if (!minePrefab) return;
            Mine mine = UnityEngine.Object.Instantiate(minePrefab, request.TargetPosition, Quaternion.LookRotation(request.TargetForward));
            mine.Initialize(team);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Spawn Particle Effect")]
    public class ParticleImpactEffect : DeployBehavior
    {
        [SerializeField] private ParticleEffectAction effect;

        public override void Execute(DeploymentRequest request)
        {
            effect?.Play(request.TargetPosition, Quaternion.LookRotation(request.TargetSurfaceNormal));
        }
    }

    [Serializable]
    [SerializableSelectorName("Spawn Visual Effect")]
    public class VisualImpactEffect : DeployBehavior
    {
        [SerializeField] private VisualEffectAction effect;

        public override void Execute(DeploymentRequest request)
        {
            effect?.Play(request.TargetPosition);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Spawn Decal Effect")]
    public class DecalImpactEffect : DeployBehavior
    {
        [SerializeField] private PoolableDecal decal;

        public override void Execute(DeploymentRequest request)
        {
            if (!decal) return;
            var instance = ObjectPooler.GetObjectFromPool(decal, request.TargetPosition, Quaternion.LookRotation(request.TargetSurfaceNormal));
            instance?.Show();
        }
    }
}