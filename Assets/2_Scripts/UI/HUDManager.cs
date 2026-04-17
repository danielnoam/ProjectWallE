using System;
using System.Collections.Generic;
using System.Text;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.GameLoop;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class HUDManager : MonoBehaviour
    { 
        [Header("Status")]
        [SerializeField] private TextMeshProUGUI currentResourcesText;
        [SerializeField] private TextMeshProUGUI structuresText;
        [SerializeField] private GameObject objectivesHolder;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] protected SOFontStyle objectiveStatusFontStyle;
        [SerializeField] protected SOFontStyle objectiveCompletedFontStyle;
        
        [Header("Bars")]
        [SerializeField] private FillBar fuelBar;
        [SerializeField] private FillBar healthBar;
        
        [Header("Actions")]
        [SerializeField] private CooldownIcon switchIcon;
        [SerializeField] private CooldownIcon buildIcon;
        [SerializeField] private CooldownIcon basicAttackIcon;
        [SerializeField] private CooldownIcon specialAttackIcon;
        [SerializeField] private CooldownIcon jumpIcon;
        [SerializeField] private CooldownIcon boostIcon;
        [SerializeField] private CooldownIcon brakeIcon;
        
        [Header("References")] 
        [SerializeField, AutoGetChildren] private RadarSystem radarSystem;
        [SerializeField, AutoGetScene] private PlayerManager player;

        private IReadOnlyList<BaseLevelObjective> _activeObjectives;
        private readonly StringBuilder _objectiveBuilder = new();

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
                player.OnHealthChanged += UpdateHealthBar;
                player.OnControllerChanged += OnControllerChanged;
                player.StructureBuilder.ActionsMenuRequested += OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested += OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested += OnMenuCloseRequested;
                player.CarController.CarBoost.OnFuelChange += UpdateFuelBar;
                player.CarController.CarBoost.OnBoostStart += OnBoostStart;
                player.Shooter.OnBasicCooldownUpdated += OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated += OnSpecialCooldownUpdated;
                
                radarSystem.worldCenter.SetTransform(player.transform);
                if (Camera.main) radarSystem.rotationTarget.Value = Camera.main.transform;
                fuelBar.SetImmediate(100, 100);
                healthBar.SetImmediate(100, 100);
                OnControllerChanged(player.PlayerControllerType);
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
                player.OnHealthChanged -= UpdateHealthBar;
                player.OnControllerChanged -= OnControllerChanged;
                player.StructureBuilder.ActionsMenuRequested -= OnActionsMenuRequested;
                player.StructureBuilder.BuildMenuRequested -= OnBuildMenuRequested;
                player.StructureBuilder.MenuCloseRequested -= OnMenuCloseRequested;
                player.CarController.CarBoost.OnFuelChange -= UpdateFuelBar;
                player.CarController.CarBoost.OnBoostStart -= OnBoostStart;
                player.Shooter.OnBasicCooldownUpdated -= OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated -= OnSpecialCooldownUpdated;
            }
        }

        private void OnBoostStart()
        {
            if (boostIcon && boostIcon.isActiveAndEnabled) boostIcon.PunchIcon();
        }

        private void OnControllerChanged(PlayerControllerType type)
        {
            switch (type)
            {
                case PlayerControllerType.Robot:
                    
                    jumpIcon.gameObject.SetActive(true);
                    basicAttackIcon.gameObject.SetActive(true);
                    specialAttackIcon.gameObject.SetActive(true);
                    
                    boostIcon.gameObject.SetActive(false);
                    brakeIcon.gameObject.SetActive(false);
                    break;
                case PlayerControllerType.Car:
                    jumpIcon.gameObject.SetActive(false);
                    basicAttackIcon.gameObject.SetActive(false);
                    specialAttackIcon.gameObject.SetActive(false);
                    
                    boostIcon.gameObject.SetActive(true);
                    brakeIcon.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            switchIcon.PunchIcon();
        }

        private void OnActionsMenuRequested(Structure structure)
        {
            structuresText.gameObject.SetActive(true);
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon();
        }
        
        private void OnBuildMenuRequested(Structure[] structures)
        {
            structuresText.gameObject.SetActive(true);
            if (buildIcon && buildIcon.isActiveAndEnabled) buildIcon.PunchIcon();
        }
        
        private void OnMenuCloseRequested()
        {
          structuresText.gameObject.SetActive(false);
        }

        private void UpdateStructuresText(StructuresData data)
        {
            structuresText.text = $"Bases: {data.BasesCount}" +
                                  $"\nTurrets: {data.TurretsCount}" +
                                  $"\nGenerators: {data.GeneratorsCount}" +
                                  $"\nRamps: {data.RampsCount}";
        }
        
        private void ResetTexts()
        {
            currentResourcesText.text = "Resources: 0"; 
            structuresText.text = $"Bases: 0" +
                                  $"\nTurrets: 0" +
                                  $"\nGenerators: 0" +
                                  $"\nRamps: 0";
            structuresText.gameObject.SetActive(false);
            objectivesHolder?.SetActive(false);
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
            if (specialAttackIcon&& basicAttackIcon.isActiveAndEnabled) specialAttackIcon.UpdateCooldown(currentCooldown, maxCooldown);
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
                    ? $"{objectiveCompletedFontStyle.ApplyStyle($"√ {objective.Description}")}"
                    : $"○ {objective.Description} - {objectiveStatusFontStyle.ApplyStyle(objective.ProgressText)}");
            }

            if (objectivesText) objectivesText.text = _objectiveBuilder.ToString();
        }

        private void UpdateResourcesDisplay(int currentResources)
        {
            if (!currentResourcesText) return;
            currentResourcesText.text = $"Resources: {currentResources}";
        }
        

        private void UpdateFuelBar(float currentFuel, float maxFuel)
        {
            if (fuelBar) fuelBar.SetValue(currentFuel, maxFuel);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar) healthBar.SetValue(currentHealth, maxHealth);
        }
    }
}