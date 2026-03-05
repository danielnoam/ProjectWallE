using ProjectWallE;

namespace _2_Scripts
{
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CarInput))]
public class CarController : MonoBehaviour, IPlayerController
{
    [SerializeField] private TireAndVisualTransform[] steeringTires;
    [SerializeField] private TireAndVisualTransform[] staticTires;

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
    [SerializeField] private float handbrakeBlendIn = 12f; // how fast it engages
    [SerializeField] private float handbrakeBlendOut = 8f; // how fast it releases

    [Header("Slope Anti-Slip (extra)")] 
    [SerializeField] private float slopeAntiSlipStrength = 1.0f; // 0..1+ (1 cancels gravity fully)
    [SerializeField] private float slopeAntiSlipMaxAccel = 30f; // m/s^2 clamp safety
    [SerializeField] private float slopeAssistFadeStart = 0.2f; // m/s lateral speed where assist starts fading out
    [SerializeField] private float slopeAssistFadeEnd = 2.0f; // m/s lateral speed where assist is fully off

    [Header("Takeoff / partial-ground tuning")] 
    [SerializeField] private float minGripWhenPartialGround = 0.15f; // 0..1
    [SerializeField] private float partialGroundGripPower = 1.0f; // curve feel
    
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
    
    private List<TireAndVisualTransform> _allTires;
    private readonly Dictionary<Transform, float> _tireNormalForces = new();

    private float _currentSteering;
    private float _tireGripFactor;
    private float _groundedRatio;
    private float _airborneBlend;
    private float _handbrake01;
    /// <summary>
    /// controls how much dv affects the gravity (bigger = less control)
    /// </summary>
    private float gravityControlFactor => gravityStrength * 0.25f;


    private void Awake()
    {
        _carInput = GetComponent<CarInput>();
        _allTires = GetAllTiresTransforms();

        foreach (var t in _allTires)
            _tireNormalForces[t.tireTransform] = 0f;
    }

    public void Initialize(PlayerReferences playerReferences)
    {
        _playerRb = playerReferences.rigidBody;
        _groundLayer = playerReferences.groundLayer;
    }

    public void ApplyMovement()
    {
        ApplyGravity();
        ApplySuspension();
        ApplyTireRotation();
        ApplyTireFriction();
        ApplyAirControl();
        ApplyLongitudinalMovement();
    }

    #region Suspension

    private void ApplySuspension()
    {
        ApplySuspensionPerTiresType(_allTires);
    }

    private void ApplySuspensionPerTiresType(List<TireAndVisualTransform> tires)
    {
        foreach (var tire in tires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit, false))
            {
                _tireNormalForces[tire.tireTransform] = 0f;
                tire.visualTransform.localPosition = Vector3.Lerp(tire.visualTransform.localPosition,
                    tire.VisualStartPosition, 5f * Time.fixedDeltaTime);
                continue;
            }

            float offset = hit.distance - groundHeight;

            Vector3 springDir = hit.normal; // push away from the ground

            Vector3 pointVel = _playerRb.GetPointVelocity(tire.tireTransform.position);
            float velAlongSpring = Vector3.Dot(pointVel, springDir);

            float suspensionForce =
                (-offset * suspensionStrength) -
                (velAlongSpring * suspensionDamping);

            _playerRb.AddForceAtPosition(springDir * (suspensionForce * _playerRb.mass), tire.tireTransform.position, ForceMode.Force);

            _tireNormalForces[tire.tireTransform] = suspensionForce;

            //tire visual
            var vector3 = tire.visualTransform.localPosition;
            vector3.y = tire.VisualStartPosition.y - offset;
            tire.visualTransform.localPosition = vector3;
        }
    }

    #endregion

    #region Steering

    private void ApplyTireRotation()
    {
        float desiredSteeringAngle = Mathf.Lerp(steeringAngle, topSpeedSteeringFactor * steeringAngle,
            Mathf.Abs(Vector3.Dot(transform.forward, _playerRb.linearVelocity)) / topForwardSpeed);

        float target = _carInput.Steering * desiredSteeringAngle;

        _currentSteering = Mathf.Lerp(
            _currentSteering,
            target,
            1f - Mathf.Exp(-steeringStrength * Time.fixedDeltaTime)
        );

        Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);
        foreach (var tire in steeringTires) tire.tireTransform.localRotation = rot;
    }

    private void ApplyTireFriction()
    {
        //air blend
        _groundedRatio = GetGroundedRatio(out _);
        float planted = Mathf.Pow(_groundedRatio, partialGroundGripPower);
        _airborneBlend = Mathf.Lerp(minGripWhenPartialGround, 1f, planted);

        //handbreak calcs
        float targetHb = _carInput.HandBreakHeld ? 1f : 0f;
        float rate = (targetHb > _handbrake01) ? handbrakeBlendIn : handbrakeBlendOut;
        _handbrake01 = Mathf.Lerp(_handbrake01, targetHb, rate * Time.fixedDeltaTime);

        ApplyFrictionPerTiresType(steeringTires, steeringTiresFrictionCurve);
        ApplyFrictionPerTiresType(staticTires, staticTiresFrictionCurve);
    }

    private void ApplyFrictionPerTiresType(TireAndVisualTransform[] tires, AnimationCurve frictionCurve)
    {
        foreach (TireAndVisualTransform tire in tires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;

            Vector3 tireVel = _playerRb.GetPointVelocity(tire.tireTransform.position);

            float steeringVel = Vector3.Dot(tire.tireTransform.right, tireVel);

            float normalGrip = CalcCurrenTireGripFactor(tire.tireTransform, frictionCurve);
            float gripFactor = Mathf.Lerp(normalGrip, handBreakGripFactor, _handbrake01);

            gripFactor *= _airborneBlend;

            float desiredVelChange = -steeringVel * gripFactor;

            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

            _playerRb.AddForceAtPosition(tire.tireTransform.right * _playerRb.mass / _allTires.Count * desiredAccel,
                tire.tireTransform.position);

            // ===== NEW: slope gravity lateral counter-force =====
            // Gravity component parallel to the ground plane at this tire
            Vector3 g = new Vector3(0, -gravityStrength, 0); // (0,-9.81,0)
            Vector3 gParallel = Vector3.ProjectOnPlane(g, -hit.normal); // "down the slope"

            // We only care about lateral (sideways) direction relative to the tire
            float gLatAccel = Vector3.Dot(gParallel, tire.tireTransform.right); // m/s^2 along tire.right

            // Fade out assist when already sliding fast sideways (so it doesn't feel sticky/weird)
            float latSpeedAbs = Mathf.Clamp(Mathf.Abs(steeringVel), slopeAssistFadeStart, slopeAssistFadeEnd);
            float fade = 1f - Mathf.InverseLerp(slopeAssistFadeStart, slopeAssistFadeEnd, latSpeedAbs);
            fade = Mathf.Clamp01(fade);

            // Counter acceleration (opposite to the gravity lateral accel)
            float counterAccel = -gLatAccel * slopeAntiSlipStrength * fade;

            // Safety clamp
            counterAccel = Mathf.Clamp(counterAccel, -slopeAntiSlipMaxAccel, slopeAntiSlipMaxAccel);

            // Apply as force (F = m * a), distributed by tire count just like your other forces
            Vector3 counterForce = tire.tireTransform.right * ((_playerRb.mass / _allTires.Count) * counterAccel);
            _playerRb.AddForceAtPosition(counterForce, tire.tireTransform.position);
        }
    }

    private float CalcCurrenTireGripFactor(Transform tire, AnimationCurve frictionCurve)
    {
        Vector3 tireVel = _playerRb.GetPointVelocity(tire.position);
        tireVel.y = 0;
        float slippingAmount = Mathf.Clamp(Vector3.Dot(tire.right, tireVel.normalized), -1.0f, 1.0f);
        float gripFactor = frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
        return gripFactor;
    }


    #endregion

    #region longitudinal Movement

    private void ApplyLongitudinalMovement()
    {
        float carSpeed = Vector3.Dot(transform.forward, _playerRb.linearVelocity);

        if (_carInput.Acceleration > 0) ApplyForwardAcceleration(carSpeed);
        else if (_carInput.Acceleration < 0) ApplyBackwardsAcceleration(carSpeed);
        else ApplyEngineBreaking(carSpeed);

        //visuals
        RotateWheels(carSpeed, 0.5f);
    }

    private void ApplyForwardAcceleration(float carSpeed)
    {
        if (carSpeed > topForwardSpeed) return;

        foreach (var tire in _allTires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;

            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topForwardSpeed);

            float availableAcceleration = carSpeed >= 0
                ? accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength
                : brakeStrength;

            _playerRb.AddForceAtPosition(tire.tireTransform.forward * (availableAcceleration * _playerRb.mass) / _allTires.Count,
                tire.tireTransform.position);
        }

    }

    private void ApplyBackwardsAcceleration(float carSpeed)
    {
        if (carSpeed < -topBackwardSpeed) return;

        foreach (var tire in _allTires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;

            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topBackwardSpeed);

            float availableAcceleration = carSpeed <= 0
                ? accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength
                : brakeStrength;

            _playerRb.AddForceAtPosition(-tire.tireTransform.forward * (availableAcceleration * _playerRb.mass) / _allTires.Count,
                tire.tireTransform.position);
        }
    }

    private void ApplyEngineBreaking(float carSpeed)
    {
        foreach (var tire in _allTires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;

            _playerRb.AddForceAtPosition(
                tire.tireTransform.forward * (-Mathf.Sign(carSpeed) * engineBrakeStrength * _playerRb.mass / _allTires.Count),
                tire.tireTransform.position);
        }

    }

    private void RotateWheels(float carSpeed, float wheelRadius)
    {
        float wheelSpeedRad = carSpeed / wheelRadius;
        float wheelSpeedDeg = wheelSpeedRad * Mathf.Rad2Deg;

        foreach (var tire in _allTires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;
            tire.visualTransform.Rotate(Vector3.up, wheelSpeedDeg * Time.fixedDeltaTime, Space.Self);
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
        // old input manager: A = -1, D = +1
        float input = _carInput.Steering;

        if (Mathf.Abs(input) < 0.001f) return;

        // target yaw rate (rad/s)
        float targetYawRate = input * maxAirSteeringVelocity;

        // current yaw rate (rad/s) measured around car up
        float currentYawRate = Vector3.Dot(_playerRb.angularVelocity, transform.up);

        // PD-ish: drive yaw rate error with torque-as-angular-acceleration
        float yawRateError = targetYawRate - currentYawRate;

        // turn that into an angular acceleration command
        float yawAccelCmd = yawRateError * airSteeringStrength;

        // apply around car up
        _playerRb.AddTorque(transform.up * yawAccelCmd, ForceMode.Acceleration);
    }

    private void AirAlignTorque()
    {
        // rotation that would align current up to world up
        Quaternion toUpright = Quaternion.FromToRotation(transform.up, Vector3.up);

        // convert rotation error to axis-angle
        toUpright.ToAngleAxis(out float angleDeg, out Vector3 axisWorld);
        if (angleDeg > 180f) angleDeg -= 360f;

        // error vector in world space (radians)
        Vector3 errorWorld = axisWorld * (angleDeg * Mathf.Deg2Rad);

        // convert to local so we can ignore yaw (local Y)
        Vector3 errorLocal = transform.InverseTransformDirection(errorWorld);
        errorLocal.y = 0f;

        // angular velocity in local space, ignore yaw
        Vector3 angVelLocal = transform.InverseTransformDirection(_playerRb.angularVelocity);
        angVelLocal.y = 0f;

        // PD control in local space (outputs "angular acceleration" if using ForceMode.Acceleration)
        Vector3 torqueLocal =
            (errorLocal * airAlignmentStrength) -
            (angVelLocal * airAlignmentDamping);

        // back to world and apply
        Vector3 torqueWorld = transform.TransformDirection(torqueLocal);

        _playerRb.AddTorque(torqueWorld, ForceMode.Acceleration);
    }


    #endregion

    #region Helpers

    private bool IsCarGrounded()
    {
        foreach (TireAndVisualTransform tire in _allTires)
        {
            if (!IsTireGrounded(tire.tireTransform, out var hit)) continue;
            return true;
        }

        return false;
    }

    private bool IsTireGrounded(Transform tire, out RaycastHit tireHit, bool isExtraDistance = true)
    {
        float distance = isExtraDistance ? groundHeight + 0.1f : groundHeight;

        bool isGrounded = Physics.Raycast(tire.position, -tire.up, out RaycastHit hit, distance, _groundLayer);
        tireHit = hit;

        return isGrounded;
    }

    private List<TireAndVisualTransform> GetAllTiresTransforms()
    {
        var allTires = new List<TireAndVisualTransform>();

        foreach (var tire in steeringTires)
        {
            var tireRef = new TireAndVisualTransform(tire);
            allTires.Add(tireRef);
        }

        foreach (var tire in staticTires)
        {
            var tireAndVisual = new TireAndVisualTransform(tire);
            allTires.Add(tireAndVisual);
        }

        return allTires;
    }

    private float GetGroundedRatio(out int groundedCount)
    {
        groundedCount = 0;
        for (int i = 0; i < _allTires.Count; i++)
        {
            var t = _allTires[i];
            if (Physics.Raycast(t.tireTransform.position, -t.tireTransform.up, out _, 0.1f + groundHeight,
                    _groundLayer))
                groundedCount++;
        }

        return (float)groundedCount / _allTires.Count;
    }


    #endregion
}

}