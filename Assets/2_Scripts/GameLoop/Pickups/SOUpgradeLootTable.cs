using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [CreateAssetMenu(fileName = "New UpgradeLootTable", menuName = "Upgrade Loot Table")]
    public class SOUpgradeLootTable : ScriptableObject
    {
        [SerializeField] private ChanceList<UpgradePickup> upgrades = new();

        public UpgradePickup GetRandomDrop() => upgrades.GetRandomItem();
    }
}
