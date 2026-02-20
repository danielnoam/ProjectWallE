using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float maxLifetime = 10f;

    private bool _isInitialized;
    private bool _hitSomething;
    private float _lifetimeTimer;
    private float _elapsed;
    private IDamageable _owner;
    private Vector3 _direction;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _arcVelocity;
    private ProjectileSettings _settings;

    private void Update()
    {
        if (!_isInitialized) return;

        switch (_settings.movementType)
        {
            case ProjectileMovementType.Linear:
                MoveLinear();
                break;
            case ProjectileMovementType.Arc:
                MoveArc();
                break;
        }

        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hitSomething || !_isInitialized) return;
        if (((1 << other.gameObject.layer) & _settings.hitLayers) == 0) return;

        _hitSomething = true;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_settings.damage, _owner);
        }

        Destroy(gameObject);
    }

    private void MoveLinear()
    {
        transform.position += _direction * (_settings.speed * Time.deltaTime);
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
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
    
        float y = toTarget.y;
        float xz = toTargetXZ.magnitude;
        float g = Mathf.Abs(Physics.gravity.y);
        float angle = 45f * Mathf.Deg2Rad;
    
        float speed = Mathf.Sqrt((xz * xz * g) / (xz * Mathf.Sin(2 * angle) - 2 * y * Mathf.Cos(angle) * Mathf.Cos(angle)));
    
        _arcVelocity = toTargetXZ.normalized * (speed * Mathf.Cos(angle)) + Vector3.up * (speed * Mathf.Sin(angle));
    }

    public void Initialize(IDamageable owner, ProjectileSettings settings, Vector3 direction, Vector3 targetPosition)
    {
        _owner = owner;
        _settings = settings;
        _direction = direction.normalized;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _isInitialized = true;
        if (_settings.movementType == ProjectileMovementType.Arc) InitializeArc();
    }
}