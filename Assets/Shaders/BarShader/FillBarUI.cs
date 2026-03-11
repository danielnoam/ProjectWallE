using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using DNExtensions.Utilities.Button;

public class FillBarUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trailDelay = 0.1f;
    [SerializeField] private float valueChangeDuration = 0.25f;
    [SerializeField] private float trailChangeDuration = 2f;
    [SerializeField] private float maxValue = 100;
    
    [Header("References")]
    [SerializeField] private Image barFill;
    [SerializeField] private Image trailFill;
    
    
    private float _currentValue;
    private Sequence _sequence;

    private void Awake()
    {
        _currentValue = maxValue;
        if (barFill) barFill.fillAmount = 1;
        if (trailFill) trailFill.fillAmount = 1;
    }


    [Button]
    private void DrainBar()
    {
        if (_currentValue <= 0 || !barFill || !trailFill) return;
        _sequence.Stop();
        
        _currentValue -= 10;
        float targetFillAmount = _currentValue / maxValue;

        _sequence = Sequence.Create(useUnscaledTime: true, sequenceEase: Ease.InOutSine)
            .Group(Tween.UIFillAmount(barFill, targetFillAmount, duration: valueChangeDuration))
            .ChainDelay(trailDelay)
            .Chain(Tween.UIFillAmount(trailFill, targetFillAmount, duration: trailChangeDuration));
    }
    
    [Button]
    private void FillBar()
    {
        if (_currentValue >= maxValue || !barFill || !trailFill) return;
        _sequence.Stop();
        
        _currentValue += 10;
        float targetFillAmount = _currentValue / maxValue;

        _sequence = Sequence.Create(useUnscaledTime: true, sequenceEase: Ease.InOutSine)
            .Group(Tween.UIFillAmount(trailFill, targetFillAmount, duration: valueChangeDuration))
            .ChainDelay(trailDelay)
            .Chain(Tween.UIFillAmount(barFill, targetFillAmount, duration: trailChangeDuration));
    }
}
