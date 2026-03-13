using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;

public class MaterialPropertyTweener : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private string propertyName = "_Visibility";
    [SerializeField] private float showValue = 1f;
    [SerializeField] private float hideValue;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private int _propertyId;
    private Tween _tween;

    [Button] public void Show() => Animate(showValue);
    [Button] public void Hide() => Animate(hideValue);

    private void Awake()
    {
        _propertyId = Shader.PropertyToID(propertyName);
        material.SetFloat(_propertyId, hideValue);
    }

    private void OnDestroy()
    {
        if (_tween.isAlive) _tween.Stop();
        material.SetFloat(_propertyId, hideValue);
    }

    private void Animate(float target)
    {
        if (_tween.isAlive) _tween.Stop();
        _tween = Tween.MaterialProperty(material, _propertyId, target, duration, ease);
    }
}