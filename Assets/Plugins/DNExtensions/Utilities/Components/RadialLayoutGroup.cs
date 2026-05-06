using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor; 
#endif


namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Radial Layout Group", 1000)]
    public class RadialLayoutGroup : LayoutGroup
    {
        [Header("Radial Settings")]
        public float radius = 100f;
        public Vector2 offset = Vector2.zero;
        [Range(0f, 360f)] public float startAngle;
        [Range(0f, 360f)] public float endAngle = 360f;
        [Tooltip("If true, only visible children are arranged, otherwise all children are arranged")]
        public bool onlyLayoutVisible = true;
        
        [Header("Child Settings")]
        [Tooltip("If true, child size is taken from the child's layout element component, otherwise child size is controlled by childSize")]
        public bool controlChildSize;
        [EnableIf("controlChildSize")] public Vector2 childSize = new Vector2(50f, 50f);
        [Tooltip("If true, children are rotated to face away from the center of the radial group")]
        public bool rotateChildren;
        [EnableIf("rotateChildren")] public float childRotationOffset = -90f;
        
        
        public float Radius
        {
            get => radius;
            set { radius = value; SetDirty(); }
        }
        
        public Vector2 Offset
        {
            get => offset;
            set { offset = value; SetDirty(); }
        }
        
        public float StartAngle
        {
            get => startAngle;
            set { startAngle = value; SetDirty(); }
        }
        
        public float EndAngle
        {
            get => endAngle;
            set { endAngle = value; SetDirty(); }
        }
        
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalculateRadial();
        }
        
        public override void CalculateLayoutInputVertical()
        {
            CalculateRadial();
        }
        
        public override void SetLayoutHorizontal()
        {
        }
        
        public override void SetLayoutVertical()
        {
        }
        
        private void CalculateRadial()
        {
            m_Tracker.Clear();
            
            int activeChildCount = GetActiveChildCount();
            if (activeChildCount == 0) return;
            
            float angleRange = endAngle - startAngle;
            float angleStep = activeChildCount > 1 ? angleRange / (activeChildCount - 1) : 0f;
            
            if (Mathf.Approximately(angleRange, 360f))
            {
                angleStep = angleRange / activeChildCount;
            }
            
            float currentAngle = startAngle;
            
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                
                if (!IsActiveChild(child)) continue;
                
                m_Tracker.Add(this, child,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.Pivot |
                    (controlChildSize ? DrivenTransformProperties.SizeDelta : 0));
                
                float angleRad = currentAngle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Cos(angleRad) * radius,
                    Mathf.Sin(angleRad) * radius
                ) + offset;
                
                child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = position;
                
                if (controlChildSize)
                {
                    child.sizeDelta = childSize;
                }
                
                if (rotateChildren)
                {
                    child.localRotation = Quaternion.Euler(0f, 0f, currentAngle + childRotationOffset);
                }
                
                currentAngle += angleStep;
            }
        }
        
        private int GetActiveChildCount()
        {
            if (!onlyLayoutVisible) return rectChildren.Count;
            
            int count = 0;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                if (IsActiveChild(rectChildren[i]))
                    count++;
            }
            return count;
        }
        
        private bool IsActiveChild(RectTransform child)
        {
            return child != null && (!onlyLayoutVisible || child.gameObject.activeInHierarchy);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateRadial();
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(RadialLayoutGroup))]
    internal class RadialLayoutGroupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;
            DrawPropertiesExcluding(serializedObject, "m_Padding", "m_ChildAlignment", "m_Script");
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
