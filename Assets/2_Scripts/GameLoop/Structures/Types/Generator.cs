using System;
using PrimeTween;
using UnityEngine;


public class Generator : ResourceGenerator
{

    [Header("Generator")]
    [SerializeField] private ShakeSettings pumpAnimationSettings;
    [SerializeField] private Transform[] pumpArray = Array.Empty<Transform>();
    private Sequence _pumpAnimation;


    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth}/{MaxHealth}";
    }
    
    protected override void OnBuild()
    {
        StartPumpingAnimation();
        StartGenerating();
    }

    protected override void OnFix()
    {
        StartPumpingAnimation();
        StartGenerating();
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        _pumpAnimation.Stop();
        StopGenerating();
        Destroy(gameObject);
    }
    
    private void StartPumpingAnimation()
    {
        if (_pumpAnimation.isAlive)
        {
            _pumpAnimation.Stop();
        }
        
        _pumpAnimation = Sequence.Create(cycles: -1);

        foreach (var pump in pumpArray)
        {
            _pumpAnimation.Chain(Tween.PunchScale(pump, pumpAnimationSettings));
        }
    }
}
