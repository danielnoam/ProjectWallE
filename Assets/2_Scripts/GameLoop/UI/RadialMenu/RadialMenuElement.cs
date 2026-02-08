using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuElement : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public Image backgroundImage;
    public string elementInfo;
    
    private Color _normalColor = Color.white;
    private Color _hoveredColor = Color.yellow;

    
    public event Action<RadialMenuElement> OnSelect;
    
    private bool _isHovered;
    
    public void SetUp(Color normalColor, Color hoveredColor) {
        _normalColor = normalColor;
        _hoveredColor = hoveredColor;
        if (backgroundImage) backgroundImage.color = normalColor;
    }

    public void SetHovered()
    {
        if (_isHovered) return;
        _isHovered = true;
        if (backgroundImage) backgroundImage.color = _hoveredColor;
    }
    
    public void SetNormal()
    {
        if (!_isHovered) return;
        _isHovered = false;
        if (backgroundImage) backgroundImage.color = _normalColor;
    }

    public void Select()
    {
        OnSelect?.Invoke(this);
    }
}