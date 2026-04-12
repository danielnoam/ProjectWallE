
using DNExtensions.Utilities.CustomFields;
using UnityEngine;


public class Generator : ResourceGenerator
{
    [Header("Animation")]
    [SerializeField] private SimpleAnimatorClipField pumpingState;
    [SerializeField] private SimpleAnimatorClipField idleState;
    
    protected override void OnBuild()
    {
        base.OnBuild();
        StartPumpingAnimation();
    }

    protected override void OnFix()
    {
        StartPumpingAnimation();
        StartGenerating();
    }

    protected override void OnBreak()
    {
        StopPumpingAnimation();
        StopGenerating();
    }
    
    private void StartPumpingAnimation()
    {
        pumpingState.Play();
    }
    
    private void StopPumpingAnimation()
    {
        idleState.Play();
    }
}
