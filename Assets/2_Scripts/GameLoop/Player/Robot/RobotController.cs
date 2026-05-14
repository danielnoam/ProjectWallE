using System;
using _2_Scripts;
using DNExtensions.Utilities.Inline;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(RobotInput))]
    public class RobotController : MonoBehaviour, IPlayerController
    {
        [SerializeField, Inline] private SORobotControllerSettings settings;
        [Space(10)]
        [SerializeField] private Transform groundRayPoint;
        [SerializeField] private Vector3 centerOfMassOffset;
        [Space(10)]
        [SerializeField] private RobotControllerVisuals visuals;

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

        public event Action OnJumped;

        /// <summary>
        /// controls how much dv affects the acceleration (bigger = less control)
        /// </summary>
        private float accelControlFactor => settings.MoveAccel * 0.25f;

        /// <summary>
        /// controls how much dv affects the gravity (bigger = less control)
        /// </summary>
        private float gravityControlFactor => settings.GravityStrength * 0.25f;

        public Vector3 CenterOfMassOffset => centerOfMassOffset;

        public bool BuildEnabled { get; private set; } = true;
        public bool ShootEnabled { get; private set; } = true;
        public bool SupportEnabled { get; } =  true;

        public void Initialize(PlayerReferences playerReferences)
        {
            if (settings == null)
            {
                Debug.LogError("RobotController: Missing settings!", this);
                enabled = false;
                return;
            }

            _playerRb = playerReferences.rigidBody;
            _cameraTransform = playerReferences.cameraTransform;
            _groundLayer = playerReferences.groundLayer;

            _input = GetComponent<RobotInput>();
            
            visuals?.Initialize();
        }

        public void OnEnter()
        {
        }

        public void OnExit()
        {
            visuals?.ResetVisuals();
        }

        public void ApplyFixedUpdate()
        {
            bool isGrounded = IsGrounded(out RaycastHit hit);

            ApplyGravity();
            UpdateJump(isGrounded, hit);
            UpdateGroundHeight(isGrounded, hit);
            UpdateLocomotion(hit);
            UpdateRotation();
        }

        public void ApplyUpdate()
        {
            JumpBufferTimer();
        }

        public void ApplyLateUpdate()
        {
            if (visuals == null) return;

            Vector3 moveDirWorld = GetCameraRelativeDirection(_input.Movement);
            bool hasMovementInput = moveDirWorld.sqrMagnitude > 0.0001f;
            visuals?.UpdateSwivelVisual(hasMovementInput ? moveDirWorld : Vector3.zero);
            visuals?.UpdateWheelRollingVisual(_playerRb.linearVelocity);
        }

        public void ApplyLateFixedUpdate()
        {
            
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

            float control = _isSlopeSliding ? settings.SlopeSlideMoveFactor :
                _isGrounded ? settings.GroundMoveFactor : settings.AirMoveFactor;

            Vector3 accel = ComputeMoveAcceleration(moveDir, control, hit);

            if (moveDir.sqrMagnitude > 0.0001f)
                ApplyMoveAccelAtOffset(accel);
            else if (!_isSlopeSliding)
                ApplyBrake(control, hit);
        }

        private void ApplyMoveAccelAtOffset(Vector3 accel)
        {
            Vector3 forcePoint = _playerRb.worldCenterOfMass - transform.up * settings.ForcePointBelowCom;
            _playerRb.AddForceAtPosition(accel, forcePoint, ForceMode.Acceleration);
        }

        private void ApplyBrake(float control, RaycastHit hit)
        {
            Vector3 vel = _playerRb.linearVelocity;

            Vector3 moveVel = _isGrounded
                ? Vector3.ProjectOnPlane(vel, hit.normal)
                : new Vector3(vel.x, 0f, vel.z);

            Vector3 dv = -moveVel;
            Vector3 brakeAccel = Vector3.ClampMagnitude(dv * accelControlFactor, settings.MoveAccel);

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

            Vector3 desiredMoveVel = moveDir * (settings.MaxMoveSpeed * _input.Movement.magnitude);
            Vector3 dv = desiredMoveVel - currentMoveVel;

            float dirChangeFactor = 0f;
            if (desiredMoveVel.sqrMagnitude > 0.0001f && currentMoveVel.sqrMagnitude > 0.0001f)
                dirChangeFactor = Vector3.Dot(desiredMoveVel.normalized, currentMoveVel.normalized);

            float accelCap = settings.DirChangeAccelFactor.Evaluate(dirChangeFactor) * settings.MoveAccel;

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
            float dv = settings.MaxSlopeSlideSpeed - currentSlideSpeed;
            float slideAccel = Mathf.Clamp(dv * accelControlFactor, 0f, settings.SlopeSlideAccel);

            _playerRb.AddForce(slideDir * slideAccel, ForceMode.Acceleration);
        }

        private float GetSlopeAngle(RaycastHit hit)
        {
            return Vector3.Angle(hit.normal, Vector3.up);
        }

        private bool IsSlopeWalkable(RaycastHit hit)
        {
            return GetSlopeAngle(hit) <= settings.MaxWalkableSlopeAngle;
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

            float groundOffset = settings.GroundHeight - hit.distance;
            Jump(groundOffset);
        }

        private void Jump(float groundOffset = 0f)
        {
            _isJumpAvail = false;
            _isJumping = true;
            _isGrounded = false;

            float targetHeight = settings.JumpHeight + groundOffset;
            float desiredJumpVel = Mathf.Sqrt(2f * settings.GravityStrength * targetHeight);

            float currentYVel = _playerRb.linearVelocity.y;
            float deltaV = desiredJumpVel - currentYVel;

            if (deltaV <= 0f)
                return;

            Vector3 forcePoint = _playerRb.worldCenterOfMass - transform.up * settings.ForcePointBelowCom;
            _playerRb.AddForceAtPosition(Vector3.up * deltaV, forcePoint, ForceMode.VelocityChange);
            OnJumped?.Invoke();
        }

        private void JumpBufferTimer()
        {
            if (_input.JumpPressed)
            {
                _isJumpAvail = true;
                _jumpTimer = Time.time + settings.JumpBuffer;
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
            float desiredVerticalVel = -settings.TerminalVelocity;
            float dv = desiredVerticalVel - verticalVel;

            float gravityForce = Mathf.Clamp(dv * gravityControlFactor, -settings.GravityStrength, Mathf.Infinity);
            _playerRb.AddForce(Vector3.up * gravityForce, ForceMode.Acceleration);
        }

        #endregion

        #region Ground

        private void UpdateGroundHeight(bool isGrounded, RaycastHit hit)
        {
            float offset = hit.distance - settings.GroundHeight;
            visuals?.UpdateSuspensionVisual(isGrounded, offset);
            
            if (!isGrounded || _isJumping)
            {
                _hadGroundHitLastFrame = false;
                return;
            }
            
            float springVel = 0f;
            if (_hadGroundHitLastFrame)
                springVel = (hit.distance - _lastGroundDistance) / Time.fixedDeltaTime;

            float suspensionAccel = (-offset * settings.SpringStrength) - (springVel * settings.SpringDamping);
            _playerRb.AddForce(Vector3.up * suspensionAccel, ForceMode.Acceleration);

            _lastGroundDistance = hit.distance;
            _hadGroundHitLastFrame = true;
        }

        private bool IsGrounded(out RaycastHit hit)
        {
            float dist = _isGrounded ? settings.MaxCheckHeight : settings.GroundHeight;

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
        
        public bool IsGrounded()
        {
            return _isGrounded;
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

            Vector3 uprightTorque = (tiltAxisWorld * settings.UprightStrength) - (tiltAngVelWorld * settings.UprightDamping);
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
            float yawAccel = (yawErrorRad * settings.YawStrength) - (yawVelLocal * settings.YawDamping);

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