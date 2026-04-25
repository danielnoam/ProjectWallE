using DNExtensions.Systems.Scriptables;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class GhostStructure : MonoBehaviour
    {
        [SerializeField] private SOColorHDR canBuildColor;
        [SerializeField] private SOColorHDR blockBuildColor;

        private static readonly int ScanlinesColor = Shader.PropertyToID("_Scanlines_Color");
        private Material[] _materials;

        private void Awake()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            _materials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                _materials[i] = renderers[i].material;
        }

        public void SetCanBuild(bool canBuild)
        {
            if (_materials == null) return;
            var color = canBuild ? canBuildColor.Value : blockBuildColor.Value;
            foreach (var mat in _materials)
                mat.SetVector(ScanlinesColor, color);
        }
    }
}