using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;

public class FullscreenVignetteController : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private static readonly int Visibility = Shader.PropertyToID("_Visibility");
    private Tween _tween;

    [Button]
    public void Show() => Animate(1f);
    
    [Button]
    public void Hide() => Animate(0f);


    private void Awake()
    {
        material.SetFloat(Visibility, 0f);
    }
    
    private void OnDestroy()
    {
        if (_tween.isAlive) _tween.Stop();
        material.SetFloat(Visibility, 0f);
    }
    

    private void Animate(float target)
    {
        if (_tween.isAlive) _tween.Stop();
        _tween = Tween.MaterialProperty(material, Visibility, target, duration, ease);
    }


}