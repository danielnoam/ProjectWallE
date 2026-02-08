using UnityEngine;
using UnityEngine.Playables;

public class ContinuousEventBehaviour : PlayableBehaviour
{
    public BaseLevelEventAsset EventAsset;
    public IExposedPropertyTable Resolver;
    public float Interval;
    private float _nextTriggerTime;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!Application.isPlaying) return;
        
        double time = playable.GetTime();
        
        if (time >= _nextTriggerTime)
        {
            EventAsset?.Execute(Resolver);
            _nextTriggerTime += Interval;
        }
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        _nextTriggerTime = 0;
    }
}