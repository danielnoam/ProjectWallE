using DNExtensions;
using UnityEngine;

public class PlayerStructureBuilder : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private float buildRange = 100f;
    [SerializeField] private LayerMask buildableLayerMask;
    [SerializeField] private LayerMask blockBuildLayerMask;
    [SerializeField] private KeyCode buildMenuKey = KeyCode.Mouse1;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private RadialMenu<Structure> radialMenu;
    [SerializeField] private Structure[] structuresArray;

    private Ray _buildRay;
    public bool CanBuild { get; private set; }
    
    
    private void OnValidate()
    {
        if (!mainCamera) mainCamera = Camera.main;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }

    private void Awake()
    {
        radialMenu.SetupMenu(structuresArray, ConfigureStructureElement);
        radialMenu.OnItemSelected += TryBuildStructure;
    }
    
    private void OnDestroy()
    {
        radialMenu.OnItemSelected -= TryBuildStructure;
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
        
        CastBuildRay();
    }

    private void CastBuildRay()
    {
        lineRenderer.SetPosition(0, transform.position.RemoveY(0.5f));
        
        _buildRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(_buildRay, buildRange, blockBuildLayerMask))
        {
            lineRenderer.SetPosition(1, transform.position + _buildRay.direction * buildRange);
            CanBuild = false;
        }
        else if (Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask))
        {
            lineRenderer.SetPosition(1, hit.point);
            CanBuild = true;
        }
        else
        {
            lineRenderer.SetPosition(1, transform.position + _buildRay.direction * buildRange);
            CanBuild = false;
        }
    }
    
    

    private void ConfigureStructureElement(RadialMenuElement element, Structure structure)
    {
        element.text.text = $"{structure.Label}\n Cost: {structure.BuildCost}";
        element.iconImage.sprite = structure.Icon;
    }
    

    private void TryBuildStructure(Structure structure)
    {
        if (!CanBuild)
        {
            Debug.Log("Cannot build here");
            return;
        }
        
        if (!Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask))
        {
            Debug.Log("No valid build surface found");
            return;
        }
        
        if (!ResourceManager.Instance.TrySpendResources(structure.BuildCost))
        {
            Debug.Log($"Not enough resources to build {structure.Label}");
            return;
        }

        StructureManager.Instance.DeployPod(structure, hit.point, hit.normal);
    }


}