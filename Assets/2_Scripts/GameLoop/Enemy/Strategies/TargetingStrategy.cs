using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class TargetingStrategy
    {
        public bool inRange = true;
        public abstract IDamageable FindTarget(Vector3 position, float range);
    }
    
    [Serializable]
    [SerializableSelectorName("Player")]
    public class PlayerTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            var player = LevelManager.Instance?.Player;
            if (!player) return null;
            
            if (inRange)
            {
                return Vector3.Distance(position, player.transform.position) <= range ? player : null;
            }
            
            return LevelManager.Instance.Player;
        }
    }
    

    [Serializable]
    [SerializableSelectorName("Nearest Base")]
    public class NearestBaseTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetNearestBaseInRange(position, range);
            }
            
            return StructureManager.Instance?.GetNearestBase(position);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Weakest Base")]
    public class WeakestBaseTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetWeakestBaseInRange(position, range);
            }
            
            return StructureManager.Instance?.GetWeakestBase(position);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Nearest Structure")]
    public class NearestStructureTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetNearestStructureInRange(position, range);
            }

            return StructureManager.Instance?.GetNearestStructure(position);
        }
    }
    
    
    [Serializable]
    [SerializableSelectorName("Weakest Structure")]
    public class WeakestStructureTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetWeakestStructureInRange(position, range);
            }

            return StructureManager.Instance?.GetWeakestStructure(position);
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Nearest Turret")]
    public class NearestTurretTarget : TargetingStrategy
    {
        public override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetNearestTurretInRange(position, range);
            }

            return StructureManager.Instance?.GetNearestTurret(position);
        }
    }
}
