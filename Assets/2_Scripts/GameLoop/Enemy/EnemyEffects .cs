using System;
using DNExtensions.Utilities.SerializableSelector;
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
#if UNITY_EDITOR
        public virtual void OnDrawGizmos(Transform owner) { }
#endif
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
                var w = wheels[i];
                if (!w.suspension) continue;

                UpdateSuspension(w, i, context.DeltaTime);

                if (w.wheel) w.wheel.Rotate(Vector3.right, spinAngle, Space.Self);
            }
        }

        private void InitializeWheels()
        {
            _suspensionVelocities = new float[wheels.Length];
            _restLocalY = new float[wheels.Length];

            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i].suspension)
                    _restLocalY[i] = wheels[i].suspension.localPosition.y;
            }

            _initialized = true;
        }

        private void UpdateSuspension(WheelData wheel, int index, float deltaTime)
        {
            var anchor = wheel.suspension.parent;
            if (!anchor) return;

            Vector3 rayOrigin = anchor.TransformPoint(new Vector3(
                wheel.suspension.localPosition.x,
                _restLocalY[index] + suspensionTravel * 0.5f,
                wheel.suspension.localPosition.z));

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

            float currentY = wheel.suspension.localPosition.y;
            float springForce = (targetLocalY - currentY) * suspensionStiffness;
            _suspensionVelocities[index] += springForce * deltaTime;
            _suspensionVelocities[index] *= Mathf.Max(0f, 1f - suspensionDamping * deltaTime);

            float newY = currentY + _suspensionVelocities[index] * deltaTime;
            newY = Mathf.Clamp(newY, _restLocalY[index] - suspensionTravel * 0.5f, _restLocalY[index] + suspensionTravel * 0.5f);

            var localPos = wheel.suspension.localPosition;
            localPos.y = newY;
            wheel.suspension.localPosition = localPos;
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos(Transform owner)
        {
            if (wheels == null) return;

            foreach (var wheel in wheels)
            {
                var wheelTransform = wheel.suspension ? wheel.suspension : wheel.wheel;
                if (!wheelTransform) continue;

                var anchor = wheelTransform.parent;
                if (!anchor) continue;

                float restY = wheelTransform.localPosition.y;

                Vector3 topLimit = anchor.TransformPoint(new Vector3(
                    wheelTransform.localPosition.x,
                    restY + suspensionTravel * 0.5f,
                    wheelTransform.localPosition.z));

                Vector3 bottomLimit = anchor.TransformPoint(new Vector3(
                    wheelTransform.localPosition.x,
                    restY - suspensionTravel * 0.5f,
                    wheelTransform.localPosition.z));

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
                Gizmos.DrawWireSphere(wheelTransform.position, wheelRadius);
            }
        }
#endif
    }

    [Serializable]
    public class WheelData
    {
        public Transform suspension;
        public Transform wheel;
    }
}