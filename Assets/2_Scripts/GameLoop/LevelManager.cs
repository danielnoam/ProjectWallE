using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    public static event Action OnLevelStarted;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelFailed;
    public static event Action<float> OnTimeUpdated;
    
    [Header("Level Settings")]
    [SerializeField] private float duration = 300f;
    [SerializeField] private List<LevelEvent> events = new List<LevelEvent>();
    
    private float _timeRemaining;
    private bool _levelActive;
    private readonly ChanceList<EnemySpawnPoint> _enemySpawnPoints = new ChanceList<EnemySpawnPoint>();

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
    
    private void SpawnStartingStructures()
    {
        StructureSpawnPoint[] spawnPoints = FindObjectsByType<StructureSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        foreach (StructureSpawnPoint spawnPoint in spawnPoints)
        {
            if (!spawnPoint.StructurePrefab) continue;
            
            StructureDispatcher.Instance.DeployPod(
                spawnPoint.StructurePrefab, 
                spawnPoint.transform.position, 
                spawnPoint.transform.up
            );
        }
    }

    public void SpawnEnemyWave(int enemiesToSpawn)
    {
        var spawnPoint = _enemySpawnPoints.GetRandomItem();
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            var enemy = spawnPoint.AvailableEnemies.GetRandomItem();
            Vector3 spawnOffset = Random.insideUnitSphere * spawnPoint.SpawnPointRange;
            spawnOffset.y = 0;
            Vector3 spawnPosition = spawnPoint.transform.position + spawnOffset;
            
            Instantiate(enemy, spawnPosition, Quaternion.identity);
        }
    }

    private IEnumerator StartLevel()
    {
        foreach (var evt in events)
        {
            evt.hasTriggered = false;
        }
        var enemySpawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        foreach (EnemySpawnPoint spawnPoint in enemySpawnPoints)
        {
            _enemySpawnPoints.AddItem(spawnPoint);
        }
        _enemySpawnPoints.NormalizeChances();
        
        SpawnStartingStructures();
        
        _timeRemaining = duration;
        _levelActive = true;
        
        OnLevelStarted?.Invoke();
        
        yield break;
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