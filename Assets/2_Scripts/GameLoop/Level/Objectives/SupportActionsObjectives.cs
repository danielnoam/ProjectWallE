using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    
    [Serializable]
    [SerializableSelectorName("Call Support", "Support Action")]
    public class CallSupportAction : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Leave the field empty to allow any support to count towards the objective.")]
        [PrefabSelector("Assets/6_Data")] public SOSupportActionData supportAction;

        public override string Description => supportAction ? $"Call {supportAction.Label}" : "Call a air support";

        protected override string DefaultTutorialText => "Press *[E]* to open air support menu";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            SOSupportActionData.OnSupportActionCalled += OnSupportActionCalled;
        }

        protected override void OnDispose()
        {
            SOSupportActionData.OnSupportActionCalled -= OnSupportActionCalled;
        }


        private void OnSupportActionCalled(SOSupportActionData data)
        {
            if (supportAction && data != supportAction) return;
            Complete();
        }
    }
}