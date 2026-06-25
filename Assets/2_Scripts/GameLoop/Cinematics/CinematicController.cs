using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, AutoGetSelf] private PlayableDirector director;

        public PlayableDirector Director => director;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        public void Play()
        {
            CinematicManager.Instance?.Play(this);
        }

        public void Stop()
        {
            CinematicManager.Instance?.Stop();
        }
    }
}
