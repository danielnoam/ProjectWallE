using System;
using System.Collections.Generic;
using DNExtensions;
using UnityEngine;

public class RadialMenu<T> : MonoBehaviour where T : class
{
    [Header("Menu Settings")]
    public float elementFillPadding = 0.02f;
    public Color hoveredColor = Color.yellow;
    public Color normalColor = Color.white;
    public RadialMenuElement elementPrefab;
    public CanvasGroup canvasGroup;
    
    [Header("Input Settings")]
    public float movementThreshold = 0.1f;
    [SerializeField, ReadOnly, Preview] public Vector2 movementDirection;
    
    private readonly Dictionary<RadialMenuElement, T> _elementToItem = new Dictionary<RadialMenuElement, T>();
    private readonly List<RadialMenuElement> _menuElements = new List<RadialMenuElement>();
    private RadialMenuElement _hoveredElement;
    private bool _open;
    
    public event Action<T> OnItemSelected;
    
    private void Awake()
    {
        CloseMenu();
    }

    private void Update()
    {
        if (_open)
        {
            SelectionInputHandling();
        }
    }
    
    private void OnDestroy()
    {
        ClearMenu();
    }
    
    
    private void SelectionInputHandling()
    {
        if (_menuElements.Count == 0) return;
        
        Vector2 mouseDelta = Input.mousePositionDelta;
        
        if (mouseDelta.magnitude > movementThreshold)
        {
            movementDirection = mouseDelta.normalized;
        }
        
        if (movementDirection.magnitude < 0.1f)
        {
            UnhoverElement();
            return;
        }
        
        float angle = Mathf.Atan2(movementDirection.x, movementDirection.y) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        float segmentSize = 360f / _menuElements.Count;
        int elementIndex = Mathf.FloorToInt(angle / segmentSize);
        
        
        HoverElement(elementIndex);
        
        if (Input.GetMouseButtonDown(0) && _hoveredElement)
        {
            _hoveredElement.Select();
        }
    }

    private void HoverElement(int index)
    {
        if (index < 0 || index >= _menuElements.Count) return;
        
        RadialMenuElement element = _menuElements[index];
        
        if (_hoveredElement == element) return;
        
        UnhoverElement();
        
        _hoveredElement = element;
        _hoveredElement.SetHovered();
    }

    private void UnhoverElement()
    {
        if (!_hoveredElement) return;
        
        _hoveredElement.SetNormal();
        _hoveredElement = null;
    }

    private void HandleElementSelected(RadialMenuElement element)
    {
        if (!_elementToItem.TryGetValue(element, out T item)) return;
        
        OnItemSelected?.Invoke(item);
        CloseMenu();
    }
    
    public void SetupMenu(T[] items, Action<RadialMenuElement, T> configureElement)
    {
        ClearMenu();
    
        for (int i = 0; i < items.Length; i++)
        {
            RadialMenuElement newElement = Instantiate(elementPrefab, transform);
            newElement.normalColor = normalColor;
            newElement.hoveredColor = hoveredColor;
        
            float fillAmount = 1f / items.Length - elementFillPadding;
            newElement.backgroundImage.fillAmount = fillAmount;
            newElement.backgroundImage.color = normalColor;
        
            float angle = 360f / items.Length * i;
            newElement.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            newElement.text.transform.localRotation = Quaternion.Euler(0, 0, angle);
            newElement.iconImage.transform.localRotation = Quaternion.Euler(0, 0, angle);
            
            newElement.PositionUIElements(fillAmount);
            configureElement?.Invoke(newElement, items[i]);
        
            newElement.OnSelect += HandleElementSelected;
        
            _elementToItem[newElement] = items[i];
            _menuElements.Add(newElement);
        }
    }
    
    private void ClearMenu()
    {
        foreach (var element in _menuElements)
        {
            if (element)
            {
                element.OnSelect -= HandleElementSelected;
                Destroy(element.gameObject);
            }
        }
        
        _menuElements.Clear();
        _elementToItem.Clear();
    }
    
    public void CloseMenu()
    {
        UnhoverElement();
        movementDirection = Vector2.zero;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        _open = false;
    }
    
    public void OpenMenu() 
    {
        movementDirection = Vector2.zero;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        _open = true;
    }
}