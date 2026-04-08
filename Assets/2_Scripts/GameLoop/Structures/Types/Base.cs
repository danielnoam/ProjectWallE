using DNExtensions.Utilities;
using UnityEngine;

public class Base : ResourceGenerator
{
    [Header("Base")]
    [SerializeField] private TransformEffector[] transformEffectors;
    [SerializeField] private Transform crystalsTransform;
    
    
    protected override void OnFix()
    {
        StartGenerating();
        foreach (var transformEffector in transformEffectors)
        {
            if (transformEffector) transformEffector.enabled = true;
            crystalsTransform?.gameObject.SetActive(true);
        }
    }

    protected override void OnUpgrade()
    {

    }
    protected override void OnBreak()
    {
        foreach (var transformEffector in transformEffectors)
        {
            if (transformEffector) transformEffector.enabled = false;
            crystalsTransform?.gameObject.SetActive(false);
        }
        StopGenerating();
    }
}
