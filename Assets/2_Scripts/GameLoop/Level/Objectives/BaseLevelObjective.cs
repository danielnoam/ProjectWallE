using System;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class BaseLevelObjective
    {
        private bool _isCompleted;
        private Action _onComplete;

        public bool IsCompleted => _isCompleted;
        public abstract string Description { get; }
        public virtual string ProgressText => _isCompleted ? "Complete" : "In Progress";

        public void Initialize(Action onComplete, IExposedPropertyTable resolver = null)
        {
            _isCompleted = false;
            _onComplete = onComplete;
            OnInitialize(resolver);
        }

        protected abstract void OnInitialize(IExposedPropertyTable resolver = null);
        public abstract void Dispose();
        public virtual void Tick(float deltaTime) { }

        protected void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _onComplete?.Invoke();
        }
    }
}