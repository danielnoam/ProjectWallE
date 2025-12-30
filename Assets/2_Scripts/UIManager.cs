using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI structureNameText;
    [SerializeField] private TextMeshProUGUI structureCostText;
    [SerializeField] private TextMeshProUGUI currentResourcesText;
    [SerializeField] private TextMeshProUGUI objectivesText;
    [SerializeField] private BeaconThrower beaconThrower;
    [SerializeField] private AudioSource audioSource;


    private void OnValidate()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (beaconThrower) beaconThrower.OnStructureChanged += UpdateStructureDisplay;
        ResourceManager.OnResourcesChanged += UpdateResourcesDisplay;
        LevelManager.OnTimeUpdated += OnTimeUpdated;
        
        UpdateResourcesDisplay(ResourceManager.Instance.CurrentResources);
        UpdateStructureDisplay(beaconThrower.CurrentStructure);
    }
    
    private void OnDisable()
    {
        if (beaconThrower) beaconThrower.OnStructureChanged -= UpdateStructureDisplay;
        ResourceManager.OnResourcesChanged -= UpdateResourcesDisplay;
        LevelManager.OnTimeUpdated -= OnTimeUpdated;
    }

    private void OnTimeUpdated(float timeRemaining)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(timeRemaining);
        string objectives = $"Survive:\n{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        UpdateObjectivesDisplay(objectives);
    }
    

    private void UpdateStructureDisplay(Structure structure)
    {
        structureNameText.text = structure.Label;
        structureCostText.text = $"Cost: {structure.BuildCost}";
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