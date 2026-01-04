using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(NavMeshAgent))]
public class GroundEnemy : Enemy
{
    [Header("Ground Enemy Settings")]
    [SerializeField] private float targetRange = 5f;
    [SerializeField] private NavMeshAgent navMeshAgent;
    
    private EnemyState _state = EnemyState.Moving;
    private enum EnemyState { Attacking, Moving }
    

    protected override void UpdateBehavior()
    {
        if (CurrentTarget == null) return;

        float distance = Vector3.Distance(transform.position, CurrentTarget.Transform().position);

        switch (_state)
        {
            case EnemyState.Moving:
                if (distance <= targetRange)
                {
                    _state = EnemyState.Attacking;
                    navMeshAgent.ResetPath();
                }
                break;

            case EnemyState.Attacking:
                if (distance > targetRange)
                {
                    _state = EnemyState.Moving;
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
        CurrentTarget.TakeDamage(attackDamage, this);
    }

    protected override void MoveToTarget()
    {
        if (CurrentTarget != null)
        {
            navMeshAgent.SetDestination(CurrentTarget.Transform().position);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Health: {CurrentHealth}/{maxHealth}\nState: {_state}\nTarget: {CurrentTarget?.Transform().name}",
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