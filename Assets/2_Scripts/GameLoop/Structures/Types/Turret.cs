using System;
using System.Collections;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public class TurretLevelData : StructureLevelData
{
    public float detectionRadius = 25f;
    public float attackRotationSpeed = 125f;
    public float attackCooldown = 0.3f;
    [SOSelector("Assets/Data")] public ProjectileData projectileData;
}

public enum TurretState { Scanning, Attacking, Broken }

public abstract class Turret : Structure
{
    [Header("Turret")]
    [SerializeReference, DrawSerializeReference] private TurretLevelData[] levels = Array.Empty<TurretLevelData>();
    [SerializeField, SOSelector("Assets/Data")] protected SOLayerMask hitLayers;
    [SerializeField] private float scanRotationSpeed = 35f;
    [SerializeField] private float scanWaitDuration = 1.5f;
    [SerializeField] protected Vector3 brokenRotation;
    [SerializeField] protected Transform headTransform;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected ShakeSettings attackAnimationSettings;

    private float _attackTimer;
    private TurretState _currentState;
    private IDamageable _currentTarget;
    private Coroutine _scanRoutine;
    private Sequence _attackSequence;

    protected TurretLevelData CurrentTurretLevelData => (TurretLevelData)Levels[currentUpgradeLevel - 1];
    protected override StructureLevelData[] Levels => levels;
    

    protected abstract Quaternion GetAimRotation(Vector3 targetPosition);
    protected abstract bool CanFire(Vector3 targetPosition);

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_attackSequence.isAlive) _attackSequence.Stop();
        if (_currentTarget != null) _currentTarget.OnDeath -= OnTargetDeath;
    }

    private void Update()
    {
        if (_currentState == TurretState.Attacking)
        {
            if (_currentTarget is MonoBehaviour target && target)
            {
                _attackTimer += Time.deltaTime;
                Vector3 targetPosition = target.transform.position;

                headTransform.rotation = Quaternion.RotateTowards(
                    headTransform.rotation,
                    GetAimRotation(targetPosition),
                    CurrentTurretLevelData.attackRotationSpeed * Time.deltaTime
                );

                if (!CanFire(targetPosition)) return;

                if (_attackTimer >= CurrentTurretLevelData.attackCooldown)
                {
                    Vector3 capturedPosition = targetPosition;
                    _attackSequence = Sequence.Create();
                    _attackSequence.Group(Tween.PunchScale(headTransform, attackAnimationSettings));
                    _attackSequence.OnComplete(() => Fire(capturedPosition));
                    _attackTimer = 0f;
                }
            }
            else
            {
                _currentTarget = null;
                StartScanning();
            }
        }

        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}\nState: {_currentState}";
    }

    private void Fire(Vector3 targetPosition)
    {
        var direction = (targetPosition - firePoint.position).normalized;
        CurrentTurretLevelData.projectileData?.Spawn(hitLayers.Value, firePoint.position, direction, targetPosition, this);
    }

    protected override void OnBuild()
    {
        StartScanning();
    }

    protected override void OnUpgrade()
    {
        StartScanning();
    }
    
    protected override void OnFix()
    {
        StartScanning();
    }


    protected override void OnBreak()
    {
        _currentState = TurretState.Broken;

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

        var endRotation = headTransform.localEulerAngles;
        endRotation.x = brokenRotation.x;
        Tween.LocalRotation(headTransform, Quaternion.Euler(endRotation), duration: 1f);
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartScanning()
    {
        _currentState = TurretState.Scanning;

        if (_scanRoutine != null) StopCoroutine(_scanRoutine);

        if (!TryAcquireTarget()) _scanRoutine = StartCoroutine(ScanningRoutine());
    }

    private IEnumerator ScanningRoutine()
    {
        float[] scanAngles = { -90, -45f, 0f, 45f, 90, 0f };
        int currentAngleIndex = 0;

        while (_currentState == TurretState.Scanning)
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
            _currentTarget.OnDeath -= OnTargetDeath;

        _currentState = TurretState.Attacking;
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, CurrentTurretLevelData.detectionRadius, hitLayers.Value);
        foreach (var hitCollider in hitColliders)
        {
            var enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy)
            {
                StartAttackingTarget(enemy);
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!Application.isPlaying || levels == null) return;

            
        Gizmos.color = Color.red;
        if (levels is { Length: > 0 } && levels[0] != null)
        {
            Gizmos.DrawWireSphere(transform.position, CurrentTurretLevelData.detectionRadius);
        }
    }
#endif

}