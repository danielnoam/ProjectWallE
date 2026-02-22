using System;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(RobotInput))]
    public class RobotController : MonoBehaviour, IPlayerController
    {
        private RobotInput _input;
        
        [SerializeField] Transform _groundRayPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Rigidbody playerRB;
        [SerializeField] private Transform cameraTransform;
        
        [Header("Grounded Settings")]
        [SerializeField] private float groundHeight = 0.5f;
        [SerializeField] private float maxCheckHeight = 1f;
        [SerializeField] private float springStrength;
        [SerializeField] private float springDamping;
        
        [Header("Locomotion Settings")]
        [SerializeField] private float moveAccel = 20f; // horizontal accel (m/s^2)
        [Tooltip("x axis is how much we are changing our direction (-1 is completely changing, 1 is not changing at all)" +
                 "y axis is the multiplier itself relative to the change")]
        [SerializeField] private AnimationCurve dirChangeAccelFactor;
        [SerializeField] private float maxMoveSpeed = 8f;
        [SerializeField] private float forcePointBelowCOM = 0.2f; // meters below COM
        [SerializeField] private float groundMoveFactor = 1f;     // optionally reduce in air
        [SerializeField] private float airMoveFactor = 0.35f;
        
        [Header("Rotation Settings")]
        [SerializeField] private float uprightStrength = 80f;
        [SerializeField] private float uprightDamping = 12f;
        [SerializeField] private float yawStrength = 40f;
        [SerializeField] private float yawDamping = 8f;
        
        private bool _isGrounded = false;


        void Awake()
        {
            _input = GetComponent<RobotInput>();
        }
        public void ApplyMovement()
        {
            UpdateGroundHeight();
            UpdateLocomotion();
            UpdateRotation();
        }

        #region Locomotion

        private void UpdateLocomotion()
        {
            Vector3 moveDir = GetCameraRelativeDirection(_input.Movement);
            Vector3 vel = playerRB.linearVelocity;

            // Horizontal velocity only
            Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);

            // Desired horizontal velocity
            Vector3 desiredHorizVel = moveDir * maxMoveSpeed;

            // "How much accel do we need this frame" (in velocity space)
            Vector3 dv = desiredHorizVel - horizVel;
            
            // Turn that into an acceleration toward target (clamped)
            float dirChangeFactor = Vector3.Dot(desiredHorizVel.normalized, horizVel.normalized);
            float accelCap = dirChangeAccelFactor.Evaluate(dirChangeFactor) * moveAccel;
            Vector3 desiredAccel = Vector3.ClampMagnitude(dv * moveAccel/4f, accelCap);

            // Optional: less control in air
            float control = _isGrounded ? groundMoveFactor : airMoveFactor;
            desiredAccel *= control;

            // Apply at a point below COM to create a little pitch/roll torque when accelerating
            Vector3 forcePoint = playerRB.worldCenterOfMass - transform.up * forcePointBelowCOM;

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                playerRB.AddForceAtPosition(desiredAccel, forcePoint, ForceMode.Acceleration);
            }
            else
            {
                // No input: you can optionally apply a "ground friction" decel
                // (otherwise it will coast forever depending on drag)
                Vector3 brakeAccel = Vector3.ClampMagnitude(dv * moveAccel/4f, moveAccel);
                playerRB.AddForce(brakeAccel * control, ForceMode.Acceleration);
            }
        }


        #endregion
        
        #region Ground

        private void UpdateGroundHeight()
        {
            if (!IsGrounded(out RaycastHit hit)) return;
            
            float offset = hit.distance - groundHeight;

            float yVel = playerRB.linearVelocity.y;

            float suspensionForce =
                (-offset * springStrength) -
                (yVel * springDamping);

            playerRB.AddForce(Vector3.up * suspensionForce);
        }

        #endregion
        
        #region Rotation

        private void UpdateRotation()
        {
            // ---------- 1) UPRIGHT SPRING (Pitch/Roll only) ----------
            // currentUp vs worldUp -> axis that would rotate us upright
            Vector3 currentUp = playerRB.transform.up;
            Vector3 worldUp = Vector3.up;

            // Cross gives an axis perpendicular to both (direction to correct tilt)
            Vector3 tiltAxisWorld = Vector3.Cross(currentUp, worldUp);

            // Damping: damp only pitch/roll angular velocity (remove local Y)
            Vector3 angVelLocal = transform.InverseTransformDirection(playerRB.angularVelocity);
            angVelLocal.y = 0f;
            Vector3 tiltAngVelWorld = transform.TransformDirection(angVelLocal);

            Vector3 uprightTorque =
                (tiltAxisWorld * uprightStrength) -
                (tiltAngVelWorld * uprightDamping);

            playerRB.AddTorque(uprightTorque);


            // ---------- 2) YAW CONTROL (Y only, camera-driven) ----------
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;

            if (camForward.sqrMagnitude < 0.0001f)
                return;

            camForward.Normalize();

            // Current forward flattened
            Vector3 forward = playerRB.rotation * Vector3.forward;
            forward.y = 0f;
            forward.Normalize();

            // Signed yaw error (degrees)
            float yawErrorDeg = Vector3.SignedAngle(forward, camForward, Vector3.up);
            float yawErrorRad = yawErrorDeg * Mathf.Deg2Rad;
            float yawVelLocal = transform.InverseTransformDirection(playerRB.angularVelocity).y;

            float yawAccel =
                (yawErrorRad * yawStrength) -
                (yawVelLocal * yawDamping);

            playerRB.AddRelativeTorque(0f, yawAccel, 0f);
        }
        
        #endregion
        
        #region Helpers

        private bool IsGrounded(out RaycastHit hit)
        {
            float dist = _isGrounded ? maxCheckHeight : groundHeight;

            bool groundedNow = Physics.Raycast(
                _groundRayPoint.position,
                Vector3.down,
                out hit,
                dist,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            _isGrounded = groundedNow;
            return groundedNow;
        }
        
        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten
            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y +
                              camRight * input.x;

            return moveDir.normalized;
        }



        #endregion
    }
}
