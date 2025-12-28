using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] Transform[] tireTransforms;
    
    [Header("Suspension Parameters")]
    public float groundHeight = 0.2f;
    public float suspensionStrength;
    public float suspensionDamping;
    
    [Header("Steering Parameters")]
    public float steeringAngle;
    public float steeringStrength;
    [Range(0,1)] public float tireGripFactor;
    
    [Header("Acceleration Parameters")]
    public float accelerationStrength;
    public float brakeStrength;
    
    private Rigidbody _carRb;
    private Transform _frontRightTire;
    private Transform _frontLeftTire;
    
    float _currentSteering;


    void Start()
    {
        _carRb = GetComponent<Rigidbody>();
        
        _frontRightTire = tireTransforms[0];
        _frontLeftTire = tireTransforms[1];
    }
    void FixedUpdate()
    {
        ApplySuspension();
        ApplyTireRotation();
        ApplyTireFriction();
        ApplyAcceleration();
    }

    private void ApplySuspension()
    {
        foreach (var tire in tireTransforms)
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

        _currentSteering = Mathf.MoveTowards(
            _currentSteering,
            target,
            steeringStrength * Time.fixedDeltaTime
        );

        Quaternion rot = Quaternion.Euler(0f, _currentSteering, 0f);

        _frontLeftTire.localRotation = rot;
        _frontRightTire.localRotation = rot;
    }

    private void ApplyTireFriction()
    {
        foreach (Transform t in tireTransforms)
        {
            bool isGrounded = Physics.Raycast(t.position, -t.up, out RaycastHit tireHit, groundHeight);
            if (!isGrounded) continue;
            
            Vector3 tireVel = _carRb.GetPointVelocity(t.position);
            
            float steeringVel = Vector3.Dot(t.right, tireVel);
            
            float desiredVelChange = -steeringVel * tireGripFactor;
            
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            
            _carRb.AddForceAtPosition(t.right * _carRb.mass/tireTransforms.Length * desiredAccel, t.position);
        }
    }


    void ApplyAcceleration()
    {
        if (Input.GetKey(KeyCode.W))
        {
            foreach (var tire in tireTransforms)
            {
                bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
                if (!isGrounded) continue;
                
                _carRb.AddForceAtPosition(tire.forward * accelerationStrength, tire.position);
            }
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            foreach (var tire in tireTransforms)
            {
                bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
                if (!isGrounded) continue;
                _carRb.AddForceAtPosition(-tire.forward * brakeStrength, tire.position);
            }
        }
    }
    
        
}
