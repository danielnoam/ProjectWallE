using System;
using DNExtensions;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class StructureBeacon : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private AudioSource audioSource;
    
    private Structure _structure;
    
    public Rigidbody Rigidbody => rigidBody;

    private void OnValidate()
    {
        if (!rigidBody) rigidBody = this.GetOrAddComponent<Rigidbody>();
        if (!audioSource) audioSource = this.GetOrAddComponent<AudioSource>();
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (((1 << other.gameObject.layer) & layerMask) == 0) return;
    
        if (_structure)
        {
            Vector3 impactPoint = other.contacts[0].point;
            Vector3 surfaceNormal = other.contacts[0].normal;
            
            StructureBuilder.Instance.CallStructurePod(_structure, impactPoint, surfaceNormal);
        }
    
        Destroy(gameObject);
    }
    
    public void SetStructure(Structure structure)
    {
        _structure = structure;
    }
}