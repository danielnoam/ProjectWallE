using DNExtensions.Utilities.AutoGet;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : Enemy
{
    [Header("Flying Settings")]
    [SerializeField] private float flightHeight = 8f;
    [SerializeField] private float moveSpeed = 14f;
    [SerializeField] private float heightAdjustSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 1.2f;

    [Header("Obstacle Settings")]
    [SerializeField] private float obstacleCheckDistance = 30f;
    [SerializeField] private float separationRadius = 5f;
    [SerializeField] private float separationForce = 15f;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;

    private const float ArrivalThreshold = 1f;
    private const int SeparationCheckInterval = 5;

    private Vector3 _targetFlyPosition;
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
        if (IsTargetValid(CurrentTarget, out var targetComponent))
        {
            _targetFlyPosition = targetComponent.transform.position + Vector3.up * flightHeight;
        }
    }

    protected override void OnPush(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }

    protected override void UpdateMovement()
    {
        if (!IsTargetValid(CurrentTarget, out var targetComponent))
        {
            State = EnemyState.Idle;
            Hover();
            return;
        }

        _targetFlyPosition = targetComponent.transform.position + Vector3.up * flightHeight;
        State = EnemyState.MovingToTarget;

        MaintainAltitude();

        if ((Time.frameCount + _separationFrameOffset) % SeparationCheckInterval == 0)
            ApplySeparation();

        float distanceXZ = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetComponent.transform.position.x, 0, targetComponent.transform.position.z)
        );

        if (distanceXZ > targetFindRange)
            FlyTowardTarget();
        else
        {
            Hover();
            FaceTarget(targetComponent.transform.position);
        }
    }

    private void MaintainAltitude()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, obstacleCheckDistance, obstacleMask)) return;

        float desiredY = hit.point.y + flightHeight;
        if (transform.position.y < desiredY)
        {
            float correction = (desiredY - transform.position.y) * heightAdjustSpeed;
            rigidBody.linearVelocity = new Vector3(
                rigidBody.linearVelocity.x,
                Mathf.Max(rigidBody.linearVelocity.y, correction),
                rigidBody.linearVelocity.z
            );
        }
    }

    private void ApplySeparation()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationRadius, obstacleMask);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 away = transform.position - col.transform.position;
            float strength = 1f - (away.magnitude / separationRadius);
            rigidBody.AddForce(away.normalized * (separationForce * strength), ForceMode.Force);
        }
    }

    private void FlyTowardTarget()
    {
        Vector3 direction = _targetFlyPosition - transform.position;
        if (direction.magnitude > ArrivalThreshold)
        {
            Vector3 velocity = direction.normalized * moveSpeed;
            velocity.y = Mathf.MoveTowards(rigidBody.linearVelocity.y, velocity.y, heightAdjustSpeed * Time.deltaTime);
            rigidBody.linearVelocity = velocity;
        }

        if (direction.sqrMagnitude > 0.01f)
            FaceTarget(transform.position + direction);
    }

    private void Hover()
    {
        float targetY = _targetFlyPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
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
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, obstacleCheckDistance, obstacleMask))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.2f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * obstacleCheckDistance);
        }
    }
#endif

}