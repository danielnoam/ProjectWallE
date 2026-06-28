using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace ProjectWallE.UI
{
    public class ActionIcon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minScale = 0.75f;
        [SerializeField] private float punchStrength = 0.2f;
        [SerializeField, Min(0f)] private float punchDuration = 0.3f;
        [SerializeField] private Color punchColor = Color.white;

        [Header("References")]
        [SerializeField] private Image icon;

        private Tween _punchTween;
        private Tween _colorPunchTween;
        private Color _baseIconColor;
        private bool _hasBaseIconColor;

        private void Awake()
        {
            if (icon)
            {
                _baseIconColor = icon.color;
                _hasBaseIconColor = true;
            }
        }

        private void OnDestroy()
        {
            if (_punchTween.isAlive) _punchTween.Stop();
            if (_colorPunchTween.isAlive) _colorPunchTween.Stop();
        }

        public void UpdateCooldown(float currentCooldown, float maxCooldown)
        {
            if (!icon || maxCooldown <= 0) return;

            float normalized = 1f - (currentCooldown / maxCooldown);
            bool ready = Mathf.Approximately(normalized, 1f);

            if (ready)
            {
                icon.color = icon.color.SetAlpha(normalized);
                PunchIcon();
            }
            else
            {
                if (_colorPunchTween.isAlive) _colorPunchTween.Stop();
                Color baseColor = _hasBaseIconColor ? _baseIconColor : icon.color;
                icon.color = baseColor.SetAlpha(normalized);
                float scale = Mathf.Lerp(minScale, 1f, normalized);
                icon.transform.localScale = Vector3.one * scale;
            }
        }

        public void PunchIcon(bool flashColor = false)
        {
            Transform punchTarget = flashColor ? transform : icon.transform;
            punchTarget.localScale = Vector3.one;
            if (_punchTween.isAlive) _punchTween.Stop();
            if (punchDuration > 0) _punchTween = Tween.PunchScale(punchTarget, Vector3.one * punchStrength, duration: punchDuration);

            if (flashColor && icon && _hasBaseIconColor && punchDuration > 0)
            {
                if (_colorPunchTween.isAlive) _colorPunchTween.Stop();
                icon.color = punchColor.SetAlpha(icon.color.a);
                _colorPunchTween = Tween.Color(icon, _baseIconColor, duration: punchDuration);
            }
        }
    }
}