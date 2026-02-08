using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LevelEventReceiver : MonoBehaviour, INotificationReceiver
{
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (!Application.isPlaying) return;
        
        if (notification is BaseLevelEventMarker marker)
        {
            marker.Execute(origin.GetGraph().GetResolver());
        }
    }
}