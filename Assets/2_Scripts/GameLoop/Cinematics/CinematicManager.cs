using System;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE
{
    public class CinematicManager : MonoBehaviour
    {
        public static CinematicManager Instance { get; private set; }

        public static event Action OnCinematicStarted;
        public static event Action OnCinematicEnded;

        private CinematicController _activeController;
        private PlayableDirector _activeDirector;
        private bool _pausedLevelTimeline;
        private bool _showUIAfter;

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

        public void Play(CinematicController controller, bool pauseLevelTimeline = true, bool hideUI = true, bool showUIAfter = true)
        {
            if (!controller || !controller.Director || _activeDirector) return;

            _activeController = controller;
            _activeDirector = controller.Director;
            _pausedLevelTimeline = pauseLevelTimeline;
            _showUIAfter = showUIAfter;

            PlayerManager.Instance?.SetCinematicMode(true);
            CameraManager.Instance?.SetCinematicMode(true);
            if (_pausedLevelTimeline) LevelManager.Instance?.PauseTimeline();
            if (hideUI) UIManager.Instance?.SetHudVisible(false);

            OnCinematicStarted?.Invoke();

            _activeDirector.stopped += OnDirectorStopped;
            _activeDirector.Play();
        }

        public void Stop()
        {
            if (_activeDirector) _activeDirector.Stop();
        }

        private void OnDirectorStopped(PlayableDirector director)
        {
            if (director != _activeDirector) return;

            _activeDirector.stopped -= OnDirectorStopped;

            if (_pausedLevelTimeline) LevelManager.Instance?.ResumeTimeline();
            PlayerManager.Instance?.SetCinematicMode(false);
            CameraManager.Instance?.SetCinematicMode(false);
            if (_showUIAfter) UIManager.Instance?.SetHudVisible(true);

            _activeController = null;
            _activeDirector = null;

            OnCinematicEnded?.Invoke();
        }
    }
}
