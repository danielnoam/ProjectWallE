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
        [SerializeField] private Transform cameraTransform;
        
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
            UpdateRotation();
        }

        #region Locomotion

        private void UpdateLocomotion()
        { 
            Vector3 moveDir = GetCameraRelativeDirection(_input.Movement);
            Vector3 targetVel = moveDir * maxMoveSpeed;

            Vector3 currentVel = playerRB.linearVelocity;
            Vector3 newVel = Vector3.MoveTowards(
                new Vector3(currentVel.x, 0, currentVel.z),
                targetVel,
                moveAccel * Time.fixedDeltaTime
            );

            playerRB.linearVelocity = new Vector3(newVel.x, currentVel.y, newVel.z);
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
        
        #region Rotation

        private void UpdateRotation()
        {
            
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
        
        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return Vector3.zero;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten
            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y +
                              camRight * input.x;

            return moveDir.normalized;
        }



        #endregion
    }
}
