using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private AnimationCurve steeringTiresFrictionCurve;
    [SerializeField] private AnimationCurve staticTiresFrictionCurve;
    [SerializeField] private float handBreakGripFactor;
    
    [Header("Acceleration Parameters")]
    [SerializeField] private float accelerationStrength;
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private float brakeStrength;
    [SerializeField] private float topSpeed;
    
    private Rigidbody _carRb;
    private List<Transform> _tireTransforms;
    
    private float _currentSteering;
    private float _currentGripFactor;
    private float _tireGripFactor = 0.5f;

    
    void Awake()
    {
        _carRb = GetComponent<Rigidbody>();
        _tireTransforms = GetAllTiresTransforms();
    }
    void FixedUpdate()
    {
        ApplyAcceleration();
        ApplySuspension();
        ApplyTireRotation();
        ApplyTireFriction();
    }

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

    private void ApplyTireRotation()
    {
        float target = Input.GetAxis("Horizontal") * steeringAngle;

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
            
            float desiredGripFactor = Input.GetKey(KeyCode.Space) ? handBreakGripFactor : CalcCurrenTireGripFactor(tire, frictionCurve);
            
            _currentGripFactor = Mathf.Lerp(_currentGripFactor, desiredGripFactor, Time.fixedDeltaTime);
            Debug.Log(_currentGripFactor);
            
            float desiredVelChange = -steeringVel * _currentGripFactor;
            
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            Debug.Log(desiredAccel);
            
            _carRb.AddForceAtPosition(tire.right * _carRb.mass/_tireTransforms.Count * desiredAccel, tire.position);
        }
    }

    void ApplyAcceleration()
    {
        float carSpeed = Vector3.Dot(transform.forward, _carRb.linearVelocity);
        if(carSpeed > topSpeed) return;
        
        if (Input.GetKey(KeyCode.W))
        {
            foreach (var tire in _tireTransforms)
            {
                bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
                if (!isGrounded) continue;
                
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
                
                float availableAcceleration = accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength;
                
                _carRb.AddForceAtPosition(tire.forward * availableAcceleration, tire.position);
            }
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            foreach (var tire in _tireTransforms)
            {
                bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
                if (!isGrounded) continue;
                
                float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
                
                float availableAcceleration = accelerationCurve.Evaluate(normalizedSpeed) * accelerationStrength;
                
                _carRb.AddForceAtPosition(-tire.forward * availableAcceleration, tire.position);
            }
        }
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

    private float CalcCurrenTireGripFactor(Transform tire, AnimationCurve frictionCurve)
    {
        
        Vector3 tireVel = _carRb.GetPointVelocity(tire.position);
        tireVel.y = 0;
        
        float slippingAmount = Mathf.Clamp(Vector3.Dot(tire.right, tireVel.normalized), -1.0f, 1.0f);
        
        return frictionCurve.Evaluate(Mathf.Abs(slippingAmount));
    }
    
        
}
