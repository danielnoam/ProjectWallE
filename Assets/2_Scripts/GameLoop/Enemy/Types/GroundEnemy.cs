using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class GroundEnemy : Enemy
    {
        [SerializeField, AutoGetSelf, HideInInspector] private NavMeshAgent navMeshAgent;
        [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;
        
        protected override void UpdateMovement()
        {
            if (!IsTargetValid(CurrentTarget))
            {
                State = EnemyState.Idle;
                navMeshAgent.ResetPath();
                return;
            }

            State = EnemyState.MovingToTarget;

            if (Vector3.Distance(navMeshAgent.destination, CurrentTarget.transform.position) > RetargetThreshold) SetDestination();
        }

        protected override void SetDestination()
        {
            if (IsTargetValid(CurrentTarget))
            {
                var positionOffset = Random.insideUnitSphere * RandomRange;
                positionOffset.y = 0;
                
                navMeshAgent.SetDestination(CurrentTarget.transform.position + positionOffset);
            }
        }

        protected override void OnPush(Vector3 direction, float force)
        {
            rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
        }
    }
}

