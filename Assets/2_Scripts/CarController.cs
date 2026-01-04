using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CarController : MonoBehaviour
{
    [SerializeField] Transform[] steeringTiresTransforms;
    [SerializeField] Transform[] staticTiresTransforms;
    
    [Header("Suspension Parameters")]
    [SerializeField] private float groundHeight = 0.2f;
    [SerializeField] private float suspensionStrength;
    [SerializeField] private float suspensionDamping;
    
    [Header("Steering Parameters")]
    [SerializeField] private float steeringAngle;
    [SerializeField] private float steeringStrength;
    [SerializeField] private float topSpeedSteeringFactor;
    [SerializeField] private AnimationCurve steeringTiresFrictionCurve;
    [SerializeField] private AnimationCurve staticTiresFrictionCurve;
    [SerializeField] private float handBreakGripFactor;
    
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
    
    private float _currentSteering;
    private float _tireGripFactor;

    
    void Awake()
    {
        _carRb = GetComponent<Rigidbody>();
        _tireTransforms = GetAllTiresTransforms();
    }
    void FixedUpdate()
    {
        ApplyLongitudinalMovement();
        ApplySuspension();
        ApplyTireRotation();
        ApplyTireFriction(); 
        ApplyAirControl();
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
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out RaycastHit tireHit, groundHeight);
            if (!isGrounded) continue;
            
            float offset = tireHit.distance - groundHeight;
            
            float suspensionForce = (-offset * suspensionStrength) - (_carRb.GetPointVelocity(tire.position).y * suspensionDamping);
            
            _carRb.AddForceAtPosition(tire.up * suspensionForce, tire.position);
        }
    }

    #endregion

    #region Steering

    private void ApplyTireRotation()
    {
        float desiredsteeringAngle = Mathf.Lerp(steeringAngle, topSpeedSteeringFactor * steeringAngle, Mathf.Abs(Vector3.Dot(transform.forward, _carRb.linearVelocity)) / topForwardSpeed);
        Debug.Log(desiredsteeringAngle);
        
        float target = Input.GetAxis("Horizontal") * desiredsteeringAngle;

        _currentSteering = Mathf.Lerp(
            _currentSteering,
            target,
            1f - Mathf.Exp(-steeringStrength * Time.fixedDeltaTime)
        );

        Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);

        foreach (var tire in steeringTiresTransforms) tire.localRotation = rot;
    }

    private void ApplyTireFriction()
    {
        ApplyFrictionPerTiresType(steeringTiresTransforms, steeringTiresFrictionCurve);
        ApplyFrictionPerTiresType(staticTiresTransforms, staticTiresFrictionCurve);
    }

    private void ApplyFrictionPerTiresType(Transform[] tires, AnimationCurve frictionCurve)
    {
        foreach (Transform tire in tires)
        {
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out RaycastHit tireHit, groundHeight);
            if (!isGrounded) continue;
            
            Vector3 tireVel = _carRb.GetPointVelocity(tire.position);
            
            float steeringVel = Vector3.Dot(tire.right, tireVel);
            
            float gripFactor = Input.GetKey(KeyCode.Space) ? handBreakGripFactor : CalcCurrenTireGripFactor(tire, frictionCurve);
            Debug.Log($"{tire.gameObject.name}" + gripFactor);
            
            float desiredVelChange = -steeringVel * gripFactor;
            
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            
            _carRb.AddForceAtPosition(tire.right * _carRb.mass/_tireTransforms.Count * desiredAccel, tire.position);
        }
    }
    
    private float CalcCurrenTireGripFactor(Transform tire, AnimationCurve frictionCurve)
    {
        
        Vector3 tireVel = _carRb.GetPointVelocity(tire.position);
        tireVel.y = 0;
        
        float slippingAmount = Mathf.Clamp(Vector3.Dot(tire.right, tireVel.normalized), -1.0f, 1.0f);
        
        return frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
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
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
            if (!isGrounded) continue;
                
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
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
            if (!isGrounded) continue;
                
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
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
            if (!isGrounded) continue;
            
            _carRb.AddForceAtPosition(tire.forward * (-Mathf.Sign(carSpeed) * engineBrakeStrength / _tireTransforms.Count), tire.position);
        }
        
    }

    #endregion
    
    #region Air Control

    private void ApplyAirControl()
    {
        if (IsGrounded()) return; 
        
        
        AirAlignAngularVelocity();
    }

    private void AirSteering()
    {
        
    }
    void AirAlignAngularVelocity()
    {
        // rotation that would align current up to world up
        Quaternion toUpright = Quaternion.FromToRotation(transform.up, Vector3.up);

        // convert that rotation error to axis-angle
        toUpright.ToAngleAxis(out float angleDeg, out Vector3 axisWorld);
        if (angleDeg > 180f) angleDeg -= 360f;

        // error as angular vector (world space), radians
        Vector3 errorWorld = axisWorld * (angleDeg * Mathf.Deg2Rad);

        // convert to local so we can kill yaw (local Y)
        Vector3 errorLocal = transform.InverseTransformDirection(errorWorld);
        errorLocal.y = 0f; // don't touch yaw

        // current angular velocity in local space (so we can damp only X/Z)
        Vector3 angVelLocal = transform.InverseTransformDirection(_carRb.angularVelocity);
        Vector3 angVelLocalNoYaw = new Vector3(angVelLocal.x, 0f, angVelLocal.z);

        // PD: target angular velocity is proportional to error, minus damping
        Vector3 correctiveLocal = (errorLocal * airAlignmentStrength) - (angVelLocalNoYaw * airAlignmentDamping);

        // add it back to angular velocity (local -> world)
        Vector3 correctiveWorld = transform.TransformDirection(correctiveLocal);
        _carRb.angularVelocity += correctiveWorld;
    }

    
    #endregion

    #region Helpers

    private bool IsGrounded()
    {
        foreach (Transform tire in _tireTransforms)
        {
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
            if (!isGrounded) continue;

            return true;
        }
        return false;
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

    #endregion

    

    
    
        
}
