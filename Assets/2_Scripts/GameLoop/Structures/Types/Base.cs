using System;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;




public class Base : ResourceGenerator
{
    
    [Header("Base")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSpeed = 0.3f;
    [SerializeField] private Transform obelisk;
    
    
    private void Update()
    {
        obelisk.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float sine = Mathf.Sin(Time.time * 2f) * moveSpeed;
        obelisk.localPosition = new Vector3(0f, sine, 0f);
        
        StateInfo = $"Health: {CurrentHealth}/{MaxHealth}";
    }
    

    protected override void OnBuild()
    {
        StartGenerating();
    }

    protected override void OnFix()
    {
        StartGenerating();
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        StopGenerating();
        LevelManager.Instance?.FailLevel();
    }
}
