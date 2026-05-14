using UnityEngine;

namespace GruelTerraSplines
{
    /// <summary>
    /// MonoBehaviour component that stores per-spline terrain settings.
    /// Automatically added to SplineContainer GameObjects to persist settings
    /// when splines are toggled on/off or reordered in the hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainSplineSettings : MonoBehaviour
    {
        [SerializeField]
        public SplineStrokeSettings settings = new SplineStrokeSettings();

        /// <summary>
        /// Resets settings to default values. Called by Unity's Reset context menu.
        /// </summary>
        void Reset()
        {
            settings = new SplineStrokeSettings();
        }

        /// <summary>
        /// Ensures the component has valid default settings.
        /// Called when the component is first created or when settings are null.
        /// </summary>
        void Awake()
        {
            if (settings == null)
            {
                settings = new SplineStrokeSettings();
            }

            EnsureDefaultCurves();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (settings == null)
            {
                settings = new SplineStrokeSettings();
            }

            EnsureDefaultCurves();
        }

        void EnsureDefaultCurves()
        {
            if (settings.sizeMultiplier == null || SplineStrokeSettings.IsBuiltInBrushSizeMultiplierCurve(settings.sizeMultiplier))
            {
                settings.sizeMultiplier = SplineStrokeSettings.CreateDefaultBrushSizeMultiplierCurve();
            }

            if (settings.treeHeightMultiplier == null || SplineStrokeSettings.IsBuiltInTreeHeightMultiplierCurve(settings.treeHeightMultiplier))
            {
                settings.treeHeightMultiplier = SplineStrokeSettings.CreateDefaultTreeHeightMultiplierCurve();
            }

            if (settings.brushNoise == null)
            {
                settings.brushNoise = new BrushNoiseSettings();
            }

            if (settings.brushNoise.noiseResponse == null || SplineStrokeSettings.IsBuiltInBrushNoiseResponseCurve(settings.brushNoise.noiseResponse))
            {
                settings.brushNoise.noiseResponse = SplineStrokeSettings.CreateDefaultBrushNoiseResponseCurve();
            }

            if (settings.paintNoiseLayers == null)
            {
                return;
            }

            for (int i = 0; i < settings.paintNoiseLayers.Count; i++)
            {
                var entry = settings.paintNoiseLayers[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.noiseResponse == null || SplineStrokeSettings.IsBuiltInPaintNoiseResponseCurve(entry.noiseResponse))
                {
                    entry.noiseResponse = SplineStrokeSettings.CreateDefaultPaintNoiseResponseCurve();
                }
            }
        }
#endif
    }
}