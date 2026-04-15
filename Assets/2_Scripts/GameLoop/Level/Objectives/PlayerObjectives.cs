using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using ProjectWallE.GameLoop.Player;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public enum AttackRequirement
    {
        Any,
        Basic,
        Special
    }

    [Serializable]
    [SerializableSelectorName("Fire", "Player")]
    public class FireObjective : BaseLevelObjective
    {
        [SerializeField] private AttackRequirement attackType = AttackRequirement.Any;
        [SerializeField, Min(1)] private int attackCount = 1;

        private int _currentAttacks;
        private PlayerShooter _shooter;

        public override string Description => attackType switch
        {
            AttackRequirement.Basic => $"Use basic attack {attackCount} times",
            AttackRequirement.Special => $"Use special attack {attackCount} times",
            _ => $"Attack {attackCount} times"
        };

        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentAttacks}/{attackCount}";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentAttacks = 0;
            _shooter = LevelManager.Instance.Player.Shooter;

            if (attackType is AttackRequirement.Any or AttackRequirement.Basic) _shooter.OnAttack1 += OnAttack;
            if (attackType is AttackRequirement.Any or AttackRequirement.Special) _shooter.OnAttack2 += OnAttack;
        }

        public override void Dispose()
        {
            if (!_shooter) return;
            _shooter.OnAttack1 -= OnAttack;
            _shooter.OnAttack2 -= OnAttack;
        }

        private void OnAttack()
        {
            _currentAttacks++;
            if (_currentAttacks >= attackCount)
            {
                Complete();
            }
        }
    }

    [Serializable]
    [SerializableSelectorName("Switch Controller", "Player")]
    public class SwitchControllerObjective : BaseLevelObjective
    {
        [SerializeField] private SwitchRequirement requirement = SwitchRequirement.Any;

        public enum SwitchRequirement { Any, SwitchToCar, SwitchToRobot }

        public override string Description => requirement switch
        {
            SwitchRequirement.SwitchToCar => "Switch to car",
            SwitchRequirement.SwitchToRobot => "Switch to robot",
            _ => "Switch controller"
        };

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            var player = LevelManager.Instance.Player;
            LevelManager.Instance.Player.OnControllerChanged += OnControllerChanged;
            
            switch (requirement)
            {
                case SwitchRequirement.SwitchToCar:
                {
                    if (player.PlayerControllerType == PlayerControllerType.Car) Complete();
                    break;
                }
                case SwitchRequirement.SwitchToRobot:
                {
                    if (player.PlayerControllerType == PlayerControllerType.Robot) Complete();
                    break;
                }
            }
        }

        public override void Dispose()
        {
            if (LevelManager.Instance && LevelManager.Instance.Player)
            {
                LevelManager.Instance.Player.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            bool matches = requirement switch
            {
                SwitchRequirement.SwitchToCar => type == PlayerControllerType.Car,
                SwitchRequirement.SwitchToRobot => type == PlayerControllerType.Robot,
                _ => true
            };

            if (matches) Complete();
        }
    }

    [Serializable]
    [SerializableSelectorName("Boost", "Player")]
    public class BoostObjective : BaseLevelObjective
    {
        [SerializeField] private float requiredDuration = 2f;

        private CarBoost _carBoost;
        private float _boostDuration;
        private bool _isBoosting;

        public override string Description => "Use car boost";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_boostDuration:F1}s / {requiredDuration:F1}s";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _boostDuration = 0f;
            _isBoosting = false;
            _carBoost = LevelManager.Instance.Player.CarController.CarBoost;
            _carBoost.OnBoostStart += OnBoostStart;
            _carBoost.OnBoostEnd += OnBoostEnd;
        }

        public override void Dispose()
        {
            if (_carBoost)
            {
                _carBoost.OnBoostStart -= OnBoostStart;
                _carBoost.OnBoostEnd -= OnBoostEnd;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (!_carBoost || !_isBoosting) return;

            _boostDuration += deltaTime;
            if (_boostDuration >= requiredDuration)
            {
                Complete();
            }
        }

        private void OnBoostStart()
        {
            _boostDuration = 0f;
            _isBoosting = true;
        }

        private void OnBoostEnd()
        {
            _boostDuration = 0f;
            _isBoosting = false;
        }
    }

    [Serializable]
    [SerializableSelectorName("Survive", "Player")]
    public class SurviveObjective : BaseLevelObjective
    {
        [SerializeField, Min(1f)] private float duration = 30f;
        [SerializeField] private bool spawnEnemies;
        [SerializeField, ShowIf("spawnEnemies")] private EnemySpawnerConfig spawner;

        private float _elapsed;

        public override string Description => $"Survive for {duration} seconds";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_elapsed:F1}s / {duration:F1}s";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _elapsed = 0f;
            if (spawnEnemies) spawner.Initialize(resolver);
        }

        public override void Dispose() { }

        public override void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= duration)
            {
                Complete();
                return;
            }
            if (spawnEnemies) spawner.Tick(deltaTime);
        }
    }

    [Serializable]
    [SerializableSelectorName("Go To Position", "Player")]
    public class GoToPositionObjective : BaseLevelObjective
    {
        [Header("Target")]
        public ExposedReference<ObjectiveGameMarker> targetMarker;
        [SerializeField, Min(1f)] private float radius = 5f;

        private ObjectiveGameMarker _marker;
        private Transform _player;
        private float _currentDistance;
        
        public override string Description => "Go to target";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_currentDistance:F1}m";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentDistance = float.MaxValue;
            _marker = targetMarker.Resolve(resolver);
            _player = LevelManager.Instance.Player.transform;
            _marker?.OnObjectiveStarted();
        }

        public override void Dispose()
        {
            _marker?.OnObjectiveCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (!_marker || !_player) return;

            _currentDistance = Vector3.Distance(_player.position, _marker.transform.position);
            if (_currentDistance <= radius)
            {
                _marker?.OnObjectiveCompleted();
                Complete();
            }
        }
    }
}