using System.Collections.Generic;
using DNExtensions;
using UnityEngine;




public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    
    [Header("Enemy Manager Settings")]
    [SerializeField] private ChanceList<Enemy> basicEnemies = new ChanceList<Enemy>();
    
    [Header("Readonly Fields")]
    [SerializeField] private ChanceList<EnemySpawnPoint> enemySpawnPoints = new ChanceList<EnemySpawnPoint>();
    [SerializeField] private List<Enemy> activeEnemies = new List<Enemy>();
    [SerializeField] private List<Base> bases = new List<Base>();

    
    private void Awake()
    {
        if (Instance) Destroy(gameObject);
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
            
            Instantiate(enemy, spawnPosition, Quaternion.identity);
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy)) return;
        
        activeEnemies.Add(enemy);
        enemy.SetMainTarget(bases[0]);
        
        if (bases.Count > 0)
        {
            enemy.SetMainTarget(bases[0]);
        }
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
    
    public void RegisterBase(Base baseStructure)
    {
        if (bases.Contains(baseStructure)) return;
        bases.Add(baseStructure);
    }

    public void UnregisterBase(Base baseStructure)
    {
        if (!bases.Contains(baseStructure)) return;
        bases.Remove(baseStructure);
    }
}