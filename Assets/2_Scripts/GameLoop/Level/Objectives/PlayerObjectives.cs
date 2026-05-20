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
        [Header("Settings")]
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

        protected override string DefaultTutorialText => attackType switch
        {
            AttackRequirement.Basic => "Press *[LMB]* to shoot",
            AttackRequirement.Special => "Press *[RMB]* to use special attack",
            _ => "Press *[LMB]* or *[RMB]* to attack"
        };

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentAttacks = 0;
            _shooter = PlayerManager.Instance.Shooter;

            if (attackType is AttackRequirement.Any or AttackRequirement.Basic) _shooter.OnAttack1 += OnAttack;
            if (attackType is AttackRequirement.Any or AttackRequirement.Special) _shooter.OnAttack2 += OnAttack;
        }

        protected override void OnDispose()
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
        [Header("Settings")]
        [SerializeField] private SwitchRequirement requirement = SwitchRequirement.Any;

        public enum SwitchRequirement { Any, SwitchToCar, SwitchToRobot }

        public override string Description => requirement switch
        {
            SwitchRequirement.SwitchToCar => "Switch to car",
            SwitchRequirement.SwitchToRobot => "Switch to robot",
            _ => "Switch controller"
        };

        protected override string DefaultTutorialText => "Press *[Tab]* to switch";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            var player = PlayerManager.Instance;
            PlayerManager.Instance.OnControllerChanged += OnControllerChanged;

            switch (requirement)
            {
                case SwitchRequirement.SwitchToCar:
                {
                    if (player.ControllerType == ControllerType.Car) Complete();
                    break;
                }
                case SwitchRequirement.SwitchToRobot:
                {
                    if (player.ControllerType == ControllerType.Robot) Complete();
                    break;
                }
            }
        }

        protected override void OnDispose()
        {
            if (PlayerManager.Instance )
            {
                PlayerManager.Instance.OnControllerChanged -= OnControllerChanged;
            }
        }

        private void OnControllerChanged(ControllerType type)
        {
            bool matches = requirement switch
            {
                SwitchRequirement.SwitchToCar => type == ControllerType.Car,
                SwitchRequirement.SwitchToRobot => type == ControllerType.Robot,
                _ => true
            };

            if (matches) Complete();
        }
    }

    [Serializable]
    [SerializableSelectorName("Boost", "Player")]
    public class BoostObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [SerializeField] private float requiredDuration = 2f;

        private CarBoost _carBoost;
        private float _boostDuration;
        private bool _isBoosting;

        public override string Description => "Use car boost";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_boostDuration:F1}s / {requiredDuration:F1}s";

        protected override string DefaultTutorialText => "Hold *[Shift]* to boost";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _boostDuration = 0f;
            _isBoosting = false;
            _carBoost = PlayerManager.Instance.CarController.CarBoost;
            _carBoost.OnBoostStart += OnBoostStart;
            _carBoost.OnBoostEnd += OnBoostEnd;
        }

        protected override void OnDispose()
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
        [Header("Settings")]
        [SerializeField, Min(1f)] private float duration = 30f;
        [SerializeField] private bool spawnEnemies;
        [SerializeField, ShowIf("spawnEnemies")] private EnemySpawnerConfig spawner;

        private float _elapsed;

        public override string Description => $"Survive";
        public override string ProgressText => IsCompleted ? "Complete" : $"{_elapsed:F1}s / {duration:F1}s";
        

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _elapsed = 0f;
            if (spawnEnemies) spawner.Initialize(resolver);
        }

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
        [Header("Settings")]
        [SerializeField, Min(1f)] private float radius = 5f;

        private Transform _player;
        private float _currentDistance;

        public override string Description => "Go to target";
        public override string ProgressText => IsCompleted ? "Complete" : 
            _currentDistance >= 10f ? $"{_currentDistance:F0}m" :
            $"{_currentDistance:F1}m";

        protected override string DefaultTutorialText => "";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            _currentDistance = 0;
            _player = PlayerManager.Instance.transform;
        }

        public override void Tick(float deltaTime)
        {
            if (!ResolvedMarker || !_player) return;

            _currentDistance = Vector3.Distance(_player.position, ResolvedMarker.transform.position);
            if (_currentDistance <= radius)
            {
                Complete();
            }
        }
    }
}