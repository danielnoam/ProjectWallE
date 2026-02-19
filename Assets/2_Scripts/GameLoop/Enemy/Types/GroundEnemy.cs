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
        navMeshAgent.stoppingDistance = attackRange * 0.8f;
    }

    protected override void UpdateState()
    {
        if (!(CurrentTarget is Component target) || !target)
        {
            State = EnemyState.MovingToTarget;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        switch (State)
        {
            case EnemyState.MovingToTarget:
                if (distance <= attackRange)
                    State = EnemyState.Attacking;
                break;

            case EnemyState.Attacking:
                if (distance > attackRange)
                {
                    State = EnemyState.MovingToTarget;
                    SetDestination();
                }
                else
                {
                    AttackTimer += Time.deltaTime;
                    if (AttackTimer >= attackCooldown)
                    {
                        AttackTarget();
                        AttackTimer = 0f;
                    }
                }
                break;
        }
    }

    protected override void SetDestination()
    {
        if (CurrentTarget is Component target && target)
        {
            navMeshAgent.SetDestination(target.transform.position);
        }
    }

    protected override void OnPush(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }
}