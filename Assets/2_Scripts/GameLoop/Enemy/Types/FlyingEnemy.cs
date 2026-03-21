using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop
{

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : Enemy
{
    [Header("Flying")]
    [SerializeField] private float stopDistance = 30f;
    [SerializeField] private float moveSpeed = 17F;
    [SerializeField] private float rotationSpeed = 5f;

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

    protected override void UpdateMovement()
    {
        if (!IsTargetValid(CurrentTarget))
        {
            State = EnemyState.Idle;
            Hover();
            return;
        }

        State = EnemyState.MovingToTarget;
        MaintainAltitude();

        if ((Time.frameCount + _separationFrameOffset) % SeparationCheckInterval == 0) ApplySeparation();

        var targetXZ = new Vector3(CurrentTarget.transform.position.x, 0, CurrentTarget.transform.position.z);
        if (Vector3.Distance(_destination, targetXZ) > RetargetThreshold) SetDestination();

        var posXZ = new Vector3(transform.position.x, 0, transform.position.z);
        if (Vector3.Distance(posXZ, _destination) > ArrivalThreshold)
        {
            FlyTowardTarget();
        }
        else
        {
            Hover();
            FaceTarget(CurrentTarget.transform.position);
        }
    }

    private void MaintainAltitude()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, heightCheckDistance, heightMask))
        {
            rigidBody.linearVelocity = new Vector3(
                rigidBody.linearVelocity.x,
                -heightAdjustSpeed,
                rigidBody.linearVelocity.z
            );
            return;
        }

        float desiredY = hit.point.y + flightHeight;
        float correction = (desiredY - transform.position.y) * heightAdjustSpeed;

        rigidBody.linearVelocity = new Vector3(
            rigidBody.linearVelocity.x,
            Mathf.MoveTowards(rigidBody.linearVelocity.y, correction, heightAdjustSpeed * Time.deltaTime),
            rigidBody.linearVelocity.z
        );
    }

    private void ApplySeparation()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, avoidanceRadius, separationMask);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 away = transform.position - col.transform.position;
            float strength = 1f - (away.magnitude / avoidanceRadius);
            rigidBody.AddForce(away.normalized * (avoidanceForce * strength), ForceMode.Force);
        }
    }

    private void FlyTowardTarget()
    {
        Vector3 direction = _destination - transform.position;
        direction.y = 0;

        if (direction.magnitude <= ArrivalThreshold) return;

        Vector3 velocity = direction.normalized * moveSpeed;
        velocity.y = rigidBody.linearVelocity.y;
        rigidBody.linearVelocity = velocity;

        FaceTarget(transform.position + direction);
    }

    private void Hover()
    {
        float targetY = transform.position.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float yVelocity = Mathf.MoveTowards(rigidBody.linearVelocity.y, (targetY - transform.position.y) * heightAdjustSpeed, heightAdjustSpeed * Time.deltaTime);

        rigidBody.linearVelocity = new Vector3(
            Mathf.MoveTowards(rigidBody.linearVelocity.x, 0, moveSpeed * Time.deltaTime),
            yVelocity,
            Mathf.MoveTowards(rigidBody.linearVelocity.z, 0, moveSpeed * Time.deltaTime)
        );
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
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