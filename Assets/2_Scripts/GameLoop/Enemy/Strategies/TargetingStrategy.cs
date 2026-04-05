using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class TargetingStrategy
    {
        [Tooltip("Should it use the targetFindRange to find the target")]
        public bool inRange = true;
        [Tooltip("Chance this strategy is selected when evaluating targets")]
        [Range(0.1f, 100f)] public float weight = 100f;
        
        
        public IDamageable GetTarget(Vector3 position, float range)
        {
            if (Random.Range(0f, 100f) > weight) return null;
            return FindTarget(position, range);
        }

        protected abstract IDamageable FindTarget(Vector3 position, float range);
    }
    
    [Serializable]
    [SerializableSelectorName("Player")]
    public class PlayerTarget : TargetingStrategy
    {
        protected override IDamageable FindTarget(Vector3 position, float range)
        {
            var player = LevelManager.Instance?.Player;
            if (!player || !player.IsAlive) return null;
            
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
        protected override IDamageable FindTarget(Vector3 position, float range)
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
        protected override IDamageable FindTarget(Vector3 position, float range)
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
        protected override IDamageable FindTarget(Vector3 position, float range)
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
        protected override IDamageable FindTarget(Vector3 position, float range)
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
        protected override IDamageable FindTarget(Vector3 position, float range)
        {
            if (inRange)
            {
                return StructureManager.Instance?.GetNearestTurretInRange(position, range);
            }

            return StructureManager.Instance?.GetNearestTurret(position);
        }
    }
}
