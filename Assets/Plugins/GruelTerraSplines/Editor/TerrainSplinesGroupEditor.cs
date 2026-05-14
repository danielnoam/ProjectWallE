using UnityEngine;
using UnityEditor;

namespace GruelTerraSplines
{
    [CustomEditor(typeof(TerrainSplinesGroup))]
    public class TerrainSplinesGroupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TerrainSplinesGroup group = (TerrainSplinesGroup)target;
            using (new EditorGUI.DisabledScope(!group.IsValid))
            {
                DrawButton(group);
            }
        }

        void DrawButton(TerrainSplinesGroup group)
        {
            if (GUILayout.Button("Set Active"))
            {
                var window = TerraSplinesWindow.GetWindow<TerraSplinesWindow>();
                window.SetTargets(group.TerrainGroup, group.SplineGroup);
            }
        }
    }

}