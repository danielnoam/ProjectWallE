using System;
using DNExtensions.Systems.Shapes;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI currentResourcesText;
    [SerializeField] private TextMeshProUGUI objectivesText;
    [SerializeField] private SDFRectangle fuelBar;
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private SDFRectangle healthBar;
    [SerializeField] private TextMeshProUGUI healthText;


    private void OnEnable()
    {
        ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
        LevelManager.OnTimeUpdated += OnTimeUpdated;
    }

    private void OnDisable()
    {
        ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
        LevelManager.OnTimeUpdated -= OnTimeUpdated;
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
    
    private void UpdateFuelBar(float fillAmount)
    {
        if (fuelBar) fuelBar.fillAmount = fillAmount;
        fuelText.text = $"Fuel: {fillAmount:P0}";
    }
    
    private void UpdateHealthBar(float fillAmount)
    {
        if (healthBar) healthBar.fillAmount = fillAmount;
        healthText.text = $"Health: {fillAmount:P0}";
    }
    
}