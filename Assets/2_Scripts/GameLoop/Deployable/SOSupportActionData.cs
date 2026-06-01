using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [CreateAssetMenu(fileName = "New SOSupport Action Data", menuName = "Support Action Data")]
    public class SOSupportActionData : ScriptableObject, IDeployableWithPod
    {
        public static event Action<SOSupportActionData> OnSupportActionCalled;
        
        [Header("Identity")]
        [SerializeField] private string label;
        [SerializeField] private Sprite icon;
        [SerializeField, PrefabSelector("Assets/5_Prefabs")] private Pod podPrefab;
        
        [Header("Behavior")]
        [SerializeField, Min(0)] private int cost;
        [SerializeField, Min(1)] private int podCount = 1;
        [SerializeField, Min(0)] private float scatterRadius;
        [SerializeReference, SerializableSelector(Foldout = false)] private DeployBehavior[] onImpact;
        
        public int PodCount => podCount;
        public float ScatterRadius => scatterRadius;
        public string Label => label;
        public Sprite Icon => icon;
        public Pod PodPrefab => podPrefab;
        public int Cost => cost;



        public void Deploy(DeploymentRequest deploymentRequest, BehaviorRequest behaviorRequest)
        {
            foreach (var effect in onImpact)
            {
                effect?.Execute(behaviorRequest);
            }
            
            OnSupportActionCalled?.Invoke(this);
        }


    }
}