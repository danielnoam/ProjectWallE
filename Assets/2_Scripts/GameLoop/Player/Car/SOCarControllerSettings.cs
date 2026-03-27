using UnityEngine;

namespace ProjectWallE
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Car Controller Settings")]
    public class SOCarControllerSettings : ScriptableObject
    {
        [Header("Suspension Parameters")]
        [SerializeField] private float groundHeight = 0.2f;
        [SerializeField] private float wheelRadius = 0.4f;
        [SerializeField] private float suspensionStrength;
        [SerializeField] private float suspensionDamping;

        [Header("Steering Parameters")]
        [SerializeField] private float steeringAngle;
        [SerializeField] private float steeringStrength;
        [SerializeField] private float topSpeedSteeringFactor;

        [Header("Tire Friction Parameters")]
        [SerializeField] private AnimationCurve steeringTiresFrictionCurve;
        [SerializeField] private AnimationCurve staticTiresFrictionCurve;
        [SerializeField] private float handBrakeGrip;
        [SerializeField] private float handbrakeBlendIn = 12f;
        [SerializeField] private float handbrakeBlendOut = 8f;
        [SerializeField] private float minLateralFrictionAccel = 6f;
        [SerializeField, Tooltip("reaching a speed in the tire slipping direction between minSlippingSpeed and maxSlippingSpeed" +
                                 " will factor the grip by a factor between minGripAtMaxSlip and 1 accordingly")] 
        private float minSlippingSpeed;
        [SerializeField, Tooltip("reaching a speed in the tire slipping direction between minSlippingSpeed and maxSlippingSpeed" +
                                 " will factor the grip by a factor between minGripAtMaxSlip and 1 accordingly")] 
        private float maxSlippingSpeed = 15f;
        [SerializeField, Range(0,1), Tooltip("the minimum factor which is applied after reaching maxSlippingSpeed")] 
        private float minGripAtMaxSlip = 0.5f;

        [Header("Takeoff / partial-ground tuning")]
        [SerializeField] private float minGripWhenPartialGround = 0.15f;
        [SerializeField] private float partialGroundGripPower = 1.0f;

        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength = 1.0f;
        [SerializeField] private float terminalVelocity = 1.0f;

        [Header("Longitudinal Parameters")]
        [SerializeField] private float accelerationStrength;
        [SerializeField] private AnimationCurve accelerationCurve;
        [SerializeField] private float topForwardSpeed;
        [SerializeField] private float topBackwardSpeed;
        
        [Header("Speedy Layer Settings")]
        [SerializeField] private LayerMask speedyLayer;
        [SerializeField] private float speedyTopSpeedMultiplier;
        [SerializeField] private float speedyAccelMultiplier;

        [Header("Breaking Parameters")]
        [SerializeField] private float brakeStrength;
        [SerializeField] private float engineBrakeStrength;

        [Header("Air Control Parameters")]
        [SerializeField] private float airAlignmentStrength;
        [SerializeField] private float airAlignmentDamping;
        [SerializeField] private float airSteeringStrength;
        [SerializeField] private float maxAirSteeringVelocity;
        
        // --- Getters ---

        public float GroundHeight => groundHeight;
        public float WheelRadius => wheelRadius;
        public float SuspensionStrength => suspensionStrength;
        public float SuspensionDamping => suspensionDamping;

        public float SteeringAngle => steeringAngle;
        public float SteeringStrength => steeringStrength;
        public float TopSpeedSteeringFactor => topSpeedSteeringFactor;

        public AnimationCurve SteeringTiresFrictionCurve => steeringTiresFrictionCurve;
        public AnimationCurve StaticTiresFrictionCurve => staticTiresFrictionCurve;
        public float HandBrakeGrip => handBrakeGrip;
        public float HandbrakeBlendIn => handbrakeBlendIn;
        public float HandbrakeBlendOut => handbrakeBlendOut;
        public float MinLateralFrictionAccel => minLateralFrictionAccel;
        public float MinSlippingSpeed => minSlippingSpeed;
        public float MaxSlippingSpeed => maxSlippingSpeed;
        public float MinGripAtMaxSlip => minGripAtMaxSlip;

        public float MinGripWhenPartialGround => minGripWhenPartialGround;
        public float PartialGroundGripPower => partialGroundGripPower;

        public float GravityStrength => gravityStrength;
        public float TerminalVelocity => terminalVelocity;

        public float AccelerationStrength => accelerationStrength;
        public AnimationCurve AccelerationCurve => accelerationCurve;
        public float TopForwardSpeed => topForwardSpeed;
        public float TopBackwardSpeed => topBackwardSpeed;

        public LayerMask SpeedyLayer => speedyLayer;
        public float SpeedyTopSpeedMultiplier => speedyTopSpeedMultiplier;
        public float SpeedyAccelMultiplier => speedyAccelMultiplier;

        public float BrakeStrength => brakeStrength;
        public float EngineBrakeStrength => engineBrakeStrength;

        public float AirAlignmentStrength => airAlignmentStrength;
        public float AirAlignmentDamping => airAlignmentDamping;
        public float AirSteeringStrength => airSteeringStrength;
        public float MaxAirSteeringVelocity => maxAirSteeringVelocity;
    }
}
