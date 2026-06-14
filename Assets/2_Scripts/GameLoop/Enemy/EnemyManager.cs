using System.Collections.Generic;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int maxEnemies = 100;
    [SerializeField] private LayerMask spawnRaycastMask;
    [SerializeField] private float spawnRaycastHeight = 200f;
    [SerializeField] private ChanceList<Enemy> enemyTypes = new ChanceList<Enemy>();
    
    private Transform _enemyHolder;
    private readonly ChanceList<EnemySpawnPoint> _activeSpawnPoints = new ChanceList<EnemySpawnPoint>();
    private readonly List<Enemy> _activeEnemies = new List<Enemy>();
    
    public int ActiveEnemiesCount => _activeEnemies.Count;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _enemyHolder = new GameObject("EnemyHolder").transform;
    }
    
    #region Spawning

    private void SpawnEnemy(Enemy enemy, EnemySpawnPoint spawnPoint)
    {
        Vector3 spawnOffset = Random.insideUnitSphere * spawnPoint.SpawnPointRange;
        spawnOffset.y = 0f;
        Vector3 spawnPosition = spawnPoint.transform.position.Add(spawnOffset);

        Vector3 rayOrigin = spawnPosition + Vector3.up * spawnRaycastHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, spawnRaycastHeight * 2f, spawnRaycastMask)) spawnPosition.y = hit.point.y;

        var request = new DeploymentRequest(spawnPosition, Vector3.forward, _enemyHolder)
        {
            Team = Team.Enemy,
        };

        DeploymentManager.Instance?.LaunchFromBaseCannon(enemy, request);
    }
    
    public void SpawnEnemyWave(int enemiesToSpawn, ChanceList<Enemy> enemySource = null, EnemySpawnPoint spawnPoint = null)
    {
        if (_activeEnemies.Count >= maxEnemies) return;

        if (!spawnPoint)
        {
            if (_activeSpawnPoints.Count == 0) return;
            spawnPoint = _activeSpawnPoints.GetRandomItem();
        }

        var source = enemySource ?? enemyTypes;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (_activeEnemies.Count >= maxEnemies) return;
            SpawnEnemy(source.GetRandomItem(), spawnPoint);
        }
    }
    

    #endregion
    
    #region Registration

    public void RegisterEnemy(Enemy enemy)
    {
        if (_activeEnemies.Contains(enemy)) return;
        
        _activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (!_activeEnemies.Contains(enemy)) return;
        
        _activeEnemies.Remove(enemy);
    }
    
    public void RegisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (_activeSpawnPoints.ToList().Contains(spawnPoint)) return;
        
        _activeSpawnPoints.AddItem(spawnPoint);
        _activeSpawnPoints.NormalizeChances();
    }
    
    public void UnregisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (!_activeSpawnPoints.ToList().Contains(spawnPoint)) return;
        
        var index = _activeSpawnPoints.IndexOf(spawnPoint);
        _activeSpawnPoints.RemoveAt(index);
        _activeSpawnPoints.NormalizeChances();
    }

    #endregion
}