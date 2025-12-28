using System;
using UnityEngine;

public class Base : Structure
{

    [Header("Base Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Transform obelisk;


    
    
    private void Update()
    {
        obelisk.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float sine = Mathf.Sin(Time.time * 2f) * moveSpeed;
        obelisk.localPosition = new Vector3(0f, sine, 0f);
    }


    protected override void OnBuild()
    {

    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        Debug.Log("Game Lost");
    }
}
