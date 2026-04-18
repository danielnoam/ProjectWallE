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

        [Header("References")]
        [SerializeField] private Image icon;

        private Tween _punchTween;

        private void OnDestroy()
        {
            if (_punchTween.isAlive) _punchTween.Stop();
        }

        public void UpdateCooldown(float currentCooldown, float maxCooldown)
        {
            if (!icon || maxCooldown <= 0) return;

            float normalized = 1f - (currentCooldown / maxCooldown);
            bool ready = Mathf.Approximately(normalized, 1f);

            icon.color = icon.color.SetAlpha(normalized);

            if (ready)
            {
                PunchIcon();
            }
            else
            {
                float scale = Mathf.Lerp(minScale, 1f, normalized);
                icon.transform.localScale = Vector3.one * scale;
            }
        }

        public void PunchIcon()
        {
            icon.transform.localScale = Vector3.one;
            if (_punchTween.isAlive) _punchTween.Stop();
            if (punchDuration > 0) _punchTween = Tween.PunchScale(icon.transform, Vector3.one * punchStrength, duration: punchDuration);
        }
    }
}