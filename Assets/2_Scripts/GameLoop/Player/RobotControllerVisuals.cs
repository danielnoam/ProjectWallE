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
        
        private float _desiredSwivelWorldYaw;
        private float _currentSwivelWorldYaw;
        private bool _hasDesiredYaw;
        
        public void Initialize(Transform carTransform)
        {
            _robotTransform = carTransform;

            suspensionVisual?.Initialize();
            swivelVisual?.Initialize();

            foreach (var wheel in wheelVisuals)
                wheel?.Initialize();

            if (swivelVisual != null && swivelVisual.visTransform != null)
            {
                Vector3 flatForward = swivelVisual.visTransform.forward;
                flatForward.y = 0f;

                if (flatForward.sqrMagnitude > 0.0001f)
                {
                    flatForward.Normalize();
                    float startYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
                    _desiredSwivelWorldYaw = startYaw;
                    _currentSwivelWorldYaw = startYaw;
                    _hasDesiredYaw = true;
                }
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

        public void UpdateSwivelVisual(Vector3 moveDirWorld)
        {
            if (swivelVisual == null || swivelVisual.visTransform == null)
                return;

            Transform t = swivelVisual.visTransform;
            Transform parent = t.parent;
            if (parent == null)
                return;

            Vector3 flatMoveDir = moveDirWorld;
            flatMoveDir.y = 0f;

            if (flatMoveDir.sqrMagnitude > 0.0001f)
            {
                flatMoveDir.Normalize();
                _desiredSwivelWorldYaw = Mathf.Atan2(flatMoveDir.x, flatMoveDir.z) * Mathf.Rad2Deg;
                _hasDesiredYaw = true;
            }

            if (!_hasDesiredYaw)
                return;

            float tSmooth = 1f - Mathf.Exp(-swivelTurnSpeed * Time.deltaTime);
            _currentSwivelWorldYaw = Mathf.LerpAngle(_currentSwivelWorldYaw, _desiredSwivelWorldYaw, tSmooth);

            Vector3 parentForwardFlat = parent.forward;
            parentForwardFlat.y = 0f;

            if (parentForwardFlat.sqrMagnitude < 0.0001f)
                parentForwardFlat = Vector3.forward;
            else
                parentForwardFlat.Normalize();

            float parentWorldYaw = Mathf.Atan2(parentForwardFlat.x, parentForwardFlat.z) * Mathf.Rad2Deg;
            float localYaw = Mathf.DeltaAngle(parentWorldYaw, _currentSwivelWorldYaw);

            t.localRotation = swivelVisual.StartLocalRotation * Quaternion.Euler(0f, localYaw, 0f);
        }
        public void UpdateWheelRollingVisual(float robotSpeed)
        {
            
        }
    }
}