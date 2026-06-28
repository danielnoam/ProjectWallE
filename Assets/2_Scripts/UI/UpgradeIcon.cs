using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class UpgradeIcon : MonoBehaviour
    {
        [Header("Value")]
        [SerializeField] private float valueDuration = 0.3f;
        [SerializeField] private string format = "{0:0.00}";
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image icon;

        [Header("Color Feedback")]
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private float colorDuration = 0.4f;

        private float _displayedValue = 1f;
        private Tween _valueTween;
        private Sequence _colorSequence;

        private string FormatValue(float value) => string.Format(format, value);

        private void OnDestroy()
        {
            if (_valueTween.isAlive) _valueTween.Stop();
            if (_colorSequence.isAlive) _colorSequence.Stop();
        }

        private void PunchColor(Color color)
        {
            if (!valueText) return;
            if (_colorSequence.isAlive) _colorSequence.Stop();
            valueText.color = color;
            _colorSequence = Sequence.Create(useUnscaledTime: true)
                .ChainDelay(valueDuration)
                .Chain(Tween.Color(valueText, Color.white, colorDuration));
        }

        public void SetValue(float value)
        {
            if (_valueTween.isAlive) _valueTween.Stop();

            if (!Mathf.Approximately(value, _displayedValue))
                PunchColor(value > _displayedValue ? increaseColor : decreaseColor);

            float from = _displayedValue;
            _valueTween = Tween.Custom(from, value, valueDuration, v =>
            {
                _displayedValue = v;
                if (valueText) valueText.text = FormatValue(_displayedValue);
            }, useUnscaledTime: true);
        }

        public void SetValueImmediate(float value)
        {
            if (_valueTween.isAlive) _valueTween.Stop();
            if (_colorSequence.isAlive) _colorSequence.Stop();
            _displayedValue = value;
            if (valueText)
            {
                valueText.text = FormatValue(value);
                valueText.color = Color.white;
            }
        }
    }
}
