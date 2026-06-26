using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using ProjectWallE.GameLoop;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SpawnEnemyEvent : BaseLevelEventAsset
{
    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 3f;

    [Header("Enemy")]
    [SerializeField] private EnemySourceType enemyType = EnemySourceType.Random;
    [SerializeField, Min(1)] private int count = 3;
    [SerializeField] private ChanceList<Enemy> enemies;
    [SerializeField] private List<EnemySpawnEntry> enemyEntries;

    [Header("Position")]
    [SerializeField] private SpawnPositionType spawnPosition = SpawnPositionType.Random;
    [SerializeField, ScenePicker] private ChanceList<ExposedReference<EnemySpawnPoint>> spawnPoints;

    private ChanceList<EnemySpawnPoint> _resolvedSpawnPoints;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ContinuousEventBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.EventAsset = this;
        behaviour.Resolver = graph.GetResolver();
        behaviour.Interval = spawnInterval;

        if (spawnPosition == SpawnPositionType.Specific)
        {
            _resolvedSpawnPoints = new ChanceList<EnemySpawnPoint>();
            var rawList = spawnPoints.ToList();
            for (int i = 0; i < rawList.Count; i++)
            {
                var resolved = rawList[i].Resolve(graph.GetResolver());
                if (resolved) _resolvedSpawnPoints.AddItem(resolved, spawnPoints.GetChance(i));
            }
        }

        return playable;
    }

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        EnemySpawnPoint point = spawnPosition switch
        {
            SpawnPositionType.Specific => _resolvedSpawnPoints?.GetRandomItem(),
            _ => null
        };

        if (enemyType == EnemySourceType.ByType)
        {
            if (enemyEntries == null) return;
            foreach (var entry in enemyEntries)
            {
                if (!entry.enemy) continue;
                var single = new ChanceList<Enemy>();
                single.AddItem(entry.enemy);
                EnemyManager.Instance?.SpawnEnemyWave(entry.count, single, point);
            }
            return;
        }

        var source = enemyType == EnemySourceType.RandomByType ? enemies : null;
        EnemyManager.Instance?.SpawnEnemyWave(count, source, point);
    }
}
