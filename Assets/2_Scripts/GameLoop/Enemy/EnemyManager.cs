using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.PrefabSelector;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int maxEnemies = 50;
    [SerializeField] private ChanceList<Enemy> enemyTypes = new ChanceList<Enemy>();
    
    
    private readonly ChanceList<EnemySpawnPoint> _enemySpawnPoints = new ChanceList<EnemySpawnPoint>();
    private readonly List<Enemy> _activeEnemies = new List<Enemy>();

    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void SpawnEnemy(Enemy enemy, EnemySpawnPoint spawnPoint)
    {
        Vector3 spawnOffset = Random.insideUnitSphere * spawnPoint.SpawnPointRange;
        spawnOffset.y = 0;
        Vector3 spawnPosition = spawnPoint.transform.position + spawnOffset;
        
        Instantiate(enemy, spawnPosition, Quaternion.identity);
    }
    
    public void TrySpawnEnemyWave(int enemiesToSpawn)
    {
        if (_enemySpawnPoints.Count == 0 || _activeEnemies.Count >= maxEnemies) return;
        
        var spawnPoint = _enemySpawnPoints.GetRandomItem();
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (_activeEnemies.Count >= maxEnemies) return;
            
            var enemy = enemyTypes.GetRandomItem();
            SpawnEnemy(enemy, spawnPoint);
        }
    }

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
        if (_enemySpawnPoints.ToList().Contains(spawnPoint)) return;
        
        _enemySpawnPoints.AddItem(spawnPoint);
        _enemySpawnPoints.NormalizeChances();
    }
    
    public void UnregisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (!_enemySpawnPoints.ToList().Contains(spawnPoint)) return;
        
        var index = _enemySpawnPoints.IndexOf(spawnPoint);
        _enemySpawnPoints.RemoveAt(index);
        _enemySpawnPoints.NormalizeChances();
    }
    
    
    public Enemy GetNearestEnemy(Vector3 position)
    {
        Enemy nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var enemy in _activeEnemies)
        {
            if (!enemy) continue;
            
            float dist = Vector3.Distance(position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
}