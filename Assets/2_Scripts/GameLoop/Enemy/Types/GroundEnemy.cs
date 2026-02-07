using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class GroundEnemy : Enemy
{
    [Header("Ground Enemy Settings")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Rigidbody rigidBody;
    
    private EnemyState _state = EnemyState.MovingToTarget;
    private enum EnemyState { Attacking, MovingToTarget, Idle }

    protected override void OnSetup()
    {
        navMeshAgent.stoppingDistance = attackRange * 0.8f;
    }

    protected override void UpdateBehavior()
    {
        if (!(CurrentTarget is Component target) || !target)
        {
            _state = EnemyState.MovingToTarget;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        switch (_state)
        {
            case EnemyState.MovingToTarget:
                
                if (distance <= attackRange)
                {
                    _state = EnemyState.Attacking;
                }
                else
                {
                    if (!target)
                    {
                        _state = EnemyState.Idle;
                        navMeshAgent.ResetPath();
                    }
                }
                break;

            case EnemyState.Attacking:
                if (distance > attackRange)
                {
                    _state = EnemyState.MovingToTarget;
                    MoveToTarget();
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
    

    protected override void MoveToTarget()
    {
        if (CurrentTarget is Component target && target)
        {
            navMeshAgent.SetDestination(target.transform.position);
        }
    }
    
    public void Push(Vector3 direction, float force)
    {
        rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        string targetName = (CurrentTarget is Component target && target) ? target.name : "None";
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {CurrentHealth}/{maxHealth}\nState: {_state}, Target: {targetName}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.red },
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
    }
#endif
}