
using DNExtensions.Utilities.CustomFields;
using UnityEngine;


public class Generator : ResourceGenerator
{

    [Header("Animation")]
    [SerializeField] private AnimatorStateField pumpingState;
    [SerializeField] private AnimatorStateField idleState;
    


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
