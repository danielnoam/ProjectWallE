using System;
using System.Collections;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

public class Turret : Structure
{

    [Header("Turret Settings")]
    [SerializeField] private float rotationSpeed = 35f;
    [SerializeField] private float waitDuration = 1.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private Transform headTransform;
    [SerializeField, ReadOnly] private TurretState currentState = TurretState.Idle;
    
    
    private float _attackTimer;
    private IDamageable _currentTarget;
    private Coroutine _scanCoroutine;
    private enum TurretState { Scanning, Idle, Attacking, Broken }


    private void Update()
    {
        if (currentState == TurretState.Attacking)
        {
            if (_currentTarget != null)
            {
                _attackTimer += Time.deltaTime;
                Vector3 directionToTarget = (_currentTarget.Transform().position - headTransform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                headTransform.rotation = Quaternion.RotateTowards(
                    headTransform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            
                if (_attackTimer >= attackCooldown)
                {
                    _currentTarget?.TakeDamage(attackDamage, this);
                    _attackTimer = 0f;
                }
            }
            else
            {
                StartScanning();
            }
        }
        
        stateInfo = $"State: {currentState} \nHealth: {currentHealth}/{startHealth}";
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if (currentState != TurretState.Scanning || _currentTarget != null) return;
        
        
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            StartAttackingTarget(enemy);
        }
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
        _scanCoroutine = StartCoroutine(ScanningRoutine());
    }

    private IEnumerator ScanningRoutine()
    {
        float[] scanAngles = { -90, -45f, 0f, 45f, 90, 0f };
        int currentAngleIndex = 0;

        while (currentState == TurretState.Scanning)
        {
            Quaternion targetRotation = Quaternion.Euler(0, scanAngles[currentAngleIndex], 0);
            
            while (Quaternion.Angle(headTransform.localRotation, targetRotation) > 0.1f)
            {
                headTransform.localRotation = Quaternion.RotateTowards(
                    headTransform.localRotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }
            yield return new WaitForSeconds(waitDuration);
            
            currentAngleIndex = (currentAngleIndex + 1) % scanAngles.Length;
        }
    }

    private void StartAttackingTarget(IDamageable target)
    {
        if (target == null) return;
        
        currentState = TurretState.Attacking;
        _currentTarget = target;
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
        _currentTarget = null;
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
    }


}
