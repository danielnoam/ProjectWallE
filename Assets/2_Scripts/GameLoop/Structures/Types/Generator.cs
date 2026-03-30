
using DNExtensions.Utilities.CustomFields;
using UnityEngine;


public class Generator : ResourceGenerator
{
    [Header("Animation")]
    [SerializeField] private AnimatorStateField pumpingState;
    [SerializeField] private AnimatorStateField idleState;
    
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

    protected override void OnBreak()
    {
        StopPumpingAnimation();
        StopGenerating();
    }
    
    private void StartPumpingAnimation()
    {
        pumpingState.Animator?.Play(pumpingState.StateName);
    }
    
    private void StopPumpingAnimation()
    {
        idleState.Animator?.Play(idleState.StateName);
    }
}
