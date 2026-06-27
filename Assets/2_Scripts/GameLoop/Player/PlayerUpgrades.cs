using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;

namespace ProjectWallE.GameLoop.Player
{
    public enum UpgradeType
    {
        Damage,
        MaxHealth,
        MaxFuel
    }

    public class PlayerUpgrades : MonoBehaviour
    {
        [SerializeField, ReadOnly] private float damageMultiplier = 1f;
        [SerializeField, ReadOnly] private float maxHealthMultiplier = 1f;
        [SerializeField, ReadOnly] private float maxFuelMultiplier = 1f;
        
        public float DamageMultiplier => damageMultiplier;
        public float MaxHealthMultiplier => maxHealthMultiplier;
        public float MaxFuelMultiplier => maxFuelMultiplier;

        public event Action<float> OnMaxHealthMultiplierChanged;
        public event Action<float> OnBoostMultiplierChanged;

        
        
        [Button]
        private void ResetUpgrades()
        {
            damageMultiplier = 1f;
            maxHealthMultiplier = 1f;
            maxFuelMultiplier = 1f;
        }
        
        
        public void ApplyUpgrade(UpgradeType type, float amount)
        {
            switch (type)
            {
                case UpgradeType.Damage:
                    damageMultiplier += amount;
                    break;
                case UpgradeType.MaxHealth:
                    maxHealthMultiplier += amount;
                    OnMaxHealthMultiplierChanged?.Invoke(amount);
                    break;
                case UpgradeType.MaxFuel:
                    maxFuelMultiplier += amount;
                    OnBoostMultiplierChanged?.Invoke(amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        

    }
}
