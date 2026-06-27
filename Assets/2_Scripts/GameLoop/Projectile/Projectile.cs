using System;
using System.Collections.Generic;
using DNExtensions.Systems.AudioLibrary;
using DNExtensions.Systems.ObjectPooling;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
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
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_hitSomething || !_isInitialized) return;

        int layer = 1 << other.gameObject.layer;
        if ((layer & CombinedMask) == 0) return;

        _hitSomething = true;
        bool hitDamageable = false;

        if ((layer & damageableLayers.Value) != 0)
        {
            hitDamageable = ProcessDamage(other);
        }

        if (_data.hitParticle)
        {
            Vector3 hitNormal = other.contacts[0].normal;
            var particle = ObjectPooler.GetObjectFromPool(_data.hitParticle, transform.position,
                Quaternion.LookRotation(hitNormal));
            particle?.Play();
        }

        if (!hitDamageable)
        {
            AudioLibrary.PlayAtPosition(_data.collisionSFX, transform.position);
        }

        ObjectPooler.ReturnObjectToPool(this);
    }

    private bool ProcessDamage(Collision other)
    {
        switch (_data.damageType)
        {
            case ProjectileDamageType.Single:
                return ProcessSingleDamage(other);
            case ProjectileDamageType.AreaOfEffect:
                return ProcessAoeDamage();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool ProcessSingleDamage(Collision other)
    {
        bool hitDamageable = false;
        
        var damageable = other.collider.GetComponentInParent<IDamageable>();
        if (damageable != null && _ownerTeam.CanDamage(damageable.Team))
        {
            hitDamageable = true;
            damageable.TakeDamage(_data.damage * _damageMultiplier, _owner);
        }

        var pushable = other.collider.GetComponentInParent<IPushable>();
        if (pushable != null)
        {
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;
            pushable.Push(pushDirection, _data.pushStrength);
        }

        return hitDamageable;
    }

    private bool ProcessAoeDamage()
    {
        bool hitDamageable = false;
        Collider[] damagedObjects = Physics.OverlapSphere(transform.position, _data.aoeRadius, damageableLayers.Value);
        _alreadyHit.Clear();

        foreach (var damagedObject in damagedObjects)
        {
            IDamageable target = damagedObject.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            if (target is EnemyDamageRelay relay) target = relay.Parent;
            if (target == null || !_alreadyHit.Add(target)) continue;
            if (!_ownerTeam.CanDamage(target.Team)) continue;

            hitDamageable = true;

            float distance = Vector3.Distance(transform.position, damagedObject.transform.position);
            float normalizedDistance = distance / _data.aoeRadius;
            float damage = _data.damageRange.Lerp(1 - normalizedDistance);
            float push = _data.pushRange.Lerp(1 - normalizedDistance);

            target.TakeDamage(damage * _damageMultiplier, _owner);

            var aoePush = damagedObject.GetComponentInParent<IPushable>();
            if (aoePush != null)
            {
                Vector3 pushDirection = (damagedObject.transform.position - transform.position).normalized;
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