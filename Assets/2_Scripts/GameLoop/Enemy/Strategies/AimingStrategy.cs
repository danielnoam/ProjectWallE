using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class AimingStrategy
    {
        public virtual bool ControlsBodyRotation => false;

        public abstract void Aim(Transform owner, Vector3 targetPosition, float deltaTime);
        public abstract bool IsAligned(Transform owner, Vector3 targetPosition);
    }

    [Serializable]
    [SerializableSelectorName("Independent")]
    public class IndependentAiming : AimingStrategy
    {
        [SerializeField] private Transform headTransform;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float alignmentThreshold = 15f;
        [SerializeField] private bool yawOnly = true;

        public override void Aim(Transform owner, Vector3 targetPosition, float deltaTime)
        {
            if (!headTransform) return;

            Vector3 direction = targetPosition - headTransform.position;
            if (yawOnly) direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            headTransform.rotation = Quaternion.RotateTowards(headTransform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        public override bool IsAligned(Transform owner, Vector3 targetPosition)
        {
            if (!headTransform) return true;

            Vector3 direction = targetPosition - headTransform.position;
            if (yawOnly) direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return false;

            return Vector3.Angle(headTransform.forward, direction) <= alignmentThreshold;
        }
    }

    [Serializable]
    [SerializableSelectorName("Body")]
    public class BodyAiming : AimingStrategy
    {
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float alignmentThreshold = 15f;

        public override bool ControlsBodyRotation => true;

        public override void Aim(Transform owner, Vector3 targetPosition, float deltaTime)
        {
            Vector3 direction = targetPosition - owner.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            owner.rotation = Quaternion.RotateTowards(owner.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        public override bool IsAligned(Transform owner, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - owner.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) return false;

            return Vector3.Angle(owner.forward, direction) <= alignmentThreshold;
        }
    }

    [Serializable]
    [SerializableSelectorName("None")]
    public class NoAiming : AimingStrategy
    {
        public override void Aim(Transform owner, Vector3 targetPosition, float deltaTime) { }
        public override bool IsAligned(Transform owner, Vector3 targetPosition) => true;
    }
}