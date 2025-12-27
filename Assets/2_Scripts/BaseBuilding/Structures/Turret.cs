using System;
using System.Collections;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

public class Turret : Structure
{

    [Header("Turret Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float waitDuration = 1.5f;
    [SerializeField] private Transform headTransform;
    [SerializeField, ReadOnly] private TurretState state = TurretState.Idle;
    


    private Coroutine _scanCoroutine;
    private enum TurretState { Scanning, Idle }



    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void Idle()
    {
        state = TurretState.Idle;
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
            _scanCoroutine = null;
        }
    }
    

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    private void StartScanning()
    {
        state = TurretState.Scanning;
        if (_scanCoroutine != null)
        {
            StopCoroutine(_scanCoroutine);
        }
        _scanCoroutine = StartCoroutine(ScanningRoutine());
    }

    private IEnumerator ScanningRoutine()
    {
        float[] scanAngles = { -90, -45f, 0f, 45f, 90, 0f };
        int currentAngleIndex = 0;

        while (state == TurretState.Scanning)
        {
            Quaternion targetRotation = Quaternion.Euler(0, scanAngles[currentAngleIndex], 0);
            
            while (Quaternion.Angle(headTransform.localRotation, targetRotation) > 0.1f)
            {
                headTransform.localRotation = Quaternion.RotateTowards(
                    headTransform.localRotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }
            yield return new WaitForSeconds(waitDuration);
            
            currentAngleIndex = (currentAngleIndex + 1) % scanAngles.Length;
        }
    }
    
    

    protected override void OnBuild()
    {
        StartScanning();
    }
    

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        Idle();
    }
}
