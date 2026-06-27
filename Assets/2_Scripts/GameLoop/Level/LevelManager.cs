using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.Button;
using PrimeTween;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class LevelManager : MonoBehaviour, INotificationReceiver
{
    public static LevelManager Instance { get; private set; }

    public static event Action OnLevelInitializing;
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnLeveTimeLineUpdated;
    public static event Action<List<BaseLevelObjective>> OnObjectivesAdded;
    public static event Action OnObjectivesCompleted;
    public static event Action<int> OnResourceGoalSet;
    public static event Action OnResourceGoalCleared;

    [Header("Settings")]
    [Tooltip("Time before the time line starts")]
    [SerializeField] private float initializeDelay = 4f;
    [SerializeField, AutoGetSelf] private PlayableDirector timeline;
    
    private PlayerSpawnPoint _activePlayerSpawnPoint;
    private bool _levelActive;
    private List<BaseLevelObjective> _activeObjectives;
    private int _completedCount;
    private bool _hasResourceGoal;
    private int _resourceGoal;
    
    
    private float TimeRemaining => _levelActive ? (float)(timeline.duration - timeline.time) : 0f;
    public PlayerSpawnPoint ActivePlayerSpawnPoint => _activePlayerSpawnPoint;
    public bool IsLevelActive => _levelActive;
    public bool HasActiveObjectives => _activeObjectives != null;
    public IReadOnlyList<BaseLevelObjective> ActiveObjectives => _activeObjectives;
    public double TimelineTime => timeline ? timeline.time : 0;
    public double TimelineDuration => timeline ? timeline.duration : 0;

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
        
        PrimeTweenConfig.SetTweensCapacity(1000);
    }

    private void OnEnable()
    {
        if (timeline)
        {
            timeline.stopped += OnTimelineStopped;
        }
    }

    private void OnDestroy()
    {
        if (timeline)
        {
            timeline.stopped -= OnTimelineStopped;
        }

        ClearResourceGoal();
    }

    private void Start()
    {
        if (timeline)
        {
            StartCoroutine(StartLevel());
        }
    }

    private void Update()
    {
        if (_levelActive)
        {
            OnLeveTimeLineUpdated?.Invoke(TimeRemaining);
            TickObjectives();
        }
    }
    
    public void SetPlayerSpawnPoint(PlayerSpawnPoint playerSpawnPoint)
    {
        _activePlayerSpawnPoint = playerSpawnPoint;
    }

    public void PauseTimeline()
    {
        if (timeline) timeline.Pause();
    }

    public void ResumeTimeline()
    {
        if (timeline) timeline.Resume();
    }
    
    #region Level Control

    private IEnumerator StartLevel()
    {
        OnLevelInitializing?.Invoke();

        yield return new WaitForSeconds(initializeDelay);

        _levelActive = true;
        timeline.Play();

        OnLevelStarted?.Invoke();
    }
    

    public void CompleteLevel()
    {
        if (!_levelActive) return;

        _levelActive = false;
        DisposeObjectives();
        ClearResourceGoal();
        timeline.Stop();
        OnLevelCompleted?.Invoke();
    }

    public void FailLevel()
    {
        if (!_levelActive) return;

        _levelActive = false;
        DisposeObjectives();
        ClearResourceGoal();
        timeline.Stop();
        OnLevelFailed?.Invoke();
    }


    #endregion

    #region Resource Goal

    public void SetResourceGoal(int targetAmount)
    {
        if (!_levelActive || targetAmount <= 0) return;

        if (_hasResourceGoal) ResourceManager.OnResourcesChanged -= OnResourcesChangedForGoal;

        _hasResourceGoal = true;
        _resourceGoal = targetAmount;
        OnResourceGoalSet?.Invoke(_resourceGoal);

        int current = ResourceManager.Instance ? ResourceManager.Instance.CurrentResources : 0;
        if (current >= _resourceGoal)
        {
            CompleteLevel();
            return;
        }

        ResourceManager.OnResourcesChanged += OnResourcesChangedForGoal;
    }

    private void ClearResourceGoal()
    {
        if (!_hasResourceGoal) return;

        _hasResourceGoal = false;
        ResourceManager.OnResourcesChanged -= OnResourcesChangedForGoal;
        OnResourceGoalCleared?.Invoke();
    }

    private void OnResourcesChangedForGoal(int currentAmount)
    {
        if (currentAmount >= _resourceGoal) CompleteLevel();
    }

    #endregion

    #region Objectives

    private void TickObjectives()
    {
        if (_activeObjectives == null) return;

        float deltaTime = Time.deltaTime;
        foreach (var objective in _activeObjectives)
        {
            objective.Tick(deltaTime);
        }
    }

    private void DisposeObjectives()
    {
        if (_activeObjectives == null) return;

        foreach (var objective in _activeObjectives)
        {
            objective.Dispose();
        }
        _activeObjectives = null;
        _completedCount = 0;
    }

    private void OnObjectiveCompleted(BaseLevelObjective objective)
    {
        objective.Dispose();
        _completedCount++;

        if (_activeObjectives != null && _completedCount >= _activeObjectives.Count)
        {
            _activeObjectives = null;
            _completedCount = 0;
            OnObjectivesCompleted?.Invoke();
            timeline.Resume();
        }
    }

    public List<BaseLevelObjective> StartObjectives(List<BaseLevelObjective> objectives, IExposedPropertyTable resolver, bool pauseTimeline = true)
    {
        if (objectives == null || objectives.Count == 0) return null;

        if (pauseTimeline) timeline.Pause();

        _activeObjectives ??= new List<BaseLevelObjective>();

        var clones = new List<BaseLevelObjective>(objectives.Count);
        foreach (var objective in objectives)
        {
            if (objective == null) continue;
            var clone = objective.Clone();
            _activeObjectives.Add(clone);
            clones.Add(clone);
            clone.Initialize(() => OnObjectiveCompleted(clone), resolver);
        }

        if (clones.Count == 0) return clones;

        OnObjectivesAdded?.Invoke(_activeObjectives);
        return clones;
    }
    
    #endregion

    #region Timeline

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (!_levelActive) return;

        CompleteLevel();
    }
    
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (!Application.isPlaying) return;

        if (notification is BaseLevelEventMarker marker)
        {
            marker.Execute(origin.GetGraph().GetResolver());
        }
    }

    #endregion
}