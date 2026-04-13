using System;
using System.Collections;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class TurretLevelData : StructureLevelData
{
    public float detectionRadius = 25f;
    public float attackRotationSpeed = 125f;
    public float attackCooldown = 0.3f;
    [SOSelector("Assets/6_Data")] public SOProjectileData soProjectileData;
}

public enum TurretState { Scanning, Attacking, Broken }

public abstract class Turret : Structure
{
    [Header("Turret")]
    [SerializeReference, DrawSerializeReference] private TurretLevelData[] levels = Array.Empty<TurretLevelData>();
    [SerializeField, SOSelector("Assets/6_Data")] protected SOLayerMask hitLayers;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected float angleThreshold = 10f;
    [SerializeField] protected Transform horizontalSwivel;
    [SerializeField] protected Transform verticalSwivel;
    [SerializeField] protected Vector3 brokenRotation;
    [SerializeField] private float scanRotationSpeed = 35f;
    [SerializeField] private float scanWaitDuration = 1.5f;
    [SerializeField] private VisualEffectAction shootEffect;

    private float _attackTimer;
    private TurretState _currentState;
    private IDamageable _currentTarget;
    private Coroutine _scanRoutine;
    private Sequence _attackSequence;

    protected TurretLevelData CurrentTurretLevelData => (TurretLevelData)Levels[CurrentUpgradeLevel - 1];

    public override StructureLevelData[] Levels => levels;

    
    
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
                RotateToTarget(targetPosition, CurrentTurretLevelData.attackRotationSpeed * Time.deltaTime);

                if (!CanFire(targetPosition)) return;

                if (_attackTimer >= CurrentTurretLevelData.attackCooldown)
                    Fire(targetPosition);
            }
            else
            {
                _currentTarget = null;
                StartScanning();
            }
        }

        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}\nState: {_currentState}";
    }

    protected virtual void RotateToTarget(Vector3 targetPosition, float rotSpeed)
    {
        // Yaw
        Vector3 flatDir = targetPosition - horizontalSwivel.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            horizontalSwivel.rotation = Quaternion.RotateTowards(
                horizontalSwivel.rotation,
                Quaternion.LookRotation(flatDir),
                rotSpeed
            );
        }

        // Pitch
        Vector3 localDir = horizontalSwivel.InverseTransformPoint(targetPosition);
        float pitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
        verticalSwivel.localRotation = Quaternion.RotateTowards(
            verticalSwivel.localRotation,
            Quaternion.Euler(-pitch, 0f, 0f),
            rotSpeed
        );
    }

    private void Fire(Vector3 targetPosition)
    {
        var direction = (targetPosition - firePoint.position).normalized;
        CurrentTurretLevelData.soProjectileData?.Spawn(hitLayers.Value, firePoint.position, direction, targetPosition, this);
        shootEffect?.Play(firePoint.position);
        _attackTimer = 0f;
    }

    protected override void OnBuild() => StartScanning();
    protected override void OnUpgrade() => StartScanning();
    protected override void OnFix() => StartScanning();

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

        Tween.LocalRotation(verticalSwivel, Quaternion.Euler(brokenRotation), duration: 1f);
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
        Vector2 scanPitchRange = new Vector2(10f, 55f);
        
        int currentAngleIndex = 0;

        while (_currentState == TurretState.Scanning)
        {
            if (TryAcquireTarget()) yield break;

            Quaternion targetRotation = Quaternion.Euler(0, scanAngles[currentAngleIndex], 0);
            float targetPitch = Random.Range(scanPitchRange.x, scanPitchRange.y);
            Quaternion pitchRotation = Quaternion.Euler(-targetPitch, 0f, 0f);

            while (Quaternion.Angle(horizontalSwivel.localRotation, targetRotation) > 0.1f)
            {
                float step = scanRotationSpeed * Time.deltaTime;
                horizontalSwivel.localRotation = Quaternion.RotateTowards(
                    horizontalSwivel.localRotation, targetRotation, step);
                verticalSwivel.localRotation = Quaternion.RotateTowards(
                    verticalSwivel.localRotation, pitchRotation, step);
                yield return null;
            }

            yield return new WaitForSeconds(scanWaitDuration);
            currentAngleIndex = (currentAngleIndex + 1) % scanAngles.Length;
        }
    }

    private void StartAttackingTarget(IDamageable target)
    {
        if (target == null) return;

        if (_currentTarget != null) _currentTarget.OnDeath -= OnTargetDeath;

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
            if (enemy && enemy.IsAlive)
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