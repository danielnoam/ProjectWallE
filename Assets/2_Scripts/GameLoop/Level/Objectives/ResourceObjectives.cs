using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    [SerializableSelectorName("Destroy Resource Nodes", "Resource")]
    public class DestroyResourceRocksObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [SerializeField, Min(1)] private int destroyCount = 5;

        private int _currentCount;

        public override string Description => $"Destroy {destroyCount} resource nodes";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentCount}/{destroyCount}";

        protected override string DefaultTutorialText => "";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentCount = 0;
            ResourceRock.OnDestroyed += OnNodeDestroyed;
        }

        protected override void OnDispose()
        {
            ResourceRock.OnDestroyed -= OnNodeDestroyed;
        }

        private void OnNodeDestroyed(ResourceRock rock)
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
        [Header("Settings")]
        [SerializeField, Min(1)] private int targetAmount = 500;

        public override string Description => $"Collect {targetAmount} resources";
        public override string ProgressText => IsCompleted ? "Complete" : $"{ResourceManager.Instance?.CurrentResources ?? 0}/{targetAmount}";

        protected override string DefaultTutorialText => "";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            if (ResourceManager.Instance && ResourceManager.Instance.CurrentResources >= targetAmount)
            {
                Complete();
                return;
            }

            ResourceManager.OnResourcesChanged += OnResourcesChanged;
        }

        protected override void OnDispose()
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
        [Header("Settings")]
        [SerializeField, Min(1)] private int targetSpend = 500;

        private int _totalSpent;
        private int _previousAmount;

        public override string Description => $"Spend {targetSpend} resources";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_totalSpent}/{targetSpend}";

        protected override string DefaultTutorialText => "Build or upgrade structures to spend resources";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _totalSpent = 0;
            _previousAmount = ResourceManager.Instance.CurrentResources;
            ResourceManager.OnResourcesChanged += OnResourcesChanged;
        }

        protected override void OnDispose()
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