using System;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum EnemySourceType { Random, [InspectorName("Random By Type")] RandomByType, ByType }
    public enum SpawnPositionType { Random, Specific }

    [Serializable]
    public class EnemySpawnEntry
    {
        public Enemy enemy;
        [Min(1)] public int count = 1;
    }
}
