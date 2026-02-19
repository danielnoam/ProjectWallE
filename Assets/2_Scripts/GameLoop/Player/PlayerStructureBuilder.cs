using DNExtensions.Utilities;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.InputSystem;


[SelectionBase]
public class PlayerStructureBuilder : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private bool holdToOpen;
    [SerializeField] private float buildRange = 100f;
    [SerializeField] private LayerMask buildableLayerMask;
    [SerializeField] private LayerMask blockBuildLayerMask;
    
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private RadialMenu<Structure> radialMenu;
    [SerializeField, PrefabSelector("Assets/Prefabs/Structures")] private Structure[] structuresArray;

    private Ray _buildRay;
    private bool _canBuild;
    private bool _buildMenuOpen;
    
    
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
        if (holdToOpen)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                radialMenu.OpenMenu();
                _buildMenuOpen = true;
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                radialMenu.CloseMenu();
                _buildMenuOpen = false;
                BuildPrompt.Instance?.Hide();
            }
        }
        else
        {
            switch (Mouse.current.rightButton.wasPressedThisFrame)
            {
                case true when !radialMenu.IsOpen:
                    radialMenu.OpenMenu();
                    _buildMenuOpen = true;
                    break;
                case true when radialMenu.IsOpen:
                    radialMenu.CloseMenu();
                    _buildMenuOpen = false;
                    BuildPrompt.Instance?.Hide();
                    break;
            }
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
            _canBuild = false;
            BuildPrompt.Instance?.Hide();
        }
        else if (Physics.Raycast(_buildRay, out RaycastHit hit, buildRange, buildableLayerMask))
        {
            lineRenderer.SetPosition(1, hit.point);
            _canBuild = true;
            if (_buildMenuOpen) BuildPrompt.Instance?.Show(hit.point);
        }
        else
        {
            lineRenderer.SetPosition(1, transform.position + _buildRay.direction * buildRange);
            _canBuild = false;
            BuildPrompt.Instance?.Hide();
        }
    }
    
    

    private void ConfigureStructureElement(RadialMenuElement element, Structure structure)
    {
        element.elementInfo = $"{structure.Label}\n Cost: {structure.BuildCost}";

        if (structure.Icon)
        {
            element.iconImage.sprite = structure.Icon;
        }
        else
        {
            element.iconImage.gameObject.SetActive(false);
        }

    }
    

    private void TryBuildStructure(Structure structure)
    {
        if (!_canBuild)
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

        StructureManager.Instance.DeployPod(structure, hit.point, hit.normal, transform.forward);
    }


}