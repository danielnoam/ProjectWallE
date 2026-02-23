using System;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;


[Serializable]
public class BaseLevelData : StructureLevelData
{
    public float generationInterval = 10;
    public int resourcesPerInterval = 5;
}


[RequireComponent(typeof(ResourceGenerator))]
public class Base : Structure
{
    
    [Header("Base")]
    [SerializeReference, DrawSerializeReference] private BaseLevelData[] levels = Array.Empty<BaseLevelData>();
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSpeed = 0.3f;
    [SerializeField] private Transform obelisk;
    [SerializeField, AutoGetSelf, HideInInspector] private ResourceGenerator resourceGenerator;

    private BaseLevelData CurrentBaseLevelData => (BaseLevelData)Levels[CurrentUpgradeLevel - 1];
    protected override StructureLevelData[] Levels => levels;
    
    private void Update()
    {
        obelisk.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float sine = Mathf.Sin(Time.time * 2f) * moveSpeed;
        obelisk.localPosition = new Vector3(0f, sine, 0f);
        
        StateInfo = $"Health: {CurrentHealth}/{MaxHealth}";
    }
    

    protected override void OnBuild()
    {
        resourceGenerator.generationInterval = CurrentBaseLevelData.generationInterval;
        resourceGenerator.resourcesPerInterval = CurrentBaseLevelData.resourcesPerInterval;
        resourceGenerator.StartGenerating();
    }

    protected override void OnFix()
    {
        
    }

    protected override void OnUpgrade()
    {
        resourceGenerator.generationInterval = CurrentBaseLevelData.generationInterval;
        resourceGenerator.resourcesPerInterval = CurrentBaseLevelData.resourcesPerInterval;
    }

    protected override void OnBreak()
    {
        resourceGenerator.StopGenerating();
        LevelManager.Instance?.FailLevel();
    }
}
