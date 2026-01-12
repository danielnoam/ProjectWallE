using System.Collections.Generic;
using DNExtensions;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("Enemy Manager Settings")]
    [SerializeField] private ChanceList<Enemy> basicEnemies = new ChanceList<Enemy>();
    
    [Header("Registered")]
    [SerializeField] private ChanceList<EnemySpawnPoint> enemySpawnPoints = new ChanceList<EnemySpawnPoint>();
    [SerializeField] private List<Enemy> activeEnemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void SpawnEnemyWave(int enemiesToSpawn)
    {
        var spawnPoint = enemySpawnPoints.GetRandomItem();
        
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            var enemy = basicEnemies.GetRandomItem();
            Vector3 spawnOffset = Random.insideUnitSphere * spawnPoint.SpawnPointRange;
            spawnOffset.y = 0;
            Vector3 spawnPosition = spawnPoint.transform.position + spawnOffset;
            
            Enemy spawnedEnemy = Instantiate(enemy, spawnPosition, Quaternion.identity);
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy)) return;
        
        activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy)) return;
        
        activeEnemies.Remove(enemy);
    }
    
    public void RegisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (enemySpawnPoints.Contains(spawnPoint)) return;
        
        enemySpawnPoints.AddItem(spawnPoint);
        enemySpawnPoints.NormalizeChances();
    }
    
    public void UnregisterSpawnPoint(EnemySpawnPoint spawnPoint)
    {
        if (!enemySpawnPoints.Contains(spawnPoint)) return;
        
        var index = enemySpawnPoints.IndexOf(spawnPoint);
        enemySpawnPoints.RemoveAt(index);
        enemySpawnPoints.NormalizeChances();
    }
    
    public List<Enemy> GetAllEnemies() => activeEnemies;
    
    public Enemy GetNearestEnemy(Vector3 position)
    {
        Enemy nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var enemy in activeEnemies)
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