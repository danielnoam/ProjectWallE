using System;
using UnityEngine;

namespace ProjectWallE
{
    public class RobotController : MonoBehaviour, IPlayerController
    {
        [SerializeField] Transform _groundRayPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Rigidbody playerRB;
        
        [Header("Grounded Settings")]
        [SerializeField] private float groundHeight = 0.5f;
        [SerializeField] private float maxCheckHeight = 1f;
        [SerializeField] private float springStrength;
        [SerializeField] private float springDamping;
        
        private bool _isGrounded = false;

        public void ApplyMovement()
        {
            UpdateGroundHeight();
        }

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
            if (_isGrounded)
            {
                _isGrounded = GroundedGroundCheck(out hit);
                return _isGrounded;
            }

            _isGrounded = AirGroundCheck(out hit);
            return _isGrounded;
        }

        private bool AirGroundCheck(out RaycastHit hit)
        {
            bool isGrounded = Physics.Raycast(_groundRayPoint.position, Vector3.down, out hit, groundHeight, groundLayer);
            return isGrounded;
        }

        private bool GroundedGroundCheck(out RaycastHit hit)
        {
            bool isGrounded = Physics.Raycast(_groundRayPoint.position, Vector3.down, out hit, maxCheckHeight, groundLayer);
            return isGrounded;
        }

        #endregion
    }
}
