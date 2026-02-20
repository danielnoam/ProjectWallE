using System.Collections;
using DNExtensions.Utilities;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int resourcesPerInterval = 10;
    [SerializeField] private float generationInterval = 5f;
    [SerializeField, ReadOnly] private bool generating;
    
    
    private Coroutine _generationCoroutine;


    public void StartGenerating()
    {
        if (_generationCoroutine != null) return;
        _generationCoroutine = StartCoroutine(GenerateResources());
    }


    public void StopGenerating()
    {
        generating = false;
        
        if (_generationCoroutine != null)
        {
            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
        }
    }

    private IEnumerator GenerateResources()
    {
        generating = true;
        while (generating)
        {
            yield return new WaitForSeconds(generationInterval);
            ResourceManager.Instance.AddResources(resourcesPerInterval);
        }
    }

    private void OnDestroy()
    {
        StopGenerating();
    }
}