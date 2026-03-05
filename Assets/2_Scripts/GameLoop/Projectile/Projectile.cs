using System;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    [SerializeField, AutoGetSelf] private Rigidbody rigidBody;
    private IDamageable _owner;
    private ProjectileData _data;
    private float _maxLifetime;
    private LayerMask _hitLayers;
    
    private bool _isInitialized;
    private bool _hitSomething;
    private float _lifetimeTimer;
    private float _elapsed;
    private Vector3 _direction;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _arcVelocity;


    private void Update()
    {
        if (!_isInitialized) return;
        
        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= _maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;

        switch (_data.movementType)
        {
            case ProjectileMovementType.Linear:
                MoveLinear();
                break;
            case ProjectileMovementType.Arc:
                MoveArc();
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hitSomething || !_isInitialized) return;
        if (((1 << other.gameObject.layer) & _hitLayers) == 0) return;

        _hitSomething = true;

        switch (_data.damageType)
        {
            case ProjectileDamageType.Single:
                
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_data.damage, _owner);
                }
        
                if (other.TryGetComponent<IPushable>(out var pushable))
                {
                    Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                    pushable.Push(pushDirection, _data.pushStrength);
                }
                
                break;
            case ProjectileDamageType.AreaOfEffect:
                
                Collider[] damagedObjects = Physics.OverlapSphere(transform.position, _data.aoeRadius, _hitLayers);
                foreach (var damagedObject in damagedObjects)
                {
                    float distance = Vector3.Distance(transform.position, damagedObject.transform.position);
                    float normalizedDistance = distance / _data.aoeRadius;
                    float damage = _data.damageRange.Lerp(1 - normalizedDistance);
                    
                    if (damagedObject.TryGetComponent<IDamageable>(out var aoeHit))
                    {
                        aoeHit .TakeDamage(damage, _owner);
                    }

                    if (damagedObject.TryGetComponent<IPushable>(out var aoePush))
                    {
                        Vector3 pushDirection = (damagedObject.transform.position - transform.position).normalized;
                        aoePush.Push(pushDirection, _data.pushStrength);
                    }
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }



        Destroy(gameObject);
    }

    private void MoveLinear()
    {
        rigidBody.position += _direction * (_data.speed * Time.fixedDeltaTime);
        rigidBody.rotation = Quaternion.LookRotation(_direction);
    }

    private void MoveArc()
    {
        _arcVelocity += Physics.gravity * Time.fixedDeltaTime;
        rigidBody.position += _arcVelocity * Time.fixedDeltaTime;
        rigidBody.rotation = Quaternion.LookRotation(_arcVelocity.normalized);
    }
    
    private void InitializeArc()
    {
        Vector3 toTarget = _targetPosition - _startPosition;
        float travelTime = toTarget.magnitude / _data.speed;
        
        _arcVelocity = toTarget / travelTime - Physics.gravity * travelTime / 2f;
    }

    
    public void Initialize(ProjectileData data, LayerMask hitLayers, Vector3 direction, Vector3 targetPosition)
    {
        _data = data;
        _hitLayers = hitLayers;
        _maxLifetime = data.maxLifetime;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _direction = direction;

        if (_data.movementType == ProjectileMovementType.Arc) InitializeArc();

        _isInitialized = true;
    }
}