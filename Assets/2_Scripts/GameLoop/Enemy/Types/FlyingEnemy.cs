using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [RequireComponent(typeof(FlyingAgent))]
    public class FlyingEnemy : Enemy
    {
        [SerializeField] private float stopDistance = 30f;
        [SerializeField, AutoGetSelf, HideInInspector] private FlyingAgent flyingAgent;

        protected override void Initialize()
        {
            base.Initialize();
            flyingAgent.UpdateRotation = !AimingControlsBodyRotation;
            Vector3 pos = transform.position;
            pos.y += 2f; // spawn offset so it wont spawn in ground
            transform.position = pos;
        }

        protected override void OnUpdate()
        {
            if (!IsTargetValid(CurrentTarget))
            {
                State = EnemyState.Idle;
                flyingAgent.ResetPath();
                return;
            }

            State = EnemyState.MovingToTarget;

            var targetXZ = new Vector3(CurrentTarget.transform.position.x, 0, CurrentTarget.transform.position.z);
            if (Vector3.Distance(flyingAgent.Destination, targetXZ) > retargetDestinationThreshold) SetDestination();
        }

        protected override void SetDestination()
        {
            if (!IsTargetValid(CurrentTarget)) return;

            Vector3 targetPos = CurrentTarget.transform.position;

            if (RequiresDirectApproach)
            {
                flyingAgent.SetDestination(targetPos);
                return;
            }

            Vector3 directionToTarget = (transform.position - targetPos).normalized;
            var offset = Random.insideUnitSphere * targetRandomOffset;
            var destination = targetPos + directionToTarget * stopDistance + offset;
            flyingAgent.SetDestination(destination);
        }

        protected override void OnPush(Vector3 direction, float force)
        {
            rigidBody.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }
}