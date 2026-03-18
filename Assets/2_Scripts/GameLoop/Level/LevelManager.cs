using System;
using System.Collections;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayableDirector))]
public class LevelManager : MonoBehaviour, INotificationReceiver
{
    public static LevelManager Instance { get; private set; }
    
    public static event Action OnLevelInitializing;
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnTimeUpdated;
    
    
    [Header("Settings")]
    [Tooltip("Time before the time line starts")]
    [SerializeField] private float initializeDelay = 4f;
    [SerializeField, AutoGetSelf, HideInInspector] private PlayableDirector timeline;
    [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;
    
    
    private bool _levelActive;
    
    private float TimeRemaining => _levelActive ? (float)(timeline.duration - timeline.time) : 0f;
    public PlayerManager Player => player;

    private void OnValidate()
    {
        AutoGetSystem.Process(this);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void OnDestroy()
    {
        if (timeline)
        {
            timeline.stopped -= OnTimelineStopped;
        }
    }

    private void Start()
    {
        if (timeline)
        {
            timeline.stopped += OnTimelineStopped;
            StartCoroutine(StartLevel());
        }
    }
    
    private void Update()
    {

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        if (_levelActive) OnTimeUpdated?.Invoke(TimeRemaining);
    }

    private IEnumerator StartLevel()
    {
        OnLevelInitializing?.Invoke();
        
        yield return new WaitForSeconds(initializeDelay);
        
        _levelActive = true;
        timeline.Play();
        
        OnLevelStarted?.Invoke();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (!_levelActive) return;
        
        CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (!_levelActive) return;
        
        _levelActive = false;
        timeline.Stop();
        OnLevelCompleted?.Invoke();
    }

    public void FailLevel()
    {
        if (!_levelActive) return;
        
        _levelActive = false;
        timeline.Stop();
        OnLevelFailed?.Invoke();
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (!Application.isPlaying) return;
        
        if (notification is BaseLevelEventMarker marker)
        {
            marker.Execute(origin.GetGraph().GetResolver());
        }
    }
}