using System;
using System.Collections.Generic;
using ProjectWallE;
using UnityEngine;

namespace _2_Scripts
{
    [RequireComponent(typeof(CarInput), typeof(CarBoost))]
    public class CarController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private Tire[] steeringTires;
        [SerializeField] private Tire[] staticTires;
        [SerializeField] private Transform boostPoint;

        [Header("Suspension Parameters")]
        [SerializeField] private float groundHeight = 0.2f;
        [SerializeField] private float suspensionStrength;
        [SerializeField] private float suspensionDamping;

        [Header("Steering Parameters")]
        [SerializeField] private float steeringAngle;
        [SerializeField] private float steeringStrength;
        [SerializeField] private float topSpeedSteeringFactor;

        [Header("Tire Friction Parameters")]
        [SerializeField] private AnimationCurve steeringTiresFrictionCurve;
        [SerializeField] private AnimationCurve staticTiresFrictionCurve;
        [SerializeField] private float handBreakGripFactor;
        [SerializeField] private float handbrakeBlendIn = 12f;
        [SerializeField] private float handbrakeBlendOut = 8f;

        [Header("Takeoff / partial-ground tuning")]
        [SerializeField] private float minGripWhenPartialGround = 0.15f;
        [SerializeField] private float partialGroundGripPower = 1.0f;

        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength = 1.0f;
        [SerializeField] private float terminalVelocity = 1.0f;

        [Header("Acceleration Parameters")]
        [SerializeField] private float accelerationStrength;
        [SerializeField] private AnimationCurve accelerationCurve;
        [SerializeField] private float topForwardSpeed;
        [SerializeField] private float topBackwardSpeed;

        [Header("Breaking Parameters")]
        [SerializeField] private float brakeStrength;
        [SerializeField] private float engineBrakeStrength;

        [Header("Air Control Parameters")]
        [SerializeField] private float airAlignmentStrength;
        [SerializeField] private float airAlignmentDamping;
        [SerializeField] private float airSteeringStrength;
        [SerializeField] private float maxAirSteeringVelocity;

        private CarInput _carInput;
        private LayerMask _groundLayer;
        private Rigidbody _playerRb;

        private List<Tire> _allTires;
        private readonly Dictionary<Transform, float> _tireNormalForces = new();
        
        private const float ExtendedGroundCheckExtraDistance = 0.1f;

        private float _currentSteering;
        private float _groundedRatio;
        private float _airborneBlend;
        private float _handbrake01;

        /// <summary>
        /// controls how much dv affects the gravity (bigger = less control)
        /// </summary>
        private float gravityControlFactor => gravityStrength * 0.25f;

        public CarBoost carBoost { get; private set; }
        public bool canBuild { get; private set; } = false;
        public bool canShoot { get; private set; } = false;

        private void Awake()
        {
            _carInput = GetComponent<CarInput>();
            carBoost = GetComponent<CarBoost>();
            _allTires = GetAllTiresTransforms();

            foreach (var tire in _allTires)
            {
                _tireNormalForces[tire.tireTransform] = 0f;
                tire.Initialize();
            }
        }

        public void Initialize(PlayerReferences playerReferences)
        {
            _playerRb = playerReferences.rigidBody;
            _groundLayer = playerReferences.groundLayer;
        }

        public void ApplyMovement()
        {
            UpdateTiresGroundState();

            ApplyGravity();
            ApplySuspensionPerTiresType(_allTires);
            ApplyTireRotation();
            ApplyTireFriction();
            ApplyAirControl();
            ApplyLongitudinalMovement();
        }

        #region Suspension

        private void ApplySuspensionPerTiresType(List<Tire> tires)
        {
            foreach (var tire in tires)
            {
                if (!tire.isGroundedExact)
                {
                    _tireNormalForces[tire.tireTransform] = 0f;
                    tire.visualTransform.localPosition = Vector3.Lerp(
                        tire.visualTransform.localPosition,
                        tire.VisualStartPosition,
                        5f * Time.fixedDeltaTime);

                    continue;
                }

                RaycastHit hit = tire.exactGroundHit;

                float offset = hit.distance - groundHeight;
                Vector3 springDir = tire.tireTransform.up;

                Vector3 pointVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
                float velAlongSpring = Vector3.Dot(pointVel, springDir);

                float suspensionForce =
                    (-offset * suspensionStrength) -
                    (velAlongSpring * suspensionDamping);

                _playerRb.AddForceAtPosition(
                    springDir * (suspensionForce * _playerRb.mass),
                    tire.tireTransform.position,
                    ForceMode.Force);

                _tireNormalForces[tire.tireTransform] = suspensionForce;

                Vector3 localPos = tire.visualTransform.localPosition;
                localPos.y = tire.VisualStartPosition.y - offset;
                tire.visualTransform.localPosition = localPos;
            }
        }

        #endregion

        #region Steering

        private void ApplyTireRotation()
        {
            float desiredSteeringAngle = Mathf.Lerp(
                steeringAngle,
                topSpeedSteeringFactor * steeringAngle,
                Mathf.Abs(Vector3.Dot(transform.forward, _playerRb.linearVelocity)) / topForwardSpeed);

            float target = _carInput.Steering * desiredSteeringAngle;

            _currentSteering = Mathf.Lerp(
                _currentSteering,
                target,
                1f - Mathf.Exp(-steeringStrength * Time.fixedDeltaTime)
            );

            Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);

            foreach (var tire in steeringTires)
                tire.tireTransform.localRotation = rot;
        }

        private void ApplyTireFriction()
        {
            ApplyTireFrictionModifiers();

            ApplyFrictionPerTiresType(steeringTires, steeringTiresFrictionCurve);
            ApplyFrictionPerTiresType(staticTires, staticTiresFrictionCurve);
        }

        private void ApplyTireFrictionModifiers()
        {
            _groundedRatio = GetGroundedRatio(out _);
            float planted = Mathf.Pow(_groundedRatio, partialGroundGripPower);
            _airborneBlend = Mathf.Lerp(minGripWhenPartialGround, 1f, planted);

            float targetHb = _carInput.HandBreakHeld ? 1f : 0f;
            float rate = (targetHb > _handbrake01) ? handbrakeBlendIn : handbrakeBlendOut;
            _handbrake01 = Mathf.Lerp(_handbrake01, targetHb, rate * Time.fixedDeltaTime);
        }

        private void ApplyFrictionPerTiresType(Tire[] tires, AnimationCurve frictionCurve)
        {
            foreach (Tire tire in tires)
            {
                if (!tire.isGroundedExtended) continue;

                Vector3 tireVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
                float steeringVel = Vector3.Dot(tire.tireTransform.right, tireVel);

                float normalGrip = CalcCurrentTireGripFactor(tire, frictionCurve);
                float gripFactor = Mathf.Lerp(normalGrip, handBreakGripFactor, _handbrake01);

                gripFactor *= _airborneBlend;

                float desiredVelChange = -steeringVel * gripFactor;
                float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                _playerRb.AddForceAtPosition(
                    tire.tireTransform.right * (_playerRb.mass / _allTires.Count * desiredAccel),
                    tire.tireTransform.position,
                    ForceMode.Force
                );
            }
        }

        private float CalcCurrentTireGripFactor(Tire tire, AnimationCurve frictionCurve)
        {
            Vector3 tireVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
            tireVel.y = 0f;

            if (tireVel.magnitude < 0.1f)
                return frictionCurve.Evaluate(0f);

            float slippingAmount = Mathf.Clamp(
                Vector3.Dot(tire.tireTransform.right, tireVel.normalized),
                -1f,
                1f);

            float gripFactor = frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
            return gripFactor;
        }

        #endregion

        #region Longitudinal Movement

        private void ApplyLongitudinalMovement()
        {
            float carSpeed = Vector3.Dot(transform.forward, _playerRb.linearVelocity);

            if (_carInput.Acceleration > 0 || _carInput.BoostHeld)
                ApplyForwardAcceleration(carSpeed);
            else if (_carInput.Acceleration < 0)
                ApplyBackwardsAcceleration(carSpeed);
            else
                ApplyEngineBreaking(carSpeed);

            if (carBoost.CanBoost(out float boostAccel, out float boostSpeedFactor))
                ApplyBoost(carSpeed, boostAccel, boostSpeedFactor);

            RotateWheels(carSpeed, 0.5f);
        }

        private void ApplyForwardAcceleration(float carSpeed)
        {
            if (carSpeed > topForwardSpeed) return;

            foreach (var tire in steeringTires)
            {
                if (!tire.isGroundedExtended) continue;

                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topForwardSpeed);

                float availableAcceleration = carSpeed >= 0
                    ? accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength
                    : brakeStrength;

                _playerRb.AddForceAtPosition(
                    tire.tireTransform.forward * (availableAcceleration * _playerRb.mass / steeringTires.Length),
                    tire.tireTransform.position);
            }
        }

        private void ApplyBackwardsAcceleration(float carSpeed)
        {
            if (carSpeed < -topBackwardSpeed) return;

            foreach (var tire in steeringTires)
            {
                if (!tire.isGroundedExtended) continue;

                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topBackwardSpeed);

                float availableAcceleration = carSpeed <= 0
                    ? accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength
                    : brakeStrength;

                _playerRb.AddForceAtPosition(
                    -tire.tireTransform.forward * (availableAcceleration * _playerRb.mass / steeringTires.Length),
                    tire.tireTransform.position);
            }
        }

        private void ApplyEngineBreaking(float carSpeed)
        {
            foreach (var tire in _allTires)
            {
                if (!tire.isGroundedExtended) continue;

                _playerRb.AddForceAtPosition(
                    tire.tireTransform.forward * (-Mathf.Sign(carSpeed) * engineBrakeStrength * _playerRb.mass / _allTires.Count),
                    tire.tireTransform.position);
            }
        }

        private void ApplyBoost(float carSpeed, float boostAccel, float boostSpeedFactor)
        {
            if (carSpeed > topForwardSpeed * boostSpeedFactor) return;

            _playerRb.AddForce(transform.forward * (boostAccel * _playerRb.mass));
        }

        private void RotateWheels(float carSpeed, float wheelRadius)
        {
            float wheelSpeedRad = carSpeed / wheelRadius;
            float wheelSpeedDeg = wheelSpeedRad * Mathf.Rad2Deg;

            foreach (var tire in _allTires)
            {
                if (!tire.isGroundedExtended) continue;

                tire.visualTransform.Rotate(Vector3.left, wheelSpeedDeg * Time.fixedDeltaTime, Space.Self);
            }
        }

        #endregion

        #region Gravity

        private void ApplyGravity()
        {
            float verticalVel = _playerRb.linearVelocity.y;
            float desiredVerticalVel = -terminalVelocity;

            float dv = desiredVerticalVel - verticalVel;
            float gravityForce = Mathf.Clamp(dv * gravityControlFactor, -gravityStrength, Mathf.Infinity);

            _playerRb.AddForce(Vector3.up * gravityForce, ForceMode.Acceleration);
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

            float targetYawRate = input * maxAirSteeringVelocity;
            float currentYawRate = Vector3.Dot(_playerRb.angularVelocity, transform.up);
            float yawRateError = targetYawRate - currentYawRate;
            float yawAccelCmd = yawRateError * airSteeringStrength;

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
                (errorLocal * airAlignmentStrength) -
                (angVelLocal * airAlignmentDamping);

            Vector3 torqueWorld = transform.TransformDirection(torqueLocal);
            _playerRb.AddTorque(torqueWorld, ForceMode.Acceleration);
        }

        #endregion

        #region Helpers

        private void UpdateTiresGroundState()
        {
            float exactDistance = groundHeight;
            float extendedDistance = groundHeight + ExtendedGroundCheckExtraDistance;

            foreach (var tire in _allTires)
            {
                Vector3 origin = tire.tireTransform.position;
                Vector3 direction = -tire.tireTransform.up;

                tire.isGroundedExact = Physics.Raycast(origin, direction, out RaycastHit exactHit, exactDistance, _groundLayer);
                tire.exactGroundHit = exactHit;
                tire.isGroundedExtended = Physics.Raycast(origin, direction, out RaycastHit extendedHit, extendedDistance, _groundLayer);
                tire.extendedGroundHit = extendedHit;
            }
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

        private List<Tire> GetAllTiresTransforms()
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

        #endregion
    }
}