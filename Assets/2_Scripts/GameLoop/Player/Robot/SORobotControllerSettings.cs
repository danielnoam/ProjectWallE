using UnityEngine;

namespace ProjectWallE
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Robot Controller Settings")]
    public class SORobotControllerSettings : ScriptableObject
    {
        [Header("Grounded Settings")]
        [SerializeField] private float groundHeight = 0.5f;
        [SerializeField] private float maxCheckHeight = 1f;
        [SerializeField] private float springStrength;
        [SerializeField] private float springDamping;

        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength;
        [SerializeField] private float terminalVelocity;

        [Header("Locomotion Settings")]
        [SerializeField] private float moveAccel = 20f;
        [SerializeField] private AnimationCurve dirChangeAccelFactor;
        [SerializeField] private float maxMoveSpeed = 8f;
        [SerializeField] private float forcePointBelowCom = 0.2f;
        [SerializeField] private float groundMoveFactor = 1f;
        [SerializeField] private float airMoveFactor = 0.35f;

        [Header("Slope Settings")]
        [SerializeField] private float maxWalkableSlopeAngle = 45f;
        [SerializeField] private float slopeSlideAccel = 8f;
        [SerializeField] private float maxSlopeSlideSpeed = 6f;
        [SerializeField] private float slopeSlideMoveFactor = 0.1f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float jumpBuffer = 0.2f;

        [Header("Rotation Settings")]
        [SerializeField] private float uprightStrength = 80f;
        [SerializeField] private float uprightDamping = 12f;
        [SerializeField] private float yawStrength = 40f;
        [SerializeField] private float yawDamping = 8f;

        // --- Getters ---

        public float GroundHeight => groundHeight;
        public float MaxCheckHeight => maxCheckHeight;
        public float SpringStrength => springStrength;
        public float SpringDamping => springDamping;

        public float GravityStrength => gravityStrength;
        public float TerminalVelocity => terminalVelocity;

        public float MoveAccel => moveAccel;
        public AnimationCurve DirChangeAccelFactor => dirChangeAccelFactor;
        public float MaxMoveSpeed => maxMoveSpeed;
        public float ForcePointBelowCom => forcePointBelowCom;
        public float GroundMoveFactor => groundMoveFactor;
        public float AirMoveFactor => airMoveFactor;

        public float MaxWalkableSlopeAngle => maxWalkableSlopeAngle;
        public float SlopeSlideAccel => slopeSlideAccel;
        public float MaxSlopeSlideSpeed => maxSlopeSlideSpeed;
        public float SlopeSlideMoveFactor => slopeSlideMoveFactor;

        public float JumpHeight => jumpHeight;
        public float JumpBuffer => jumpBuffer;

        public float UprightStrength => uprightStrength;
        public float UprightDamping => uprightDamping;
        public float YawStrength => yawStrength;
        public float YawDamping => yawDamping;
    }
}