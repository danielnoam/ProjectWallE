using System;
using UnityEngine;

public class Ramp : Structure
{
    [Header("Ramp")]
    [SerializeReference, DrawSerializeReference] private StructureLevelData[] levels = Array.Empty<StructureLevelData>();
    public override StructureLevelData[] Levels => levels;

    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth:N0}/{MaxHealth}";
    }

    protected override void OnBreak()
    {
        Destroy(gameObject);
    }
}
