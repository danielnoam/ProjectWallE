using System;
using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI currentResourcesText;
    [SerializeField] private TextMeshProUGUI objectivesText;

    
    private void Start()
    {
        ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
        LevelManager.OnTimeUpdated += OnTimeUpdated;
        
        UpdateResourcesDisplay(ResourceManager.Instance.CurrentResources);
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
        currentResourcesText.text = $"Resources: {currentResources}";
    }
    
    private void UpdateObjectivesDisplay(string objectives)
    {
        objectivesText.text = objectives;
    }
}