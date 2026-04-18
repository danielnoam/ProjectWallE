using System;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class BaseLevelObjective
    {
        [Header("Marker")]
        [SerializeField] protected ExposedReference<ObjectiveGameMarker> objectiveMarker;
        [SerializeField] protected bool showInGame = true;
        [SerializeField] protected bool showOnRadar = true;
        

        private bool _isDisposed;
        private bool _isCompleted;
        private Action _onComplete;

        protected ObjectiveGameMarker ResolvedMarker { get; private set; }

        public bool IsCompleted => _isCompleted;
        public abstract string Description { get; }
        public virtual string ProgressText => _isCompleted ? "Complete" : "In Progress";
        

        protected abstract void OnInitialize(IExposedPropertyTable resolver = null);


        protected virtual void OnDispose()
        {
            
        }

        protected void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _onComplete?.Invoke();
        }

        public virtual void Tick(float deltaTime)
        {
            
        }

        public BaseLevelObjective Clone()
        {
            return (BaseLevelObjective)MemberwiseClone();
        }
        
        public void Initialize(Action onComplete, IExposedPropertyTable resolver = null)
        {
            _isCompleted = false;
            _isDisposed = false;
            _onComplete = onComplete;
            ResolvedMarker = objectiveMarker.Resolve(resolver);
            if (ResolvedMarker) ResolvedMarker.OnObjectiveStarted(showInGame, showOnRadar);
            OnInitialize(resolver);
        }

        public void ForceComplete()
        {
            Complete();
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (ResolvedMarker) ResolvedMarker.OnObjectiveCompleted(showInGame, showOnRadar);
            OnDispose();
        }
    }
}