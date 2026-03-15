using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField, AutoGetSelf] private Rigidbody rigidBody;
    [SerializeField, AutoGetSelf] private Collider col;
    
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
    private readonly HashSet<IDamageable> _alreadyHit = new();


    private void Update()
    {
        if (!_isInitialized) return;
        
        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= _maxLifetime)
        {
            ReturnToPool();
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

    private void OnCollisionEnter(Collision other)
    {
        if (_hitSomething || !_isInitialized) return;
        if (((1 << other.gameObject.layer) & _hitLayers) == 0) return;
        
        _hitSomething = true;

        switch (_data.damageType)
        {
            case ProjectileDamageType.Single:
                
                var damageable = other.transform.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_data.damage, _owner);
                }
        
                var pushable = other.transform.GetComponentInParent<IPushable>();
                if (pushable != null)
                {
                    Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                    pushable.Push(pushDirection, _data.pushStrength);
                }
                
                break;
            case ProjectileDamageType.AreaOfEffect:
                
                Collider[] damagedObjects = Physics.OverlapSphere(transform.position, _data.aoeRadius, _hitLayers);
                _alreadyHit.Clear();
                
                foreach (var damagedObject in damagedObjects)
                {
                    var aoeHit = damagedObject.GetComponentInParent<IDamageable>();
                    if (aoeHit != null && !_alreadyHit.Add(aoeHit)) continue;
                    
                    float distance = Vector3.Distance(transform.position, damagedObject.transform.position);
                    float normalizedDistance = distance / _data.aoeRadius;
                    float damage = _data.damageRange.Lerp(1 - normalizedDistance);
                    float push = _data.pushRange.Lerp(1 - normalizedDistance);
                    
                    if (aoeHit != null)
                    {
                        aoeHit.TakeDamage(damage, _owner);
                    }

                    var aoePush = damagedObject.GetComponentInParent<IPushable>();
                    if (aoePush != null)
                    {
                        Vector3 pushDirection = (damagedObject.transform.position - transform.position).normalized;
                        aoePush.Push(pushDirection, push);
                    }
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        if (_data.hitParticle)
        {
            Vector3 hitNormal = other.contacts[0].normal;
            var particle = ObjectPooler.GetObjectFromPool(_data.hitParticle, transform.position, Quaternion.LookRotation(hitNormal));
            particle?.Play();
        }
        AudioLibrary.PlayAtPosition(_data.collisionSFX, transform.position);
        ReturnToPool();
    }

    private void MoveLinear()
    {
        Vector3 newPos = rigidBody.position + _direction * (_data.speed * Time.fixedDeltaTime);
        rigidBody.MovePosition(newPos);
        rigidBody.MoveRotation(Quaternion.LookRotation(_direction));
    }

    private void MoveArc()
    {
        _arcVelocity += Physics.gravity * Time.fixedDeltaTime;
        Vector3 newPos = rigidBody.position + _arcVelocity * Time.fixedDeltaTime;
        rigidBody.MovePosition(newPos);
        rigidBody.MoveRotation(Quaternion.LookRotation(_arcVelocity.normalized));
    }
    private void InitializeArc()
    {
        Vector3 toTarget = _targetPosition - _startPosition;
        float travelTime = toTarget.magnitude / _data.speed;
        
        _arcVelocity = toTarget / travelTime - Physics.gravity * travelTime / 2f;
    }
    
    private void ReturnToPool()
    {
        ObjectPooler.ReturnObjectToPool(this);
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

    public void OnPoolGet()
    {
        
    }

    public void OnPoolReturn()
    {
        _isInitialized = false;
        _hitSomething = false;
        _lifetimeTimer = 0;
    }

    public void OnPoolRecycle()
    {
       
    }
}