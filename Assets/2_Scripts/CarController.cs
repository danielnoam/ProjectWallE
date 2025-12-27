using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] Transform[] tireTransforms;
    
    [Header("Suspension Parameters")]
    public float groundHeight = 0.2f;
    public float suspensionStrength;
    public float suspensionDamping;
    
    [Header("Acceleration Parameters")]
    public float accelerationStrength;
    public float brakeStrength;
    
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        ApplySuspension();
        ApplyAcceleration();
    }

    private void ApplySuspension()
    {
        foreach (var tire in tireTransforms)
        {
            bool isGrounded = Physics.Raycast(tire.position, -tire.up, out RaycastHit tireHit, groundHeight);
            if (!isGrounded) continue;
            
            float offset = tireHit.distance - groundHeight;
            
            float suspensionForce = (-offset * suspensionStrength) - (_rb.GetPointVelocity(tire.position).y * suspensionDamping);
            
            _rb.AddForceAtPosition(tire.up * suspensionForce, tire.position);
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
                
                _rb.AddForceAtPosition(tire.forward * accelerationStrength, tire.position);
            }
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            foreach (var tire in tireTransforms)
            {
                bool isGrounded = Physics.Raycast(tire.position, -tire.up, out _, groundHeight);
                if (!isGrounded) continue;
                _rb.AddForceAtPosition(-tire.forward * brakeStrength, tire.position);
            }
        }
    }
    
        
}
