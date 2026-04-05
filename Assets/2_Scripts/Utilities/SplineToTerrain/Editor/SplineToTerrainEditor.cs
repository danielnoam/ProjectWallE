using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    internal class LabelWidthScope : System.IDisposable
    {
        private readonly float _previousLabelWidth;

        public LabelWidthScope(float labelWidth)
        {
            _previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public void Dispose()
        {
            EditorGUIUtility.labelWidth = _previousLabelWidth;
        }
    }

    [CustomEditor(typeof(SplineToTerrain))]
    [CanEditMultipleObjects]
    internal class SplineToTerrainEditor : UnityEditor.Editor
    {
        private SerializedProperty _container;
        private SerializedProperty _terrain;
        private SerializedProperty _rebuildOnSplineChange;
        private SerializedProperty _rebuildFrequency;
        private SerializedProperty _snapSplineToTerrain;
        private SerializedProperty _splineHeightOffset;
        private SerializedProperty _deformTerrainToSpline;
        private SerializedProperty _deformWidth;
        private SerializedProperty _smoothingDistance;
        private SerializedProperty _terrainOffset;
        private SerializedProperty _samplesPerUnit;
        private SerializedProperty _deformStrength;
        private SerializedProperty _paintTerrainLayer;
        private SerializedProperty _terrainLayer;
        private SerializedProperty _paintWidth;
        private SerializedProperty _paintSmoothingDistance;
        private SerializedProperty _paintStrength;

        private static readonly GUIContent k_SplineToTerrainContent = new GUIContent(L10n.Tr("Spline to Terrain"), L10n.Tr("Snap spline points to terrain height."));
        private static readonly GUIContent k_TerrainToSplineContent = new GUIContent(L10n.Tr("Terrain to Spline"), L10n.Tr("Deform terrain to match spline path."));
        private static readonly GUIContent k_PaintTerrainLayerContent = new GUIContent(L10n.Tr("Paint Terrain Layer"), L10n.Tr("Paint a terrain layer along the spline path."));
        private static readonly GUIContent k_GeneralContent = new GUIContent(L10n.Tr("General"), L10n.Tr("General settings."));

        private static readonly string k_SourceSplineContainer = L10n.Tr("Source Spline Container");
        private static readonly string k_TargetTerrain = L10n.Tr("Target Terrain");
        private static readonly string k_AutoRefreshGeneration = L10n.Tr("Auto Refresh Generation");
        private static readonly string k_Helpbox = L10n.Tr("Spline Container must be set.");

        private SplineToTerrain[] _components;

        private void OnEnable()
        {
            _container = serializedObject.FindProperty("container");
            _terrain = serializedObject.FindProperty("terrain");
            _rebuildOnSplineChange = serializedObject.FindProperty("rebuildOnSplineChange");
            _rebuildFrequency = serializedObject.FindProperty("rebuildFrequency");
            _snapSplineToTerrain = serializedObject.FindProperty("snapSplineToTerrain");
            _splineHeightOffset = serializedObject.FindProperty("splineHeightOffset");
            _deformTerrainToSpline = serializedObject.FindProperty("deformTerrainToSpline");
            _deformWidth = serializedObject.FindProperty("deformWidth");
            _smoothingDistance = serializedObject.FindProperty("smoothingDistance");
            _terrainOffset = serializedObject.FindProperty("terrainOffset");
            _samplesPerUnit = serializedObject.FindProperty("samplesPerUnit");
            _deformStrength = serializedObject.FindProperty("deformStrength");
            _paintTerrainLayer = serializedObject.FindProperty("paintTerrainLayer");
            _terrainLayer = serializedObject.FindProperty("terrainLayer");
            _paintWidth = serializedObject.FindProperty("paintWidth");
            _paintSmoothingDistance = serializedObject.FindProperty("paintSmoothingDistance");
            _paintStrength = serializedObject.FindProperty("paintStrength");

            _components = new SplineToTerrain[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                _components[i] = targets[i] as SplineToTerrain;

            EditorSplineUtility.AfterSplineWasModified += OnSplineModified;
        }

        private void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnSplineModified;
        }

        private void OnSplineModified(Spline spline)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var component in _components)
            {
                if (component != null && component.Container != null && component.Container.Splines.Contains(spline))
                    component.Rebuild();
            }
        }

        private void SetRebuildOnSplineChange(bool value)
        {
            foreach (var component in _components)
            {
                if (component != null)
                {
                    Undo.RecordObject(component, "Set Rebuild on Spline Change.");
                    component.RebuildOnSplineChange = value;
                }
            }
        }

        private void Rebuild()
        {
            foreach (var component in _components)
            {
                if (component != null)
                    component.Rebuild();
            }
        }

        private bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick)
        {
            var style = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };
            return EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, style);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            _container.isExpanded = Foldout(_container.isExpanded, k_GeneralContent, true);

            if (_container.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_container, new GUIContent(k_SourceSplineContainer, _container.tooltip));

                if (_container.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(k_Helpbox, MessageType.Warning);

                EditorGUILayout.PropertyField(_terrain, new GUIContent(k_TargetTerrain, _terrain.tooltip));

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_rebuildOnSplineChange, new GUIContent(k_AutoRefreshGeneration, _rebuildOnSplineChange.tooltip));
                if (_rebuildOnSplineChange.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(!_rebuildOnSplineChange.boolValue);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(15);
                        using (new LabelWidthScope(80f))
                            EditorGUILayout.PropertyField(_rebuildFrequency, new GUIContent() { text = L10n.Tr("Frequency") });
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(L10n.Tr("Regenerate"))))
                        Rebuild();
                }

                if (EditorGUI.EndChangeCheck() && !_rebuildOnSplineChange.boolValue)
                    SetRebuildOnSplineChange(_rebuildOnSplineChange.boolValue);

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            _snapSplineToTerrain.isExpanded = Foldout(_snapSplineToTerrain.isExpanded, k_SplineToTerrainContent, true);

            if (_snapSplineToTerrain.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_snapSplineToTerrain);

                if (_snapSplineToTerrain.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_splineHeightOffset);
                    if (EditorGUI.EndChangeCheck())
                        _splineHeightOffset.floatValue = Mathf.Clamp(_splineHeightOffset.floatValue, -1000f, 1000f);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            _deformTerrainToSpline.isExpanded = Foldout(_deformTerrainToSpline.isExpanded, k_TerrainToSplineContent, true);

            if (_deformTerrainToSpline.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_deformTerrainToSpline);

                if (_deformTerrainToSpline.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_deformWidth);
                    if (EditorGUI.EndChangeCheck())
                        _deformWidth.floatValue = Mathf.Max(_deformWidth.floatValue, 0.1f);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_smoothingDistance);
                    if (EditorGUI.EndChangeCheck())
                        _smoothingDistance.floatValue = Mathf.Max(_smoothingDistance.floatValue, 0f);

                    EditorGUILayout.PropertyField(_terrainOffset);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_samplesPerUnit);
                    if (EditorGUI.EndChangeCheck())
                        _samplesPerUnit.floatValue = Mathf.Clamp(_samplesPerUnit.floatValue, 0.1f, 100f);

                    EditorGUILayout.PropertyField(_deformStrength);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            _paintTerrainLayer.isExpanded = Foldout(_paintTerrainLayer.isExpanded, k_PaintTerrainLayerContent, true);

            if (_paintTerrainLayer.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_paintTerrainLayer);

                if (_paintTerrainLayer.boolValue)
                {
                    EditorGUILayout.PropertyField(_terrainLayer);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_paintWidth);
                    if (EditorGUI.EndChangeCheck())
                        _paintWidth.floatValue = Mathf.Max(_paintWidth.floatValue, 0.1f);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_paintSmoothingDistance);
                    if (EditorGUI.EndChangeCheck())
                        _paintSmoothingDistance.floatValue = Mathf.Max(_paintSmoothingDistance.floatValue, 0f);

                    EditorGUILayout.PropertyField(_paintStrength);
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                Rebuild();
        }
    }
}