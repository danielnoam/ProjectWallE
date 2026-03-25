using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public struct EffectContext
    {
        public float DeltaTime;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3? TargetPosition;
        public bool IsVisible;
    }

    [Serializable]
    public abstract class EnemyEffect
    {
        public virtual void Tick(EffectContext context) { }
        public virtual void OnHit(Vector3 position, EnemyDamageRelay relay) { }
        public virtual void OnHitAll(Vector3 position, List<EnemyDamageRelay> relays) { }
        public virtual void OnDeath(Vector3 position) { }
#if UNITY_EDITOR
        public virtual void OnDrawGizmos(Transform owner) { }
#endif
    }

    [Serializable]
    [SerializableSelectorName("On Hit")]
    [SerializableSelectorAllowOnce]
    public class HitEffect : EnemyEffect
    {
        [Header("Emission")]
        [SerializeField] private float punchStrength = 1f;
        [SerializeField] private float punchDuration = 0.15f;
        [SerializeField] private Ease punchEase = Ease.Linear;
        [SerializeField, ColorUsage(false, true)] private Color normalEmissionColor = Color.red;
        [SerializeField, ColorUsage(false, true)] private Color criticalEmissionColor = Color.red;

        [Header("SFX")]
        [SerializeField, AudioLibraryID] private string normalDamagedSoundId;
        [SerializeField, AudioLibraryID] private string criticalDamageSoundId;

        private static readonly int EmissionStrength = Shader.PropertyToID("_Emission_Strength");
        private static readonly int EmissionColor = Shader.PropertyToID("_Emission_Color");

        private void PunchEmission(Material[] materials, Color color)
        {
            if (materials == null || materials.Length == 0) return;

            foreach (var mat in materials)
            {
                mat.SetColor(EmissionColor, color);
            }

            var seq = Sequence.Create();
            foreach (var mat in materials)
            {
                seq.Group(Tween.MaterialProperty(mat, EmissionStrength, punchStrength, punchDuration * 0.5f, punchEase));
            }

            seq.Chain(Tween.MaterialProperty(materials[0], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            for (var i = 1; i < materials.Length; i++)
            {
                seq.Group(Tween.MaterialProperty(materials[i], EmissionStrength, 0f, punchDuration * 0.5f, punchEase));
            }
        }

        public override void OnHitAll(Vector3 position, List<EnemyDamageRelay> relays)
        {
            foreach (var relay in relays)
            {
                PunchEmission(relay.Materials, normalEmissionColor);
            }

            AudioLibrary.PlayAtPosition(normalDamagedSoundId, position);
        }

        public override void OnHit(Vector3 position, EnemyDamageRelay relay)
        {
            Color color = relay.HitZone == EnemyHitZone.Critical ? criticalEmissionColor : normalEmissionColor;
            string soundId = relay.HitZone == EnemyHitZone.Critical ? criticalDamageSoundId : normalDamagedSoundId;

            PunchEmission(relay.Materials, color);
            AudioLibrary.PlayAtPosition(soundId, position);
        }
    }

    [Serializable]
    [SerializableSelectorName("On Death")]
    [SerializableSelectorAllowOnce]
    public class DeathEffect : EnemyEffect
    {
        [SerializeField] private PoolableParticleSystem particle;
        [SerializeField, AudioLibraryID] private string soundId;

        public override void OnDeath(Vector3 position)
        {
            if (particle)
            {
                var effect = ObjectPooler.GetObjectFromPool(particle, position);
                effect?.Play();
            }

            AudioLibrary.PlayAtPosition(soundId, position);
        }
    }

    [Serializable]
    [SerializableSelectorName("Propeller")]
    [SerializableSelectorAllowOnce]
    public class PropellerEffect : EnemyEffect
    {
        [SerializeField] private Transform[] propellersCw;
        [SerializeField] private Transform[] propellersCcw;
        [SerializeField] private float rotationSpeed = 1000f;

        public override void Tick(EffectContext context)
        {
            if (!context.IsVisible) return;

            foreach (var prop in propellersCw)
            {
                prop.Rotate(Vector3.up, rotationSpeed * context.DeltaTime);
            }

            foreach (var prop in propellersCcw)
            {
                prop.Rotate(Vector3.up, -rotationSpeed * context.DeltaTime);
            }
        }
    }

    [Serializable]
    [SerializableSelectorName("Head Tracking")]
    [SerializableSelectorAllowOnce]
    public class HeadTrackingEffect : EnemyEffect
    {
        [SerializeField] private Transform headTransform;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private bool yawOnly = true;

        public override void Tick(EffectContext context)
        {
            if (!headTransform || !context.TargetPosition.HasValue) return;
            if (!context.IsVisible) return;

            Vector3 direction = context.TargetPosition.Value - headTransform.position;
            if (yawOnly) direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            headTransform.rotation = Quaternion.RotateTowards(headTransform.rotation, targetRotation, rotationSpeed * context.DeltaTime);
        }
    }

    [Serializable]
    [SerializableSelectorName("Wheels")]
    [SerializableSelectorAllowOnce]
    public class WheelEffect : EnemyEffect
    {
        [SerializeField] private WheelData[] wheels;
        [SerializeField] private float wheelRadius = 0.3f;
        [SerializeField] private float suspensionTravel = 0.4f;
        [SerializeField] private float suspensionStiffness = 20f;
        [SerializeField] private float suspensionDamping = 5f;
        [SerializeField] private float raycastDistance = 1.5f;
        [SerializeField] private LayerMask groundMask;

        private float[] _suspensionVelocities;
        private float[] _restLocalY;
        private bool _initialized;

        public override void Tick(EffectContext context)
        {
            if (!context.IsVisible) return;
            if (wheels == null || wheels.Length == 0) return;

            if (!_initialized) InitializeWheels();

            float speed = new Vector3(context.Velocity.x, 0, context.Velocity.z).magnitude;
            float spinAngle = (speed / wheelRadius) * Mathf.Rad2Deg * context.DeltaTime;

            for (int i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                if (!wheel.transform) continue;

                UpdateSuspension(wheel, i, context.DeltaTime);
                wheel.transform.Rotate(Vector3.right, spinAngle, Space.Self);
            }
        }

        private void InitializeWheels()
        {
            _suspensionVelocities = new float[wheels.Length];
            _restLocalY = new float[wheels.Length];

            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i].transform)
                    _restLocalY[i] = wheels[i].transform.localPosition.y;
            }

            _initialized = true;
        }

        private void UpdateSuspension(WheelData wheel, int index, float deltaTime)
        {
            var anchor = wheel.transform.parent;
            if (!anchor) return;

            Vector3 rayOrigin = anchor.TransformPoint(new Vector3(
                wheel.transform.localPosition.x,
                _restLocalY[index] + suspensionTravel * 0.5f,
                wheel.transform.localPosition.z));

            float targetLocalY;

            if (Physics.Raycast(rayOrigin, -anchor.up, out RaycastHit hit, raycastDistance, groundMask))
            {
                float groundLocalY = anchor.InverseTransformPoint(hit.point).y + wheelRadius;
                targetLocalY = Mathf.Clamp(groundLocalY, _restLocalY[index] - suspensionTravel * 0.5f, _restLocalY[index] + suspensionTravel * 0.5f);
            }
            else
            {
                targetLocalY = _restLocalY[index] - suspensionTravel * 0.5f;
            }

            float currentY = wheel.transform.localPosition.y;
            float springForce = (targetLocalY - currentY) * suspensionStiffness;
            _suspensionVelocities[index] += springForce * deltaTime;
            _suspensionVelocities[index] *= Mathf.Max(0f, 1f - suspensionDamping * deltaTime);

            float newY = currentY + _suspensionVelocities[index] * deltaTime;
            newY = Mathf.Clamp(newY, _restLocalY[index] - suspensionTravel * 0.5f, _restLocalY[index] + suspensionTravel * 0.5f);

            var localPos = wheel.transform.localPosition;
            localPos.y = newY;
            wheel.transform.localPosition = localPos;
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos(Transform owner)
        {
            if (wheels == null) return;

            foreach (var wheel in wheels)
            {
                if (!wheel.transform) continue;

                var anchor = wheel.transform.parent;
                if (!anchor) continue;

                float restY = wheel.transform.localPosition.y;

                Vector3 topLimit = anchor.TransformPoint(new Vector3(
                    wheel.transform.localPosition.x,
                    restY + suspensionTravel * 0.5f,
                    wheel.transform.localPosition.z));

                Vector3 bottomLimit = anchor.TransformPoint(new Vector3(
                    wheel.transform.localPosition.x,
                    restY - suspensionTravel * 0.5f,
                    wheel.transform.localPosition.z));

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(topLimit, bottomLimit);
                Gizmos.DrawWireSphere(topLimit, 0.03f);
                Gizmos.DrawWireSphere(bottomLimit, 0.03f);

                Vector3 rayOrigin = topLimit;
                Vector3 localDown = -anchor.up;
                bool hit = Physics.Raycast(rayOrigin, localDown, out RaycastHit hitInfo, raycastDistance, groundMask);
                Gizmos.color = hit ? Color.green : Color.red;
                Gizmos.DrawLine(rayOrigin, hit ? hitInfo.point : rayOrigin + localDown * raycastDistance);

                if (hit)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(hitInfo.point, 0.05f);
                }

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(wheel.transform.position, wheelRadius);
            }
        }
#endif
    }

    [Serializable]
    public class WheelData
    {
        public Transform transform;
    }
}