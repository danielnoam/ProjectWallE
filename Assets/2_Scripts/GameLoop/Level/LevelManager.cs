using System;
using System.Collections;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    public static event Action OnLevelInitializing;
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnTimeUpdated;
    
    [SerializeField, AutoGetSelf] private PlayableDirector timeline;

    public bool LevelActive { get; private set; }
    public float TimeRemaining => LevelActive ? (float)(timeline.duration - timeline.time) : 0f;
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
        timeline.stopped += OnTimelineStopped;
        StartCoroutine(StartLevel());
    }

    private void OnDestroy()
    {
        if (timeline != null)
        {
            timeline.stopped -= OnTimelineStopped;
        }
    }

    private void Update()
    {
        if (!LevelActive) return;
        
        OnTimeUpdated?.Invoke(TimeRemaining);
    }

    private IEnumerator StartLevel()
    {
        Player = FindFirstObjectByType<PlayerStructureBuilder>();
        OnLevelInitializing?.Invoke();
        
        yield return new WaitForSeconds(3f);
        
        LevelActive = true;
        timeline.Play();
        
        OnLevelStarted?.Invoke();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director == timeline && LevelActive)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        LevelActive = false;
        OnLevelCompleted?.Invoke();
    }

    public void FailLevel()
    {
        if (!LevelActive) return;
        
        LevelActive = false;
        timeline.Stop();
        OnLevelFailed?.Invoke();
    }
}