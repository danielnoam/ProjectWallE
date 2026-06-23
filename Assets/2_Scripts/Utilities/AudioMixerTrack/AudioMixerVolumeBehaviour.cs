using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE.Utilities
{
    public class AudioMixerVolumeBehaviour : PlayableBehaviour
    {
        public string ExposedParameter;
        public AnimationCurve VolumeCurve;
    }
}
