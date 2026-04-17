using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public sealed class UIRenderTextureScaler : MonoBehaviour
    {
        
        [Header("Settings")]
        [SerializeField] private Vector2 minTextureSize = new Vector2(1920, 1080);
        
        [Header("References")]
        [SerializeField] private Camera uiCamera;
        [SerializeField] private RawImage displayImage;

        private RenderTexture _renderTexture;
        private int _lastWidth;
        private int _lastHeight;

        private void OnEnable()
        {
            RebuildRenderTexture();
        }

        private void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;

            RebuildRenderTexture();
        }

        private void OnDisable()
        {
            ReleaseRenderTexture();
        }

        private void RebuildRenderTexture()
        {
            ReleaseRenderTexture();

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;


            _renderTexture = new RenderTexture(_lastWidth, _lastHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "UIRenderTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _renderTexture.Create();

            if (uiCamera) uiCamera.targetTexture = _renderTexture;

            if (displayImage) displayImage.texture = _renderTexture;
        }

        private void ReleaseRenderTexture()
        {
            if (!_renderTexture)
                return;

            if (uiCamera && uiCamera.targetTexture == _renderTexture)
                uiCamera.targetTexture = null;

            if (displayImage && displayImage.texture == _renderTexture)
                displayImage.texture = null;

            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}