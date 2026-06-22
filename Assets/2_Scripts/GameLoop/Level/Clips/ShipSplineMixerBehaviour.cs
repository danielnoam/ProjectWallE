using ProjectWallE.GameLoop;
using UnityEngine.Playables;
using UnityEngine.Splines;

public class ShipSplineMixerBehaviour : PlayableBehaviour
{
    private ShipMovement _ship;
    private SplineContainer _activeSpline;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playerData is not ShipMovement ship) return;

        SplineContainer spline = null;
        float normalizedTime = 0f;
        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            if (playable.GetInputWeight(i) <= 0f) continue;

            var input = (ScriptPlayable<ShipSplineBehaviour>)playable.GetInput(i);
            SplineContainer clipSpline = input.GetBehaviour().Spline;
            if (!clipSpline) continue;

            spline = clipSpline;
            double duration = input.GetDuration();
            normalizedTime = duration > 0d ? (float)(input.GetTime() / duration) : 0f;
            break;
        }

        if (!spline)
        {
            ReleaseControl();
            return;
        }

        if (_ship != ship || _activeSpline != spline)
        {
            _ship = ship;
            _activeSpline = spline;
            _ship.BeginTimelineControl(spline);
        }

        _ship.EvaluateTimeline(normalizedTime);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        ReleaseControl();
    }

    private void ReleaseControl()
    {
        if (_ship) _ship.EndTimelineControl();
        _ship = null;
        _activeSpline = null;
    }
}
