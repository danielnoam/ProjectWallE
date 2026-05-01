using System;
using System.Collections.Generic;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CinemachineExtensions;
using DNExtensions.Utilities.SerializableSelector;
using Unity.Cinemachine;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public struct BehaviorRequest
    {
        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 SurfaceNormal;
        public Team Team;
        public AudioSource AudioSource;
        public CinemachineImpulseSource ImpulseSource;

        public BehaviorRequest(Vector3 position, Vector3 forward, Vector3 surfaceNormal)
        {
            Position = position;
            Forward = forward;
            SurfaceNormal = surfaceNormal;
            Team = Team.Neutral;
            AudioSource = null;
            ImpulseSource = null;
        }
        
        public BehaviorRequest(Vector3 position, Vector3 forward, Vector3 surfaceNormal, AudioSource audioSource = null, CinemachineImpulseSource impulseSource = null)
        {
            Position = position;
            Forward = forward;
            SurfaceNormal = surfaceNormal;
            Team = Team.Neutral;
            AudioSource = null;
            ImpulseSource = null;
        }
    }
    
    [Serializable]
    public abstract class DeployBehavior
    {
        public abstract void Execute(BehaviorRequest request);
    }

    [Serializable]
    [SerializableSelectorName("Damage")]
    public class DamageBehavior : DeployBehavior
    {
        [SerializeField] private float radius = 5f;
        [SerializeField, MinMaxRange(0f, 500f)] private RangedFloat damageRange = new RangedFloat(50f, 100f);
        [SerializeField, SOSelector("Assets/6_Data")] private SOLayerMask hitLayers;

        public override void Execute(BehaviorRequest request)
        {
            var hit = new HashSet<IDamageable>();
            var colliders = Physics.OverlapSphere(request.Position, radius, hitLayers.Value);
            foreach (var col in colliders)
            {
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !hit.Add(damageable)) continue;
                if (!request.Team.CanDamage(damageable.Team)) continue;

                float distance = Vector3.Distance(request.Position, col.transform.position);
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

        public override void Execute(BehaviorRequest request)
        {
            var hit = new HashSet<IPushable>();
            var colliders = Physics.OverlapSphere(request.Position, radius);
            foreach (var col in colliders)
            {
                IPushable pushable = col.GetComponentInParent<IPushable>();
                if (pushable == null || !hit.Add(pushable)) continue;

                float distance = Vector3.Distance(request.Position, col.transform.position);
                float push = pushStrength.Lerp(1f - distance / radius);
                Vector3 direction = (col.transform.position - request.Position).normalized;
                pushable.Push(direction, push);
            }
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Generate Impulse")]
    public class GenerateImpulseBehavior : DeployBehavior
    {
        [SerializeField] private ImpulseSettings impulseSettings;


        public override void Execute(BehaviorRequest request)
        {
            request.ImpulseSource?.GenerateImpulse(impulseSettings);
        }
    }
    

    [Serializable]
    [SerializableSelectorName("Spawn Mine")]
    public class SpawnMineBehavior : DeployBehavior
    {
        [SerializeField] private Mine minePrefab;
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Team team;

        public override void Execute(BehaviorRequest request)
        {
            if (!minePrefab) return;
            Mine mine = UnityEngine.Object.Instantiate(minePrefab, request.Position.Add(positionOffset), Quaternion.LookRotation(request.Forward));
            mine.Initialize(team);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Particle Effect")]
    public class ParticleImpactEffect : DeployBehavior
    {
        [SerializeField] private ParticleEffectAction effect;

        public override void Execute(BehaviorRequest request)
        {
            effect?.Play(request.Position, Quaternion.LookRotation(request.SurfaceNormal), request.AudioSource);
        }
    }

    [Serializable]
    [SerializableSelectorName("Visual Effect")]
    public class VisualImpactEffect : DeployBehavior
    {
        [SerializeField] private VisualEffectAction effect;

        public override void Execute(BehaviorRequest request)
        {
            effect?.Play(request.Position, request.AudioSource);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Decal Effect")]
    public class DecalImpactEffect : DeployBehavior
    {
        [SerializeField] private PoolableDecal decal;

        public override void Execute(BehaviorRequest request)
        {
            if (!decal) return;
            var instance = ObjectPooler.GetObjectFromPool(decal, request.Position, Quaternion.LookRotation(request.SurfaceNormal));
            instance?.Show();
        }
    }
}