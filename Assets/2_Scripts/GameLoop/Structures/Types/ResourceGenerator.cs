using System;
using System.Collections;
using UnityEngine;


[Serializable]
public class ResourceGeneratorLevelData : StructureLevelData
{
    public float generationInterval = 10;
    public int resourcesPerInterval = 5;
}


public abstract class ResourceGenerator : Structure
{
    [Header("Resource Generator")]
    [SerializeReference, DrawSerializeReference] private ResourceGeneratorLevelData[] levels = Array.Empty<ResourceGeneratorLevelData>();
    
    private bool _generating;
    private Coroutine _generationCoroutine;
    private ResourceGeneratorLevelData CurrentBaseLevelData => (ResourceGeneratorLevelData)Levels[CurrentUpgradeLevel - 1];
    protected override StructureLevelData[] Levels => levels;

    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}";
    }
    
    protected void StartGenerating()
    {
        if (_generating) return;
        
        _generating = true;
        
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
        _generationCoroutine = StartCoroutine(GenerateResources());
    }


    protected void StopGenerating()
    {
        _generating = false;
        
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
        }
    }

    private IEnumerator GenerateResources()
    {
        while (_generating)
        {
            yield return new WaitForSeconds(CurrentBaseLevelData.generationInterval);
            ResourceManager.Instance?.AddResources(CurrentBaseLevelData.resourcesPerInterval);
        }
    }
}