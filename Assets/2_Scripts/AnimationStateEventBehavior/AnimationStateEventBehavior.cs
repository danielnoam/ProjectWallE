using UnityEngine;

public class AnimationStateEventBehavior : StateMachineBehaviour
{
    public string eventName;
    [Range(0f, 1f)] public float triggerTime;
    
    private bool _hasTriggered;
    private float _previousNormalizedTime;
    
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasTriggered = false;
        _previousNormalizedTime = 0f;
    }
    
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = triggerTime >= 1f ? stateInfo.normalizedTime : stateInfo.normalizedTime % 1f;

        if (currentTime < _previousNormalizedTime)
            _hasTriggered = false;

        if (!_hasTriggered && currentTime >= triggerTime)
        {
            NotifyReceiver(animator);
            _hasTriggered = true;
        }

        _previousNormalizedTime = currentTime;
    }

    private void NotifyReceiver(Animator animator)
    {
        AnimationEventReceiver receiver = animator.GetComponent<AnimationEventReceiver>();

        receiver?.OnAnimationEventTriggered(eventName);
    }

}