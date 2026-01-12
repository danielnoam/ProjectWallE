using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GroundEnemy : Enemy
{
    [Header("Ground Enemy Settings")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    
    private EnemyState _state = EnemyState.MovingToTarget;
    private enum EnemyState { Attacking, MovingToTarget, Idle }

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
                    navMeshAgent.ResetPath();
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
                        PerformAttack();
                        AttackTimer = 0f;
                    }
                }
                break;
        }
    }

    protected override void PerformAttack()
    {
        CurrentTarget?.TakeDamage(attackDamage, this);
    }

    protected override void MoveToTarget()
    {
        if (CurrentTarget is Component target && target)
        {
            navMeshAgent.SetDestination(target.transform.position);
        }
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