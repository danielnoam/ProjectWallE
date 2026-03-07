using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public static event Action<int> OnResourcesChanged;
    
    [SerializeField] private int startingResources = 1000;
    private int _currentResources;
    
    public int CurrentResources => _currentResources;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        LevelManager.OnLevelInitializing += SetStartingResources;
    }

    private void OnDisable()
    {
        LevelManager.OnLevelInitializing -= SetStartingResources;
    }

    private void SetStartingResources()
    {
        AddResources(startingResources);
    }
    
    public void AddResources(int amount)
    {
        _currentResources += amount;
        OnResourcesChanged?.Invoke(_currentResources);
    }


    public bool TrySpendResources(int cost)
    {
        if (!CanAfford(cost)) return false;
        
        _currentResources -= cost;
        OnResourcesChanged?.Invoke(_currentResources);
        return true;
    }
    
    public bool CanAfford(int cost) => _currentResources >= cost;
}