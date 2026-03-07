using DNExtensions.Utilities;
using UnityEngine;




public class Base : ResourceGenerator
{
    
    [Header("Base")]
    [SerializeField] private TransformEffector obelisk;
    
    
    private void Update()
    {
        StateInfo = $"Health: {CurrentHealth}/{MaxHealth}";
    }
    

    protected override void OnBuild()
    {
        StartGenerating();
    }

    protected override void OnFix()
    {
        StartGenerating();
        obelisk.enabled = true;
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        obelisk.enabled = false;
        StopGenerating();
        LevelManager.Instance?.FailLevel();
    }
}
