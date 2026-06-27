using System;
using System.Collections;
using DNExtensions.Systems.AudioLibrary;
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
    [SerializeField] private AudioPlayer generationAudio;
    
    
    private ResourceBoostZone _boostZone;
    private bool _generating;
    private Coroutine _generationCoroutine;
    
    private int ResourcePerInterval => _boostZone ? CurrentBaseLevelData.resourcesPerInterval * _boostZone.BoostMultiplier : CurrentBaseLevelData.resourcesPerInterval;
    private ResourceGeneratorLevelData CurrentBaseLevelData => (ResourceGeneratorLevelData)Levels[CurrentUpgradeLevel - 1];
    
    
    
    
    public override StructureLevelData[] Levels => levels;

    protected override void OnBuild()
    {
        CheckForBoostZone();
        StartGenerating();
    }
    
    protected override void Update()
    {
        base.Update();
        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}\nIn Boost Zone: {_boostZone != null}";
    }
    
    protected void StartGenerating()
    {
        if (_generating) return;
        
        generationAudio?.PlayAudio();
        _generating = true;
        
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
        _generationCoroutine = StartCoroutine(GenerateResources());
    }


    protected void StopGenerating()
    {
        _generating = false;
        generationAudio?.StopAudio();
        
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
        }
    }
    
    private void CheckForBoostZone()
    {
        var colliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (var col in colliders)
        {
            if (!col.TryGetComponent(out ResourceBoostZone boostZone)) continue;
            _boostZone = boostZone;
            return;
        }
    }


    private IEnumerator GenerateResources()
    {
        while (_generating)
        {
            yield return new WaitForSeconds(CurrentBaseLevelData.generationInterval);
            ResourceManager.Instance?.AddResources(ResourcePerInterval);
        }
    }
}