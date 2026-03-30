using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{
    
    [Flags]
    public enum TargetType
    {
        Player    = 1,
        Turret    = 2,
        Structure = 4,
    }
    
    [Serializable]
    public abstract class RetaliationStrategy
    {
        public abstract bool ShouldRetarget(IDamageable currentTarget, IDamageable attacker);
    }

    [Serializable]
    [SerializableSelectorName("Never")]
    public class NoRetaliation : RetaliationStrategy
    {
        public override bool ShouldRetarget(IDamageable currentTarget, IDamageable attacker) => false;
    }

    [Serializable]
    [SerializableSelectorName("Conditional")]
    public class ConditionalRetaliation : RetaliationStrategy
    {
        [SerializeField, Tooltip("If current target is one of these, never switch.")]
        private TargetType loyalTo;

        [SerializeField, Tooltip("Only retaliate against these types (when not loyal to current target).")]
        private TargetType retaliateAgainst;

        public override bool ShouldRetarget(IDamageable currentTarget, IDamageable attacker)
        {
            if (currentTarget != null && (loyalTo & Classify(currentTarget)) != 0)
                return false;

            return (retaliateAgainst & Classify(attacker)) != 0;
        }

        private static TargetType Classify(IDamageable target) => target switch
        {
            PlayerManager => TargetType.Player,
            Turret        => TargetType.Turret,
            Structure     => TargetType.Structure,
            _             => 0
        };
    }
}
