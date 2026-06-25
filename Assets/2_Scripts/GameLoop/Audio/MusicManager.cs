using System.Collections;
using System.Collections.Generic;
using DNExtensions.Systems.AudioTrack;
using UnityEngine;

namespace ProjectWallE
{
    public class MusicManager : MonoBehaviour
    {
        public enum MusicAction { None, StartGameplay, PlaySpecific, Stop }

        public static MusicManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField, Min(0f)] private float fadeDuration = 1f;
        [SerializeField] private bool crossfade = true;

        [Header("Gameplay Tracks")]
        [Tooltip("Tracks rotated through randomly while gameplay music is active")]
        [SerializeField, AudioTrackID] private List<string> gameplayTracks = new();

        private string _currentTrackId;
        private bool _gameplayActive;
        private Coroutine _gameplayRoutine;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Execute(MusicAction action, string trackId = null)
        {
            switch (action)
            {
                case MusicAction.StartGameplay:
                    StartGameplayMusic();
                    break;
                case MusicAction.PlaySpecific:
                    PlayTrack(trackId);
                    break;
                case MusicAction.Stop:
                    StopMusic();
                    break;
            }
        }

        public void PlayTrack(string id)
        {
            if (string.IsNullOrEmpty(id) || id == _currentTrackId) return;

            StopGameplayRoutine();
            TransitionTo(id);
        }

        public void StartGameplayMusic()
        {
            if (gameplayTracks.Count == 0) return;

            _gameplayActive = true;
            StopGameplayRoutine();
            _gameplayRoutine = StartCoroutine(GameplayRoutine());
        }

        public void StopMusic()
        {
            StopGameplayRoutine();
            if (!string.IsNullOrEmpty(_currentTrackId)) AudioTrack.Stop(_currentTrackId, fadeDuration);
            _currentTrackId = null;
        }

        private void TransitionTo(string id)
        {
            if (string.IsNullOrEmpty(_currentTrackId)) AudioTrack.Play(id, fadeDuration);
            else AudioTrack.Transition(_currentTrackId, id, fadeDuration, crossfade);

            _currentTrackId = id;
        }

        private IEnumerator GameplayRoutine()
        {
            while (_gameplayActive)
            {
                string next = PickRandomGameplayTrack();
                if (string.IsNullOrEmpty(next)) yield break;

                TransitionTo(next);

                float length = GetTrackLength(next);
                if (length <= 0f) yield break;

                yield return new WaitForSeconds(Mathf.Max(0.1f, length - fadeDuration));
            }
        }

        private string PickRandomGameplayTrack()
        {
            if (gameplayTracks.Count == 0) return null;
            if (gameplayTracks.Count == 1) return gameplayTracks[0];

            string next;
            do
            {
                next = gameplayTracks[Random.Range(0, gameplayTracks.Count)];
            }
            while (next == _currentTrackId);

            return next;
        }

        private void StopGameplayRoutine()
        {
            _gameplayActive = false;
            if (_gameplayRoutine == null) return;

            StopCoroutine(_gameplayRoutine);
            _gameplayRoutine = null;
        }

        private static float GetTrackLength(string id)
        {
            var settings = SOAudioTrackSettings.Instance;
            if (!settings) return 0f;

            foreach (var track in settings.Tracks)
            {
                if (track.id == id) return track.clip ? track.clip.length : 0f;
            }

            return 0f;
        }
    }
}
