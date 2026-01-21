using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Helper scope for temporarily changing label width
    /// </summary>
    internal class LabelWidthScope : System.IDisposable
    {
        private readonly float m_PreviousLabelWidth;

        public LabelWidthScope(float labelWidth)
        {
            m_PreviousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public void Dispose()
        {
            EditorGUIUtility.labelWidth = m_PreviousLabelWidth;
        }
    }

    [CustomEditor(typeof(SplineToTerrain))]
    [CanEditMultipleObjects]
    class SplineToTerrainEditor : UnityEditor.Editor
    {
        SerializedProperty m_Container;
        SerializedProperty m_Terrain;
        SerializedProperty m_RebuildOnSplineChange;
        SerializedProperty m_RebuildFrequency;
        SerializedProperty m_SnapSplineToTerrain;
        SerializedProperty m_SplineHeightOffset;
        SerializedProperty m_DeformTerrainToSpline;
        SerializedProperty m_DeformWidth;
        SerializedProperty m_SmoothingDistance;
        SerializedProperty m_TerrainOffset;
        SerializedProperty m_SamplesPerUnit;
        SerializedProperty m_DeformStrength;

        static readonly GUIContent k_SplineToTerrainContent = new GUIContent(L10n.Tr("Spline to Terrain"), L10n.Tr("Snap spline points to terrain height."));
        static readonly GUIContent k_TerrainToSplineContent = new GUIContent(L10n.Tr("Terrain to Spline"), L10n.Tr("Deform terrain to match spline path."));
        static readonly GUIContent k_GeneralContent = new GUIContent(L10n.Tr("General"), L10n.Tr("General settings."));

        static readonly string k_SourceSplineContainer = L10n.Tr("Source Spline Container");
        static readonly string k_TargetTerrain = L10n.Tr("Target Terrain");
        static readonly string k_AutoRefreshGeneration = L10n.Tr("Auto Refresh Generation");
        static readonly string k_Helpbox = L10n.Tr("Spline Container must be set.");

        SplineToTerrain[] m_Components;

        void OnEnable()
        {
            m_Container = serializedObject.FindProperty("m_Container");
            m_Terrain = serializedObject.FindProperty("m_Terrain");
            m_RebuildOnSplineChange = serializedObject.FindProperty("m_RebuildOnSplineChange");
            m_RebuildFrequency = serializedObject.FindProperty("m_RebuildFrequency");
            m_SnapSplineToTerrain = serializedObject.FindProperty("m_SnapSplineToTerrain");
            m_SplineHeightOffset = serializedObject.FindProperty("m_SplineHeightOffset");
            m_DeformTerrainToSpline = serializedObject.FindProperty("m_DeformTerrainToSpline");
            m_DeformWidth = serializedObject.FindProperty("m_DeformWidth");
            m_SmoothingDistance = serializedObject.FindProperty("m_SmoothingDistance");
            m_TerrainOffset = serializedObject.FindProperty("m_TerrainOffset");
            m_SamplesPerUnit = serializedObject.FindProperty("m_SamplesPerUnit");
            m_DeformStrength = serializedObject.FindProperty("m_DeformStrength");

            m_Components = new SplineToTerrain[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                m_Components[i] = targets[i] as SplineToTerrain;

            EditorSplineUtility.AfterSplineWasModified += OnSplineModified;
        }

        void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnSplineModified;
        }

        void OnSplineModified(Spline spline)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var component in m_Components)
            {
                if (component != null && component.Container != null && component.Container.Splines.Contains(spline))
                    component.Rebuild();
            }
        }

        void SetRebuildOnSplineChange(bool value)
        {
            foreach (var component in m_Components)
            {
                if (component != null)
                {
                    Undo.RecordObject(component, "Set Rebuild on Spline Change.");
                    component.RebuildOnSplineChange = value;
                }
            }
        }

        void Rebuild()
        {
            foreach (var component in m_Components)
            {
                if (component != null)
                    component.Rebuild();
            }
        }

        bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick)
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

            // General Section
            m_Container.isExpanded = Foldout(m_Container.isExpanded, k_GeneralContent, true);

            if (m_Container.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_Container, new GUIContent(k_SourceSplineContainer, m_Container.tooltip));

                if (m_Container.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(k_Helpbox, MessageType.Warning);

                EditorGUILayout.PropertyField(m_Terrain, new GUIContent(k_TargetTerrain, m_Terrain.tooltip));

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_RebuildOnSplineChange, new GUIContent(k_AutoRefreshGeneration, m_RebuildOnSplineChange.tooltip));
                if (m_RebuildOnSplineChange.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(!m_RebuildOnSplineChange.boolValue);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(15);
                        using (new LabelWidthScope(80f))
                            EditorGUILayout.PropertyField(m_RebuildFrequency, new GUIContent() { text = L10n.Tr("Frequency") });
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(L10n.Tr("Regenerate"))))
                        Rebuild();
                }

                if (EditorGUI.EndChangeCheck() && !m_RebuildOnSplineChange.boolValue)
                    SetRebuildOnSplineChange(m_RebuildOnSplineChange.boolValue);

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            // Spline to Terrain Section
            m_SnapSplineToTerrain.isExpanded = Foldout(m_SnapSplineToTerrain.isExpanded, k_SplineToTerrainContent, true);

            if (m_SnapSplineToTerrain.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_SnapSplineToTerrain);

                if (m_SnapSplineToTerrain.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SplineHeightOffset);
                    if (EditorGUI.EndChangeCheck())
                        m_SplineHeightOffset.floatValue = Mathf.Clamp(m_SplineHeightOffset.floatValue, -1000f, 1000f);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            // Terrain to Spline Section
            m_DeformTerrainToSpline.isExpanded = Foldout(m_DeformTerrainToSpline.isExpanded, k_TerrainToSplineContent, true);

            if (m_DeformTerrainToSpline.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_DeformTerrainToSpline);

                if (m_DeformTerrainToSpline.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_DeformWidth);
                    if (EditorGUI.EndChangeCheck())
                        m_DeformWidth.floatValue = Mathf.Max(m_DeformWidth.floatValue, 0.1f);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SmoothingDistance);
                    if (EditorGUI.EndChangeCheck())
                        m_SmoothingDistance.floatValue = Mathf.Max(m_SmoothingDistance.floatValue, 0f);

                    EditorGUILayout.PropertyField(m_TerrainOffset);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SamplesPerUnit);
                    if (EditorGUI.EndChangeCheck())
                        m_SamplesPerUnit.floatValue = Mathf.Clamp(m_SamplesPerUnit.floatValue, 0.1f, 100f);

                    EditorGUILayout.PropertyField(m_DeformStrength);
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
                Rebuild();
        }
    }
}