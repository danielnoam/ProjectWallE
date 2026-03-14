using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectileData projectileData;
    [SerializeField] private SOLayerMask enemyLayerMask;
    [SerializeField, AutoGetSelf, HideInInspector] private PlayerManager playerManager;
    
    private float _nextTimeToFire;
    private Camera _mainCamera;
    
    
    
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (_nextTimeToFire > 0)
        {
            _nextTimeToFire -= Time.deltaTime;
        }
        
        if (Mouse.current.leftButton.isPressed && _nextTimeToFire <= 0 && playerManager.CanShoot)
        {
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        _nextTimeToFire = fireRate;
        Vector3 position = firePoint ? firePoint.position : transform.position;
        Vector3 direction = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).direction;
        projectileData?.Spawn(enemyLayerMask.Value, position, direction.Add(aimOffset));
    }
}