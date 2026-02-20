
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxLifetime = 10f;

    
    private bool _isInitialized;
    private bool _hitSomething;
    private float _speed;
    private float _damage;
    private Vector3 _direction;
    private LayerMask _hitLayers;
    private float _lifetimeTimer;
    private IDamageable _owner;
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        MoveForward();
        UpdateLifeTime();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hitSomething || !_isInitialized) return;
        
        if (((1 << other.gameObject.layer) & _hitLayers) == 0) return;
        
        _hitSomething = true;
        
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage, _owner);
        }
        
        Destroy(gameObject);
    }

    private void MoveForward()
    {
        float distance = _speed * Time.deltaTime;
        
        transform.position += _direction * distance;
        transform.forward = _direction;
    }

    private void UpdateLifeTime()
    {
        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }
    
    public void Initialize(IDamageable owner, float speed, float damage, Vector3 direction, LayerMask hitLayers)
    {
        _owner = owner;
        _speed = speed;
        _damage = damage;
        _direction = direction.normalized;
        _hitLayers = hitLayers;
        _isInitialized = true;
    }
}