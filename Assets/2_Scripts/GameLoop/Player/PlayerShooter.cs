using System.Collections.Generic;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerShooter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private ProjectileData projectileData;
    [SerializeField] private SOLayerMask enemyLayerMask;
    [SerializeField] private Transform firePoint;
    [SerializeField, AutoGetScene] private Camera mainCamera;
    
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
        if (!projectileData || !firePoint) return;

        _nextTimeToFire = fireRate;
    
        var lookDir = transform.forward;
        projectileData.Spawn(null, enemyLayerMask.Value, firePoint.position, lookDir);
    }
}