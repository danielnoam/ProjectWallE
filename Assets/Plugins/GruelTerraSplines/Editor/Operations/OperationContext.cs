using UnityEngine;

namespace GruelTerraSplines
{
    public class OperationContext
    {
        public float strength;
        public BrushNoiseSettings brushNoise; // Optional brush noise settings
        public float[,] heights; // For height operations
        public bool[,] holes; // For hole/fill operations
        public float[,,] alphamaps; // For paint operations
        public int targetLayerIndex; // For paint operations
        public float paintStrength; // For paint operations
        public float paintBlend; // For paint operations
        public float paintThreshold; // For paint operations
        public PaintNoiseLayerSettings[] paintNoiseLayers; // Optional per-layer noise settings

        // Detail operation properties
        public int detailLayerIndex; // For detail operations
        public int[] detailLayerIndices; // For detail operations (multi)
        public float detailStrength; // For detail operations
        public int[,,] detailData; // For detail operations (detail layer data)
        public int[,] detailLayer; // For detail operations (selected detail layer buffer)
        public int[][,] detailLayers; // For detail operations (multi)
        public DetailOperationMode detailMode; // For detail operations
        public float detailSlopeLimitDegrees; // For detail operations
        public float detailFalloffPower; // For detail operations
        public int detailSpreadRadius; // For detail operations
        public float detailRemoveThreshold; // For detail operations
        public DetailNoiseLayerSettings[] detailNoiseLayers; // Optional per-layer noise settings

        // Tree operation properties
        public int treePrototypeIndex; // For tree operations
        public int[] treePrototypeIndices; // For tree operations (multi)
        public float treeStrength; // For tree operations
        public int treeTargetDensity; // For tree operations
        public float treeSlopeLimitDegrees; // For tree operations
        public float treeFalloffPower; // For tree operations
        public AnimationCurve treeHeightMultiplier; // For tree operations
        public float treeRemoveThreshold; // For tree operations
        public bool treeRemoveMode; // For tree operations
        public TreeNoisePrototypeSettings[] treeNoiseLayers; // Optional per-prototype noise settings
        public System.Collections.Generic.List<TreeInstance> treeInstances; // Mutable working tree buffer

        // Mask building for tracking spline operations
        public float[,] paintSplineAlphaMask; // Optional: accumulate paint operation masks
        public bool[,] holeSplineMask; // Optional: accumulate hole/fill operation masks

        // Batch normalization tracking
        public System.Collections.Generic.HashSet<(int x, int z)> pixelsNeedingNormalization; // Optional: track pixels that need alphamap normalization
    }
}
