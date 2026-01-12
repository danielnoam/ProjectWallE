using System;
using System.Collections;
using DNExtensions;
using DNExtensions.CinemachineImpulseSystem;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class StructurePod : MonoBehaviour
{
    [Header("Pod Settings")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float arcHeight = 125f;
    [SerializeField] private float travelDuration = 3f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private AudioClip collisionSfx;
    [SerializeField] private ImpulseSettings collisionImpulseSettings;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    
    private Vector3 _startPosition;
    private Structure _structure;
    private Vector3 _targetPoint;
    private Vector3 _forward;
    private bool _hasCollided;
    private Coroutine _moveCoroutine;

    private void OnValidate()
    {
        if (!audioSource) audioSource = this.GetOrAddComponent<AudioSource>();
    }
    
    public void Initialize(Structure structure, Vector3 targetPoint, Vector3 surfaceNormal, Vector3 forward)
    {
        _startPosition = transform.position;
        _structure = structure;
        _targetPoint = targetPoint;
        _forward = forward;
    
        _moveCoroutine = StartCoroutine(MoveInArc());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & collisionMask) == 0 || _hasCollided) return;
        _hasCollided = true;
        
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }
        
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 surfaceNormal = collision.contacts[0].normal;
        
        
        if (collisionSfx)
        {
            audioSource.PlayOneShot(collisionSfx);
        }
        
        impulseSource?.GenerateImpulse(collisionImpulseSettings);

        var enemiesInRange = Physics.OverlapSphere(impactPoint, 25f);
        
        foreach (Collider col in enemiesInRange)
        {
            if (col.TryGetComponent(out GroundEnemy enemy))
            {
                Vector3 direction = (col.transform.position - impactPoint).normalized;
                enemy.Push(direction, 50);
            }
        }
        SpawnStructure(impactPoint, surfaceNormal);
    }

    private IEnumerator MoveInArc()
    {
        float elapsed = 0f;
        Vector3 previousPosition = _startPosition;
    
        while (elapsed < travelDuration && !_hasCollided)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;
            Vector3 position = Vector3.Lerp(_startPosition, _targetPoint, t);
            
            position.y += arcHeight * Mathf.Sin(t * Mathf.PI);
            
            Vector3 movementDirection = position - previousPosition;
            if (movementDirection.sqrMagnitude > 0.001f)
            {
                Quaternion directionRotation = Quaternion.LookRotation(movementDirection);
                Quaternion offset = Quaternion.Euler(new Vector3(90,0,0));
                float yRotation = elapsed * rotationSpeed * 360f;
                Quaternion spinRotation = Quaternion.Euler(0, yRotation, 0);
            
                transform.rotation = directionRotation * offset * spinRotation;
            }
        
            transform.position = position;
            previousPosition = position;
            yield return null;
        }
        
        if (!_hasCollided)
        {
            if (Physics.Raycast(_targetPoint, Vector3.down, out RaycastHit hit, 100f, collisionMask))
            {
                SpawnStructure(hit.point, hit.normal);
            }
            else
            {
                SpawnStructure(_targetPoint, Vector3.up);
            }
        }
    }
    
    
    private void SpawnStructure(Vector3 impactPoint, Vector3 surfaceNormal)
    {   

        
        Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        Vector3 projectedForward = Vector3.ProjectOnPlane(_forward, surfaceNormal).normalized;
        
        Quaternion yawRotation = Quaternion.identity;
        if (projectedForward.sqrMagnitude > 0.001f)
        {
            yawRotation = Quaternion.LookRotation(projectedForward, surfaceNormal);
        }
        
        Quaternion finalRotation = yawRotation;
        Vector3 rotatedBottom = finalRotation * _structure.BottomPoint;
        Vector3 structureSpawnPoint = impactPoint - rotatedBottom;
    
        Structure structure = Instantiate(_structure, structureSpawnPoint, finalRotation);
        structure.Build();
    
        Destroy(gameObject);
    }
}