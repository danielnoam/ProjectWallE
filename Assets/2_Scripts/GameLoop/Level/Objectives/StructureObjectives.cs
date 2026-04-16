using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    [Serializable]
    [SerializableSelectorName("Build", "Structure")]
    public class BuildStructureObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Leave the structure field empty to allow any structure to count towards the objective. (Works by type of class not prefab)")]
        [PrefabSelector("Assets/5_Prefabs/Structures")] public Structure structurePrefab;
        
        public override string Description => structurePrefab ? $"Build {structurePrefab.StructureUIData.Label}" : "Build a structure";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            Structure.OnStructureBuilt += OnStructureBuilt;
        }

        protected override void OnDispose()
        {
            Structure.OnStructureBuilt -= OnStructureBuilt;
        }

        private void OnStructureBuilt(Structure structure)
        {
            if (structurePrefab && structure.GetType() != structurePrefab.GetType()) return;
            Complete();
        }
    }

    [Serializable]
    [SerializableSelectorName("Upgrade", "Structure")]
    public class UpgradeStructureObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Leave the structure field empty to allow any structure to count towards the objective. (Works by type of class not prefab)")]
        [PrefabSelector("Assets/5_Prefabs/Structures")] public Structure structurePrefab;

        public override string Description => structurePrefab ? $"Upgrade {structurePrefab.StructureUIData.Label}" : "Upgrade a structure";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            Structure.OnStructureUpgraded += OnStructureUpgraded;
        }

        protected override void OnDispose()
        {
            Structure.OnStructureUpgraded -= OnStructureUpgraded;
        }

        private void OnStructureUpgraded(Structure structure)
        {
            if (structurePrefab && structure.GetType() != structurePrefab.GetType()) return;
            Complete();
        }
    }
    
    [Serializable]
    [SerializableSelectorName("Demolish", "Structure")]
    public class DemolishStructureObjective : BaseLevelObjective
    {
        [Header("Settings")]
        [Tooltip("Leave the structure field empty to allow any structure to count towards the objective. (Works by type of class not prefab)")]
        [PrefabSelector("Assets/5_Prefabs/Structures")] public Structure structurePrefab;

        public override string Description => structurePrefab ? $"Demolish {structurePrefab.StructureUIData.Label}" : "Demolish a structure";

        protected override void OnInitialize(IExposedPropertyTable resolver = null)
        {
            Structure.OnStructureDemolished += OnStructureDemolished;
        }

        protected override void OnDispose()
        {
            Structure.OnStructureDemolished -= OnStructureDemolished;
        }

        private void OnStructureDemolished(Structure structure)
        {
            if (structurePrefab && structure.GetType() != structurePrefab.GetType()) return;
            Complete();
        }
    }
}