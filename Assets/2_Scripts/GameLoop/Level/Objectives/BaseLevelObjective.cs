using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using ProjectWallE.GameLoop.UI;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public abstract class BaseLevelObjective
    {
        [Header("Marker")]
        [SerializeField, ScenePicker] protected ExposedReference<ObjectiveGameMarker> objectiveMarker;
        [SerializeField] protected bool showInGame = true;
        [SerializeField] protected bool showOnRadar = true;

        [Header("Tutorial")]
        [Tooltip("Use * to wrap text for styled formatting, e.g. 'Press *Space* to jump'")]
        [SerializeField] private OptionalField<string> tutorialTextOverride;

        private bool _isDisposed;
        private bool _isCompleted;
        private Action _onComplete;

        protected ObjectiveGameMarker ResolvedMarker { get; private set; }

        public bool HasMarker => objectiveMarker.Resolve(null);
        public bool IsCompleted => _isCompleted;
        public string TutorialText => tutorialTextOverride.isSet ? tutorialTextOverride.Value : DefaultTutorialText;
        public abstract string Description { get; }
        public virtual string ProgressText => _isCompleted ? "Complete" : "In Progress";
        protected virtual string DefaultTutorialText => string.Empty;

        protected abstract void OnInitialize(IExposedPropertyTable resolver = null);

        protected virtual void OnDispose() { }

        protected void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _onComplete?.Invoke();
        }

        public virtual void Tick(float deltaTime) { }

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