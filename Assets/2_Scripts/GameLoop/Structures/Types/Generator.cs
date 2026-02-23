using System;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using UnityEngine;


[Serializable]
public class GeneratorLevelData : StructureLevelData
{
    public float generationInterval = 10;
    public int resourcesPerInterval = 5;
}


[RequireComponent(typeof(ResourceGenerator))]
public class Generator : Structure
{

    [Header("Generator")]
    [SerializeReference, DrawSerializeReference] private GeneratorLevelData[] levels = Array.Empty<GeneratorLevelData>();
    [SerializeField] private ShakeSettings pumpAnimationSettings;
    [SerializeField] private Transform[] pumpArray = Array.Empty<Transform>();
    [SerializeField, AutoGetSelf, HideInInspector]  private ResourceGenerator resourceGenerator;



    private Sequence _pumpAnimation;
    private GeneratorLevelData CurrentGeneratorLevelData => (GeneratorLevelData)Levels[CurrentUpgradeLevel - 1];
    
    protected override StructureLevelData[] Levels => levels;


    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth}/{MaxHealth}";
    }
    
    protected override void OnBuild()
    {
        StartPumpingAnimation();
        resourceGenerator.generationInterval = CurrentGeneratorLevelData.generationInterval;
        resourceGenerator.resourcesPerInterval = CurrentGeneratorLevelData.resourcesPerInterval;
        resourceGenerator.StartGenerating();
    }

    protected override void OnFix()
    {
        
    }

    protected override void OnUpgrade()
    {
        resourceGenerator.generationInterval = CurrentGeneratorLevelData.generationInterval;
        resourceGenerator.resourcesPerInterval = CurrentGeneratorLevelData.resourcesPerInterval;
    }

    protected override void OnBreak()
    {
        _pumpAnimation.Stop();
        resourceGenerator.StopGenerating();
        Destroy(gameObject);
    }
    
    private void StartPumpingAnimation()
    {
        if (_pumpAnimation.isAlive)
        {
            _pumpAnimation.Stop();
        }
        
        _pumpAnimation = Sequence.Create(cycles: -1);

        foreach (var pump in pumpArray)
        {
            _pumpAnimation.Chain(Tween.PunchScale(pump, pumpAnimationSettings));
        }
    }
}
