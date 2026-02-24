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
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float jumpBuffer = 0.2f;
        
        [Header("Rotation Settings")]
        [SerializeField] private float uprightStrength = 80f;
        [SerializeField] private float uprightDamping = 12f;
        [SerializeField] private float yawStrength = 40f;
        [SerializeField] private float yawDamping = 8f;
        
        private bool _isGrounded = false;
        private bool _isJumpAvail = false;
        private bool _isJumping = false;
        private float _jumpTimer = 0f;
        
        /// <summary>
        /// controls how much dv affects the acceleration (bigger = less control)
        /// </summary>
        private float accelControlFactor => moveAccel * 0.25f;


        void Awake()
        {
            _input = GetComponent<RobotInput>();
        }

        void Update()
        {
            JumpBufferTimer();
        }
        public void ApplyMovement()
        {
            UpdateJump();
            UpdateGroundHeight();
            UpdateLocomotion();
            UpdateRotation();
        }
        

        #region Locomotion

        private void UpdateLocomotion()
        {
            Vector3 moveDir = GetCameraRelativeDirection(_input.Movement);
            float control = _isGrounded ? groundMoveFactor : airMoveFactor;

            Vector3 desiredAccel = ComputeMoveAcceleration(moveDir, control);

            if (moveDir.sqrMagnitude > 0.0001f)
                ApplyMoveAccelAtOffset(desiredAccel);
            else
                ApplyBrake(control);
        }
        
        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f) return Vector3.zero;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten
            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;

            return moveDir.normalized;
        }

        private Vector3 ComputeMoveAcceleration(Vector3 moveDir, float control)
        {
            Vector3 vel = playerRB.linearVelocity;

            Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
            Vector3 desiredHorizVel = moveDir * maxMoveSpeed;

            Vector3 dv = desiredHorizVel - horizVel;

            float dirChangeFactor = Vector3.Dot(desiredHorizVel.normalized, horizVel.normalized);
            float accelCap = dirChangeAccelFactor.Evaluate(dirChangeFactor) * moveAccel;

            Vector3 accel = Vector3.ClampMagnitude(dv * accelControlFactor, accelCap);
            return accel * control;
        }

        private void ApplyMoveAccelAtOffset(Vector3 accel)
        {
            Vector3 forcePoint = playerRB.worldCenterOfMass - transform.up * forcePointBelowCOM;
            playerRB.AddForceAtPosition(accel, forcePoint, ForceMode.Acceleration);
        }

        private void ApplyBrake(float control)
        {
            Vector3 vel = playerRB.linearVelocity;

            Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
            Vector3 dv = -horizVel;

            Vector3 brakeAccel = Vector3.ClampMagnitude(dv * accelControlFactor, moveAccel);
            playerRB.AddForce(brakeAccel * control, ForceMode.Acceleration);
        }
        
        #endregion
        
        #region Jumping

        private void UpdateJump()
        {
            bool isGrounded = IsGrounded(out RaycastHit hit);
            _isJumping = _isJumping && isGrounded;
            if(!_isJumpAvail || !isGrounded || _isJumping) return;
            
            float groundOffset = groundHeight - hit.distance;
           Jump(groundOffset);
        }

        private void Jump(float groundOffset = 0)
        {
            _isJumpAvail = false;
            _isJumping = true;
            _isGrounded = false;

            float gravity = Mathf.Abs(Physics.gravity.y);
            float targetHeight = jumpHeight + groundOffset;

            float desiredJumpVel = Mathf.Sqrt(2f * gravity * targetHeight);

            float currentYVel = playerRB.linearVelocity.y;
            float deltaV = desiredJumpVel - currentYVel;

            if (deltaV <= 0f)
                return;

            Vector3 forcePoint = playerRB.worldCenterOfMass - transform.up * forcePointBelowCOM;
            playerRB.AddForceAtPosition(Vector3.up * deltaV, forcePoint, ForceMode.VelocityChange);
        }

        //called in update
        private void JumpBufferTimer()
        {
            if (_input.JumpPressed)
            {
                _isJumpAvail = true;
                _jumpTimer = Time.time + jumpBuffer;
            }
            
            if(_jumpTimer <= Time.time) { _isJumpAvail = false; }

        }
        
        #endregion
        
        #region Ground

        private void UpdateGroundHeight()
        {
            if (!IsGrounded(out RaycastHit hit) || _isJumping) return;
            
            float offset = hit.distance - groundHeight;

            float yVel = playerRB.linearVelocity.y;

            float gravityComp = -Physics.gravity.y;
            float suspensionAccel = gravityComp + (-offset * springStrength) - (yVel * springDamping);

            playerRB.AddForce(Vector3.up * suspensionAccel, ForceMode.Acceleration);
            
            print(hit.distance);
        }
        
        //helpers
        
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

        #endregion
        
        #region Rotation

        private void UpdateRotation()
        {
            ApplyUprightSpringTorque();                 // Pitch/Roll
            ApplyCameraYawTorqueIfValid();              // Yaw only
        }

        #region  UPRIGHT SPRING (Pitch/Roll only)

        private void ApplyUprightSpringTorque()
        {
            Vector3 tiltAxisWorld = GetUprightCorrectionAxisWorld();
            Vector3 tiltAngVelWorld = GetTiltAngularVelocityWorld(); // pitch/roll only

            Vector3 uprightTorque = (tiltAxisWorld * uprightStrength) - (tiltAngVelWorld * uprightDamping);

            playerRB.AddTorque(uprightTorque, ForceMode.Acceleration);
        }

        private Vector3 GetUprightCorrectionAxisWorld()
        {
            // Axis that would rotate current up toward world up
            Vector3 currentUp = playerRB.transform.up;
            return Vector3.Cross(currentUp, Vector3.up);
        }

        private Vector3 GetTiltAngularVelocityWorld()
        {
            // Remove yaw component in local space, then return to world for damping torque
            Vector3 angVelLocal = transform.InverseTransformDirection(playerRB.angularVelocity);
            angVelLocal.y = 0f;
            return transform.TransformDirection(angVelLocal);
        }

        #endregion
        
        #region YAW CONTROL (Y only, camera-driven)

        private void ApplyCameraYawTorqueIfValid()
        {
            if (!TryGetFlattenedCameraForward(out Vector3 camForwardFlat))
                return;

            Vector3 forwardFlat = GetFlattenedForwardWorld();
            float yawErrorRad = GetSignedYawErrorRadians(forwardFlat, camForwardFlat);

            float yawVelLocal = GetLocalYawAngularVelocity();
            float yawAccel = (yawErrorRad * yawStrength) - (yawVelLocal * yawDamping);

            playerRB.AddRelativeTorque(0f, yawAccel, 0f, ForceMode.Acceleration);
        }

        private bool TryGetFlattenedCameraForward(out Vector3 camForwardFlat)
        {
            camForwardFlat = cameraTransform.forward;
            camForwardFlat.y = 0f;

            if (camForwardFlat.sqrMagnitude < 0.0001f)
                return false;

            camForwardFlat.Normalize();
            return true;
        }

        private Vector3 GetFlattenedForwardWorld()
        {
            Vector3 forward = playerRB.transform.forward;
            forward.y = 0f;

            // If we're basically vertical for some reason, avoid NaN normalize
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            forward.Normalize();
            return forward;
        }

        private float GetSignedYawErrorRadians(Vector3 currentForwardFlat, Vector3 desiredForwardFlat)
        {
            float yawErrorDeg = Vector3.SignedAngle(currentForwardFlat, desiredForwardFlat, Vector3.up);
            return yawErrorDeg * Mathf.Deg2Rad;
        }

        private float GetLocalYawAngularVelocity()
        {
            return transform.InverseTransformDirection(playerRB.angularVelocity).y;
        }
        
        #endregion

        #endregion
    }
}
