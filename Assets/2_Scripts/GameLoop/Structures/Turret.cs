using System;
using System.Collections;
using DNExtensions;
using DNExtensions.Button;
using PrimeTween;
using Unity.VisualScripting;
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
    private Coroutine _scanCoroutine;
    private enum TurretState { Scanning, Idle, Attacking, Broken }
    private Sequence _attackSequence;

    private void Update()
    {
        if (currentState == TurretState.Attacking)
        {
            if (_currentTarget is Component targetComponent && targetComponent)
            {
                _attackTimer += Time.deltaTime;
                Vector3 directionToTarget = (targetComponent.transform.position - headTransform.position).normalized;
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
                    _attackSequence = Sequence.Create();
                    _attackSequence.Group(Tween.PunchScale(headTransform, attackAnimationSettings));
                    _attackSequence.OnComplete(() =>
                    {
                        var projectile = Instantiate(projectilePrefab, headTransform.position, headTransform.rotation);
                        projectile.Initialize(this, projectileSpeed, projectileDamage, directionToTarget, projectileHitLayers);
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
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Idle()
    {
        currentState = TurretState.Idle;
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartScanning()
    {
        currentState = TurretState.Scanning;
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
        }
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, projectileHitLayers);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<Enemy>(out var enemy))
            {
                StartAttackingTarget(enemy);
                return;
            }
        }
        
        _scanCoroutine = StartCoroutine(ScanningRoutine());
    }

    private IEnumerator ScanningRoutine()
    {
        float[] scanAngles = { -90, -45f, 0f, 45f, 90, 0f };
        int currentAngleIndex = 0;

        while (currentState == TurretState.Scanning)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, projectileHitLayers);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<Enemy>(out var enemy))
                {
                    StartAttackingTarget(enemy);
                    yield break;
                }
            }
        
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
            _currentTarget.OnDeath -= HandleTargetDeath;
        }
        
        currentState = TurretState.Attacking;
        _currentTarget = target;
        _currentTarget.OnDeath += HandleTargetDeath;
    }

    private void HandleTargetDeath(IDamageable deadTarget)
    {
        if (deadTarget == _currentTarget)
        {
            deadTarget.OnDeath -= HandleTargetDeath;
            _currentTarget = null;
            StartScanning();
        }
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
            _currentTarget.OnDeath -= HandleTargetDeath;
            _currentTarget = null;
        }
        
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
        
        Tween.LocalRotation(headTransform, Quaternion.Euler(brokenRotation), duration: 0.5f);
    }

    private void OnDestroy()
    {
        if (_attackSequence.isAlive) _attackSequence.Stop();
        
        if (_currentTarget != null)
        {
            _currentTarget.OnDeath -= HandleTargetDeath;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}