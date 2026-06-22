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

        public void Play(CinematicController controller)
        {
            if (!controller || !controller.Director || _activeDirector) return;

            _activeController = controller;
            _activeDirector = controller.Director;
            _pausedLevelTimeline = controller.PauseLevelTimeline;

            PlayerManager.Instance?.SetCinematicMode(true);
            CameraManager.Instance?.SetCinematicMode(true);
            if (_pausedLevelTimeline) LevelManager.Instance?.PauseTimeline();
            if (controller.HideUI) UIManager.Instance?.SetHudVisible(false);

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
            if (_activeController.HideUI) UIManager.Instance?.SetHudVisible(true);

            _activeController = null;
            _activeDirector = null;

            OnCinematicEnded?.Invoke();
        }
    }
}
