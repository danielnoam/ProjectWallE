using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.Systems.PrefabScatter
{
    [CustomEditor(typeof(PrefabScatter))]
    internal class PrefabScatterEditor : Editor
    {
        private PrefabScatter _scatter;
        private SerializedProperty _terrains;

        private List<PrefabScatter.ScatterLayerMask> _contextMasks;

        private void OnEnable()
        {
            _scatter = (PrefabScatter)target;
            _terrains = serializedObject.FindProperty("terrains");
        }

        public override void OnInspectorGUI()
        {
            DrawTerrains();

            EditorGUILayout.Space();

            DrawItems();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Spawn All", GUILayout.Height(30f))) _scatter.SpawnAll();
                if (GUILayout.Button("Clear All", GUILayout.Height(30f))) _scatter.ClearAll();
            }
        }

        private void DrawTerrains()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Terrains", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_terrains, true);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add active terrains"))
                {
                    _scatter.terrains.RemoveAll(x => x == null);
                    foreach (Terrain terrain in Terrain.activeTerrains)
                    {
                        if (!_scatter.terrains.Contains(terrain)) _scatter.terrains.Add(terrain);
                    }
                    EditorUtility.SetDirty(_scatter);
                }
                if (GUILayout.Button("Clear"))
                {
                    _scatter.terrains.Clear();
                    EditorUtility.SetDirty(_scatter);
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (_scatter.terrains.Count == 0)
            {
                EditorGUILayout.HelpBox("Assign one or more terrains to scatter on.", MessageType.Info);
            }
        }

        private void DrawItems()
        {
            EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

            Undo.RecordObject(_scatter, "Modified prefab scatter");

            serializedObject.Update();
            SerializedProperty itemsProp = serializedObject.FindProperty("items");

            int duplicateIndex = -1;
            int removeIndex = -1;

            for (int i = 0; i < _scatter.items.Count; i++)
            {
                SerializedProperty prefabsProp = itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("prefabs");
                ItemAction action = DrawItem(_scatter.items[i], prefabsProp);
                if (action == ItemAction.Duplicate) duplicateIndex = i;
                else if (action == ItemAction.Remove) removeIndex = i;
            }

            serializedObject.ApplyModifiedProperties();

            if (duplicateIndex >= 0)
            {
                _scatter.items.Insert(duplicateIndex + 1, PrefabScatter.ScatterItem.Duplicate(_scatter.items[duplicateIndex]));
                EditorUtility.SetDirty(_scatter);
            }
            else if (removeIndex >= 0)
            {
                _scatter.ClearItem(_scatter.items[removeIndex]);
                _scatter.items.RemoveAt(removeIndex);
                EditorUtility.SetDirty(_scatter);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add item", GUILayout.Width(120f)))
                {
                    _scatter.items.Add(new PrefabScatter.ScatterItem());
                    EditorUtility.SetDirty(_scatter);
                }
            }
        }

        private enum ItemAction { None, Duplicate, Remove }

        private ItemAction DrawItem(PrefabScatter.ScatterItem item, SerializedProperty prefabsProp)
        {
            ItemAction action = ItemAction.None;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    item.foldout = EditorGUILayout.Foldout(item.foldout, $"{item.name}  ({item.instanceCount})", true);
                    item.enabled = EditorGUILayout.Toggle(item.enabled, GUILayout.Width(20f));

                    if (GUILayout.Button("Spawn", EditorStyles.miniButtonLeft, GUILayout.Width(55f))) _scatter.SpawnItem(item);
                    if (GUILayout.Button("Clear", EditorStyles.miniButtonMid, GUILayout.Width(45f))) _scatter.ClearItem(item);
                    if (GUILayout.Button("Dup", EditorStyles.miniButtonMid, GUILayout.Width(40f))) action = ItemAction.Duplicate;
                    if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(22f))) action = ItemAction.Remove;
                }

                if (!item.foldout) return action;

                using (new EditorGUI.DisabledScope(!item.enabled))
                {
                    EditorGUI.BeginChangeCheck();

                    item.name = EditorGUILayout.TextField("Name", item.name);
                    item.parent = (Transform)EditorGUILayout.ObjectField("Parent", item.parent, typeof(Transform), true);

                    EditorGUILayout.PropertyField(prefabsProp, new GUIContent("Prefabs", "One is picked at random for each spawn point, using its own transform settings"), true);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Spawn rules", EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        item.seed = EditorGUILayout.IntField("Seed", item.seed);
                        if (GUILayout.Button("Randomize", GUILayout.Width(90f))) item.seed = Random.Range(0, 99999);
                    }
                    item.spawnChance = EditorGUILayout.Slider("Spawn chance %", item.spawnChance, 0f, 100f);
                    item.density = EditorGUILayout.FloatField(new GUIContent("Density (min distance)", "Minimum distance between instances. Lower values spawn denser"), Mathf.Max(0.5f, item.density));
                    item.maxInstances = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Max instances", "Caps how many instances spawn across all terrains (0 = unlimited)"), item.maxInstances));

                    DrawMinMax("Height range", ref item.heightRange, -100f, 2000f);
                    DrawMinMax("Slope range", ref item.slopeRange, 0f, 90f);
                    DrawMinMax("Curvature range", ref item.curvatureRange, 0f, 1f);

                    item.collisionCheck = EditorGUILayout.Toggle(new GUIContent("Collision check", "Skip points that overlap a collider on the selected layers"), item.collisionCheck);
                    using (new EditorGUI.DisabledScope(!item.collisionCheck))
                    {
                        EditorGUI.indentLevel++;
                        int mask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(item.collisionLayers);
                        mask = EditorGUILayout.MaskField(new GUIContent("Obstacle layers", "Layers treated as obstacles. Do not include the terrain's own layer"), mask, InternalEditorUtility.layers);
                        item.collisionLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(mask);
                        item.collisionRadius = Mathf.Max(0f, EditorGUILayout.FloatField("Radius", item.collisionRadius));
                        EditorGUI.indentLevel--;
                    }

                    DrawLayerMasks(item.layerMasks);

                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_scatter);
                }
            }

            return action;
        }

        private void DrawLayerMasks(List<PrefabScatter.ScatterLayerMask> masks)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Layer masks", "Restrict spawning to specific terrain layers. Empty means spawn on any layer"), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(60f))) ShowLayerMenu(masks);
            }

            TerrainLayer[] layers = GetTerrainLayers();

            for (int i = 0; i < masks.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = masks[i].layerIndex < layers.Length && layers[masks[i].layerIndex] ? layers[masks[i].layerIndex].name : $"Layer {masks[i].layerIndex}";
                    EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                    masks[i].threshold = EditorGUILayout.Slider(masks[i].threshold, 0f, 1f);
                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                    {
                        masks.RemoveAt(i);
                        EditorUtility.SetDirty(_scatter);
                        return;
                    }
                }
            }
        }

        private void ShowLayerMenu(List<PrefabScatter.ScatterLayerMask> masks)
        {
            TerrainLayer[] layers = GetTerrainLayers();
            if (layers.Length == 0)
            {
                Debug.LogWarning("[Prefab Scatter] The assigned terrain has no terrain layers to mask against.");
                return;
            }

            _contextMasks = masks;

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < layers.Length; i++)
            {
                if (!layers[i]) continue;
                if (masks.Find(x => x.layerIndex == i) != null) continue;

                menu.AddItem(new GUIContent(layers[i].name), false, AddLayerMask, i);
            }
            menu.ShowAsContext();
        }

        private void AddLayerMask(object index)
        {
            _contextMasks.Add(new PrefabScatter.ScatterLayerMask { layerIndex = (int)index });
            EditorUtility.SetDirty(_scatter);
        }

        private TerrainLayer[] GetTerrainLayers()
        {
            if (_scatter.terrains.Count > 0 && _scatter.terrains[0]) return _scatter.terrains[0].terrainData.terrainLayers;
            return System.Array.Empty<TerrainLayer>();
        }

        private static void DrawMinMax(string label, ref RangedFloat value, float min, float max)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                value.minValue = EditorGUILayout.FloatField(value.minValue, GUILayout.Width(45f));
                EditorGUILayout.MinMaxSlider(ref value.minValue, ref value.maxValue, min, max);
                value.maxValue = EditorGUILayout.FloatField(value.maxValue, GUILayout.Width(45f));
            }
        }
    }
}
