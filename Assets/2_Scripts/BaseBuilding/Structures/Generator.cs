using System;
using System.Collections;
using DNExtensions.Button;
using PrimeTween;
using UnityEngine;

public class Generator : Structure
{

    [Header("Generator Settings")]
    [SerializeField] private ShakeSettings pumpAnimationSettings;
    [SerializeField] private Transform[] pumpArray = Array.Empty<Transform>();


    private Sequence _pumpAnimation;


    [Button]
    private void StartPumping()
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
    
    protected override void OnBuild()
    {
        StartPumping();
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        _pumpAnimation.Stop();
        Destroy(gameObject);
    }
}
