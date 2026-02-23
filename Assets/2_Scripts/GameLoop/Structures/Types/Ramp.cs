using System;
using UnityEngine;

public class Ramp : Structure
{
    [SerializeReference, DrawSerializeReference] private StructureLevelData[] levels = Array.Empty<StructureLevelData>();
    protected override StructureLevelData[] Levels => levels;

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

    }
}
