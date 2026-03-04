using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class GroundEnemy : Enemy
{
    [SerializeField, AutoGetSelf, HideInInspector] private NavMeshAgent navMeshAgent;
    [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;

    protected override void Initialize()
    {
       
    }

    protected override void UpdateMovement()
    {
        if (!IsTargetValid(CurrentTarget, out _))
        {
            State = EnemyState.Idle;
            return;
        }

        State = EnemyState.MovingToTarget;
        SetDestination();
    }

    protected override void SetDestination()
    {
        if (IsTargetValid(CurrentTarget, out var targetComponent))
        {
            navMeshAgent.SetDestination(targetComponent.transform.position);
        }
    }

    protected override void OnPush(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }
}