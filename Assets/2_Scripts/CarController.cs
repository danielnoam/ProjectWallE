using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Serialization;

public class CarController : MonoBehaviour
{
    [SerializeField] Transform[] steeringTiresTransforms;
    [SerializeField] Transform[] staticTiresTransforms;
    [SerializeField] LayerMask groundLayer;
    
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
    [SerializeField] private float handbrakeBlendIn = 12f;   // how fast it engages
    [SerializeField] private float handbrakeBlendOut = 8f;   // how fast it releases
    
    [Header("Slope Anti-Slip (extra)")]
    [SerializeField] private float slopeAntiSlipStrength = 1.0f; // 0..1+ (1 cancels gravity fully)
    [SerializeField] private float slopeAntiSlipMaxAccel = 30f;  // m/s^2 clamp safety
    [SerializeField] private float slopeAssistFadeStart = 0.2f;  // m/s lateral speed where assist starts fading out
    [SerializeField] private float slopeAssistFadeEnd = 2.0f;    // m/s lateral speed where assist is fully off
    
    [Header("Takeoff / partial-ground tuning")]
    [SerializeField] float minGripWhenPartialGround = 0.15f; // 0..1
    [SerializeField] float partialGroundGripPower = 1.0f;     // curve feel
    
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
    
    private Rigidbody _carRb;
    private List<Transform> _tireTransforms;
    private readonly Dictionary<Transform, float> _tireNormalForces = new();
    
    private float _currentSteering;
    private float _tireGripFactor;
    private float _groundedRatio;
    private float _airborneBlend;
    private float _handbrake01;

    
    void Awake()
    {
        _carRb = GetComponent<Rigidbody>();
        _tireTransforms = GetAllTiresTransforms();

        foreach (var t in _tireTransforms)
            _tireNormalForces[t] = 0f;
    }
    void FixedUpdate()
    {
        ApplySuspension();
        ApplyTireRotation();
        ApplyTireFriction(); 
        ApplyAirControl();
        ApplyLongitudinalMovement();
    }

    #region Suspension

    private void ApplySuspension()
    {
        ApplySuspensionPerTiresType(steeringTiresTransforms);
        ApplySuspensionPerTiresType(staticTiresTransforms);
    }

    private void ApplySuspensionPerTiresType(Transform[] tires)
    {
        foreach (var tire in tires)
        {
            if (!IsTireGrounded(tire, out var hit ))
            {
                _tireNormalForces[tire] = 0f;
                continue;
            }
            
            float offset = hit.distance - groundHeight;
            
            Vector3 springDir = hit.normal; // push away from the ground

            Vector3 pointVel = _carRb.GetPointVelocity(tire.position);
            float velAlongSpring = Vector3.Dot(pointVel, springDir);

            float suspensionForce =
                (-offset * suspensionStrength) -
                (velAlongSpring * suspensionDamping);

            _carRb.AddForceAtPosition(springDir * suspensionForce, tire.position);
            
            _tireNormalForces[tire] = suspensionForce;
        }
    }

    #endregion

    #region Steering

    private void ApplyTireRotation()
    {
        float desiredSteeringAngle = Mathf.Lerp(steeringAngle, topSpeedSteeringFactor * steeringAngle, Mathf.Abs(Vector3.Dot(transform.forward, _carRb.linearVelocity)) / topForwardSpeed);
        
        float target = Input.GetAxis("Horizontal") * desiredSteeringAngle;

        _currentSteering = Mathf.Lerp(
            _currentSteering,
            target,
            1f - Mathf.Exp(-steeringStrength * Time.fixedDeltaTime)
        );

        Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);
        Debug.Log(_currentSteering);
        foreach (var tire in steeringTiresTransforms) tire.localRotation = rot;
    }

    private void ApplyTireFriction()
    {
        //air blend
        _groundedRatio = GetGroundedRatio(out _);
        float planted = Mathf.Pow(_groundedRatio, partialGroundGripPower);
        _airborneBlend = Mathf.Lerp(minGripWhenPartialGround, 1f, planted);
        
        //handbreak calcs
        float targetHB = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        float rate = (targetHB > _handbrake01) ? handbrakeBlendIn : handbrakeBlendOut;
        _handbrake01 = Mathf.Lerp(_handbrake01, targetHB, rate * Time.fixedDeltaTime);
        
        ApplyFrictionPerTiresType(steeringTiresTransforms, steeringTiresFrictionCurve);
        ApplyFrictionPerTiresType(staticTiresTransforms, staticTiresFrictionCurve);
    }

    private void ApplyFrictionPerTiresType(Transform[] tires, AnimationCurve frictionCurve)
    {
        foreach (Transform tire in tires)
        {
            if (!IsTireGrounded(tire, out var hit )) continue;
            
            Vector3 tireVel = _carRb.GetPointVelocity(tire.position);
            
            float steeringVel = Vector3.Dot(tire.right, tireVel);
            
            float normalGrip = CalcCurrenTireGripFactor(tire, frictionCurve);
            float gripFactor = Mathf.Lerp(normalGrip, handBreakGripFactor, _handbrake01);
            
            gripFactor *= _airborneBlend;
            Debug.Log($"{tire.gameObject.name}" + gripFactor);
            
            float desiredVelChange = -steeringVel * gripFactor;
            
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            
            _carRb.AddForceAtPosition(tire.right * _carRb.mass/_tireTransforms.Count * desiredAccel, tire.position);
            
            // ===== NEW: slope gravity lateral counter-force =====
            // Gravity component parallel to the ground plane at this tire
            Vector3 g = Physics.gravity; // (0,-9.81,0)
            Vector3 gParallel = Vector3.ProjectOnPlane(g, hit.normal); // "down the slope"

            // We only care about lateral (sideways) direction relative to the tire
            float gLatAccel = Vector3.Dot(gParallel, tire.right); // m/s^2 along tire.right

            // Fade out assist when already sliding fast sideways (so it doesn't feel sticky/weird)
            float latSpeedAbs = Mathf.Clamp(Mathf.Abs(steeringVel), slopeAssistFadeStart, slopeAssistFadeEnd);
            float fade = 1f - Mathf.InverseLerp(slopeAssistFadeStart, slopeAssistFadeEnd, latSpeedAbs);
            fade = Mathf.Clamp01(fade);
            
            // Counter acceleration (opposite to the gravity lateral accel)
            float counterAccel = -gLatAccel * slopeAntiSlipStrength * fade;

            // Safety clamp
            counterAccel = Mathf.Clamp(counterAccel, -slopeAntiSlipMaxAccel, slopeAntiSlipMaxAccel);
            
            // Apply as force (F = m * a), distributed by tire count just like your other forces
            Vector3 counterForce = tire.right * ((_carRb.mass / _tireTransforms.Count) * counterAccel);
            _carRb.AddForceAtPosition(counterForce, tire.position);
        }
    }
    
    private float CalcCurrenTireGripFactor(Transform tire, AnimationCurve frictionCurve)
    {
        Vector3 tireVel = _carRb.GetPointVelocity(tire.position);
        tireVel.y = 0;
        float slippingAmount = Mathf.Clamp(Vector3.Dot(tire.right, tireVel.normalized), -1.0f, 1.0f);
        float gripFactor = frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
        return gripFactor;
    }


    #endregion

    #region longitudinal Movement

    void ApplyLongitudinalMovement()
    {
        float carSpeed = Vector3.Dot(transform.forward, _carRb.linearVelocity);

        if(Input.GetKey(KeyCode.W)) ApplyForwardAcceleration(carSpeed);
        else if(Input.GetKey(KeyCode.S)) ApplyBackwardsAcceleration(carSpeed);
        else ApplyEngineBreaking(carSpeed);
    }
    void ApplyForwardAcceleration(float carSpeed)
    {
        if(carSpeed > topForwardSpeed) return;
        
        foreach (var tire in _tireTransforms)
        {
            if (!IsTireGrounded(tire, out var hit )) continue;
                
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topForwardSpeed);
                
            float availableAcceleration = carSpeed >= 0 ? 
                                          accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength :
                                          brakeStrength;
                
            _carRb.AddForceAtPosition(tire.forward * availableAcceleration / _tireTransforms.Count, tire.position);
        }
        
    }

    private void ApplyBackwardsAcceleration(float carSpeed)
    {
        if(carSpeed < -topBackwardSpeed) return;
        
        foreach (var tire in _tireTransforms)
        {
            if (!IsTireGrounded(tire, out var hit )) continue;
                
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topBackwardSpeed);
                
            float availableAcceleration = carSpeed <= 0 ? 
                                          accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength :
                                          brakeStrength;  
            
            _carRb.AddForceAtPosition(-tire.forward * availableAcceleration / _tireTransforms.Count, tire.position);
        }
    }

    private void ApplyEngineBreaking(float carSpeed)
    {
        foreach (var tire in _tireTransforms)
        {
            if (!IsTireGrounded(tire, out var hit )) continue;

            _carRb.AddForceAtPosition(tire.forward * (-Mathf.Sign(carSpeed) * engineBrakeStrength / _tireTransforms.Count), tire.position);
        }
        
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
        float input =
            (Input.GetKey(KeyCode.D) ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) ? 1f : 0f);

        if (Mathf.Abs(input) < 0.001f) return;

        // target yaw rate (rad/s)
        float targetYawRate = input * maxAirSteeringVelocity;

        // current yaw rate (rad/s) measured around car up
        float currentYawRate = Vector3.Dot(_carRb.angularVelocity, transform.up);

        // PD-ish: drive yaw rate error with torque-as-angular-acceleration
        float yawRateError = targetYawRate - currentYawRate;

        // turn that into an angular acceleration command
        float yawAccelCmd = yawRateError * airSteeringStrength;

        // apply around car up
        _carRb.AddTorque(transform.up * yawAccelCmd, ForceMode.Acceleration);
    }
    void AirAlignTorque()
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
        Vector3 angVelLocal = transform.InverseTransformDirection(_carRb.angularVelocity);
        angVelLocal.y = 0f;

        // PD control in local space (outputs "angular acceleration" if using ForceMode.Acceleration)
        Vector3 torqueLocal =
            (errorLocal * airAlignmentStrength) -
            (angVelLocal * airAlignmentDamping);

        // back to world and apply
        Vector3 torqueWorld = transform.TransformDirection(torqueLocal);

        _carRb.AddTorque(torqueWorld, ForceMode.Acceleration);
    }

    
    #endregion

    #region Helpers

    private bool IsCarGrounded()
    {
        foreach (Transform tire in _tireTransforms)
        {
            if (!IsTireGrounded(tire, out var hit )) continue;
            return true;
        }
        return false;
    }

    private bool IsTireGrounded(Transform tire, out RaycastHit tireHit )
    {
        bool isGrounded = Physics.Raycast(tire.position, -tire.up, out RaycastHit hit,  0.1f + groundHeight, groundLayer);
        tireHit = hit;
        
        return isGrounded;
    }
    private List<Transform> GetAllTiresTransforms()
    {
        List<Transform> tires = new List<Transform>();

        foreach (var tire in steeringTiresTransforms)
        {
            tires.Add(tire);
        }

        foreach (var tire in staticTiresTransforms)
        {
            tires.Add(tire);
        }
        
        return tires;
    }
    
    float GetGroundedRatio(out int groundedCount)
    {
        groundedCount = 0;
        for (int i = 0; i < _tireTransforms.Count; i++)
        {
            var t = _tireTransforms[i];
            if (Physics.Raycast(t.position, -t.up, out _, 0.1f + groundHeight, groundLayer))
                groundedCount++;
        }
        return (float)groundedCount / _tireTransforms.Count;
    }


    #endregion

    

    
    
        
}
