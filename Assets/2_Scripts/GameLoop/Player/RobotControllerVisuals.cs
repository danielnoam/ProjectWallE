using System;
using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    [Serializable]
    public class RobotControllerVisuals
    {
        [SerializeField] private TireVisual wheelVisual;
        [SerializeField] private float suspensionReturnSpeed = 15f;
        [SerializeField] private float wheelRadius = 15f;

        public void Initialize()
        {
            wheelVisual?.Initialize();
        }

        public void ResetVisuals()
        {
            wheelVisual?.ResetVisual();
        }

        public void UpdateSuspensionVisual(bool isGrounded, float offset)
        {
            if (wheelVisual == null || wheelVisual.visualTransform == null)
                return;

            if (!isGrounded)
            {
                wheelVisual.visualTransform.localPosition = Vector3.Lerp(
                    wheelVisual.visualTransform.localPosition,
                    wheelVisual.VisualStartPosition,
                    suspensionReturnSpeed * Time.fixedDeltaTime);

                return;
            }

            Vector3 target = wheelVisual.VisualStartPosition;
            target.y -= offset;

            wheelVisual.visualTransform.localPosition = offset > 0f
                ? Vector3.Lerp(
                    wheelVisual.visualTransform.localPosition,
                    target,
                    suspensionReturnSpeed * Time.fixedDeltaTime)
                : target;
        }

        public void UpdateWheelRollingVisual(float robotSpeed)
        {
            
        }
    }
}