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
        [SerializeField] private FillBar healthBar;
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
            if (structureName)
            {
                structureName.text = _structure.Levels.Length > 1 ? $"{_structure.StructureUIData.Label} ({_structure.CurrentUpgradeLevel})" : $"{_structure.StructureUIData.Label}";
            }
            
            healthBar.SetValue(_structure.CurrentHealth, _structure.MaxHealth);
        }
        
        public void Show(Structure structure)
        {
            if (!structure || structure == _structure) return;
            
            _structure = structure;
            _structure.OnDamaged += OnDamaged;
            healthBar.SetImmediate(_structure.CurrentHealth, _structure.MaxHealth);
            UpdateStatus();
            transform.position = _structure.TopPoint.Add(offset);
            canvasGroup.alpha = 1;
        }
        

        public void Hide()
        {
            if (_structure) _structure.OnDamaged -= OnDamaged;
            _structure = null;
            if (canvasGroup) canvasGroup.alpha = 0;
        }
    }
}
