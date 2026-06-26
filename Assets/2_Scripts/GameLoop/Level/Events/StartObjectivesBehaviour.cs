using System.Collections.Generic;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

public class StartObjectivesBehaviour : PlayableBehaviour
{
    public StartObjectivesEvent EventAsset;
    public IExposedPropertyTable Resolver;

    private List<BaseLevelObjective> _activeClones;
    private bool _started;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || _started) return;
        if (EventAsset != null && EventAsset.SkipInEditor && Application.isEditor) return;

        _started = true;
        _activeClones = LevelManager.Instance?.StartObjectives(EventAsset.Objectives, Resolver, pauseTimeline: false);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || !_started) return;

        _started = false;

        bool stillIncomplete = _activeClones != null && _activeClones.Exists(objective => !objective.IsCompleted);
        if (stillIncomplete) LevelManager.Instance?.PauseTimeline();

        _activeClones = null;
    }
}
