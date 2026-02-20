using System;
using System.Collections;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
[RequireComponent(typeof(LevelEventReceiver))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    public static event Action OnLevelInitializing;
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnTimeUpdated;
    
    
    [SerializeField, AutoGetSelf] private PlayableDirector timeline;

    private bool _levelActive;
    private float TimeRemaining => _levelActive ? (float)(timeline.duration - timeline.time) : 0f;
    
    
    public PlayerStructureBuilder Player { get; private set; }
    
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (timeline)
        {
            timeline.stopped += OnTimelineStopped;
            StartCoroutine(StartLevel());
        }
    }

    private void OnDestroy()
    {
        if (timeline)
        {
            timeline.stopped -= OnTimelineStopped;
        }
    }

    private void Update()
    {
        if (!_levelActive) return;
        
        OnTimeUpdated?.Invoke(TimeRemaining);
    }

    private IEnumerator StartLevel()
    {
        Player = FindFirstObjectByType<PlayerStructureBuilder>();
        OnLevelInitializing?.Invoke();
        
        yield return new WaitForSeconds(3f);
        
        _levelActive = true;
        timeline.Play();
        
        OnLevelStarted?.Invoke();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director == timeline && _levelActive)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        _levelActive = false;
        OnLevelCompleted?.Invoke();
    }

    public void FailLevel()
    {
        if (!_levelActive) return;
        
        _levelActive = false;
        timeline.Stop();
        OnLevelFailed?.Invoke();
    }
}