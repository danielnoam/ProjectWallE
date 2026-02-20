using System.Collections;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;
using Sequence = PrimeTween.Sequence;

public abstract class Turret : Structure
{
    [Header("Turret")]
    [SerializeField] private float scanRotationSpeed = 35f;
    [SerializeField] private float scanWaitDuration = 1.5f;
    [SerializeField] private float detectionRadius = 25f;
    [SerializeField] private Vector3 brokenRotation;
    [SerializeField] protected Transform headTransform;

    [Header("Attack")]
    [SerializeField] private ShakeSettings attackAnimationSettings;
    [SerializeField] protected float attackRotationSpeed = 125f;
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private Transform firePoint;
    [SerializeField, PrefabSelector("Assets/Prefabs")] protected Projectile projectilePrefab;
    [SerializeField] protected ProjectileSettings projectileSettings;

    private float _attackTimer;
    private TurretState _currentState;
    private IDamageable _currentTarget;
    private Coroutine _scanRoutine;
    private Sequence _attackSequence;

    private enum TurretState { Scanning, Idle, Attacking, Broken }

    protected abstract Quaternion GetAimRotation(Vector3 targetPosition);
    protected abstract bool CanFire(Vector3 targetPosition);

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
        if (_currentState == TurretState.Attacking)
        {
            if (_currentTarget is MonoBehaviour target && target)
            {
                _attackTimer += Time.deltaTime;
                Vector3 targetPosition = target.transform.position;

                headTransform.rotation = Quaternion.RotateTowards(
                    headTransform.rotation,
                    GetAimRotation(targetPosition),
                    attackRotationSpeed * Time.deltaTime
                );

                if (!CanFire(targetPosition)) return;

                if (_attackTimer >= attackCooldown)
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

        StateInfo = $"State: {_currentState} \nHealth: {CurrentHealth}/{startHealth}";
    }

    private void Fire(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - headTransform.position).normalized;
        var projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.Initialize(this, projectileSettings, direction, targetPosition);
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

        Tween.LocalRotation(headTransform, Quaternion.Euler(brokenRotation), duration: 0.5f);
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Idle()
    {
        _currentState = TurretState.Idle;
        if (_scanRoutine != null)
        {
            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartScanning()
    {
        _currentState = TurretState.Scanning;

        if (_scanRoutine != null)
            StopCoroutine(_scanRoutine);

        if (!TryAcquireTarget())
            _scanRoutine = StartCoroutine(ScanningRoutine());
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
        {
            _currentTarget.OnDeath -= OnTargetDeath;
        }

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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, projectileSettings.hitLayers);
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