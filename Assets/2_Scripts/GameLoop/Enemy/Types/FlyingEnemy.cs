using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : Enemy
{
    [Header("Flying")]
    [SerializeField] private float stopDistance = 30f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float moveSpeed = 17F;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 20f;

    [Header("Height")]
    [SerializeField] private float flightHeight = 8f;
    [SerializeField] private float heightAdjustSpeed = 4f;
    [SerializeField] private float heightCheckDistance = 30f;
    [SerializeField] private LayerMask heightMask;
    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 1.2f;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private float avoidanceRadius = 4f;
    [SerializeField] private float avoidanceForce = 15f;
    [SerializeField] private LayerMask separationMask;
    
    [Header("Propellers")]
    [SerializeField] private Transform[] propellersBLFR = Array.Empty<Transform>();
    [SerializeField] private Transform[] propellersBRFL = Array.Empty<Transform>();
    [SerializeField] private float propellerRotationSpeed = 1000;
    
    [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;
    private const float ArrivalThreshold = 3f;
    private const int SeparationCheckInterval = 5;

    private Vector3 _destination;
    private int _separationFrameOffset;
    private static int _frameOffsetCounter;

    protected override void Initialize()
    {
        base.Initialize();
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

        _separationFrameOffset = _frameOffsetCounter % SeparationCheckInterval;
        _frameOffsetCounter++;
    }

    protected override void SetDestination()
    {
        if (!IsTargetValid(CurrentTarget)) return;

        Vector3 targetPos = CurrentTarget.transform.position;
        Vector3 directionToTarget = (transform.position - targetPos).normalized;

        var offset = Random.insideUnitSphere * RandomRange;
        var destination = targetPos + directionToTarget * stopDistance + offset;
        _destination = new Vector3(destination.x, 0, destination.z);
    }

    protected override void OnPush(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }
    
    protected override void OnUpdate()
    {
        RotatePropellers();
        State = IsTargetValid(CurrentTarget) ? EnemyState.MovingToTarget : EnemyState.Idle;
    }

    protected override void OnFixedUpdate()
    {
        var velocity = rigidBody.linearVelocity;

        switch (State)
        {
            case EnemyState.Idle:
                ApplyHover(ref velocity);
                break;

            case EnemyState.MovingToTarget:
                if ((Time.frameCount + _separationFrameOffset) % SeparationCheckInterval == 0) ApplySeparation();

                var targetXZ = new Vector3(CurrentTarget.transform.position.x, 0, CurrentTarget.transform.position.z);
                if (Vector3.Distance(_destination, targetXZ) > RetargetThreshold) SetDestination();

                var posXZ = new Vector3(transform.position.x, 0, transform.position.z);
                if (Vector3.Distance(posXZ, _destination) > ArrivalThreshold)
                {
                    MaintainAltitude(ref velocity);
                    FlyToward(ref velocity);
                }
                else
                {
                    ApplyHover(ref velocity);
                    FaceTarget(CurrentTarget.transform.position);
                }
                break;
        }

        rigidBody.linearVelocity = velocity;
    }
    
    private void MaintainAltitude(ref Vector3 velocity)
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, heightCheckDistance, heightMask))
        {
            velocity.y = -heightAdjustSpeed;
            return;
        }

        float desiredY = hit.point.y + flightHeight;
        float correction = (desiredY - transform.position.y) * heightAdjustSpeed;
        velocity.y = Mathf.MoveTowards(velocity.y, correction, heightAdjustSpeed * Time.fixedDeltaTime);
    }

    private void ApplySeparation()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, avoidanceRadius, separationMask);
        foreach (var col in nearby)
        {
            Vector3 away = transform.position - col.transform.position;
            rigidBody.AddForce(away.normalized * avoidanceForce, ForceMode.Force);
        }
    }

    private void FlyToward(ref Vector3 velocity)
    {
        Vector3 direction = _destination - transform.position;
        direction.y = 0;

        float distance = direction.magnitude;
        if (distance <= ArrivalThreshold) return;

        Vector3 dir = direction / distance;

        float currentSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        float brakingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

        float targetSpeed = distance <= brakingDistance
            ? Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime)
            : Mathf.MoveTowards(currentSpeed, moveSpeed, acceleration * Time.fixedDeltaTime);

        velocity.x = dir.x * targetSpeed;
        velocity.z = dir.z * targetSpeed;

        FaceTarget(transform.position + direction);
    }

    private void ApplyHover(ref Vector3 velocity)
    {
        float targetY = transform.position.y + Mathf.Sin(Time.time * hoverFrequency + _separationFrameOffset) * hoverAmplitude;
        velocity.y = Mathf.MoveTowards(velocity.y, (targetY - transform.position.y) * heightAdjustSpeed, heightAdjustSpeed * Time.fixedDeltaTime);
        velocity.x = Mathf.MoveTowards(velocity.x, 0, moveSpeed * Time.fixedDeltaTime);
        velocity.z = Mathf.MoveTowards(velocity.z, 0, moveSpeed * Time.fixedDeltaTime);
    }

    private void RotatePropellers()
    {
        foreach (var prop in propellersBLFR)
            prop.Rotate(Vector3.up, propellerRotationSpeed * Time.fixedDeltaTime);

        foreach (var prop in propellersBRFL)
            prop.Rotate(Vector3.up, -propellerRotationSpeed * Time.fixedDeltaTime);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - rigidBody.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        rigidBody.rotation = Quaternion.Slerp(rigidBody.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.fixedDeltaTime);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, heightCheckDistance, heightMask))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.2f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * heightCheckDistance);
        }
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_destination.AddY(transform.position.y), 1f);
    }
#endif
}
}