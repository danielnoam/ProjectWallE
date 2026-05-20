using System;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    public class EnemySpawnerConfig
    {
        [Header("Spawn")]
        public float spawnInterval = 3f;
        [SerializeField] private EnemySpawnParams spawnParams;

        private float _timer;

        public void Initialize(IExposedPropertyTable resolver = null)
        {
            _timer = 0f;
            spawnParams.Initialize(resolver);
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < spawnInterval) return;
            _timer -= spawnInterval;
            spawnParams.Spawn();
        }
    }
}