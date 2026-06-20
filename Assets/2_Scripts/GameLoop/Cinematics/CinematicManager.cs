using System;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE
{
    public class CinematicManager : MonoBehaviour
    {
        public static CinematicManager Instance { get; private set; }

        public static event Action OnCinematicStarted;
        public static event Action OnCinematicEnded;

        private PlayableDirector _activeDirector;
        private bool _pausedLevelTimeline;

        public bool IsPlaying => _activeDirector;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDisable()
        {
            if (_activeDirector) _activeDirector.stopped -= OnDirectorStopped;
        }

        public void Play(PlayableDirector director, bool pauseLevelTimeline = true)
        {
            if (!director || _activeDirector) return;

            _activeDirector = director;
            _pausedLevelTimeline = pauseLevelTimeline;

            PlayerManager.Instance?.SetCinematicMode(true);
            CameraManager.Instance?.SetCinematicMode(true);
            if (pauseLevelTimeline) LevelManager.Instance?.PauseTimeline();

            OnCinematicStarted?.Invoke();

            director.stopped += OnDirectorStopped;
            director.Play();
        }

        public void Stop()
        {
            if (_activeDirector) _activeDirector.Stop();
        }

        private void OnDirectorStopped(PlayableDirector director)
        {
            if (director != _activeDirector) return;

            _activeDirector.stopped -= OnDirectorStopped;
            _activeDirector = null;

            if (_pausedLevelTimeline) LevelManager.Instance?.ResumeTimeline();
            PlayerManager.Instance?.SetCinematicMode(false);
            CameraManager.Instance?.SetCinematicMode(false);

            OnCinematicEnded?.Invoke();
        }
    }
}
