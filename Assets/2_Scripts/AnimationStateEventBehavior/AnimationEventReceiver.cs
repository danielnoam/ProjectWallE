using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{

    [SerializeField] private List<AnimationEvent> animationEvents = new();
    
    
    public void OnAnimationEventTriggered(string eventName)
    {
        var eventToTrigger = animationEvents.Find(se => se.eventName == eventName);
        eventToTrigger?.onAnimationEvent?.Invoke();
    }
    
}