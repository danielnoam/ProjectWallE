using DNExtensions.Systems.Scriptables;
using PrimeTween;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class GhostStructure : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private SOColor canBuildColor;
        [SerializeField] private SOColor blockBuildColor;

        private const float ConfirmDuration = 0.4f;
        private const float PunchStrength = 1.2f;
        private const Ease PunchEase = Ease.OutQuad;

        private static readonly int ScanlinesColor = Shader.PropertyToID("_Scanlines_Color");
        private Material[] _materials;
        private Sequence _confirmSequence;

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
            if (_confirmSequence.isAlive) _confirmSequence.Stop();
            var color = canBuild ? canBuildColor.Value : blockBuildColor.Value;
            foreach (var mat in _materials)
                mat.SetVector(ScanlinesColor, color);
        }

        public void PlayConfirmAndDisable()
        {
            _confirmSequence.Stop();
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;

            _confirmSequence = Sequence.Create()
                .Group(Tween.Scale(transform, Vector3.one * PunchStrength, ConfirmDuration * 0.5f, PunchEase))
                .Chain(Tween.Scale(transform, Vector3.zero, ConfirmDuration * 0.5f, Ease.InQuad))
                .ChainCallback(this, target => target.gameObject.SetActive(false));
        }
    }
}