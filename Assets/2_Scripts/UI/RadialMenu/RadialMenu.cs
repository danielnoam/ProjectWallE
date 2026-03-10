
using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RadialMenu<T> : MonoBehaviour where T : class
{
    [Header("Menu Settings")]
    public Color hoveredColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Input Settings")]
    public float selectionDeadzone = 50f;
    public float maxRadius = 150;
    [SerializeField, ReadOnly, Preview] private Vector2 mousePositionFromCenter;

    [Header("References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI selectedItemText;
    public RadialMenuElement elementPrefab;
    public Transform elementsContainer;

    private readonly Dictionary<RadialMenuElement, T> _elementToItem = new Dictionary<RadialMenuElement, T>();
    private readonly List<RadialMenuElement> _menuElements = new List<RadialMenuElement>();
    private RadialMenuElement _hoveredElement;
    private Vector2 _accumulatedMouseDelta;
    private int _currentSegmentIndex = -1;
    private bool _isOpen;
    

    public event Action<T> OnItemSelected;

    private void Awake()
    {
        CloseMenu();
    }

    private void Update()
    {
        if (_isOpen) SelectionInputHandling();
    }

    private void OnDestroy()
    {
        ClearMenu();
    }

    public void SetupMenu(T[] items, Action<RadialMenuElement, T> configureElement)
    {
        ClearMenu();

        foreach (var item in items)
        {
            RadialMenuElement newElement = Instantiate(elementPrefab, elementsContainer);
            newElement.SetUp(normalColor, hoveredColor);
            configureElement?.Invoke(newElement, item);
            newElement.OnSelect += OnElementSelected;
            _elementToItem[newElement] = item;
            _menuElements.Add(newElement);
        }
    }

    public void SetupMenu(List<T> items, Action<RadialMenuElement, T> configureElement)
    {
        SetupMenu(items.ToArray(), configureElement);
    }

    private void HoverElement(int index)
    {
        if (index < 0 || index >= _menuElements.Count) return;

        RadialMenuElement element = _menuElements[index];
        if (_hoveredElement == element) return;

        UnhoverElement();
        _hoveredElement = element;
        _hoveredElement.SetHovered();
        if (selectedItemText) selectedItemText.text = element.elementInfo;
    }

    private void UnhoverElement()
    {
        if (!_hoveredElement) return;
        if (selectedItemText) selectedItemText.text = "";
        _hoveredElement.SetNormal();
        _hoveredElement = null;
    }

    private void OnElementSelected(RadialMenuElement element)
    {
        if (!_elementToItem.TryGetValue(element, out T item)) return;
        OnItemSelected?.Invoke(item);
        CloseMenu();
    }

    private void SelectionInputHandling()
    {
        if (_menuElements.Count == 0) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        _accumulatedMouseDelta += mouseDelta;

        if (_accumulatedMouseDelta.magnitude > maxRadius)
            _accumulatedMouseDelta = _accumulatedMouseDelta.normalized * maxRadius;

        mousePositionFromCenter = _accumulatedMouseDelta;

        if (mousePositionFromCenter.magnitude < selectionDeadzone)
        {
            UnhoverElement();
            _currentSegmentIndex = -1;
            return;
        }

        float mouseAngle = Mathf.Atan2(mousePositionFromCenter.y, mousePositionFromCenter.x) * Mathf.Rad2Deg;
        int closestIndex = -1;
        float smallestAngleDiff = float.MaxValue;

        for (int i = 0; i < _menuElements.Count; i++)
        {
            Vector2 elementScreenPos = RectTransformUtility.WorldToScreenPoint(null, _menuElements[i].transform.position);
            Vector2 menuCenter = RectTransformUtility.WorldToScreenPoint(null, elementsContainer.position);
            Vector2 elementDirection = elementScreenPos - menuCenter;
            float elementAngle = Mathf.Atan2(elementDirection.y, elementDirection.x) * Mathf.Rad2Deg;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(mouseAngle, elementAngle));

            if (angleDiff < smallestAngleDiff)
            {
                smallestAngleDiff = angleDiff;
                closestIndex = i;
            }
        }

        if (closestIndex != _currentSegmentIndex)
        {
            _currentSegmentIndex = closestIndex;
            HoverElement(closestIndex);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && _hoveredElement)
            _hoveredElement.Select();
    }

    private void ClearMenu()
    {
        foreach (var element in _menuElements)
        {
            if (element)
            {
                element.OnSelect -= OnElementSelected;
                Destroy(element.gameObject);
            }
        }

        _menuElements.Clear();
        _elementToItem.Clear();
    }

    public void CloseMenu()
    {
        UnhoverElement();
        _accumulatedMouseDelta = Vector2.zero;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        _isOpen = false;
    }

    public void OpenMenu()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        _isOpen = true;
    }
}