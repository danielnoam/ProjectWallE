using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class StructureNode : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, PrefabSelector("Assets/5_Prefabs/Structures")] private Structure[] allowedStructures;
        [SerializeField] private Vector3 snapOffset;
        
        [Header("References")]
        [SerializeField] private Transform visuals;

        private Structure _occupant;
        private bool _reserved;

        public bool IsOccupied => _occupant || _reserved;
        public Structure[] AllowedStructures => allowedStructures;
        public Vector3 SnapPoint => transform.position + transform.TransformVector(snapOffset);

        public void Reserve()
        {
            _reserved = true;
        }

        public void Occupy(Structure structure)
        {
            _reserved = false;
            _occupant = structure;
            _occupant.OnDeath += OnOccupantDeath;
            Structure.OnStructureDemolished += OnOccupantDemolished;
        }

        private void OnOccupantDeath(IDamageable damageable)
        {
            Clear();
        }

        private void OnOccupantDemolished(Structure structure)
        {
            if (structure != _occupant) return;
            
            Clear();
        }

        private void Clear()
        {
            if (_occupant)
            {
                _occupant.OnDeath -= OnOccupantDeath;
                Structure.OnStructureDemolished -= OnOccupantDemolished;
            }
            _occupant = null;
            _reserved = false;
        }

        private void OnDestroy()
        {
            Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (IsOccupied) return;

            var sphereCollider = GetComponent<SphereCollider>();
            float radius = sphereCollider ? sphereCollider.radius * transform.lossyScale.x : 0.3f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawWireSphere(transform.position + transform.TransformVector(snapOffset), 0.3f);

            UnityEditor.Handles.Label(
                transform.position.AddY(0.5f) + Vector3.up * radius , 
                "Structure Node",
                new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.cyan },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                });
        }
#endif
    }
}