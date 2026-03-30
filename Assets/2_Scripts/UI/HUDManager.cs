using System;
using DNExtensions.Systems.Shapes;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.CustomFields;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Game Status")]
        [SerializeField] private TextMeshProUGUI currentResourcesText;
        [SerializeField] private TextMeshProUGUI structuresText;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private string objectivePrefix = "Protect Bases:";
        
        [Header("Player Status")]
        [SerializeField] private OptionalField<string> showFuelPrefix = new OptionalField<string>("Fuel: ", true);
        [SerializeField] private SDFRectangle fuelBar;
        [SerializeField] private TextMeshProUGUI fuelText;
        [SerializeField] private OptionalField<string> showHealthPrefix = new OptionalField<string>("Health: ", true);
        [SerializeField] private SDFRectangle healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image basicAttackIcon;
        [SerializeField] private Image specialAttackIcon;
        
        
        [Header("References")] 
        [SerializeField] private GameObject crosshair;
        [SerializeField, AutoGetChildren, HideInInspector] private RadarSystem radarSystem;
        [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;


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
            StructureManager.OnStructureCreated += UpdateStructuresText;
            StructureManager.OnStructureDestroyed += UpdateStructuresText;
            LevelManager.OnLevelInitializing += ResetTexts;
            LevelManager.OnLevelStarted += OnLevelStarted;
            LevelManager.OnTimeUpdated += OnTimeUpdated;
            LevelManager.OnLevelCompleted += OnLevelCompleted;
            LevelManager.OnLevelFailed += OnLevelFailed;

            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnHealthChanged += UpdateHealthBar;
                player.CarController.CarBoost.OnFuelChange += UpdateFuelBar;
                player.Shooter.OnBasicCooldownUpdated += OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated += OnSpecialCooldownUpdated;
                
                radarSystem.worldCenter.SetTransform(player.transform);
                if (Camera.main) radarSystem.rotationTarget.Value = Camera.main.transform;
                UpdateFuelBar(100,100);
                UpdateHealthBar(100, 100);
            }
        }

        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            StructureManager.OnStructureCreated -= UpdateStructuresText;
            StructureManager.OnStructureDestroyed -= UpdateStructuresText;
            LevelManager.OnLevelInitializing -= ResetTexts;
            LevelManager.OnLevelStarted -= OnLevelStarted;
            LevelManager.OnTimeUpdated -= OnTimeUpdated;
            LevelManager.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.OnLevelFailed -= OnLevelFailed;

            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.OnHealthChanged -= UpdateHealthBar;
                player.CarController.CarBoost.OnFuelChange -= UpdateFuelBar;
                player.Shooter.OnBasicCooldownUpdated -= OnBasicCooldownUpdated;
                player.Shooter.OnSpecialCooldownUpdated -= OnSpecialCooldownUpdated;
            }
        }
        
        private void UpdateStructuresText(StructuresData data)
        {
            structuresText.text = $"Bases - {data.BasesCount}" +
                                  $"\nTurrets - {data.TurretsCount}" +
                                  $"\nGenerators - {data.GeneratorsCount}";
        }
        
        
        private void ResetTexts()
        {
            objectivesText.text = ""; 
            currentResourcesText.text = ""; 
            structuresText.text = "";
        }
        
        private void OnLevelStarted()
        {
            currentResourcesText.color = currentResourcesText.color.SetAlpha(1f);
        }


        private void OnLevelFailed()
        {
            UpdateObjectiveDisplay("Fail");
        }

        private void OnLevelCompleted()
        {
            UpdateObjectiveDisplay("Success");  
        }


        private void OnControllerChanged(PlayerControllerType controllerType)
        {
            switch (controllerType)
            {
                case PlayerControllerType.Robot:
                    fuelBar.color = Color.black;
                    fuelText.color = fuelText.color.SetAlpha(0.2f);
                    crosshair.gameObject.SetActive(true);
                    break;
                case PlayerControllerType.Car:
                    fuelBar.color = Color.white;
                    fuelText.color = fuelText.color.SetAlpha(1f);
                    crosshair.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
            }
        }
        

        private void OnTimeUpdated(float timeRemaining)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeRemaining);
            string objectives = $"{objectivePrefix}\n{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            UpdateObjectiveDisplay(objectives);
        }

        private void UpdateResourcesDisplay(int currentResources)
        {
            if (!currentResourcesText) return;
            currentResourcesText.text = $"Resources: {currentResources}";
        }

        private void UpdateObjectiveDisplay(string objectives)
        {
            if (!objectivesText) return;
            objectivesText.text = objectives;
        }

        private void UpdateFuelBar(float currentFuel, float maxFuel)
        {
            if (fuelBar) fuelBar.fillAmount = currentFuel / maxFuel;
            if (fuelText)
            {
                fuelText.text = showFuelPrefix.isSet ? $"{showFuelPrefix.Value}{currentFuel:N0}/{maxFuel}" : $"{currentFuel:N0}/{maxFuel}";
            }
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar) healthBar.fillAmount = currentHealth / maxHealth;
            if (healthText)
            {
                healthText.text = showHealthPrefix.isSet ? $"{showHealthPrefix.Value}{currentHealth:N0}/{maxHealth}" : $"{currentHealth:N0}/{maxHealth}";
            }
        }
        
        private void OnSpecialCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (!specialAttackIcon) return;
            
            var normalizedCooldown = 1f - (currentCooldown / maxCooldown);
            specialAttackIcon.color = specialAttackIcon.color.SetAlpha(normalizedCooldown);
        }

        private void OnBasicCooldownUpdated(float currentCooldown, float maxCooldown)
        {
            if (!basicAttackIcon) return;
            
            var normalizedCooldown = 1f - (currentCooldown / maxCooldown);
            basicAttackIcon.color = basicAttackIcon.color.SetAlpha(normalizedCooldown);
        }

    }
}