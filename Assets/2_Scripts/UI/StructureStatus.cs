using System;
using DNExtensions.Systems.Shapes;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using TMPro;
using UnityEngine;

namespace ProjectWallE.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StructureStatus : MonoBehaviour
    {
        public static StructureStatus Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private SDFRectangle healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI structureName;
        [SerializeField, AutoGetSelf, HideInInspector] private CanvasGroup canvasGroup;

        private Structure _structure;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            canvasGroup.alpha = 0;
        }

        private void OnDestroy()
        {
            if (_structure) _structure.OnDamaged -= OnDamaged;
        }
        
        private void OnDamaged(float damage)
        {
            if (!_structure) return;
            
            UpdateStatus();
        }
        
        private void UpdateStatus()
        {
            if (structureName) structureName.text = _structure.Label;
            
            if (_structure.CurrentHealth <= 0)
            {
                healthBar.color = healthBar.color.SetAlpha(0.5f);
                healthText.text = "Broken";
            }
            else
            {
                healthBar.color = healthBar.color.SetAlpha(1f);
                healthText.text = $"{_structure.CurrentHealth:F0}/{_structure.MaxHealth}";
            }
            
            healthBar.fillAmount = _structure.CurrentHealth / _structure.MaxHealth;
        }
        
        public void Show(Structure structure)
        {
            if (!structure || structure == _structure) return;
            
            _structure = structure;
            _structure.OnDamaged += OnDamaged;
            UpdateStatus();
            transform.position = _structure.TopPoint.Add(offset);
            canvasGroup.alpha = 1;
        }
        

        public void Hide()
        {
            if (_structure) _structure.OnDamaged -= OnDamaged;
            _structure = null;
            canvasGroup.alpha = 0;
        }
    }
}
