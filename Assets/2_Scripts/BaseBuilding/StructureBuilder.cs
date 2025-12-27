using System;
using TMPro;
using UnityEngine;

public class StructureBuilder : MonoBehaviour
{
    public static StructureBuilder Instance { get; private set; }
    
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upwardForce;
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode nextStructureKey = KeyCode.E;
    [SerializeField] private KeyCode previousStructureKey = KeyCode.Q;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI structureNameText;
    [SerializeField] private StructureBeacon beaconPrefab;
    [SerializeField] private StructurePod podPrefab;
    [SerializeField] private Transform podSpawnPosition;
    [SerializeField] private Structure[] structuresArray;
    
    private int _selectedStructureIndex;

    private void OnValidate()
    {
        if (!mainCamera) mainCamera = Camera.main;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void Start()
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
        if (structuresArray.Length == 0) { return; }
        
        var newIndex = (int)Mathf.Repeat(_selectedStructureIndex + direction, structuresArray.Length);
        SetCurrentStructure(newIndex);
    }
    
    private void SetCurrentStructure(int index)
    {
        if (structuresArray.Length == 0) { return; }

        _selectedStructureIndex = index;
        structureNameText.text = $"Current Structure: {structuresArray[_selectedStructureIndex].GetType().Name}";
    }

    private void ThrowBeacon()
    {
        if (structuresArray.Length == 0) { return; }

        StructureBeacon beacon = Instantiate(beaconPrefab, mainCamera.transform.position, Quaternion.identity);
        beacon.SetStructure(structuresArray[_selectedStructureIndex]);
        
        Vector3 throwDirection = mainCamera.transform.forward + Vector3.up * upwardForce;
        beacon.Rigidbody.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);
    }
    
    public void CallStructurePod(Structure structure, Vector3 impactPoint, Vector3 surfaceNormal)
    {
        StructurePod pod = Instantiate(podPrefab, podSpawnPosition.position, Quaternion.LookRotation(podSpawnPosition.forward));
        pod.Initialize(structure, impactPoint, surfaceNormal);
    }
}