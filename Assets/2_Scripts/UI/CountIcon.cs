using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class CountIcon : MonoBehaviour
    {
        [SerializeField] private float countDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image icon;

        private int _displayedCount;
        private Tween _countTween;

        private void OnDestroy()
        {
            if (_countTween.isAlive) _countTween.Stop();
        }

        public void SetCount(int count)
        {
            if (_countTween.isAlive) _countTween.Stop();
            int from = _displayedCount;
            _countTween = Tween.Custom(from, count, countDuration, value =>
            {
                _displayedCount = (int)value;
                if (countText) countText.text = $"{_displayedCount:N0}";
            });
        }

        public void SetCountImmediate(int count)
        {
            if (_countTween.isAlive) _countTween.Stop();
            _displayedCount = count;
            if (countText) countText.text = count.ToString();
        }
    }
}