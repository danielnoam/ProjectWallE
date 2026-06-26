using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private struct EnemySpawn
    {
        public Enemy enemy;
        public EnemySpawnPoint spawnPoint;
    }

    [Header("Settings")]
    [SerializeField] private int maxEnemies = 100;
    [SerializeField] private LayerMask spawnRaycastMask;
    [SerializeField] private float spawnRaycastHeight = 200f;
    [Tooltip("Random delay between releasing each queued enemy from a base")]
    [SerializeField, MinMaxRange(0f, 5f)] private RangedFloat spawnInterval = new RangedFloat(0.2f, 0.6f);
    [SerializeField] private ChanceList<Enemy> enemyTypes = new ChanceList<Enemy>();

    private Transform _enemyHolder;
    private readonly ChanceList<EnemySpawnPoint> _activeSpawnPoints = new ChanceList<EnemySpawnPoint>();
    private readonly List<Enemy> _activeEnemies = new List<Enemy>();
    private readonly Dictionary<EnemyBase, Queue<EnemySpawn>> _baseQueues = new Dictionary<EnemyBase, Queue<EnemySpawn>>();
    private readonly Dictionary<EnemyBase, Coroutine> _baseRoutines = new Dictionary<EnemyBase, Coroutine>();

    private int _pendingEnemySpawns;

    public int ActiveEnemiesCount => _activeEnemies.Count;
    public int PendingEnemySpawns => _pendingEnemySpawns;
    public bool HasActiveOrPendingEnemies => ActiveEnemiesCount > 0 || PendingEnemySpawns > 0;

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
        if (!enemy || !spawnPoint || _activeEnemies.Count >= maxEnemies)
        {
            _pendingEnemySpawns--;
            return;
        }

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
        if (!spawnPoint)
        {
            if (_activeSpawnPoints.Count == 0) return;
            spawnPoint = _activeSpawnPoints.GetRandomItem();
        }

        var source = enemySource ?? enemyTypes;
        EnemyBase enemyBase = DeploymentManager.Instance?.GetNearestBase(spawnPoint.transform.position);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            var spawn = new EnemySpawn { enemy = source.GetRandomItem(), spawnPoint = spawnPoint };
            _pendingEnemySpawns++;

            if (!enemyBase) SpawnEnemy(spawn.enemy, spawn.spawnPoint);
            else EnqueueSpawn(enemyBase, spawn);
        }
    }

    private void EnqueueSpawn(EnemyBase enemyBase, EnemySpawn spawn)
    {
        if (!_baseQueues.TryGetValue(enemyBase, out var queue))
        {
            queue = new Queue<EnemySpawn>();
            _baseQueues[enemyBase] = queue;
        }

        queue.Enqueue(spawn);

        if (!_baseRoutines.ContainsKey(enemyBase))
            _baseRoutines[enemyBase] = StartCoroutine(DrainBaseQueue(enemyBase));
    }

    private IEnumerator DrainBaseQueue(EnemyBase enemyBase)
    {
        var queue = _baseQueues[enemyBase];

        while (enemyBase && queue.Count > 0)
        {
            var spawn = queue.Dequeue();
            SpawnEnemy(spawn.enemy, spawn.spawnPoint);
            yield return new WaitForSeconds(spawnInterval.RandomValue);
        }

        _pendingEnemySpawns -= queue.Count;
        _baseQueues.Remove(enemyBase);
        _baseRoutines.Remove(enemyBase);
    }


    #endregion
    
    #region Registration

    public void RegisterEnemy(Enemy enemy)
    {
        if (_activeEnemies.Contains(enemy)) return;

        _activeEnemies.Add(enemy);
        _pendingEnemySpawns = Mathf.Max(0, _pendingEnemySpawns - 1);
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