using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using ProjectWallE.UI;
using UnityEngine;
using UnityEngine.InputSystem;


[SelectionBase]
public class PlayerStructureBuilder : MonoBehaviour
{
    [Header("Build Settings")]
    [SerializeField] private bool holdToOpen = true;
    [SerializeField] private float buildRange = 100f;
    [SerializeField] private LayerMask buildableLayerMask;
    [SerializeField] private LayerMask blockBuildLayerMask;
    
    [Header("References")]
    [SerializeField, AutoGetSelf] private LineRenderer lineRenderer;
    [SerializeField, AutoGetScene] private Camera mainCamera;
    [SerializeField, AutoGetScene] private RadialMenu<Structure> radialMenu;
    [SerializeField, AutoGetSelf] private FreeFormCameraController cameraController;
    [SerializeField, PrefabSelector("Assets/Prefabs/Structures")] private Structure[] structuresArray;

    private Ray _buildRay;
    private bool _canBuild;
    private bool _buildMenuOpen;
    
    
    private void OnValidate()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position);
    }

    private void Awake()
    {
        radialMenu.SetupMenu(structuresArray, ConfigureStructureElement);
        radialMenu.OnItemSelected += TryBuildStructure;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
                OpenMenu();
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                CloseMenu();
            }
        }
        else
        {
            switch (Mouse.current.rightButton.wasPressedThisFrame)
            {
                case true when !radialMenu.IsOpen:
                    OpenMenu();
                    break;
                case true when radialMenu.IsOpen:
                    CloseMenu();
                    break;
            }
        }

        CastBuildRay();
    }
    
    private void OpenMenu()
    {
        cameraController.enabled = false;
        radialMenu.OpenMenu();
        
        _buildMenuOpen = true;
    }
    
    private void CloseMenu()
    {
        cameraController.enabled = true;
        radialMenu.CloseMenu();
        BuildPrompt.Instance?.Hide();
        
        _buildMenuOpen = false;
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

        StructureManager.Instance?.DeployPod(structure, hit.point, transform.forward);
    }


}