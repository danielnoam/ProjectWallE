
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
        pumpingState.Play();
    }

    protected override void OnFix()
    {
        idleState.Play();
        StartGenerating();
    }

    protected override void OnBreak()
    {
        idleState.Play();
        StopGenerating();
    }
}
