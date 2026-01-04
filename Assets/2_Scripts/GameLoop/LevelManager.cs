using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    public static event Action OnLevelInitializing;
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnTimeUpdated;
    
    [Header("Level Settings")]
    [SerializeField] private float duration = 300f;
    [SerializeReference] private List<LevelEvent> events = new List<LevelEvent>();
    
    private float _timeRemaining;
    private bool _levelActive;

    public float TimeRemaining => _timeRemaining;
    public bool LevelActive => _levelActive;
    public float Duration => duration;
    public List<LevelEvent> GetEvents() => events;
    


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
        StartCoroutine(StartLevel());
    }

    private void Update()
    {
        if (!_levelActive) return;

        _timeRemaining -= Time.deltaTime;
        OnTimeUpdated?.Invoke(_timeRemaining);
        
        float elapsedTime = duration - _timeRemaining;
        CheckEvents(elapsedTime);

        if (_timeRemaining <= 0)
        {
            CompleteLevel();
        }
    }
    
    private void CheckEvents(float currentTime)
    {
        foreach (var evt in events)
        {
            if (!evt.hasTriggered && currentTime >= evt.triggerTime)
            {
                evt.Execute();
                evt.hasTriggered = true;
            }
        }
    }
    


    private IEnumerator StartLevel()
    {
        foreach (var evt in events)
        {
            evt.hasTriggered = false;
        }
        
        OnLevelInitializing?.Invoke();
        
        yield return new WaitForSeconds(3f);
        
        _timeRemaining = duration;
        _levelActive = true;
        
        OnLevelStarted?.Invoke();
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
        OnLevelFailed?.Invoke();
    }
    
    public void AddEvent(LevelEvent evt)
    {
        events.Add(evt);
        events.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
    }
    
    
}