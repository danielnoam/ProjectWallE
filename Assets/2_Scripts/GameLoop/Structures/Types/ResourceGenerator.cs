using System;
using System.Collections;
using DNExtensions.Utilities;
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
    [SerializeField, ReadOnly] private bool generating;
    
    
    private Coroutine _generationCoroutine;
    private ResourceGeneratorLevelData CurrentBaseLevelData => (ResourceGeneratorLevelData)Levels[currentUpgradeLevel - 1];
    protected override StructureLevelData[] Levels => levels;

    protected void StartGenerating()
    {
        if (_generationCoroutine != null) return;
        _generationCoroutine = StartCoroutine(GenerateResources());
    }


    protected void StopGenerating()
    {
        generating = false;
        
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
        }
    }

    private IEnumerator GenerateResources()
    {
        generating = true;
        while (generating)
        {
            yield return new WaitForSeconds(CurrentBaseLevelData.generationInterval);
            ResourceManager.Instance.AddResources(CurrentBaseLevelData.resourcesPerInterval);
        }
    }

    private void OnDestroy()
    {
        StopGenerating();
    }
}