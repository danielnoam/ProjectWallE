using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class BuildNode : MonoBehaviour
    {
        [Header("Node Settings")]
        [SerializeField, PrefabSelector("Assets/5_Prefabs/Structures")] private Structure[] allowedStructures;
        [SerializeField] private Vector3 snapOffset;

        private Structure _occupant;

        public bool IsOccupied => _occupant;
        public Structure[] AllowedStructures => allowedStructures;
        public Vector3 SnapPoint => transform.position + transform.TransformVector(snapOffset);

        public void Occupy(Structure structure)
        {
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
        }

        private void OnDestroy()
        {
            Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(SnapPoint, 0.3f);
        }
#endif
    }
}