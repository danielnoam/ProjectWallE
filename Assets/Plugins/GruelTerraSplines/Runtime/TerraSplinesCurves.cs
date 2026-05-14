using System.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GruelTerraSplines
{
    [Serializable]
    public sealed class NamedCurvePreset
    {
        public string name = "Curve";
        public AnimationCurve curve;
    }

    public enum CurvePresetListType
    {
        BrushSizeMultiplier = 0,
        BrushNoiseContrast = 1,
        PaintNoiseContrast = 2,
        TreeHeightDistribution = 3,
    }

    [CreateAssetMenu(fileName = ResourceName, menuName = "Gruel Terra Splines/Curves Preset")]
    public sealed class TerraSplinesCurves : ScriptableObject
    {
        public const string ResourceName = "TerraSplinesCurves";
        public const string AssetPath = "Assets/Plugins/GruelTerraSplines/Resources/TerraSplinesCurves.asset";

        static TerraSplinesCurves cachedInstance;
        static bool isResolving;

        [Header("Curve Presets")]
        [SerializeField]
        List<NamedCurvePreset> brushSizeMultiplierPresets = new List<NamedCurvePreset>();

        [SerializeField]
        List<NamedCurvePreset> brushNoiseContrastPresets = new List<NamedCurvePreset>();

        [SerializeField]
        List<NamedCurvePreset> paintNoiseContrastPresets = new List<NamedCurvePreset>();

        [SerializeField]
        List<NamedCurvePreset> treeHeightDistributionPresets = new List<NamedCurvePreset>();

        [SerializeField, HideInInspector]
        AnimationCurve brushSizeMultiplier = CreateBuiltInBrushSizeMultiplierCurve();

        [SerializeField, HideInInspector]
        AnimationCurve brushNoiseContrast = CreateBuiltInNoiseContrastCurve();

        [SerializeField, HideInInspector]
        AnimationCurve paintNoiseContrast = CreateBuiltInNoiseContrastCurve();

        [SerializeField, HideInInspector]
        AnimationCurve treeHeightDistribution = CreateBuiltInTreeHeightDistributionCurve();

        [Header("Curve Remaps")]
        [SerializeField]
        AnimationCurve paintBlendResponse = CreateBuiltInPaintBlendResponseCurve();

        [SerializeField]
        AnimationCurve treeStrengthResponse = CreateBuiltInTreeStrengthResponseCurve();

        [SerializeField]
        AnimationCurve detailStrengthResponse = CreateBuiltInDetailStrengthResponseCurve();

        public static AnimationCurve GetBrushNoiseContrastClone()
        {
            return GetCurvePresetClone(CurvePresetListType.BrushNoiseContrast);
        }

        public static AnimationCurve GetPaintNoiseContrastClone()
        {
            return GetCurvePresetClone(CurvePresetListType.PaintNoiseContrast);
        }

        public static AnimationCurve GetTreeHeightDistributionClone()
        {
            return GetCurvePresetClone(CurvePresetListType.TreeHeightDistribution);
        }

        public static AnimationCurve GetBrushSizeMultiplierClone()
        {
            return GetCurvePresetClone(CurvePresetListType.BrushSizeMultiplier);
        }

        public static string[] GetCurvePresetNames(CurvePresetListType curveType)
        {
            TerraSplinesCurves preset = LoadOrCreatePreset();
            List<NamedCurvePreset> presets = preset != null ? preset.GetPresetList(curveType) : null;

            if (presets == null || presets.Count == 0)
            {
                return new[] { GetDefaultPresetName(curveType) };
            }

            string[] names = new string[presets.Count];
            for (int i = 0; i < presets.Count; i++)
            {
                names[i] = GetPresetDisplayName(presets[i], i, curveType);
            }

            return names;
        }

        public static AnimationCurve GetCurvePresetClone(CurvePresetListType curveType, int index = 0)
        {
            TerraSplinesCurves preset = LoadOrCreatePreset();
            List<NamedCurvePreset> presets = preset != null ? preset.GetPresetList(curveType) : null;
            Func<AnimationCurve> builtInFactory = GetBuiltInFactory(curveType);

            if (presets == null || presets.Count == 0)
            {
                return builtInFactory();
            }

            int clampedIndex = Mathf.Clamp(index, 0, presets.Count - 1);
            NamedCurvePreset entry = presets[clampedIndex];
            return CloneOrBuiltIn(entry != null ? entry.curve : null, builtInFactory);
        }

        public static AnimationCurve GetPaintBlendResponseClone()
        {
            TerraSplinesCurves preset = LoadOrCreatePreset();
            return preset != null
                ? CloneOrBuiltIn(preset.paintBlendResponse, CreateBuiltInPaintBlendResponseCurve)
                : CreateBuiltInPaintBlendResponseCurve();
        }

        public static AnimationCurve GetTreeStrengthResponseClone()
        {
            TerraSplinesCurves preset = LoadOrCreatePreset();
            return preset != null
                ? CloneOrBuiltIn(preset.treeStrengthResponse, CreateBuiltInTreeStrengthResponseCurve)
                : CreateBuiltInTreeStrengthResponseCurve();
        }

        public static AnimationCurve GetDetailStrengthResponseClone()
        {
            TerraSplinesCurves preset = LoadOrCreatePreset();
            return preset != null
                ? CloneOrBuiltIn(preset.detailStrengthResponse, CreateBuiltInDetailStrengthResponseCurve)
                : CreateBuiltInDetailStrengthResponseCurve();
        }

        public static float EvaluateDetailStrengthResponse(float strength)
        {
            float normalizedStrength = Mathf.Clamp01(strength);
            TerraSplinesCurves preset = LoadOrCreatePreset();
            AnimationCurve responseCurve = preset != null ? preset.detailStrengthResponse : null;
            if (responseCurve == null)
            {
                responseCurve = CreateBuiltInDetailStrengthResponseCurve();
            }

            return Mathf.Clamp01(responseCurve.Evaluate(normalizedStrength));
        }

        public static AnimationCurve CreateBuiltInNoiseContrastCurve()
        {
            return AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        public static AnimationCurve CreateBuiltInTreeHeightDistributionCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(1f, 1.2f)
            );
        }

        public static AnimationCurve CreateBuiltInBrushSizeMultiplierCurve()
        {
            return new AnimationCurve(
                        new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        public static AnimationCurve CreateBuiltInPaintBlendResponseCurve()
        {
            return new AnimationCurve(
                        new Keyframe(0f, 0f, 0.205586314f, 0.205586314f, 0f, 0.117079891f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0.5f, 1.87230158f, 1.87230158f, 0.0192837715f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        public static AnimationCurve CreateBuiltInTreeStrengthResponseCurve()
        {
            return new AnimationCurve(
                        new Keyframe(0f, 0.1f, 0.045710545f, 0.045710545f, 0f, 0.0730027556f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0.25f, 0.0231671035f, 0.0231671035f, 0.181818187f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        public static AnimationCurve CreateBuiltInDetailStrengthResponseCurve()
        {
            return new AnimationCurve(
                        new Keyframe(0f, 0.001f, 0.6562828f, 0.6562828f, 0f, 0.08402204f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 2.287617f, 2.287617f, 0.04132229f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        void OnEnable()
        {
            cachedInstance = this;
            if (EnsureCurves())
            {
                MarkDirty();
            }
        }

        void OnValidate()
        {
            if (EnsureCurves())
            {
                MarkDirty();
            }
        }

        void Reset()
        {
            PopulateBuiltInValues();
            MarkDirty();
        }

#if UNITY_EDITOR
        [ContextMenu("Copy As Code")]
        void CopyCurrentValuesAsResetCode()
        {
            EnsureCurves();
            UnityEditor.EditorGUIUtility.systemCopyBuffer = BuildResetCode();
            Debug.Log("Copied TerraSplinesCurves reset code to clipboard.", this);
        }
#endif

        bool EnsureCurves()
        {
            bool changed = false;

            brushSizeMultiplierPresets = EnsurePresetList(
                brushSizeMultiplierPresets,
                GetDefaultPresetName(CurvePresetListType.BrushSizeMultiplier),
                brushSizeMultiplier,
                CreateBuiltInBrushSizeMultiplierCurve,
                ref changed);

            brushNoiseContrastPresets = EnsurePresetList(
                brushNoiseContrastPresets,
                GetDefaultPresetName(CurvePresetListType.BrushNoiseContrast),
                brushNoiseContrast,
                CreateBuiltInNoiseContrastCurve,
                ref changed);

            paintNoiseContrastPresets = EnsurePresetList(
                paintNoiseContrastPresets,
                GetDefaultPresetName(CurvePresetListType.PaintNoiseContrast),
                paintNoiseContrast,
                CreateBuiltInNoiseContrastCurve,
                ref changed);

            treeHeightDistributionPresets = EnsurePresetList(
                treeHeightDistributionPresets,
                GetDefaultPresetName(CurvePresetListType.TreeHeightDistribution),
                treeHeightDistribution,
                CreateBuiltInTreeHeightDistributionCurve,
                ref changed);

            changed |= ReplaceIfCurveChanged(ref brushSizeMultiplier, GetPrimaryPresetCurveOrBuiltIn(brushSizeMultiplierPresets, CreateBuiltInBrushSizeMultiplierCurve));
            changed |= ReplaceIfCurveChanged(ref brushNoiseContrast, GetPrimaryPresetCurveOrBuiltIn(brushNoiseContrastPresets, CreateBuiltInNoiseContrastCurve));
            changed |= ReplaceIfCurveChanged(ref paintNoiseContrast, GetPrimaryPresetCurveOrBuiltIn(paintNoiseContrastPresets, CreateBuiltInNoiseContrastCurve));
            changed |= ReplaceIfCurveChanged(ref treeHeightDistribution, GetPrimaryPresetCurveOrBuiltIn(treeHeightDistributionPresets, CreateBuiltInTreeHeightDistributionCurve));
            changed |= ReplaceIfCurveChanged(ref paintBlendResponse, CloneOrBuiltIn(paintBlendResponse, CreateBuiltInPaintBlendResponseCurve));
            changed |= ReplaceIfCurveChanged(ref treeStrengthResponse, CloneOrBuiltIn(treeStrengthResponse, CreateBuiltInTreeStrengthResponseCurve));
            changed |= ReplaceIfCurveChanged(ref detailStrengthResponse, CloneOrBuiltIn(detailStrengthResponse, CreateBuiltInDetailStrengthResponseCurve));

            return changed;
        }

        void PopulateBuiltInValues()
        {
            brushSizeMultiplierPresets = new List<NamedCurvePreset>
            {
                new NamedCurvePreset
                {
                    name = "Flat",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Bump",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Dip",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Waves",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -6.66666651f, -6.66666651f, 0f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.15f, 0f, 0.08261657f, 0.08261657f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.296372175f, 1f, 0.96048975f, 0.96048975f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.5f, 0f, 0.980875969f, 0.980875969f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.6455038f, 1f, 0.991302967f, 0.991302967f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.85f, 0f, 0.8883009f, 0.8883009f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(1f, 1f, 6.666668f, 6.666668f, 0.333333343f, 0f) { weightedMode = WeightedMode.Both }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Taper",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0.5f, 0.3597804f, 0.3597804f, 0f, 0.0736728f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                }
            };

            brushNoiseContrastPresets = new List<NamedCurvePreset>
            {
                new NamedCurvePreset
                {
                    name = "Linear",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, 1f, 1f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 1f, 1f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Plato",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, 11.3102036f, 11.3102036f, 0f, 0.10548503f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.1f, 1f, 0f, 0f, 0.333333343f, 0.111286953f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.9f, 1f, -0.00271586236f, -0.00271586236f, 0.9984175f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0f, -11.9268961f, -11.9268961f, 0.210974172f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Dip",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -2f, -2f, 0f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 2f, 2f, 0.333333343f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Step",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0.296687454f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Waves",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -6.66666651f, -6.66666651f, 0f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.15f, 0f, 0.08261657f, 0.08261657f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.296372175f, 1f, 0.96048975f, 0.96048975f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.5f, 0f, 0.980875969f, 0.980875969f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.6455038f, 1f, 0.991302967f, 0.991302967f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.85f, 0f, 0.8883009f, 0.8883009f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(1f, 1f, 6.666668f, 6.666668f, 0.333333343f, 0f) { weightedMode = WeightedMode.Both }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Details",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -2.2985127f, -2.2985127f, 0f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.432952881f, 0.004852295f, -0.7701405f, -0.7701405f, 0.333333343f, 0.05773955f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.65f, 1f, 0.0360404f, 0.0360404f, 0.333333343f, 0.135969535f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                }
            };

            paintNoiseContrastPresets = new List<NamedCurvePreset>
            {
                new NamedCurvePreset
                {
                    name = "Linear",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, 1f, 1f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 1f, 1f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Plato",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, 11.3102036f, 11.3102036f, 0f, 0.10548503f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.1f, 1f, 0f, 0f, 0.333333343f, 0.111286953f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.9f, 1f, -0.00271586236f, -0.00271586236f, 0.9984175f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0f, -11.9268961f, -11.9268961f, 0.210974172f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Dip",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -2f, -2f, 0f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 2f, 2f, 0.333333343f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Step",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0.296687454f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.5f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Waves",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -6.66666651f, -6.66666651f, 0f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.15f, 0f, 0.08261657f, 0.08261657f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.296372175f, 1f, 0.96048975f, 0.96048975f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.5f, 0f, 0.980875969f, 0.980875969f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.6455038f, 1f, 0.991302967f, 0.991302967f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(0.85f, 0f, 0.8883009f, 0.8883009f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.Both },
                        new Keyframe(1f, 1f, 6.666668f, 6.666668f, 0.333333343f, 0f) { weightedMode = WeightedMode.Both }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Details",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, -2.2985127f, -2.2985127f, 0f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.432952881f, 0.004852295f, -0.7701405f, -0.7701405f, 0.333333343f, 0.05773955f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.65f, 1f, 0.0360404f, 0.0360404f, 0.333333343f, 0.135969535f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0.333333343f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                }
            };

            treeHeightDistributionPresets = new List<NamedCurvePreset>
            {
                new NamedCurvePreset
                {
                    name = "Natural",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0.8f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1.2f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Flat",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Deep",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.1f, 0.8f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.95f, 1.2f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 2f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                },
                new NamedCurvePreset
                {
                    name = "Step",
                    curve = new AnimationCurve(
                        new Keyframe(0f, 0.5f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0.296687454f) { weightedMode = WeightedMode.None },
                        new Keyframe(0.25f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0.333333343f, 0.333333343f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, float.PositiveInfinity, float.PositiveInfinity, 0f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }
                }
            };

            paintBlendResponse = new AnimationCurve(
                        new Keyframe(0f, 0f, 0.205586314f, 0.205586314f, 0f, 0.117079891f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0.5f, 1.87230158f, 1.87230158f, 0.0192837715f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };

            treeStrengthResponse = new AnimationCurve(
                        new Keyframe(0f, 0.1f, 0.045710545f, 0.045710545f, 0f, 0.0730027556f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 0.25f, 0.0231671035f, 0.0231671035f, 0.181818187f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };

            detailStrengthResponse = new AnimationCurve(
                        new Keyframe(0f, 0.001f, 0.6562828f, 0.6562828f, 0f, 0.08402204f) { weightedMode = WeightedMode.None },
                        new Keyframe(1f, 1f, 2.287617f, 2.287617f, 0.04132229f, 0f) { weightedMode = WeightedMode.None }
            ) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        List<NamedCurvePreset> GetPresetList(CurvePresetListType curveType)
        {
            return curveType switch
            {
                CurvePresetListType.BrushSizeMultiplier => brushSizeMultiplierPresets,
                CurvePresetListType.BrushNoiseContrast => brushNoiseContrastPresets,
                CurvePresetListType.PaintNoiseContrast => paintNoiseContrastPresets,
                CurvePresetListType.TreeHeightDistribution => treeHeightDistributionPresets,
                _ => brushNoiseContrastPresets,
            };
        }

        static Func<AnimationCurve> GetBuiltInFactory(CurvePresetListType curveType)
        {
            return curveType switch
            {
                CurvePresetListType.BrushSizeMultiplier => CreateBuiltInBrushSizeMultiplierCurve,
                CurvePresetListType.BrushNoiseContrast => CreateBuiltInNoiseContrastCurve,
                CurvePresetListType.PaintNoiseContrast => CreateBuiltInNoiseContrastCurve,
                CurvePresetListType.TreeHeightDistribution => CreateBuiltInTreeHeightDistributionCurve,
                _ => CreateBuiltInNoiseContrastCurve,
            };
        }

        static string GetDefaultPresetName(CurvePresetListType curveType)
        {
            return curveType switch
            {
                CurvePresetListType.BrushSizeMultiplier => "Default",
                CurvePresetListType.BrushNoiseContrast => "Default",
                CurvePresetListType.PaintNoiseContrast => "Default",
                CurvePresetListType.TreeHeightDistribution => "Default",
                _ => "Default",
            };
        }

        static string GetPresetDisplayName(NamedCurvePreset preset, int index, CurvePresetListType curveType)
        {
            if (preset != null && !string.IsNullOrWhiteSpace(preset.name))
            {
                return preset.name;
            }

            return index == 0 ? GetDefaultPresetName(curveType) : $"Preset {index + 1}";
        }

        static List<NamedCurvePreset> EnsurePresetList(
            List<NamedCurvePreset> presets,
            string defaultName,
            AnimationCurve legacyPrimary,
            Func<AnimationCurve> builtInFactory,
            ref bool changed)
        {
            presets ??= new List<NamedCurvePreset>();

            if (presets.Count == 0)
            {
                presets.Add(new NamedCurvePreset
                {
                    name = defaultName,
                    curve = CloneOrBuiltIn(legacyPrimary, builtInFactory)
                });
                changed = true;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                NamedCurvePreset entry = presets[i];
                if (entry == null)
                {
                    entry = new NamedCurvePreset();
                    presets[i] = entry;
                    changed = true;
                }

                string expectedName = i == 0 ? defaultName : $"Preset {i + 1}";
                if (string.IsNullOrWhiteSpace(entry.name))
                {
                    entry.name = expectedName;
                    changed = true;
                }

                if (entry.curve == null)
                {
                    entry.curve = i == 0
                        ? CloneOrBuiltIn(legacyPrimary, builtInFactory)
                        : builtInFactory();
                    changed = true;
                }
            }

            return presets;
        }

        static AnimationCurve GetPrimaryPresetCurveOrBuiltIn(List<NamedCurvePreset> presets, Func<AnimationCurve> builtInFactory)
        {
            if (presets == null || presets.Count == 0)
            {
                return builtInFactory();
            }

            NamedCurvePreset primary = presets[0];
            return CloneOrBuiltIn(primary != null ? primary.curve : null, builtInFactory);
        }

        static bool ReplaceIfCurveChanged(ref AnimationCurve target, AnimationCurve replacement)
        {
            int targetHash = target != null ? target.GetAnimationCurveHash() : 0;
            int replacementHash = replacement != null ? replacement.GetAnimationCurveHash() : 0;
            if (targetHash == replacementHash)
            {
                return false;
            }

            target = replacement;
            return true;
        }

        static TerraSplinesCurves LoadOrCreatePreset()
        {
            if (cachedInstance != null)
            {
                return cachedInstance;
            }

            if (isResolving)
            {
                return null;
            }

            isResolving = true;
            try
            {
                cachedInstance = Resources.Load<TerraSplinesCurves>(ResourceName);

#if UNITY_EDITOR
                if (cachedInstance == null)
                {
                    cachedInstance = LoadOrCreatePresetAssetInEditor();
                }
#endif

                return cachedInstance;
            }
            finally
            {
                isResolving = false;
            }
        }

        static AnimationCurve CloneOrBuiltIn(AnimationCurve primary, System.Func<AnimationCurve> builtInFactory)
        {
            if (primary != null)
            {
                return primary.CloneCurve();
            }

            return builtInFactory();
        }

        void MarkDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        string BuildResetCode()
        {
            var builder = new StringBuilder();
            AppendPresetListAssignment(builder, nameof(brushSizeMultiplierPresets), brushSizeMultiplierPresets, CreateBuiltInBrushSizeMultiplierCurve);
            builder.AppendLine();
            AppendPresetListAssignment(builder, nameof(brushNoiseContrastPresets), brushNoiseContrastPresets, CreateBuiltInNoiseContrastCurve);
            builder.AppendLine();
            AppendPresetListAssignment(builder, nameof(paintNoiseContrastPresets), paintNoiseContrastPresets, CreateBuiltInNoiseContrastCurve);
            builder.AppendLine();
            AppendPresetListAssignment(builder, nameof(treeHeightDistributionPresets), treeHeightDistributionPresets, CreateBuiltInTreeHeightDistributionCurve);
            builder.AppendLine();
            AppendCurveAssignment(builder, nameof(paintBlendResponse), paintBlendResponse, CreateBuiltInPaintBlendResponseCurve);
            builder.AppendLine();
            AppendCurveAssignment(builder, nameof(treeStrengthResponse), treeStrengthResponse, CreateBuiltInTreeStrengthResponseCurve);
            builder.AppendLine();
            AppendCurveAssignment(builder, nameof(detailStrengthResponse), detailStrengthResponse, CreateBuiltInDetailStrengthResponseCurve);
            return builder.ToString().TrimEnd();
        }

        static void AppendPresetListAssignment(
            StringBuilder builder,
            string fieldName,
            List<NamedCurvePreset> presets,
            Func<AnimationCurve> builtInFactory)
        {
            builder.Append(fieldName);
            builder.AppendLine(" = new List<NamedCurvePreset>");
            builder.AppendLine("{");

            List<NamedCurvePreset> source = presets != null && presets.Count > 0
                ? presets
                : new List<NamedCurvePreset>
                {
                    new NamedCurvePreset
                    {
                        name = "Default",
                        curve = builtInFactory()
                    }
                };

            for (int i = 0; i < source.Count; i++)
            {
                NamedCurvePreset preset = source[i] ?? new NamedCurvePreset();
                builder.AppendLine("    new NamedCurvePreset");
                builder.AppendLine("    {");
                builder.Append("        name = ");
                builder.Append(ToCSharpString(string.IsNullOrWhiteSpace(preset.name) ? $"Preset {i + 1}" : preset.name));
                builder.AppendLine(",");
                builder.Append("        curve = ");
                builder.AppendLine(SerializeCurveExpression(preset.curve, builtInFactory));
                builder.Append("    }");
                builder.AppendLine(i < source.Count - 1 ? "," : string.Empty);
            }

            builder.AppendLine("};");
        }

        static void AppendCurveAssignment(
            StringBuilder builder,
            string fieldName,
            AnimationCurve curve,
            Func<AnimationCurve> builtInFactory)
        {
            builder.Append(fieldName);
            builder.Append(" = ");
            builder.Append(SerializeCurveExpression(curve, builtInFactory));
            builder.AppendLine(";");
        }

        static string SerializeCurveExpression(AnimationCurve curve, Func<AnimationCurve> builtInFactory)
        {
            AnimationCurve source = CloneOrBuiltIn(curve, builtInFactory);
            var builder = new StringBuilder();
            builder.AppendLine("new AnimationCurve(");

            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                builder.Append("            ");
                builder.Append(SerializeKeyframeExpression(keys[i]));
                builder.AppendLine(i < keys.Length - 1 ? "," : string.Empty);
            }

            builder.Append(") { preWrapMode = WrapMode.");
            builder.Append(source.preWrapMode);
            builder.Append(", postWrapMode = WrapMode.");
            builder.Append(source.postWrapMode);
            builder.Append(" }");
            return builder.ToString();
        }

        static string SerializeKeyframeExpression(Keyframe keyframe)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "new Keyframe({0}, {1}, {2}, {3}, {4}, {5}) {{ weightedMode = WeightedMode.{6} }}",
                FormatFloat(keyframe.time),
                FormatFloat(keyframe.value),
                FormatFloat(keyframe.inTangent),
                FormatFloat(keyframe.outTangent),
                FormatFloat(keyframe.inWeight),
                FormatFloat(keyframe.outWeight),
                keyframe.weightedMode);
        }

        static string FormatFloat(float value)
        {
            if (float.IsNaN(value))
            {
                return "float.NaN";
            }

            if (float.IsPositiveInfinity(value))
            {
                return "float.PositiveInfinity";
            }

            if (float.IsNegativeInfinity(value))
            {
                return "float.NegativeInfinity";
            }

            string formatted = value.ToString("R", CultureInfo.InvariantCulture);
            return formatted.Contains("E", StringComparison.Ordinal)
                ? $"{formatted}f"
                : $"{formatted}f";
        }

        static string ToCSharpString(string value)
        {
            return string.Concat("\"", value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty, "\"");
        }

        static TerraSplinesCurves LoadOrCreatePresetAssetInEditor()
        {
            TerraSplinesCurves asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TerraSplinesCurves>(AssetPath);
            if (asset != null)
            {
                asset.EnsureCurves();
                return asset;
            }

            EnsureFolderHierarchy(Path.GetDirectoryName(AssetPath));

            asset = CreateInstance<TerraSplinesCurves>();
            asset.EnsureCurves();
            UnityEditor.AssetDatabase.CreateAsset(asset, AssetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            return asset;
        }

        static void EnsureFolderHierarchy(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalizedPath = folderPath.Replace('\\', '/');
            string[] segments = normalizedPath.Split('/');
            string currentPath = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = $"{currentPath}/{segments[i]}";
                if (!UnityEditor.AssetDatabase.IsValidFolder(nextPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder(currentPath, segments[i]);
                }

                currentPath = nextPath;
            }
        }
#endif
    }
}