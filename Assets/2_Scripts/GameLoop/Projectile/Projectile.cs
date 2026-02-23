using UnityEngine;

public class Projectile : MonoBehaviour
{



    

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

        switch (_data.movementType)
        {
            case ProjectileMovementType.Linear:
                MoveLinear();
                break;
            case ProjectileMovementType.Arc:
                MoveArc();
                break;
        }

        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= _maxLifetime)
        {
            Destroy(gameObject);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hitSomething || !_isInitialized) return;
        if (((1 << other.gameObject.layer) & _hitLayers) == 0) return;

        _hitSomething = true;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_data.damage, _owner);
        }

        Destroy(gameObject);
    }

    private void MoveLinear()
    {
        transform.position += _direction * (_data.speed * Time.deltaTime);
        transform.forward = _direction;
    }

    private void MoveArc()
    {
        _arcVelocity += Physics.gravity * Time.deltaTime;
        transform.position += _arcVelocity * Time.deltaTime;
        transform.forward = _arcVelocity.normalized;
    }
    
    private void InitializeArc()
    {
        Vector3 toTarget = _targetPosition - _startPosition;
        float travelTime = toTarget.magnitude / _data.speed;
        
        _arcVelocity = toTarget / travelTime - Physics.gravity * travelTime / 2f;
    }

    
    public void Initialize(IDamageable owner, ProjectileData data, LayerMask hitLayers, Vector3 direction, Vector3 targetPosition)
    {
        _owner = owner;
        _data = data;
        _hitLayers = hitLayers;
        _maxLifetime = data.maxLifetime;
        _direction = direction.normalized;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _isInitialized = true;
        if (_data.movementType == ProjectileMovementType.Arc) InitializeArc();
    }
}