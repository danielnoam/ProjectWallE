using System;
using _2_Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectWallE
{
    [Serializable]
    public class RobotControllerVisuals
    {
        [SerializeField] private TireVisual suspensionVisual;
        [SerializeField] private TireVisual swivelVisual;
        [SerializeField] private TireVisual[] wheelVisuals;
        [SerializeField] private float suspensionReturnSpeed = 15f;
        [SerializeField] private float swivelTurnSpeed = 15f;
        [SerializeField] private float wheelRadius = 15f;
        
        private Transform _robotTransform;
        
        public void Initialize(Transform carTransform)
        {
            _robotTransform = carTransform;
            
            suspensionVisual?.Initialize();
            swivelVisual?.Initialize();
            foreach (var wheel in wheelVisuals)
            {
                wheel?.Initialize();
            }
        }

        public void ResetVisuals()
        {
            suspensionVisual?.ResetVisual();
            swivelVisual?.ResetVisual();
            foreach (var wheel in wheelVisuals)
            {
                wheel?.ResetVisual();
            }
        }

        public void UpdateSuspensionVisual(bool isGrounded, float offset)
        {
            if (suspensionVisual == null || suspensionVisual.visTransform == null)
                return;

            if (!isGrounded)
            {
                suspensionVisual.visTransform.localPosition = Vector3.Lerp(
                    suspensionVisual.visTransform.localPosition,
                    suspensionVisual.StartLocalPosition,
                    suspensionReturnSpeed * Time.fixedDeltaTime);

                return;
            }

            Vector3 target = suspensionVisual.StartLocalPosition;
            target.y -= offset;

            suspensionVisual.visTransform.localPosition = offset > 0f
                ? Vector3.Lerp(
                    suspensionVisual.visTransform.localPosition,
                    target,
                    suspensionReturnSpeed * Time.fixedDeltaTime)
                : target;
        }

        

        public void UpdateSwivelVisual(Vector3 moveDirWorld, float yawDelta)
        {
            if (swivelVisual == null || swivelVisual.visTransform == null)
                return;

            Transform t = swivelVisual.visTransform;

            t.localRotation *= Quaternion.Euler(0f, -yawDelta, 0f);
            
            if (moveDirWorld.sqrMagnitude < 0.0001f)
                return;

            Transform parent = t.parent;
            if (parent == null)
                return;

            Vector3 localDir = parent.InverseTransformDirection(moveDirWorld.normalized);
            localDir.y = 0f;

            if (localDir.sqrMagnitude < 0.0001f)
                return;

            float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

            Quaternion targetRot =
                swivelVisual.StartLocalRotation * Quaternion.Euler(0f, angle, 0f);

            t.localRotation = Quaternion.Lerp(
                t.localRotation,
                targetRot,
                swivelTurnSpeed * Time.fixedDeltaTime
            );
        }
        public void UpdateWheelRollingVisual(float robotSpeed)
        {
            
        }
    }
}