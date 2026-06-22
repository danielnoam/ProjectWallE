using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool pauseLevelTimeline = true;
        [SerializeField] private bool hideUI = true;
        
        
        [Header("References")]
        [SerializeField, AutoGetSelf] private PlayableDirector director;

        public PlayableDirector Director => director;
        public bool PauseLevelTimeline => pauseLevelTimeline;
        public bool HideUI => hideUI;

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
