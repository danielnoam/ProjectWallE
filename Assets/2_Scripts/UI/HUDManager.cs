using System;
using System.Collections.Generic;
using System.Text;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using ProjectWallE.GameLoop;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class HUDManager : MonoBehaviour
    { 
        [Header("Visibility")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Resources")]
        [SerializeField] private CountIcon resourcesIcon;
        [SerializeField] private CountIcon basesIcon;
        [SerializeField] private CountIcon turretsIcon;
        [SerializeField] private CountIcon generatorsIcon;
        [SerializeField] private CountIcon rampsIcon;
        
        [Header("Objective")]
        [SerializeField] private GameObject objectivesHolder;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] protected SOFontStyle objectiveStatusFontStyle;
        [SerializeField] protected SOFontStyle objectiveCompletedFontStyle;
        
        [Header("Bars")]
        [SerializeField] private FillBar fuelBar;
        [SerializeField] private FillBar healthBar;
        
        [Header("Actions")]
        [SerializeField] private ActionIcon switchIcon;
        [SerializeField] private ActionIcon buildIcon;
        [SerializeField] private ActionIcon supportIcon;
        [SerializeField] private ActionIcon basicAttackIcon;
        [SerializeField] private ActionIcon specialAttackIcon;
        [SerializeField] private ActionIcon jumpIcon;
        [SerializeField] private ActionIcon boostIcon;
        [SerializeField] private ActionIcon brakeIcon;
        
        [Header("References")] 
        [SerializeField, AutoGetChildren] private RadarSystem radarSystem;
        [SerializeField, AutoGetScene] private PlayerManager player;

        private IReadOnlyList<BaseLevelObjective> _activeObjectives;
        private readonly StringBuilder _objectiveBuilder = new();
        private Tween _fadeTween;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            ResetTexts();
        }

        private void OnEnable()
        {
            ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged += UpdateStructuresText;
            LevelManager.OnLevelInitializing += ResetTexts;
            LevelManager.OnLeveTimeLineUpdated += OnLeveTimeLineUpdated;
            LevelManager.OnLevelCompleted += OnLevelCompleted;
            LevelManager.OnLevelFailed += OnLevelFailed;
            LevelManager.OnObjectivesAdded += OnObjectivesAdded;
            LevelManager.OnObjectivesCompleted += OnObjectivesCompleted;

            if (player)
            {
                player.OnDeath += OnPlayerDeath;
                player.OnSpawn += OnPlayerSpawn;
                player.OnHealthChanged += UpdateHealthBar;
                player.OnControllerChanged += OnControllerChanged;
                player.OnSwitchCooldownUpdated += OnSwitchCooldownUpdated;
                player.SupportCaller.SupportMenuRequested += OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested += OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
                player.CarController.OnBrakeStarted += OnBrakeStarted;
                player.CarController.CarBoost.OnFuelChange += UpdateFuelBar;
                player.CarController.CarBoost.OnBoostStart += OnBoostStart;
                player.RobotController.OnJumped += OnJumped;
                player.Shooter.OnBasicCooldownUpdated += OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated += OnSpecialCooldownUpdated;
                
                radarSystem.worldCenter.SetTransform(player.transform);
                if (Camera.main) radarSystem.rotationTarget.Value = Camera.main.transform;
                fuelBar.SetImmediate(100, 100);
                healthBar.SetImmediate(100, 100);
                OnControllerChanged(player.ControllerType);
            }
        }
        
        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            StructureManager.OnStructureCountChanged -= UpdateStructuresText;
            LevelManager.OnLevelInitializing -= ResetTexts;
            LevelManager.OnLeveTimeLineUpdated -= OnLeveTimeLineUpdated;
            LevelManager.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.OnLevelFailed -= OnLevelFailed;
            LevelManager.OnObjectivesAdded -= OnObjectivesAdded;
            LevelManager.OnObjectivesCompleted -= OnObjectivesCompleted;

            if (player)
            {
                player.OnDeath -= OnPlayerDeath;
                player.OnSpawn -= OnPlayerSpawn;
                player.OnHealthChanged -= UpdateHealthBar;
                player.OnControllerChanged -= OnControllerChanged;
                player.OnSwitchCooldownUpdated -= OnSwitchCooldownUpdated;
                player.SupportCaller.SupportMenuRequested -= OnSupportMenuRequested;
                player.SupportCaller.MenuCloseRequested -= OnSupportMenuCloseRequested;
                player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
                player.CarController.OnBrakeStarted -= OnBrakeStarted;
                player.CarController.CarBoost.OnFuelChange -= UpdateFuelBar;
                player.CarController.CarBoost.OnBoostStart -= OnBoostStart;
                player.RobotController.OnJumped -= OnJumped;
                player.Shooter.OnBasicCooldownUpdated -= OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated -= OnSpecialCooldownUpdated;
            }
        }

        private void OnPlayerSpawn()
        {
            if (!canvasGroup) return;
            if (Mathf.Approximately(canvasGroup.alpha, 1f)) return;
            
            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 1f, fadeDuration);
        }

        private void OnPlayerDeath(IDamageable attacker)
        {
            if (!canvasGroup) return;
            if (Mathf.Approximately(canvasGroup.alpha, 0f)) return;
            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(canvasGroup, 0f, fadeDuration);
        }

        private void OnBrakeStarted()
        {
            if (brakeIcon && brakeIcon.isActiveAndEnabled) brakeIcon.PunchIcon();
        }
        
        private void OnBoostStart()
        {
            if (boostIcon && boostIcon.isActiveAndEnabled) boostIcon.PunchIcon();
        }
        
        private void OnJumped()
        {
            if (jumpIcon && jumpIcon.isActiveAndEnabled) jumpIcon.PunchIcon();
        }

        private void OnControllerChanged(ControllerType type)
        {
            switch (type)
            {
                case ControllerType.Robot:
                    jumpIcon.gameObject.SetActive(true);
                    basicAttackIcon.gameObject.SetActive(true);
                    specialAttackIcon.gameObject.SetActive(true);
                    boostIcon.gameObject.SetActive(false);
                    brakeIcon.gameObject.SetActive(false);
                    break;
                case ControllerType.Car:
                    jumpIcon.gameObject.SetActive(false);
                    basicAttackIcon.gameObject.SetActive(false);
                    specialAttackIcon.gameObject.SetActive(false);
                    boostIcon.gameObject.SetActive(true);
                    brakeIcon.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        private void OnSwitchCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (switchIcon && switchIcon.isActiveAndEnabled)
            {
                switchIcon.UpdateCooldown(currentCooldown, maxCooldown);
            }
        }

        private void OnActionsMenuRequested(Structure structure)
        {
            basesIcon?.gameObject.SetActive(true);
            turretsIcon?.gameObject.SetActive(true);
            generatorsIcon?.gameObject.SetActive(true);
            rampsIcon?.gameObject.SetActive(true);
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon();
        }
        
        private void OnBuildMenuRequested(Structure[] structures, bool canBuild)
        {
            basesIcon?.gameObject.SetActive(true);
            turretsIcon?.gameObject.SetActive(true);
            generatorsIcon?.gameObject.SetActive(true);
            rampsIcon?.gameObject.SetActive(true);
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon();
        }
        

        private void OnSupportMenuRequested(SOSupportActionData[] obj)
        {
            basesIcon?.gameObject.SetActive(true);
            turretsIcon?.gameObject.SetActive(true);
            generatorsIcon?.gameObject.SetActive(true);
            rampsIcon?.gameObject.SetActive(true);
            if (supportIcon && supportIcon.isActiveAndEnabled) supportIcon.PunchIcon();
        }
        
        private void OnSupportMenuCloseRequested()
        {
            basesIcon?.gameObject.SetActive(false);
            turretsIcon?.gameObject.SetActive(false);
            generatorsIcon?.gameObject.SetActive(false);
            rampsIcon?.gameObject.SetActive(false);
        }
        
        private void OnMenuCloseRequested()
        {
            basesIcon?.gameObject.SetActive(false);
            turretsIcon?.gameObject.SetActive(false);
            generatorsIcon?.gameObject.SetActive(false);
            rampsIcon?.gameObject.SetActive(false);
        }

        private void UpdateStructuresText(StructuresData data)
        {
            basesIcon?.SetCount(data.BasesCount);
            turretsIcon?.SetCount(data.TurretsCount);
            generatorsIcon?.SetCount(data.GeneratorsCount);
            rampsIcon?.SetCount(data.RampsCount);
        }
        
        private void OnLevelFailed()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
        }

        private void OnLevelCompleted()
        {
            _activeObjectives = null;
            objectivesText?.gameObject.SetActive(false);
        }

        private void OnObjectivesAdded(List<BaseLevelObjective> objectives)
        {
            _activeObjectives = objectives;
            objectivesHolder?.SetActive(true);
        }

        private void OnObjectivesCompleted()
        {
            _activeObjectives = null;
            objectivesHolder?.SetActive(false);
        }
        
        private void OnBasicCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (basicAttackIcon && basicAttackIcon.isActiveAndEnabled) basicAttackIcon.UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void OnSpecialCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (specialAttackIcon && basicAttackIcon.isActiveAndEnabled) specialAttackIcon.UpdateCooldown(currentCooldown, maxCooldown);
        }

        private void OnLeveTimeLineUpdated(float timeRemaining)
        {
            if (_activeObjectives != null)
            {
                UpdateObjectivesDisplay();
            }
        }

        private void UpdateObjectivesDisplay()
        {
            _objectiveBuilder.Clear();

            foreach (var objective in _activeObjectives)
            {
                _objectiveBuilder.AppendLine(objective.IsCompleted
                    ? $"○ {objectiveCompletedFontStyle.ApplyStyle($"{objective.Description}")}"
                    : $"○ {objective.Description} - {objectiveStatusFontStyle.ApplyStyle(objective.ProgressText)}");
            }

            if (objectivesText) objectivesText.text = _objectiveBuilder.ToString();
        }

        private void UpdateResourcesDisplay(int currentResources)
        {
            resourcesIcon?.SetCount(currentResources);
        }
        
        private void UpdateFuelBar(float currentFuel, float maxFuel)
        {
            if (fuelBar) fuelBar.SetValue(currentFuel, maxFuel);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar) healthBar.SetValue(currentHealth, maxHealth);
        }
        
        private void ResetTexts()
        {
            resourcesIcon?.SetCountImmediate(0);
            basesIcon?.SetCountImmediate(0);
            turretsIcon?.SetCountImmediate(0);
            generatorsIcon?.SetCountImmediate(0);
            rampsIcon?.SetCountImmediate(0);
            objectivesHolder?.SetActive(false);
        }
    }
}