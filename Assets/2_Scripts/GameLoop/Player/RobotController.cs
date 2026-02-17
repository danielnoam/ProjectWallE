using System;
using UnityEngine;

namespace ProjectWallE
{
    [RequireComponent(typeof(RobotInput))]
    public class RobotController : MonoBehaviour, IPlayerController
    {
        private RobotInput _input;
        
        [SerializeField] Transform _groundRayPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Rigidbody playerRB;
        
        [Header("Grounded Settings")]
        [SerializeField] private float groundHeight = 0.5f;
        [SerializeField] private float maxCheckHeight = 1f;
        [SerializeField] private float springStrength;
        [SerializeField] private float springDamping;
        
        [Header("Locomotion Settings")]
        [SerializeField] float moveAccel;
        [SerializeField] float maxMoveSpeed;
        
        private bool _isGrounded = false;


        void Awake()
        {
            _input = GetComponent<RobotInput>();
        }
        public void ApplyMovement()
        {
            UpdateGroundHeight();
            UpdateLocomotion();
        }

        #region Locomotion

        private void UpdateLocomotion()
        {
            Vector3 targetDir = new Vector3(_input.Movement.x, 0, _input.Movement.y);
            
            playerRB.AddForce(targetDir * moveAccel);
        }

        #endregion
        
        #region Ground

        private void UpdateGroundHeight()
        {
            if (!IsGrounded(out RaycastHit hit)) return;
            
            float offset = hit.distance - groundHeight;

            float yVel = playerRB.linearVelocity.y;

            float suspensionForce =
                (-offset * springStrength) -
                (yVel * springDamping);

            playerRB.AddForce(Vector3.up * suspensionForce);
        }

        #endregion
        
        #region Helpers

        private bool IsGrounded(out RaycastHit hit)
        {
            float dist = _isGrounded ? maxCheckHeight : groundHeight;

            bool groundedNow = Physics.Raycast(
                _groundRayPoint.position,
                Vector3.down,
                out hit,
                dist,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            _isGrounded = groundedNow;
            return groundedNow;
        }


        #endregion
    }
}
