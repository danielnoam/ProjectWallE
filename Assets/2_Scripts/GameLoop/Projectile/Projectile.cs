using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] private bool reyCastCollision;
    [SerializeField] private SOLayerMask environmentLayerMask;
    [SerializeField] private SOLayerMask damageableLayers;
    [SerializeField, AutoGetSelf] private Rigidbody rigidBody;
    [SerializeField, AutoGetSelf] private Collider col;
    
    private Team _ownerTeam;
    private IDamageable _owner;
    private SOProjectileData _data;
    private float _maxLifetime;
    private float _damageMultiplier = 1f;
    
    private bool _isInitialized;
    private bool _hitSomething;
    private float _lifetimeTimer;
    private Vector3 _lastPosition;
    private Vector3 _direction;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _arcVelocity;
    private readonly HashSet<IDamageable> _alreadyHit = new();

    private int CombinedMask => damageableLayers.Value | environmentLayerMask.Value;

    private void Update()
    {
        if (!_isInitialized) return;
        
        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= _maxLifetime)
        {
            ObjectPooler.ReturnObjectToPool(this);
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

        if (reyCastCollision && !_hitSomething)
        {
            Vector3 currentPosition = rigidBody.position;
            Vector3 sweepDelta = currentPosition - _lastPosition;
            float sweepDistance = sweepDelta.magnitude;

            if (sweepDistance > 0f && Physics.Raycast(_lastPosition, sweepDelta.normalized, out RaycastHit hit, sweepDistance, CombinedMask, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit.point, hit.normal, hit.collider.gameObject, hit.collider);
            }
        }
        
        _lastPosition = rigidBody.position;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_hitSomething || !_isInitialized) return;

        int layer = 1 << other.gameObject.layer;
        if ((layer & CombinedMask) == 0) return;

        Vector3 contactPoint = other.contacts[0].point;
        Vector3 contactNormal = other.contacts[0].normal;
        HandleHit(contactPoint, contactNormal, other.gameObject, other.collider);
    }

    private void HandleHit(Vector3 hitPoint, Vector3 hitNormal, GameObject hitObject, Collider hitCollider)
    {
        _hitSomething = true;
        bool hitDamageable = false;

        int layer = 1 << hitObject.layer;
        bool hitDamageableLayer = (layer & damageableLayers.Value) != 0;

        if (_data.damageType == ProjectileDamageType.AreaOfEffect)
        {
            hitDamageable = ProcessAoeDamage(hitPoint);
        }
        else if (hitDamageableLayer)
        {
            hitDamageable = ProcessSingleDamage(hitPoint, hitCollider);
        }

        if (_data.hitParticle)
        {
            var particle = ObjectPooler.GetObjectFromPool(_data.hitParticle, hitPoint, Quaternion.LookRotation(hitNormal));
            particle?.Play();
        }

        if (!hitDamageable)
        {
            AudioLibrary.PlayAtPosition(_data.collisionSFX, hitPoint);
        }

        ObjectPooler.ReturnObjectToPool(this);
    }

    private bool ProcessSingleDamage(Vector3 hitPoint, Collider hitCollider)
    {
        bool hitDamageable = false;
        
        var damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null && _ownerTeam.CanDamage(damageable.Team))
        {
            hitDamageable = true;
            damageable.TakeDamage(_data.damage * _damageMultiplier, _owner);
        }

        var pushable = hitCollider.GetComponentInParent<IPushable>();
        if (pushable != null)
        {
            Vector3 pushDirection = (hitPoint - transform.position).normalized;
            pushable.Push(pushDirection, _data.pushStrength);
        }

        return hitDamageable;
    }

    private bool ProcessAoeDamage(Vector3 hitPoint)
    {
        bool hitDamageable = false;
        Collider[] damagedObjects = Physics.OverlapSphere(hitPoint, _data.aoeRadius, damageableLayers.Value);
        _alreadyHit.Clear();

        foreach (var damagedObject in damagedObjects)
        {
            IDamageable target = damagedObject.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            if (target is EnemyDamageRelay relay) target = relay.Parent;
            if (target == null || !_alreadyHit.Add(target)) continue;
            if (!_ownerTeam.CanDamage(target.Team)) continue;

            hitDamageable = true;

            float distance = Vector3.Distance(hitPoint, damagedObject.transform.position);
            float normalizedDistance = distance / _data.aoeRadius;
            float damage = _data.damageRange.Lerp(1 - normalizedDistance);
            float push = _data.pushRange.Lerp(1 - normalizedDistance);

            target.TakeDamage(damage * _damageMultiplier, _owner);

            var aoePush = damagedObject.GetComponentInParent<IPushable>();
            if (aoePush != null)
            {
                Vector3 pushDirection = (damagedObject.transform.position - hitPoint).normalized;
                aoePush.Push(pushDirection, push);
            }
        }

        return hitDamageable;
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
    
    private void ResetData()
    {
        _isInitialized = false;
        _hitSomething = false;
        _lifetimeTimer = 0;
        
        _data = null;
        _owner = null;
        _ownerTeam = default;
        _maxLifetime = 0;
        _damageMultiplier = 1f;
        _startPosition = Vector3.zero;
        _targetPosition = Vector3.zero;
        _direction = Vector3.zero;
    }

    public void Initialize(SOProjectileData data, Vector3 direction, Vector3 targetPosition, IDamageable owner, float damageMultiplier = 1f)
    {
        _data = data;
        _owner = owner;
        _ownerTeam = owner?.Team ?? Team.Neutral;
        _maxLifetime = data.maxLifetime;
        _damageMultiplier = damageMultiplier;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _direction = direction;
        _lastPosition = transform.position;

        if (_data.movementType == ProjectileMovementType.Arc) InitializeArc();

        _isInitialized = true;
    }

    public void OnPoolGet()
    {
        ResetData();
    }

    public void OnPoolReturn() { }
    public void OnPoolRecycle() { }
}