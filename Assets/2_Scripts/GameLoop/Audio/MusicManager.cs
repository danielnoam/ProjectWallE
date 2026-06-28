using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace ProjectWallE
{
    public class MusicManager : MonoBehaviour
    {
        public enum MusicAction { None, StartGameplay, PlaySpecific, Stop }

        public static MusicManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private Key muteKey = Key.M;
        [SerializeField, Min(0f)] private float fadeDuration = 1f;
        [SerializeField, Min(0f)] private float stopFadeDuration = 1f;
        [SerializeField] private bool crossfade = true;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private AudioMixerGroup mixerGroup;

        [Header("Gameplay Tracks")]
        [Tooltip("Clips rotated through randomly while gameplay music is active")]
        [SerializeField] private List<AudioClip> gameplayTracks = new();

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _activeSource;
        private AudioClip _currentClip;
        private bool _gameplayActive;
        private bool _muted;
        private Coroutine _gameplayRoutine;
        private Coroutine _crossfadeRoutine;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sourceA = CreateSource("MusicSourceA");
            _sourceB = CreateSource("MusicSourceB");
            _activeSource = _sourceA;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[muteKey].wasPressedThisFrame)
                ToggleMute();
        }

        public void ToggleMute()
        {
            _muted = !_muted;
            _sourceA.mute = _muted;
            _sourceB.mute = _muted;
        }

        public void Execute(MusicAction action, AudioClip clip = null)
        {
            switch (action)
            {
                case MusicAction.StartGameplay:
                    StartGameplayMusic();
                    break;
                case MusicAction.PlaySpecific:
                    PlayClip(clip);
                    break;
                case MusicAction.Stop:
                    StopMusic();
                    break;
            }
        }

        public void PlayClip(AudioClip clip)
        {
            if (!clip || clip == _currentClip) return;

            StopGameplayRoutine();
            TransitionTo(clip, true);
        }

        public void StartGameplayMusic()
        {
            if (gameplayTracks.Count == 0) return;

            StopGameplayRoutine();
            _gameplayActive = true;
            _gameplayRoutine = StartCoroutine(GameplayRoutine());
        }

        public void StopMusic()
        {
            StopGameplayRoutine();
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(StopRoutine());
            _currentClip = null;
        }

        private IEnumerator StopRoutine()
        {
            AudioSource source = _activeSource;
            yield return FadeRoutine(source, 0f, stopFadeDuration);
            source.Stop();
            _crossfadeRoutine = null;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = 0f;
            return source;
        }

        private void TransitionTo(AudioClip clip, bool loop)
        {
            AudioSource next = _activeSource == _sourceA ? _sourceB : _sourceA;

            next.clip = clip;
            next.loop = loop;
            next.time = 0f;
            next.volume = 0f;
            next.Play();

            StartCrossfade(_activeSource, next);

            _activeSource = next;
            _currentClip = clip;
        }

        private void StartCrossfade(AudioSource from, AudioSource to)
        {
            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(from, to));
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to)
        {
            if (!crossfade && from && from.isPlaying)
            {
                yield return FadeRoutine(from, 0f, fadeDuration);
                if (from != to) from.Stop();
            }

            float elapsed = 0f;
            float fromStart = from ? from.volume : 0f;
            float toStart = to ? to.volume : 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                if (from && from != to) from.volume = Mathf.Lerp(fromStart, 0f, t);
                if (to) to.volume = Mathf.Lerp(toStart, volume, t);
                yield return null;
            }

            if (from && from != to)
            {
                from.volume = 0f;
                from.Stop();
            }
            if (to) to.volume = volume;

            _crossfadeRoutine = null;
        }

        private IEnumerator FadeRoutine(AudioSource source, float target, float duration)
        {
            float start = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            source.volume = target;
        }

        private IEnumerator GameplayRoutine()
        {
            while (_gameplayActive)
            {
                AudioClip next = PickRandomGameplayTrack();
                if (!next) yield break;

                TransitionTo(next, false);

                float length = next.length;
                if (length <= 0f) yield break;

                yield return new WaitForSeconds(Mathf.Max(0.1f, length - fadeDuration));
            }
        }

        private AudioClip PickRandomGameplayTrack()
        {
            if (gameplayTracks.Count == 0) return null;
            if (gameplayTracks.Count == 1) return gameplayTracks[0];

            AudioClip next;
            do
            {
                next = gameplayTracks[Random.Range(0, gameplayTracks.Count)];
            }
            while (next == _currentClip);

            return next;
        }

        private void StopGameplayRoutine()
        {
            _gameplayActive = false;
            if (_gameplayRoutine == null) return;

            StopCoroutine(_gameplayRoutine);
            _gameplayRoutine = null;
        }
    }
}
