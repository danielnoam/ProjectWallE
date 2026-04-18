using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Inline;
using ProjectWallE;
using UnityEngine;

namespace _2_Scripts
{
    public enum CarLongitudinalState
    {
        Idle,
        Forward,
        Backward,
    }

    [RequireComponent(typeof(CarInput), typeof(CarBoost))]
    public class CarController : MonoBehaviour, IPlayerController
    {
        [SerializeField, Inline] private SOCarControllerSettings settings;
        [Space(10)]
        [Tooltip("Make sure this array and the corresponding steeringTires array in CarControllerVisuals are in the same order")]
        [SerializeField] private Tire[] steeringTires;
        [Tooltip("Make sure this array and the corresponding staticTires array in CarControllerVisuals are in the same order")]
        [SerializeField] private Tire[] staticTires;
        [SerializeField] private Transform boostPoint;
        [SerializeField] private Vector3 centerOfMassOffset;
        [Space(10)]
        [SerializeField] private CarControllerVisuals visuals;
        
        [HideInInspector] [SerializeField] private CarBoost carBoost;

        private CarInput _carInput;
        private LayerMask _groundLayer;
        private Rigidbody _playerRb;

        private List<Tire> _allTires;

        private const float ExtendedGroundCheckExtraDistance = 0.1f;
        
        private Vector3 _groundPlaneNormal;
        
        private CarLongitudinalState _longitudinalState;

        private bool _wasBraking;
        private float _currentSteering;
        private float _groundedRatio;
        private float _airborneFactor;
        private float _handbrake01;
        private float _slippingFactor;

        /// <summary>
        /// controls how much dv affects the gravity (bigger = less control)
        /// </summary>
        private float GravityControlFactor => settings.GravityStrength * 0.25f;

        public CarBoost CarBoost => carBoost;
        public Vector3 CenterOfMassOffset => centerOfMassOffset;
        public bool canBuild { get; private set; } = true;
        public bool canShoot { get; private set; } = false;

        public event Action OnBrakeStarted;

        private void OnValidate()
        {
            if (carBoost == null)
                carBoost = GetComponent<CarBoost>();
        }

        public void Initialize(PlayerReferences playerReferences)
        {
            if (settings == null)
            {
                Debug.LogError("CarController: Missing settings!", this);
                enabled = false;
                return;
            }

            _playerRb = playerReferences.rigidBody;
            _groundLayer = playerReferences.groundLayer;

            _carInput = GetComponent<CarInput>();
            _allTires = GetAllTires();
            visuals.GetAllTireVisuals();

            foreach (var tire in _allTires)
                tire.Initialize();

            visuals.Initialize(transform);
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

            ApplyGravity();
            ApplySuspensionPerTiresType(_allTires);
            ApplyTireRotation();
            ApplyTireFriction(_groundPlaneNormal);
            ApplyAirControl();
            ApplyLongitudinalMovement();
        }

        public void ApplyUpdate()
        {
            UpdateTiresGroundState(out Vector3 planeNormal);
            _groundPlaneNormal = planeNormal;
        }
        
        public void ApplyLateUpdate()
        {
            
        }

        public void ApplyLateFixedUpdate()
        {
            StopJittering();
        }

        #region Suspension

        private void ApplySuspensionPerTiresType(List<Tire> tires)
        {
            int i = 0;
            foreach (var tire in tires)
            {
                if (tire.isGroundedExact)
                {
                    RaycastHit hit = tire.exactGroundHit;

                    float offset = hit.distance - settings.GroundHeight;
                    Vector3 springDir = tire.tireTransform.up;

                    Vector3 pointVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
                    float velAlongSpring = Vector3.Dot(pointVel, springDir);

                    float suspensionForce =
                        (-offset * settings.SuspensionStrength) -
                        (velAlongSpring * settings.SuspensionDamping);
                    tire.normalForceMag = Mathf.Max(0f, suspensionForce);
                    
                    
                    _playerRb.AddForceAtPosition(
                        springDir * (suspensionForce * _playerRb.mass),
                        tire.tireTransform.position,
                        ForceMode.Force);

                    visuals.UpdateTireSuspensionVisuals(true, i, offset);
                }
                else
                {
                    visuals.UpdateTireSuspensionVisuals(false, i, 0f);
                    tire.normalForceMag = 0f;
                }

                i++;
            }
        }

        #endregion

        #region Steering

        private void ApplyTireRotation()
        {
            float speed01 = Mathf.Clamp01(
                Mathf.Abs(Vector3.Dot(transform.forward, _playerRb.linearVelocity)) / settings.TopForwardSpeed);

            float desiredSteeringAngle = Mathf.Lerp(
                settings.SteeringAngle,
                settings.TopSpeedSteeringFactor * settings.SteeringAngle,
                speed01);

            float target = _carInput.Steering * desiredSteeringAngle;

            _currentSteering = Mathf.Lerp(
                _currentSteering,
                target,
                1f - Mathf.Exp(-settings.SteeringStrength * Time.fixedDeltaTime)
            );

            Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);

            foreach (var tire in steeringTires)
                tire.tireTransform.localRotation = rot;

            visuals.SteerWheels(_currentSteering);
        }

        private void ApplyTireFriction(Vector3 planeNormal)
        {
            ApplyTireFrictionModifiers(planeNormal);

            ApplyFrictionPerTiresType(steeringTires, settings.SteeringTiresFrictionCurve);
            ApplyFrictionPerTiresType(staticTires, settings.StaticTiresFrictionCurve);
        }

        private void ApplyTireFrictionModifiers(Vector3 planeNormal)
        {
            _groundedRatio = GetGroundedRatio(out _);

            float planted = Mathf.Pow(_groundedRatio, settings.PartialGroundGripPower);
            _airborneFactor = Mathf.Lerp(settings.MinGripWhenPartialGround, 1f, planted);

            if (!_wasBraking && _carInput.HandBreakHeld)
            {
                OnBrakeStarted?.Invoke();
            }
            _wasBraking = _carInput.HandBreakHeld;
            
            float targetHb = _carInput.HandBreakHeld ? 1f : 0f;
            float rate = targetHb > _handbrake01 ? settings.HandbrakeBlendIn : settings.HandbrakeBlendOut;
            _handbrake01 = Mathf.Lerp(_handbrake01, targetHb, rate * Time.fixedDeltaTime);

            Vector3 planarVelocity = Vector3.ProjectOnPlane(_playerRb.linearVelocity, planeNormal);
            float slippingSpeed = Vector3.Dot(transform.right, planarVelocity);
            float absSlipSpeed = Mathf.Abs(slippingSpeed);
            float t1 = Mathf.InverseLerp(settings.MinSlippingSpeed, settings.MaxSlippingSpeed, absSlipSpeed);
             _slippingFactor = Mathf.Lerp(1f, settings.MinGripAtMaxSlip, t1);
        }

        private void ApplyFrictionPerTiresType(Tire[] tires, AnimationCurve frictionCurve)
        {
            foreach (Tire tire in tires)
            {
                if (!tire.isGroundedExtended) continue;

                Vector3 tireVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
                float steeringVel = Vector3.Dot(tire.tireTransform.right, tireVel);

                float gripFactor = CalcCurrentTireGripFactor(tire, frictionCurve, tireVel);
                gripFactor *= _airborneFactor;
                gripFactor *= _slippingFactor;
                gripFactor = Mathf.Lerp(gripFactor, settings.HandBrakeGrip, _handbrake01);
                
                float desiredVelChange = -steeringVel * gripFactor;
                float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                if (!_carInput.HandBreakHeld)
                {
                    float accelSign = Mathf.Sign(-steeringVel);
                    float exactStopAccel = Mathf.Abs(steeringVel) / Time.fixedDeltaTime;
                    float accelMagnitude = Mathf.Max(Mathf.Abs(desiredAccel), settings.MinLateralFrictionAccel);
                    accelMagnitude = Mathf.Min(accelMagnitude, exactStopAccel);

                    desiredAccel = accelSign * accelMagnitude;
                }

                _playerRb.AddForceAtPosition(
                    tire.tireTransform.right * (_playerRb.mass / _allTires.Count * desiredAccel),
                    tire.tireTransform.position,
                    ForceMode.Force
                );
            }
        }

        private float CalcCurrentTireGripFactor(Tire tire, AnimationCurve frictionCurve, Vector3 tireVel)
        {
            Vector3 carVel = Vector3.ProjectOnPlane(_playerRb.linearVelocity, _groundPlaneNormal);
            if (carVel.magnitude < 0.3f)
                return frictionCurve.Evaluate(0f);
            
            Vector3 tireVelAlongGround = Vector3.ProjectOnPlane(tireVel, tire.extendedGroundHit.normal);
            float slippingAmount = Mathf.Clamp(
                Vector3.Dot(tire.tireTransform.right, tireVelAlongGround.normalized),
                -1f,
                1f);

            float gripFactor = frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
            gripFactor = Mathf.Clamp(gripFactor * GetNormalForceGripFactor(tire, tireVel.magnitude), frictionCurve.Evaluate(1f), 1);
            return gripFactor;
        }

        private float GetNormalForceGripFactor(Tire tire, float tireVelMag)
        {
            float normalizedNormalForce = tire.normalForceMag / settings.GravityStrength * _allTires.Count;
            float factor = 1f;
            
            if (settings.UnaffectedNormalForceRange.Contains(normalizedNormalForce))
                factor = 1f;
            else if (normalizedNormalForce < settings.UnaffectedNormalForceRange.minValue)
            {
                float t = Mathf.InverseLerp(0, settings.UnaffectedNormalForceRange.minValue, normalizedNormalForce);
                factor = Mathf.Lerp(settings.GripByNormalForceFactor.minValue, 1f, t);
            }
            else if (normalizedNormalForce > settings.UnaffectedNormalForceRange.maxValue)
            {
                float t = Mathf.InverseLerp(settings.UnaffectedNormalForceRange.maxValue, 2f, normalizedNormalForce);
                factor = Mathf.Lerp(1f, settings.GripByNormalForceFactor.maxValue, t);
            }
            return factor;
        }

        #endregion

        #region Longitudinal Movement

        private void ApplyLongitudinalMovement()
        {
            float carSpeed = Vector3.Dot(transform.forward, _playerRb.linearVelocity);
            
            UpdateLongitudinalState();
            GetLongitudinalValues(out float topForwardSpeed, out float topBackwardSpeed,
                                  out float accelForce, out float brakeForce);

            if (_carInput.Acceleration > 0 || _carInput.BoostHeld)
                ApplyForwardAcceleration(carSpeed, topForwardSpeed, accelForce, brakeForce);
            else if (_carInput.Acceleration < 0)
                ApplyBackwardsAcceleration(carSpeed, topBackwardSpeed, accelForce, brakeForce);
            else
                ApplyEngineBreaking(carSpeed);

            if (CarBoost.CanBoost(out float boostAccel, out float boostSpeedFactor))
                ApplyBoost(carSpeed, boostAccel, boostSpeedFactor);

            for (int i = 0; i < _allTires.Count; i++)
                visuals.RotateWheels(carSpeed, i);
        }
        
        private void ApplyForwardAcceleration(float carSpeed, float topSpeed, float accelForce, float brakeForce)
        {
            if (carSpeed > topSpeed) return;
            
            foreach (var tire in _allTires)
            {
                if(!tire.isGroundedExtended) continue;
                
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);

                float availableAcceleration = carSpeed >= 0
                    ? settings.AccelerationCurve.Evaluate(normalizedSpeed) * accelForce
                    : brakeForce;
                
                Vector3 accelDir = Vector3.ProjectOnPlane(tire.tireTransform.forward, tire.extendedGroundHit.normal);

                _playerRb.AddForceAtPosition(
                    accelDir * (availableAcceleration * _playerRb.mass / _allTires.Count),
                    tire.tireTransform.position);
            }
        }

        private void ApplyBackwardsAcceleration(float carSpeed, float topSpeed, float accelForce, float brakeForce)
        {
            if (carSpeed < -topSpeed) return;

            foreach (var tire in steeringTires)
            {
                if (!tire.isGroundedExtended) continue;

                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);

                float availableAcceleration = carSpeed <= 0
                    ? settings.AccelerationCurve.Evaluate(normalizedSpeed) * accelForce
                    : brakeForce;
                
                Vector3 accelDir = Vector3.ProjectOnPlane(-tire.tireTransform.forward, tire.extendedGroundHit.normal);

                _playerRb.AddForceAtPosition(
                    accelDir * (availableAcceleration * _playerRb.mass / steeringTires.Length),
                    tire.tireTransform.position);
            }
        }

        private void ApplyEngineBreaking(float carSpeed)
        {
            foreach (var tire in _allTires)
            {
                if (!tire.isGroundedExtended) continue;

                Vector3 accelDir = Vector3.ProjectOnPlane(-tire.tireTransform.forward, tire.extendedGroundHit.normal);
                
                _playerRb.AddForceAtPosition(
                    accelDir * (Mathf.Sign(carSpeed) * settings.EngineBrakeStrength * _playerRb.mass / _allTires.Count),
                    tire.tireTransform.position);
            }
        }

        private void ApplyBoost(float carSpeed, float boostAccel, float boostSpeedFactor)
        {
            if (carSpeed > settings.TopForwardSpeed * boostSpeedFactor) return;

            _playerRb.AddForce(transform.forward * (boostAccel * _playerRb.mass));
        }
        
        private void GetLongitudinalValues(out float topForwardSpeed, out float topBackwardSpeed,
            out float accelForce, out float brakeForce)
        {
            bool isSpeedyLayer = IsOnSpeedyLayer();

            float speedMultiplier = isSpeedyLayer ? settings.SpeedySpeedMultiplier : 1f;
            float accelMultiplier = isSpeedyLayer ? settings.SpeedyAccelMultiplier : GetAccelAngleStrength();

            topForwardSpeed = settings.TopForwardSpeed * speedMultiplier;
            topBackwardSpeed = settings.TopBackwardSpeed * speedMultiplier;
            accelForce = settings.AccelerationStrength * accelMultiplier;
            brakeForce = settings.BrakeStrength * accelMultiplier;
        }

        private bool IsOnSpeedyLayer()
        {
            bool isSpeedyLayer = false;
            foreach (Tire tire in _allTires)
            {
                if (tire.hitLayer != settings.SpeedyLayer)
                {
                    isSpeedyLayer = false;
                    continue;
                }
                isSpeedyLayer = true;
            }
            return isSpeedyLayer;
        }

        private float GetAccelAngleStrength()
        {
            float moveDir = _longitudinalState is CarLongitudinalState.Backward ?
                -1f : 1f;
            
            float carAccelAngle = 90 - Vector3.Angle(moveDir * transform.forward, Vector3.up);
            float strength = 1f;
            
            if(settings.AccelAngleRange.Contains(carAccelAngle))
                strength = Mathf.InverseLerp(settings.AccelAngleRange.maxValue, settings.AccelAngleRange.minValue, carAccelAngle);
            else if (carAccelAngle < settings.AccelAngleRange.minValue)
                strength = 1;
            else if (carAccelAngle > settings.AccelAngleRange.maxValue)
                strength = 0;
            return strength;
        }

        private void UpdateLongitudinalState()
        {
            switch (_carInput.Acceleration)
            {
                case > 0.01f: _longitudinalState = CarLongitudinalState.Forward; break;
                case < -0.01f: _longitudinalState = CarLongitudinalState.Backward; break;
                default: _longitudinalState = CarLongitudinalState.Idle; break;
            }
        }

        #endregion

        #region Gravity

        private void ApplyGravity()
        {
            float verticalVel = _playerRb.linearVelocity.y;
            float desiredVerticalVel = -settings.TerminalVelocity;

            float dv = desiredVerticalVel - verticalVel;
            float gravityForce = Mathf.Clamp(dv * GravityControlFactor, -settings.GravityStrength, Mathf.Infinity);

            _playerRb.linearVelocity += Vector3.up * (gravityForce * Time.fixedDeltaTime);
        }

        #endregion

        #region Air Control

        private void ApplyAirControl()
        {
            if (IsCarGrounded()) return;

            AirAlignTorque();
            AirSteeringTorque();
        }

        private void AirSteeringTorque()
        {
            float input = _carInput.Steering;

            if (Mathf.Abs(input) < 0.001f) return;

            float targetYawRate = input * settings.MaxAirSteeringVelocity;
            float currentYawRate = Vector3.Dot(_playerRb.angularVelocity, transform.up);
            float yawRateError = targetYawRate - currentYawRate;
            float yawAccelCmd = yawRateError * settings.AirSteeringStrength;

            _playerRb.AddTorque(transform.up * yawAccelCmd, ForceMode.Acceleration);
        }

        private void AirAlignTorque()
        {
            Quaternion toUpright = Quaternion.FromToRotation(transform.up, Vector3.up);

            toUpright.ToAngleAxis(out float angleDeg, out Vector3 axisWorld);
            if (angleDeg > 180f) angleDeg -= 360f;

            Vector3 errorWorld = axisWorld * (angleDeg * Mathf.Deg2Rad);
            Vector3 errorLocal = transform.InverseTransformDirection(errorWorld);
            errorLocal.y = 0f;

            Vector3 angVelLocal = transform.InverseTransformDirection(_playerRb.angularVelocity);
            angVelLocal.y = 0f;

            Vector3 torqueLocal =
                (errorLocal * settings.AirAlignmentStrength) -
                (angVelLocal * settings.AirAlignmentDamping);

            Vector3 torqueWorld = transform.TransformDirection(torqueLocal);
            _playerRb.AddTorque(torqueWorld, ForceMode.Acceleration);
        }

        #endregion

        #region Helpers

        private void UpdateTiresGroundState(out Vector3 planeNormal)
        {
            float exactDistance = settings.GroundHeight;
            float extendedDistance = settings.GroundHeight + ExtendedGroundCheckExtraDistance;

            Vector3 normalSum = Vector3.zero;
            int groundedCount = 0;

            foreach (var tire in _allTires)
            {
                Vector3 origin = tire.tireTransform.position;
                Vector3 direction = -tire.tireTransform.up;

                tire.isGroundedExact = Physics.Raycast(origin, direction, out RaycastHit exactHit, exactDistance, _groundLayer);
                tire.exactGroundHit = exactHit;

                tire.isGroundedExtended = Physics.Raycast(origin, direction, out RaycastHit extendedHit, extendedDistance, _groundLayer);
                tire.extendedGroundHit = extendedHit;

                if (!tire.isGroundedExtended)
                    continue;

                normalSum += extendedHit.normal;
                groundedCount++;

                tire.hitLayer = 1 << extendedHit.collider.gameObject.layer;
            }

            planeNormal = groundedCount > 0
                ? (normalSum / groundedCount).normalized
                : transform.up;
        }

        private bool IsCarGrounded()
        {
            foreach (Tire tire in _allTires)
            {
                if (tire.isGroundedExtended)
                    return true;
            }

            return false;
        }

        private List<Tire> GetAllTires()
        {
            var allTires = new List<Tire>();

            foreach (var tire in steeringTires)
                allTires.Add(tire);

            foreach (var tire in staticTires)
                allTires.Add(tire);

            return allTires;
        }

        private float GetGroundedRatio(out int groundedCount)
        {
            groundedCount = 0;

            for (int i = 0; i < _allTires.Count; i++)
            {
                if (_allTires[i].isGroundedExtended)
                    groundedCount++;
            }

            return (float)groundedCount / _allTires.Count;
        }

        private void StopJittering()
        {
            if (_playerRb.linearVelocity.magnitude > 0.1f) return;
            _playerRb.linearVelocity = Vector3.zero;
            _playerRb.angularVelocity = Vector3.zero;
        }
        
        public bool IsTireGrounded(int index) => _allTires[index].isGroundedExact;
        #endregion
    }
}