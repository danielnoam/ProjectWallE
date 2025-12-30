using System;
using UnityEngine;

public class BeaconThrower : MonoBehaviour
{

    
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upwardForce;
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode nextStructureKey = KeyCode.E;
    [SerializeField] private KeyCode previousStructureKey = KeyCode.Q;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private StructureBeacon beaconPrefab;
    [SerializeField] private Structure[] structuresArray;
    
    private int _selectedStructureIndex;
    private Structure _currentStructure;
    
    public event Action<Structure> OnStructureChanged;
    public Structure CurrentStructure => _currentStructure;
    
    

    private void OnValidate()
    {
        if (!mainCamera) mainCamera = Camera.main;
    }

    private void Awake()
    {
        SetCurrentStructure(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(throwKey))
        {
            ThrowBeacon();
        }

        if (Input.GetKeyDown(nextStructureKey))
        {
            CycleStructure(1);
        }

        if (Input.GetKeyDown(previousStructureKey))
        {
            CycleStructure(-1);
        }
    }
    
    private void CycleStructure(int direction)
    {
        if (structuresArray.Length == 0) return;
        
        var newIndex = (int)Mathf.Repeat(_selectedStructureIndex + direction, structuresArray.Length);
        SetCurrentStructure(newIndex);
    }
    
    private void SetCurrentStructure(int index)
    {
        if (structuresArray.Length == 0) return;

        _selectedStructureIndex = index;
        _currentStructure = structuresArray[_selectedStructureIndex];
        OnStructureChanged?.Invoke(_currentStructure);
    }

    private void ThrowBeacon()
    {
        if (structuresArray.Length == 0) return;
        
        if (!ResourceManager.Instance.TrySpendResources(_currentStructure.BuildCost))
        {
            Debug.Log("Not enough resources to throw beacon for " + _currentStructure.Label);
            return;
        }
        
        StructureBeacon beacon = Instantiate(beaconPrefab, mainCamera.transform.position, Quaternion.identity);
        beacon.SetStructure(_currentStructure);
        
        Vector3 throwDirection = mainCamera.transform.forward + Vector3.up * upwardForce;
        beacon.Rigidbody.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);
    }
}