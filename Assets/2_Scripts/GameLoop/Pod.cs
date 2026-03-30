using System.Collections;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CinemachineExtensions;
using ProjectWallE.GameLoop;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class Pod : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float arcHeight = 125f;
    [SerializeField] private float travelDuration = 3f;
    [SerializeField] private float rotationSpeed = 300f;
    [SerializeField] private LayerMask collisionMask;
    
    [Header("Push")]
    [SerializeField] private float pushRange = 15f;
    [SerializeField] private float pushPower = 15f;
    
    [Header("Effects")]
    [SerializeField] private PoolableParticleSystem collisionParticle;
    [SerializeField] private ImpulseSettings collisionImpulseSettings;
    [SerializeField, AudioLibraryID] private string collisionSfx;
    [SerializeField, AutoGetSelf, HideInInspector] private CinemachineImpulseSource impulseSource;
    
    private Vector3 _startPosition;
    private IDeployable _deployable;
    private Vector3 _targetPoint;
    private Vector3 _forward;
    private bool _hasCollided;
    private Coroutine _moveCoroutine;
    

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
        
        Land(impactPoint, surfaceNormal);
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
            if (Physics.Raycast(_targetPoint, Vector3.down, out RaycastHit hit, 200f, collisionMask))
            {
                Land(hit.point, hit.normal);
            }
            else
            {
                Land(_targetPoint, Vector3.up);
            }
        }
    }
    
    
    private void Land(Vector3 impactPoint, Vector3 surfaceNormal)
    {
        // Effects
        impulseSource?.GenerateImpulse(collisionImpulseSettings);
        AudioLibrary.PlayAtPosition(collisionSfx, transform.position);
        if (collisionParticle)
        {
            var particle = ObjectPooler.GetObjectFromPool(collisionParticle, impactPoint, Quaternion.LookRotation(surfaceNormal));
            particle?.Play();
        }
        
        // Push && Damage
        var colliders = Physics.OverlapSphere(impactPoint, pushRange);
        foreach (Collider col in colliders)
        {
            Enemy enemy = col.GetComponentInParent<IDamageable>() as Enemy;
            enemy?.TakeDamage(100f);
            
            IPushable pushable = col.GetComponentInParent<IPushable>();
            if (pushable != null)
            {
                Vector3 direction = (col.transform.position - impactPoint).normalized;
                pushable.Push(direction, pushPower);
            }
        }
        
        // Deployable
        var instance = Instantiate(_deployable as MonoBehaviour);
        ((IDeployable)instance).Deploy(impactPoint, surfaceNormal, _forward);
        
        
        Destroy(gameObject);
    }
    
    public void Initialize<T>(T deployable, Vector3 targetPoint, Vector3 forward) where T : MonoBehaviour, IDeployable
    {
        _startPosition = transform.position;
        _deployable = deployable;
        _targetPoint = targetPoint;
        _forward = forward;
        
        _moveCoroutine = StartCoroutine(MoveInArc());
    }
}