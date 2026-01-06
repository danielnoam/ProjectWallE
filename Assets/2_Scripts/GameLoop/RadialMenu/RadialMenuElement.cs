using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenuElement : MonoBehaviour
{
    [Header("Element Settings")]
    [SerializeField] private float textRadiusOffset = 250f; 
    [SerializeField] private float iconRadiusOffset = 50f;
    public Color normalColor = Color.white;
    public Color hoveredColor = Color.yellow;
    
    [Header("References")]
    public Image iconImage;
    public Image backgroundImage;
    public TextMeshProUGUI text;
    

    
    public event Action<RadialMenuElement> OnSelect;
    
    private bool _isHovered;

    public void PositionUIElements(float fillAmount)
    {
        float centerAngle = (fillAmount * 360f) / 2f;
        float radians = centerAngle * Mathf.Deg2Rad;
        
        if (text)
        {
            Vector2 textPosition = new Vector2(
                Mathf.Sin(radians) * textRadiusOffset,
                Mathf.Cos(radians) * textRadiusOffset
            );
            text.rectTransform.anchoredPosition = textPosition;
        }
        
        if (iconImage)
        {
            Vector2 iconPosition = new Vector2(
                Mathf.Sin(radians) * iconRadiusOffset,
                Mathf.Cos(radians) * iconRadiusOffset
            );
            iconImage.rectTransform.anchoredPosition = iconPosition;
        }
    }

    public void SetHovered()
    {
        if (_isHovered) return;
        _isHovered = true;
        backgroundImage.color = hoveredColor;
    }
    
    public void SetNormal()
    {
        if (!_isHovered) return;
        _isHovered = false;
        backgroundImage.color = normalColor;
    }

    public void Select()
    {
        OnSelect?.Invoke(this);
    }
}