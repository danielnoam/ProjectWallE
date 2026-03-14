using System;
using DNExtensions.Systems.Shapes;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE;
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

        [SerializeField, AutoGetScene, HideInInspector]
        private PlayerManager player;


        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void OnEnable()
        {
            ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
            LevelManager.OnTimeUpdated += OnTimeUpdated;

            if (player)
            {
                player.OnControllerChanged += OnControllerChanged;
                var car = player.GetComponentInChildren<CarBoost>();
                if (car) car.OnFuelChange += UpdateFuelBar;
            }
        }


        private void OnDisable()
        {
            ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
            LevelManager.OnTimeUpdated -= OnTimeUpdated;

            if (player)
            {
                player.OnControllerChanged -= OnControllerChanged;
                var car = player.GetComponentInChildren<CarBoost>();
                if (car) car.OnFuelChange -= UpdateFuelBar;
            }
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
            UpdateObjectivesDisplay(objectives);
        }

        private void UpdateResourcesDisplay(int currentResources)
        {
            if (!currentResourcesText) return;
            currentResourcesText.text = $"Resources: {currentResources}";
        }

        private void UpdateObjectivesDisplay(string objectives)
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