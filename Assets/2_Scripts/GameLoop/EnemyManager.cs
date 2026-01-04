using System.Collections.Generic;
using System.Linq;
using DNExtensions;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    private readonly ChanceList<EnemySpawnPoint> _enemySpawnPoints = new ChanceList<EnemySpawnPoint>();
    private List<Base> _mainTargets = new List<Base>();
    private readonly List<Enemy> _activeEnemies = new List<Enemy>();

    
    private void Awake()
    {
        if (Instance) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        LevelManager.OnLevelStarted += OnLevelStarted;
        LevelManager.OnLevelCompleted += OnLevelEnded;
        LevelManager.OnLevelFailed += OnLevelEnded;
    }

    private void OnDisable()
    {
        LevelManager.OnLevelStarted -= OnLevelStarted;
        LevelManager.OnLevelCompleted -= OnLevelEnded;
        LevelManager.OnLevelFailed -= OnLevelEnded;
    }

    private void OnLevelStarted()
    {
        _mainTargets = FindObjectsByType<Base>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        
        var enemySpawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        foreach (EnemySpawnPoint spawnPoint in enemySpawnPoints)
        {
            _enemySpawnPoints.AddItem(spawnPoint);
        }
        _enemySpawnPoints.NormalizeChances();
    }
    
    private void OnLevelEnded()
    {
        _mainTargets.Clear();
    }
    
    
    public void SpawnEnemyWave(int enemiesToSpawn)
    {
        var spawnPoint = _enemySpawnPoints.GetRandomItem();
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            var enemy = spawnPoint.AvailableEnemies.GetRandomItem();
            Vector3 spawnOffset = UnityEngine.Random.insideUnitSphere * spawnPoint.SpawnPointRange;
            spawnOffset.y = 0;
            Vector3 spawnPosition = spawnPoint.transform.position + spawnOffset;
            
            Instantiate(enemy, spawnPosition, Quaternion.identity);
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        _activeEnemies.Add(enemy);
        enemy.SetMainTarget(_mainTargets[0]);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
    }
}