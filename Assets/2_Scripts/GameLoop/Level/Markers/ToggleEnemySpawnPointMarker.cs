using System;
using System.Collections.Generic;
using ProjectWallE.GameLoop;
using UnityEngine;

[Serializable]
public struct EnemySpawnPointToggle
{
    [ScenePicker] public ExposedReference<EnemySpawnPoint> spawnPoint;
    public bool spawnPointState;
}

[Serializable]
public class ToggleEnemySpawnPointMarker : BaseLevelEventMarker
{
    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPointToggle> spawnPoints = new();

    public List<EnemySpawnPointToggle> SpawnPoints => spawnPoints;

#if UNITY_EDITOR
    private void OnValidate()
    {
        var seenNames = new HashSet<string>();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var entry = spawnPoints[i];
            string name = entry.spawnPoint.exposedName.ToString();
            if (string.IsNullOrEmpty(name)) continue;

            if (!seenNames.Add(name))
            {
                entry.spawnPoint = new ExposedReference<EnemySpawnPoint>();
                spawnPoints[i] = entry;
            }
        }
    }
#endif

    protected override void OnExecute(IExposedPropertyTable resolver = null)
    {
        foreach (var entry in spawnPoints)
        {
            var point = entry.spawnPoint.Resolve(resolver);
            point?.SetActiveState(entry.spawnPointState);
        }
    }
}
