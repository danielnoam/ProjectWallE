using System;
using DNExtensions.Systems.Shapes;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TextMeshProUGUI currentResourcesText;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private SDFRectangle fuelBar;
        [SerializeField] private TextMeshProUGUI fuelText;
        [SerializeField] private SDFRectangle healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField, AutoGetChildren] private RadarSystem radarSystem;
        [SerializeField, AutoGetScene, HideInInspector] private PlayerManager player;


        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
            LevelManager.OnLevelInitializing += OnLevelInitializing;
            LevelManager.OnLevelStarted += OnLevelStarted;
            LevelManager.OnTimeUpdated += OnTimeUpdated;
            LevelManager.OnLevelCompleted += OnLevelCompleted;
            LevelManager.OnLevelFailed += OnLevelFailed;

            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                player.OnHealthChanged += UpdateHealthBar;
                player.CarController.CarBoost.OnFuelChange += UpdateFuelBar;
                radarSystem.worldCenter.SetTransform(player.transform);
                if (Camera.main) radarSystem.rotationTarget.Value = Camera.main.transform;
            }
        }
        


        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            LevelManager.OnLevelInitializing -= OnLevelInitializing;
            LevelManager.OnLevelStarted -= OnLevelStarted;
            LevelManager.OnTimeUpdated -= OnTimeUpdated;
            LevelManager.OnLevelCompleted -= OnLevelCompleted;
            LevelManager.OnLevelFailed -= OnLevelFailed;

            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                player.OnHealthChanged -= UpdateHealthBar;
                player.CarController.CarBoost.OnFuelChange -= UpdateFuelBar;
            }
        }
        
        private void OnLevelInitializing()
        {
            objectivesText.text = ""; 
            currentResourcesText.color = currentResourcesText.color.SetAlpha(0f);
        }
        
        private void OnLevelStarted()
        {
            currentResourcesText.color = currentResourcesText.color.SetAlpha(1f);
        }


        private void OnLevelFailed()
        {
            UpdateObjectiveDisplay("Fail");
            currentResourcesText.color = currentResourcesText.color.SetAlpha(0f);
        }

        private void OnLevelCompleted()
        {
            UpdateObjectiveDisplay("Success");  
            currentResourcesText.color = currentResourcesText.color.SetAlpha(0f);
        }


        private void OnControllerChanged(PlayerControllerType controllerType)
        {
            switch (controllerType)
            {
                case PlayerControllerType.Robot:
                    fuelBar.color = Color.black;
                    fuelText.color = fuelText.color.SetAlpha(0.2f);
                    break;
                case PlayerControllerType.Car:
                    fuelBar.color = Color.white;
                    fuelText.color = fuelText.color.SetAlpha(1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
            }
        }
        

        private void OnTimeUpdated(float timeRemaining)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeRemaining);
            string objectives = $"Survive:\n{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
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
            if (fuelText) fuelText.text = $"Fuel: {currentFuel:N0}/{maxFuel}";
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar) healthBar.fillAmount = currentHealth / maxHealth;
            if (healthText) healthText.text = $"Health: {currentHealth:N0}/{maxHealth}";
        }

    }
}