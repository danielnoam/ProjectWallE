using DNExtensions.Utilities.AutoGet;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : Enemy
{
    [Header("Flying Settings")]
    [SerializeField, Tooltip("Flight height above terrain and targets.")]
    private float flightHeight = 8f;
    [SerializeField, Tooltip("Movement speed toward the target.")]
    private float moveSpeed = 6f;
    [SerializeField, Tooltip("How fast the enemy adjusts its vertical position.")]
    private float heightAdjustSpeed = 4f;
    [SerializeField, Tooltip("Rotation speed when facing the target.")]
    private float rotationSpeed = 5f;
    [SerializeField, Tooltip("Vertical bob amplitude while hovering.")]
    private float hoverAmplitude = 0.4f;
    [SerializeField, Tooltip("Vertical bob frequency while hovering.")]
    private float hoverFrequency = 1.2f;

    [Header("Altitude Settings")]
    [SerializeField, Tooltip("Maximum distance to check for ground or obstacles below.")]
    private float obstacleCheckDistance = 30f;
    [SerializeField, Tooltip("Layers considered as ground or obstacles for altitude maintenance.")]
    private LayerMask obstacleMask;

    [Header("Separation Settings")]
    [SerializeField, Tooltip("Radius within which other flyers trigger separation.")]
    private float separationRadius = 3f;
    [SerializeField, Tooltip("Force applied to push away from nearby flyers.")]
    private float separationForce = 8f;
    [SerializeField, Tooltip("Layer mask identifying other flying enemies.")]
    private LayerMask separationMask;

    [SerializeField,AutoGetSelf,HideInInspector] private Rigidbody rigidBody;
    
    private const float ArrivalThreshold = 1f;
    private const int SeparationCheckInterval = 5;
    
    
    private Vector3 _targetFlyPosition;
    private int _separationFrameOffset;
    private static int _frameOffsetCounter;

    protected override void Initialize()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.useGravity = false;
        rigidBody.linearDamping = 4f;
        rigidBody.angularDamping = 10f;
        rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

        _separationFrameOffset = _frameOffsetCounter % SeparationCheckInterval;
        _frameOffsetCounter++;
    }

    protected override void SetDestination()
    {
        if (CurrentTarget is not Component targetComponent || !targetComponent) return;
        _targetFlyPosition = targetComponent.transform.position + Vector3.up * flightHeight;
    }

    protected override void OnPush(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }

    protected override void UpdateState()
    {
        if (!(CurrentTarget is Component target) || !target)
        {
            State = EnemyState.Idle;
            return;
        }

        _targetFlyPosition = target.transform.position + Vector3.up * flightHeight;

        MaintainAltitude();

        if ((Time.frameCount + _separationFrameOffset) % SeparationCheckInterval == 0)
            ApplySeparation();

        float distanceXZ = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(target.transform.position.x, 0, target.transform.position.z)
        );

        switch (State)
        {
            case EnemyState.MovingToTarget:
                FlyTowardTarget();
                if (distanceXZ <= attackRange)
                    State = EnemyState.Attacking;
                break;

            case EnemyState.Attacking:
                Hover();
                FaceTarget(target.transform.position);
                if (distanceXZ > attackRange)
                {
                    State = EnemyState.MovingToTarget;
                    break;
                }
                AttackTimer += Time.deltaTime;
                if (AttackTimer >= attackCooldown)
                {
                    AttackTarget();
                    AttackTimer = 0f;
                }
                break;

            case EnemyState.Idle:
                Hover();
                break;
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
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationRadius, separationMask);
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

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

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
}