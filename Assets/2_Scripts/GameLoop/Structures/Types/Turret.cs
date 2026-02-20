using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;
using Sequence = PrimeTween.Sequence;

public class Turret : Structure
{
    [Header("Turret Settings")]
    [SerializeField] private float scanRotationSpeed = 35f;
    [SerializeField] private float scanWaitDuration = 1.5f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private Vector3 brokenRotation;
    [SerializeField] private Transform headTransform;
    [SerializeField] private LayerMask targetScanLayers;
    [Separator]
    [SerializeField] private ShakeSettings attackAnimationSettings;
    [SerializeField] private float attackRotationSpeed = 100f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float projectileDamage = 20f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private LayerMask projectileHitLayers;
    [SerializeField] private Projectile projectilePrefab;
    [Separator]
    [SerializeField, ReadOnly] private TurretState currentState = TurretState.Idle;
    
    private float _attackTimer;
    private IDamageable _currentTarget;
    private Coroutine _scanRoutine;
    private enum TurretState { Scanning, Idle, Attacking, Broken }
    private Sequence _attackSequence;
    
    private void OnDestroy()
    {
        if (_attackSequence.isAlive) _attackSequence.Stop();
        
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= OnTargetDeath;
        }
    }


    private void Update()
    {
        if (currentState == TurretState.Attacking)
        {
            if (_currentTarget is MonoBehaviour target && target)
            {
                _attackTimer += Time.deltaTime;
                Vector3 directionToTarget = (target.transform.position - headTransform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                headTransform.rotation = Quaternion.RotateTowards(
                    headTransform.rotation,
                    targetRotation,
                    attackRotationSpeed * Time.deltaTime
                );
                
                
                float angleToTarget = Quaternion.Angle(headTransform.rotation, targetRotation);
                if (angleToTarget > 10f) return;
            
                if (_attackTimer >= attackCooldown)
                {
                    Vector3 fireDirection = directionToTarget;
                    _attackSequence = Sequence.Create();
                    _attackSequence.Group(Tween.PunchScale(headTransform, attackAnimationSettings));
                    _attackSequence.OnComplete(() =>
                    {
                        var projectile = Instantiate(projectilePrefab, headTransform.position, headTransform.rotation);
                        projectile.Initialize(this, projectileSpeed, projectileDamage, fireDirection, projectileHitLayers);
                    });
                    _attackTimer = 0f;
                }
            }
            else
            {
                _currentTarget = null;
                StartScanning();
            }
        }
    
        StateInfo = $"State: {currentState} \nHealth: {currentHealth}/{startHealth}";
    }
    
    protected override void OnBuild()
    {
        StartScanning();
    }

    protected override void OnUpgrade()
    {
        
    }

    protected override void OnBreak()
    {
        currentState = TurretState.Broken;
        
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= OnTargetDeath;
            _currentTarget = null;
        }
        
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }
        
        Tween.LocalRotation(headTransform, Quaternion.Euler(brokenRotation), duration: 0.5f);
    }
    
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Idle()
    {
        currentState = TurretState.Idle;
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartScanning()
    {
        currentState = TurretState.Scanning;
        
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
        }

        if (!TryAcquireTarget())
        {
            _scanRoutine = StartCoroutine(ScanningRoutine());
        }
        
    }

    private IEnumerator ScanningRoutine()
    {
        float[] scanAngles = { -90, -45f, 0f, 45f, 90, 0f };
        int currentAngleIndex = 0;

        while (currentState == TurretState.Scanning)
        {
            if (TryAcquireTarget()) yield break;
        
            Quaternion targetRotation = Quaternion.Euler(0, scanAngles[currentAngleIndex], 0);
        
            while (Quaternion.Angle(headTransform.localRotation, targetRotation) > 0.1f)
            {
                headTransform.localRotation = Quaternion.RotateTowards(
                    headTransform.localRotation, 
                    targetRotation, 
                    scanRotationSpeed * Time.deltaTime
                );
                yield return null;
            }
            yield return new WaitForSeconds(scanWaitDuration);
        
            currentAngleIndex = (currentAngleIndex + 1) % scanAngles.Length;
        }
    }

    private void StartAttackingTarget(IDamageable target)
    {
        if (target == null) return;
        
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= OnTargetDeath;
        }
        
        currentState = TurretState.Attacking;
        _currentTarget = target;
        _currentTarget.OnDeath += OnTargetDeath;
    }

    private void OnTargetDeath(IDamageable deadTarget)
    {
        if (deadTarget == _currentTarget)
        {
            deadTarget.OnDeath -= OnTargetDeath;
            _currentTarget = null;
            StartScanning();
        }
    }
    
    private bool TryAcquireTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, projectileHitLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<Enemy>(out var enemy))
            {
                StartAttackingTarget(enemy);
                return true;
            }
        }
        return false;
    }
    
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}