using System;
using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    [Serializable]
    public class RobotControllerVisuals
    {
        [Header("Visual References")]
        [SerializeField] private TireVisual suspensionVisual;
        [SerializeField] private TireVisual swivelVisual;
        [SerializeField] private TireVisual[] wheelVisuals;

        [Header("Visual Settings")]
        [SerializeField] private float suspensionReturnSpeed = 15f;
        [SerializeField] private float swivelTurnSpeed = 15f;
        [SerializeField] private float wheelRadius = 15f;

        private float _desiredSwivelWorldYaw;
        private float _currentSwivelWorldYaw;
        private bool _hasDesiredYaw;

        #region Initialization

        public void Initialize()
        {
            suspensionVisual?.Initialize();
            swivelVisual?.Initialize();
            foreach (var wheel in wheelVisuals)
                wheel.Initialize();

            InitializeSwivelYaw();
        }

        public void ResetVisuals()
        {
            suspensionVisual.ResetVisual();
            swivelVisual.ResetVisual();
            _hasDesiredYaw = false;
            _currentSwivelWorldYaw = swivelVisual.visTransform.eulerAngles.y;
            _desiredSwivelWorldYaw = _currentSwivelWorldYaw;
            foreach (var wheel in wheelVisuals)
                wheel.ResetVisual();
        }

        private void InitializeSwivelYaw()
        {
            if (swivelVisual == null || swivelVisual.visTransform == null)
                return;

            Vector3 flatForward = swivelVisual.visTransform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.0001f)
                return;

            flatForward.Normalize();

            float startYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            _desiredSwivelWorldYaw = startYaw;
            _currentSwivelWorldYaw = startYaw;
            _hasDesiredYaw = true;
        }

        #endregion

        #region Suspension

        public void UpdateSuspensionVisual(bool isGrounded, float offset)
        {
            if (!HasValidTransform(suspensionVisual))
                return;

            if (!isGrounded)
            {
                ReturnSuspensionToStart();
                return;
            }

            ApplySuspensionOffset(offset);
        }

        private void ReturnSuspensionToStart()
        {
            suspensionVisual.visTransform.localPosition = Vector3.Lerp(
                suspensionVisual.visTransform.localPosition,
                suspensionVisual.StartLocalPosition,
                suspensionReturnSpeed * Time.fixedDeltaTime);
        }

        private void ApplySuspensionOffset(float offset)
        {
            Vector3 target = suspensionVisual.StartLocalPosition;
            target.y -= offset;

            suspensionVisual.visTransform.localPosition = offset > 0f
                ? Vector3.Lerp(
                    suspensionVisual.visTransform.localPosition,
                    target,
                    suspensionReturnSpeed * Time.fixedDeltaTime)
                : target;
        }

        #endregion

        #region Swivel

        public void UpdateSwivelVisual(Vector3 moveDirWorld)
        {
            if (!TryGetSwivelTransforms(out Transform swivelTransform, out Transform parentTransform))
                return;

            UpdateDesiredSwivelYaw(moveDirWorld);

            if (!_hasDesiredYaw)
                return;

            SmoothCurrentSwivelYaw();
            ApplySwivelRotation(swivelTransform, parentTransform);
        }

        private bool TryGetSwivelTransforms(out Transform swivelTransform, out Transform parentTransform)
        {
            swivelTransform = null;
            parentTransform = null;

            if (!HasValidTransform(swivelVisual))
                return false;

            swivelTransform = swivelVisual.visTransform;
            parentTransform = swivelTransform.parent;

            return parentTransform != null;
        }

        private void UpdateDesiredSwivelYaw(Vector3 moveDirWorld)
        {
            Vector3 flatMoveDir = FlattenDirection(moveDirWorld);

            if (flatMoveDir.sqrMagnitude < 0.0001f)
                return;

            flatMoveDir.Normalize();
            _desiredSwivelWorldYaw = Mathf.Atan2(flatMoveDir.x, flatMoveDir.z) * Mathf.Rad2Deg;
            _hasDesiredYaw = true;
        }

        private void SmoothCurrentSwivelYaw()
        {
            float t = 1f - Mathf.Exp(-swivelTurnSpeed * Time.deltaTime);
            _currentSwivelWorldYaw = Mathf.LerpAngle(_currentSwivelWorldYaw, _desiredSwivelWorldYaw, t);
        }

        private void ApplySwivelRotation(Transform swivelTransform, Transform parentTransform)
        {
            float parentWorldYaw = GetWorldYaw(parentTransform.forward);
            float localYaw = Mathf.DeltaAngle(parentWorldYaw, _currentSwivelWorldYaw);

            swivelTransform.localRotation =
                swivelVisual.StartLocalRotation * Quaternion.Euler(0f, localYaw, 0f);
        }

        #endregion

        #region Wheel Rolling

        public void UpdateWheelRollingVisual(Vector3 robotVelocity)
        {
            if (wheelVisuals == null)
                return;

            foreach (var wheel in wheelVisuals)
            {
                if (!HasValidTransform(wheel))
                    continue;

                Transform t = swivelVisual.visTransform;

                // Wheel forward in world (after swivel!)
                Vector3 forward = t.forward;
                forward.y = 0f;

                if (forward.sqrMagnitude < 0.0001f)
                    continue;

                forward.Normalize();

                // Project velocity onto wheel forward
                float forwardSpeed = Vector3.Dot(robotVelocity, forward);

                // Convert to angular velocity
                float angularSpeed = forwardSpeed / wheelRadius; // rad/sec

                // Convert to degrees per frame
                float deltaAngle = angularSpeed * Mathf.Rad2Deg * Time.deltaTime;

                // Accumulate spin
                wheel.spinX += deltaAngle;

                // Apply rotation
                ApplyWheelRotation(wheel);
            }
        }
        
        private void ApplyWheelRotation(TireVisual wheel)
        {
            Quaternion spinRotation = Quaternion.AngleAxis(wheel.spinX, Vector3.right);

            wheel.visTransform.localRotation =
                wheel.StartLocalRotation * spinRotation;
        }

        #endregion

        #region Helpers

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction;
        }

        private static float GetWorldYaw(Vector3 worldDirection)
        {
            Vector3 flatDirection = FlattenDirection(worldDirection);

            if (flatDirection.sqrMagnitude < 0.0001f)
                return 0f;

            flatDirection.Normalize();
            return Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
        }

        private static bool HasValidTransform(TireVisual visual)
        {
            return visual != null && visual.visTransform != null;
        }

        #endregion
    }
}