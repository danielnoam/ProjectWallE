using System;
using UnityEngine;

public class Ramp : Structure
{
    [SerializeReference, DrawSerializeReference] private StructureLevelData[] levels = Array.Empty<StructureLevelData>();
    protected override StructureLevelData[] Levels => levels;

    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}";
    }

    protected override void OnBuild()
    {

    }

    protected override void OnFix()
    {
        
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        Destroy(gameObject);
    }
}
