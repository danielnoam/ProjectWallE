using System;
using DNExtensions.Button;
using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(ResourceGenerator))]
public class Generator : Structure
{

    [Header("Generator Settings")]
    [SerializeField] private ShakeSettings pumpAnimationSettings;
    [SerializeField] private Transform[] pumpArray = Array.Empty<Transform>();
    [SerializeField] private ResourceGenerator resourceGenerator;

    private Sequence _pumpAnimation;


    private void Update()
    {
        StateInfo = $"Health: {currentHealth}/{startHealth}";
    }

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
        resourceGenerator.StartGenerating();
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        _pumpAnimation.Stop();
        resourceGenerator.StopGenerating();
        Destroy(gameObject);
    }
}
