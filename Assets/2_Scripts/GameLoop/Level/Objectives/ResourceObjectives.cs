using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    [SerializableSelectorName("Destroy Resource Nodes", "Resource")]
    public class DestroyResourceNodesObjective : BaseLevelObjective
    {
        [SerializeField, Min(1)] private int destroyCount = 5;

        private int _currentCount;

        public override string Description => $"Destroy {destroyCount} resource nodes";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentCount}/{destroyCount}";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentCount = 0;
            ResourceNode.OnDestroyed += OnNodeDestroyed;
        }

        public override void Dispose()
        {
            ResourceNode.OnDestroyed -= OnNodeDestroyed;
        }

        private void OnNodeDestroyed(ResourceNode node)
        {
            _currentCount++;
            if (_currentCount >= destroyCount)
            {
                Complete();
            }
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Reach Resource Amount", "Resource")]
    public class ReachResourceAmountObjective : BaseLevelObjective
    {
        [SerializeField, Min(1)] private int targetAmount = 500;
 
        public override string Description => $"Collect {targetAmount} resources";
        public override string ProgressText => IsCompleted ? "Complete" : $"{ResourceManager.Instance?.CurrentResources ?? 0}/{targetAmount}";
 
        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            if (ResourceManager.Instance && ResourceManager.Instance.CurrentResources >= targetAmount)
            {
                Complete();
                return;
            }
 
            ResourceManager.OnResourcesChanged += OnResourcesChanged;
        }
 
        public override void Dispose()
        {
            ResourceManager.OnResourcesChanged -= OnResourcesChanged;
        }
 
        private void OnResourcesChanged(int currentAmount)
        {
            if (currentAmount >= targetAmount)
            {
                Complete();
            }
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Spend Resources", "Resource")]
    public class SpendResourcesObjective : BaseLevelObjective
    {
        [SerializeField, Min(1)] private int targetSpend = 500;
 
        private int _totalSpent;
        private int _previousAmount;
 
        public override string Description => $"Spend {targetSpend} resources";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_totalSpent}/{targetSpend}";
 
        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _totalSpent = 0;
            _previousAmount = ResourceManager.Instance.CurrentResources;
            ResourceManager.OnResourcesChanged += OnResourcesChanged;
        }
 
        public override void Dispose()
        {
            ResourceManager.OnResourcesChanged -= OnResourcesChanged;
        }
 
        private void OnResourcesChanged(int currentAmount)
        {
            if (currentAmount < _previousAmount)
            {
                _totalSpent += _previousAmount - currentAmount;
            }

            _previousAmount = currentAmount;

            if (_totalSpent >= targetSpend)
            {
                Complete();
            }
        }
    }
}