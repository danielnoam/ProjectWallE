using UnityEngine;
using UnityEngine.Playables;

public class LevelEventBehaviour : PlayableBehaviour
{
    public BaseLevelEventAsset EventAsset;
    public IExposedPropertyTable Resolver;
    private bool _hasTriggered;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || _hasTriggered) return;
        _hasTriggered = true;
        
        EventAsset?.Execute(Resolver);
    }

    public override void OnGraphStop(Playable playable)
    {
        _hasTriggered = false;
    }
}