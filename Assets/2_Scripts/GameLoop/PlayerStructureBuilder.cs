using UnityEngine;

public class PlayerStructureBuilder : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private float buildRange = 100f;
    [SerializeField] private LayerMask buildableLayerMask;
    [SerializeField] private KeyCode buildMenuKey = KeyCode.Mouse1;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RadialMenu<Structure> radialMenu;
    [SerializeField] private Structure[] structuresArray;

    private void OnValidate()
    {
        if (!mainCamera) mainCamera = Camera.main;
    }

    private void Awake()
    {
        radialMenu.SetupMenu(structuresArray, ConfigureStructureElement);
        radialMenu.OnItemSelected += OnStructureSelected;
    }

    private void Update()
    {
        if (Input.GetKeyDown(buildMenuKey))
        {
            radialMenu.OpenMenu();
        }
        else if (Input.GetKeyUp(buildMenuKey))
        {
            radialMenu.CloseMenu();
        }
    }

    private void ConfigureStructureElement(RadialMenuElement element, Structure structure)
    {
        element.text.text = $"{structure.Label}\n Cost: {structure.BuildCost}";
        element.iconImage.sprite = structure.Icon;
    }

    private void OnStructureSelected(Structure structure)
    {
        if (!TryBuildStructure(structure))
        {
            Debug.Log($"Cannot build {structure.Label}");
        }
    }

    private bool TryBuildStructure(Structure structure)
    {
        if (!ResourceManager.Instance.TrySpendResources(structure.BuildCost))
        {
            Debug.Log($"Not enough resources to build {structure.Label}");
            return false;
        }
        
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (Physics.Raycast(ray, out RaycastHit hit, buildRange, buildableLayerMask))
        {
            StructureDispatcher.Instance.DeployPod(structure, hit.point, hit.normal);
            return true;
        }
        
        Debug.Log("No valid build surface found");
        return false;
    }

    private void OnDestroy()
    {
        radialMenu.OnItemSelected -= OnStructureSelected;
    }
}