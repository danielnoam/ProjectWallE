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
    [SerializeField] private float levelDuration = 300f;
    
    private float _timeRemaining;
    private bool _levelActive;
    private readonly ChanceList<EnemySpawnPoint> _enemySpawnPoints = new ChanceList<EnemySpawnPoint>();

    public float TimeRemaining => _timeRemaining;
    public bool LevelActive => _levelActive;

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

        if (_timeRemaining <= 0)
        {
            CompleteLevel();
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

    private void SpawnEnemyWave(int enemiesToSpawn)
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
        var enemySpawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        foreach (EnemySpawnPoint spawnPoint in enemySpawnPoints)
        {
            _enemySpawnPoints.AddItem(spawnPoint);
        }
        _enemySpawnPoints.NormalizeChances();
        
        SpawnStartingStructures();
        
        yield return new WaitForSeconds(3f);
        SpawnEnemyWave(5);

        
        _timeRemaining = levelDuration;
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

}