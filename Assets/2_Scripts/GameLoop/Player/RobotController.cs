using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(RobotInput))]
    public class RobotController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private Transform groundRayPoint;
        [SerializeField] private RobotControllerVisuals visuals;
        [SerializeField] private Vector3 centerOfMassOffset;

        [Header("Grounded Settings")]
        [SerializeField] private float groundHeight = 0.5f;
        [SerializeField] private float maxCheckHeight = 1f;
        [SerializeField] private float springStrength;
        [SerializeField] private float springDamping;

        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength;
        [SerializeField] private float terminalVelocity;

        [Header("Locomotion Settings")]
        [SerializeField] private float moveAccel = 20f;
        [Tooltip("x axis is how much we are changing our direction (-1 is completely changing, 1 is not changing at all)" +
                 "y axis is the multiplier itself relative to the change")]
        [SerializeField] private AnimationCurve dirChangeAccelFactor;
        [SerializeField] private float maxMoveSpeed = 8f;
        [SerializeField] private float forcePointBelowCom = 0.2f;
        [SerializeField] private float groundMoveFactor = 1f;
        [SerializeField] private float airMoveFactor = 0.35f;

        [Header("Slope Settings")]
        [SerializeField] private float maxWalkableSlopeAngle = 45f;
        [SerializeField] private float slopeSlideAccel = 8f;
        [SerializeField] private float maxSlopeSlideSpeed = 6f;
        [SerializeField] private float slopeSlideMoveFactor = 0.1f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float jumpBuffer = 0.2f;

        [Header("Rotation Settings")]
        [SerializeField] private float uprightStrength = 80f;
        [SerializeField] private float uprightDamping = 12f;
        [SerializeField] private float yawStrength = 40f;
        [SerializeField] private float yawDamping = 8f;

        private RobotInput _input;
        private LayerMask _groundLayer;
        private Rigidbody _playerRb;
        private Transform _cameraTransform;

        private bool _isGrounded = false;

        private bool _isJumpAvail = false;
        private bool _isJumping = false;
        private float _jumpTimer = 0f;

        private float _lastGroundDistance;
        private bool _hadGroundHitLastFrame;

        private bool _isSlopeSliding = false;

        /// <summary>
        /// controls how much dv affects the acceleration (bigger = less control)
        /// </summary>
        private float accelControlFactor => moveAccel * 0.25f;

        /// <summary>
        /// controls how much dv affects the gravity (bigger = less control)
        /// </summary>
        private float gravityControlFactor => gravityStrength * 0.25f;
        public Vector3 CenterOfMassOffset => centerOfMassOffset;

        public bool canBuild { get; private set; } = true;
        public bool canShoot { get; private set; } = true;

        public void Initialize(PlayerReferences playerReferences)
        {
            _playerRb = playerReferences.rigidBody;
            _cameraTransform = playerReferences.cameraTransform;
            _groundLayer = playerReferences.groundLayer;

            _input = GetComponent<RobotInput>();
            
            visuals?.Initialize();
        }

        public void OnEnter()
        {
            visuals?.ResetVisuals();

        }

        public void OnExit()
        {
            visuals?.ResetVisuals();
        }

        private void Update()
        {
            JumpBufferTimer();
        }
        
        private void LateUpdate()
        {
            if (visuals == null) return;

            Vector3 moveDirWorld = GetCameraRelativeDirection(_input.Movement);
            bool hasMovementInput = moveDirWorld.sqrMagnitude > 0.0001f;
            visuals?.UpdateSwivelVisual(hasMovementInput ? moveDirWorld : Vector3.zero); //visuals
            visuals?.UpdateWheelRollingVisual(_playerRb.linearVelocity);
        }

        public void ApplyMovement()
        {
            bool isGrounded = IsGrounded(out RaycastHit hit);

            ApplyGravity();
            UpdateJump(isGrounded, hit);
            UpdateGroundHeight(isGrounded, hit);
            UpdateLocomotion(hit);
            UpdateRotation();
        }
        

        #region Locomotion

        private void UpdateLocomotion(RaycastHit hit)
        {
            _isSlopeSliding = false;
            Vector3 moveDir = GetCameraRelativeDirection(_input.Movement);

            if (_isGrounded)
            {
                bool isWalkable = IsSlopeWalkable(hit);
                if (!isWalkable)
                {
                    ApplySlopeSlide(hit);
                    _isSlopeSliding = true;
                }

                moveDir = GetSurfaceAlignedMoveDirection(moveDir, hit);
            }

            float control = _isSlopeSliding ? slopeSlideMoveFactor :
                _isGrounded ? groundMoveFactor : airMoveFactor;

            Vector3 accel = ComputeMoveAcceleration(moveDir, control, hit);

            if (moveDir.sqrMagnitude > 0.0001f)
                ApplyMoveAccelAtOffset(accel);
            else if (!_isSlopeSliding)
                ApplyBrake(control, hit);
        }

        private void ApplyMoveAccelAtOffset(Vector3 accel)
        {
            Vector3 forcePoint = _playerRb.worldCenterOfMass - transform.up * forcePointBelowCom;
            _playerRb.AddForceAtPosition(accel, forcePoint, ForceMode.Acceleration);
        }

        private void ApplyBrake(float control, RaycastHit hit)
        {
            Vector3 vel = _playerRb.linearVelocity;

            Vector3 moveVel = _isGrounded
                ? Vector3.ProjectOnPlane(vel, hit.normal)
                : new Vector3(vel.x, 0f, vel.z);

            Vector3 dv = -moveVel;
            Vector3 brakeAccel = Vector3.ClampMagnitude(dv * accelControlFactor, moveAccel);

            _playerRb.AddForce(brakeAccel * control, ForceMode.Acceleration);
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f) return Vector3.zero;

            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;
            return moveDir.normalized;
        }

        private Vector3 GetSurfaceAlignedMoveDirection(Vector3 moveDir, RaycastHit hit)
        {
            if (!_isGrounded || moveDir.sqrMagnitude < 0.0001f)
                return moveDir;

            Vector3 slopeMoveDir = Vector3.ProjectOnPlane(moveDir, hit.normal);

            if (slopeMoveDir.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return slopeMoveDir.normalized;
        }

        private Vector3 ComputeMoveAcceleration(Vector3 moveDir, float control, RaycastHit hit)
        {
            Vector3 vel = _playerRb.linearVelocity;

            Vector3 currentMoveVel = _isGrounded
                ? Vector3.ProjectOnPlane(vel, hit.normal)
                : new Vector3(vel.x, 0f, vel.z);

            Vector3 desiredMoveVel = moveDir * maxMoveSpeed;
            Vector3 dv = desiredMoveVel - currentMoveVel;

            float dirChangeFactor = 0f;
            if (desiredMoveVel.sqrMagnitude > 0.0001f && currentMoveVel.sqrMagnitude > 0.0001f)
                dirChangeFactor = Vector3.Dot(desiredMoveVel.normalized, currentMoveVel.normalized);

            float accelCap = dirChangeAccelFactor.Evaluate(dirChangeFactor) * moveAccel;

            Vector3 accel = Vector3.ClampMagnitude(dv * accelControlFactor, accelCap);
            return accel * control;
        }

        #endregion

        #region Slope

        private void ApplySlopeSlide(RaycastHit hit)
        {
            Vector3 slideDir = GetSlopeDownDirection(hit);
            if (slideDir.sqrMagnitude < 0.0001f) return;

            float currentSlideSpeed = Vector3.Dot(_playerRb.linearVelocity, slideDir);
            float dv = maxSlopeSlideSpeed - currentSlideSpeed;
            float slideAccel = Mathf.Clamp(dv * accelControlFactor, 0f, slopeSlideAccel);

            _playerRb.AddForce(slideDir * slideAccel, ForceMode.Acceleration);
        }

        private float GetSlopeAngle(RaycastHit hit)
        {
            return Vector3.Angle(hit.normal, Vector3.up);
        }

        private bool IsSlopeWalkable(RaycastHit hit)
        {
            return GetSlopeAngle(hit) <= maxWalkableSlopeAngle;
        }

        private Vector3 GetSlopeDownDirection(RaycastHit hit)
        {
            Vector3 downSlope = Vector3.ProjectOnPlane(Vector3.down, hit.normal);

            if (downSlope.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return downSlope.normalized;
        }

        #endregion

        #region Jumping

        private void UpdateJump(bool isGrounded, RaycastHit hit)
        {
            _isJumping = _isJumping && isGrounded;
            if (!_isJumpAvail || !isGrounded || _isJumping) return;

            float groundOffset = groundHeight - hit.distance;
            Jump(groundOffset);
        }

        private void Jump(float groundOffset = 0f)
        {
            _isJumpAvail = false;
            _isJumping = true;
            _isGrounded = false;

            float targetHeight = jumpHeight + groundOffset;
            float desiredJumpVel = Mathf.Sqrt(2f * gravityStrength * targetHeight);

            float currentYVel = _playerRb.linearVelocity.y;
            float deltaV = desiredJumpVel - currentYVel;

            if (deltaV <= 0f)
                return;

            Vector3 forcePoint = _playerRb.worldCenterOfMass - transform.up * forcePointBelowCom;
            _playerRb.AddForceAtPosition(Vector3.up * deltaV, forcePoint, ForceMode.VelocityChange);
        }

        private void JumpBufferTimer()
        {
            if (_input.JumpPressed)
            {
                _isJumpAvail = true;
                _jumpTimer = Time.time + jumpBuffer;
            }

            if (_jumpTimer <= Time.time)
                _isJumpAvail = false;
        }

        #endregion

        #region Gravity

        private void ApplyGravity()
        {
            if (_isGrounded) return;

            float verticalVel = _playerRb.linearVelocity.y;
            float desiredVerticalVel = -terminalVelocity;
            float dv = desiredVerticalVel - verticalVel;

            float gravityForce = Mathf.Clamp(dv * gravityControlFactor, -gravityStrength, Mathf.Infinity);
            _playerRb.AddForce(Vector3.up * gravityForce, ForceMode.Acceleration);
        }

        #endregion

        #region Ground

        private void UpdateGroundHeight(bool isGrounded, RaycastHit hit)
        {
            float offset = hit.distance - groundHeight;
            visuals?.UpdateSuspensionVisual(isGrounded, offset);             //visuals
            
            if (!isGrounded || _isJumping)
            {
                _hadGroundHitLastFrame = false;
                return;
            }
            
            float springVel = 0f;
            if (_hadGroundHitLastFrame)
                springVel = (hit.distance - _lastGroundDistance) / Time.fixedDeltaTime;

            float suspensionAccel = (-offset * springStrength) - (springVel * springDamping);
            _playerRb.AddForce(Vector3.up * suspensionAccel, ForceMode.Acceleration);

            _lastGroundDistance = hit.distance;
            _hadGroundHitLastFrame = true;
        }

        private bool IsGrounded(out RaycastHit hit)
        {
            float dist = _isGrounded ? maxCheckHeight : groundHeight;

            bool groundedNow = Physics.Raycast(
                groundRayPoint.position,
                Vector3.down,
                out hit,
                dist,
                _groundLayer,
                QueryTriggerInteraction.Ignore
            );

            _isGrounded = groundedNow;
            return groundedNow;
        }

        #endregion

        #region Rotation

        private void UpdateRotation()
        {
            ApplyUprightSpringTorque();
            ApplyCameraYawTorqueIfValid();
        }

        private void ApplyUprightSpringTorque()
        {
            Vector3 tiltAxisWorld = GetUprightCorrectionAxisWorld();
            Vector3 tiltAngVelWorld = GetTiltAngularVelocityWorld();

            Vector3 uprightTorque = (tiltAxisWorld * uprightStrength) - (tiltAngVelWorld * uprightDamping);
            _playerRb.AddTorque(uprightTorque, ForceMode.Acceleration);
        }

        private Vector3 GetUprightCorrectionAxisWorld()
        {
            Vector3 currentUp = _playerRb.transform.up;
            return Vector3.Cross(currentUp, Vector3.up);
        }

        private Vector3 GetTiltAngularVelocityWorld()
        {
            Vector3 angVelLocal = transform.InverseTransformDirection(_playerRb.angularVelocity);
            angVelLocal.y = 0f;
            return transform.TransformDirection(angVelLocal);
        }

        private void ApplyCameraYawTorqueIfValid()
        {
            if (!TryGetFlattenedCameraForward(out Vector3 camForwardFlat))
                return;

            Vector3 forwardFlat = GetFlattenedForwardWorld();
            float yawErrorRad = GetSignedYawErrorRadians(forwardFlat, camForwardFlat);

            float yawVelLocal = GetLocalYawAngularVelocity();
            float yawAccel = (yawErrorRad * yawStrength) - (yawVelLocal * yawDamping);

            _playerRb.AddRelativeTorque(0f, yawAccel, 0f, ForceMode.Acceleration);
            
        }

        private bool TryGetFlattenedCameraForward(out Vector3 camForwardFlat)
        {
            camForwardFlat = _cameraTransform.forward;
            camForwardFlat.y = 0f;

            if (camForwardFlat.sqrMagnitude < 0.0001f)
                return false;

            camForwardFlat.Normalize();
            return true;
        }

        private Vector3 GetFlattenedForwardWorld()
        {
            Vector3 forward = _playerRb.transform.forward;
            forward.y = 0f;

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
            return transform.InverseTransformDirection(_playerRb.angularVelocity).y;
        }

        #endregion
    }
}