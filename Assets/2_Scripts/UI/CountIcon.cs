using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWallE.UI
{
    public class CountIcon : MonoBehaviour
    {
        [Header("Count")]
        [SerializeField] private float countDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image icon;

        [Header("Color Feedback")]
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private float colorDuration = 0.4f;

        private int _displayedCount;
        private bool _hasGoal;
        private int _goal;
        private Tween _countTween;
        private Sequence _colorSequence;

        private string FormatCount(int count) => _hasGoal ? $"{count:N0}/{_goal:N0}" : $"{count:N0}";

        private void OnDestroy()
        {
            if (_countTween.isAlive) _countTween.Stop();
            if (_colorSequence.isAlive) _colorSequence.Stop();
        }
        
        private void PunchColor(Color color)
        {
            if (!countText) return;
            if (_colorSequence.isAlive) _colorSequence.Stop();
            countText.color = color;
            _colorSequence = Sequence.Create(useUnscaledTime: true)
                .ChainDelay(countDuration)
                .Chain(Tween.Color(countText, Color.white, colorDuration));
        }

        public void SetCount(int count)
        {
            if (_countTween.isAlive) _countTween.Stop();

            if (count != _displayedCount) PunchColor(count > _displayedCount ? increaseColor : decreaseColor);

            int from = _displayedCount;
            _countTween = Tween.Custom(from, count, countDuration, value =>
            {
                _displayedCount = (int)value;
                if (countText) countText.text = FormatCount(_displayedCount);
            }, useUnscaledTime: true);
        }

        public void SetGoal(int goal)
        {
            _hasGoal = true;
            _goal = goal;
            if (countText) countText.text = FormatCount(_displayedCount);
        }

        public void ClearGoal()
        {
            if (!_hasGoal) return;
            _hasGoal = false;
            if (countText) countText.text = FormatCount(_displayedCount);
        }

        public void SetCountImmediate(int count)
        {
            if (_countTween.isAlive) _countTween.Stop();
            if (_colorSequence.isAlive) _colorSequence.Stop();
            _displayedCount = count;
            if (countText)
            {
                countText.text = FormatCount(count);
                countText.color = Color.white;
            }
        }
    }
}