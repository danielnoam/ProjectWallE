using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
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

    [Header("Settings")]
    [Tooltip("Time before the time line starts")]
    [SerializeField] private float initializeDelay = 4f;
    [SerializeField, AutoGetSelf] private PlayableDirector timeline;
    [SerializeField, AutoGetScene] private PlayerManager player;

    private bool _levelActive;
    private List<BaseLevelObjective> _activeObjectives;
    private int _completedCount;

    private float TimeRemaining => _levelActive ? (float)(timeline.duration - timeline.time) : 0f;
    public PlayerManager Player => player;
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
        DisposeObjectives();
        timeline.Stop();
        OnLevelCompleted?.Invoke();
    }

    private void FailLevel()
    {
        if (!_levelActive) return;

        _levelActive = false;
        DisposeObjectives();
        timeline.Stop();
        OnLevelFailed?.Invoke();
    }

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

    public void StartObjectives(List<BaseLevelObjective> objectives, IExposedPropertyTable resolver)
    {
        if (objectives == null || objectives.Count == 0) return;

        timeline.Pause();

        _activeObjectives ??= new List<BaseLevelObjective>();

        foreach (var objective in objectives)
        {
            var clone = objective.Clone();
            clone.Initialize(() => OnObjectiveCompleted(clone), resolver);
            _activeObjectives.Add(clone);
        }

        OnObjectivesAdded?.Invoke(_activeObjectives);
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