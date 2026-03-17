using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    [Serializable]
    public class PositionField
    {
        [SerializeField] private Transform positionTransform;
        [SerializeField] private Vector3 positionVector;

        public Vector3 Position => positionTransform ? positionTransform.position : positionVector;

        public Transform Transform => positionTransform;
        
        public void SetTransform(Transform newTransform)
        {
            positionTransform = newTransform;
            if (newTransform)
            {
                positionVector = newTransform.position;
            }
        }
    }
}