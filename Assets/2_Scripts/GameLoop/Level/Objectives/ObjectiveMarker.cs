using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class ObjectiveGameMarker : MonoBehaviour
    {
        
        [Header("References")]
        [SerializeField, AutoGetSelf] private Note note;
        [SerializeField, AutoGetSelf] private RadarTarget radarTarget;
        


        public void OnObjectiveStarted()
        {
            radarTarget?.EnableBlip();   
            radarTarget?.PingBlip();
        }
        
        public void OnObjectiveCompleted()
        {
            radarTarget?.DisableBlip();
        }
    }
}