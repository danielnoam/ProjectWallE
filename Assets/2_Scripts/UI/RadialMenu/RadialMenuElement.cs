using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class RadialMenuElement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;

        private Color _normalColor = Color.white;
        private Color _hoveredColor = Color.yellow;
        private readonly Color _disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        private bool _isHovered;
        private bool _isDisabled;

        public string Info { get; private set; }

        public void SetUp(Color normalColor, Color hoveredColor)
        {
            _normalColor = normalColor;
            _hoveredColor = hoveredColor;
            if (backgroundImage) backgroundImage.color = normalColor;
        }

        public void Configure(string info, Sprite icon, bool isAvailable)
        {
            Info = info;
            SetDisabled(!isAvailable);

            if (icon)
            {
                iconImage.sprite = icon;
            }
            else if (iconImage)
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        public void SetHovered()
        {
            if (_isHovered) return;
            _isHovered = true;
            if (backgroundImage) backgroundImage.color = _isDisabled ? _disabledColor : _hoveredColor;
        }

        public void SetNormal()
        {
            if (!_isHovered) return;
            _isHovered = false;
            if (backgroundImage) backgroundImage.color = _isDisabled ? _disabledColor : _normalColor;
        }

        private void SetDisabled(bool disabled)
        {
            _isDisabled = disabled;
            if (backgroundImage) backgroundImage.color = _isDisabled ? _disabledColor : _normalColor;
        }
    }
}