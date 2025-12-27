using System;
using System.Collections;
using DNExtensions;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class StructurePod : MonoBehaviour
{
    [Header("Pod Settings")]
    [SerializeField] private float arcHeight = 100f;
    [SerializeField] private float travelDuration = 2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LayerMask collisionMask;
    
    private Vector3 _startPosition;
    private Structure _structure;
    private Vector3 _targetPoint;
    private bool _hasCollided;
    private Coroutine _moveCoroutine;

    private void OnValidate()
    {
        if (!audioSource) audioSource = this.GetOrAddComponent<AudioSource>();
    }
    
    public void Initialize(Structure structure, Vector3 targetPoint, Vector3 surfaceNormal)
    {
        _startPosition = transform.position;
        _structure = structure;
        _targetPoint = targetPoint;
        
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
                transform.rotation = directionRotation * offset;
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
        Vector3 rotatedBottom = surfaceRotation * _structure.BottomPoint;
        Vector3 structureSpawnPoint = impactPoint - rotatedBottom;
        
        Structure structure = Instantiate(_structure, structureSpawnPoint, surfaceRotation);
        structure.Build();
        
        Destroy(gameObject);
    }
}