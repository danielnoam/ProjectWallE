using DNExtensions.Systems.Scriptables;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private ProjectileData projectileData;
    [SerializeField] private SOLayerMask enemyLayerMask;
    
    private float _nextTimeToFire;
    
    private void Update()
    {
        if (_nextTimeToFire > 0)
        {
            _nextTimeToFire -= Time.deltaTime;
        }
        
        if (Mouse.current.leftButton.wasPressedThisFrame && _nextTimeToFire <= 0)
        {
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        _nextTimeToFire = fireRate;
        if (Physics.Raycast(transform.position, transform.forward, out var hit, 200, enemyLayerMask.Value))
        {
            projectileData?.Spawn(enemyLayerMask.Value, transform.position, hit.point);
        }
        else
        {
            projectileData?.Spawn(enemyLayerMask.Value, transform.position, transform.forward * 200);
        }

    }
}