using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GroundEnemy : Enemy
    {
        [SerializeField, AutoGetSelf, HideInInspector] private NavMeshAgent navMeshAgent;

        protected override Vector3 Velocity => navMeshAgent.velocity;

        protected override void Initialize()
        {
            base.Initialize();
            navMeshAgent.updateRotation = !AimingControlsBodyRotation;
        }

        protected override void OnUpdate()
        {
            if (!IsTargetValid(CurrentTarget))
            {
                State = EnemyState.Idle;
                navMeshAgent.ResetPath();
                return;
            }

            State = EnemyState.MovingToTarget;

            if (Vector3.Distance(navMeshAgent.destination, CurrentTarget.transform.position) > retargetDestinationThreshold) SetDestination();
        }

        protected override void SetDestination()
        {
            if (!IsTargetValid(CurrentTarget)) return;

            Vector3 destination = CurrentTarget.transform.position;

            if (!RequiresDirectApproach)
            {
                var positionOffset = Random.insideUnitSphere * targetRandomOffset;
                positionOffset.y = 0;
                destination += positionOffset;
            }

            navMeshAgent?.SetDestination(destination);
        }

        protected override void OnPush(Vector3 direction, float force)
        {
            rigidBody.AddForce(direction.normalized * force, ForceMode.Force);
        }
    }
}