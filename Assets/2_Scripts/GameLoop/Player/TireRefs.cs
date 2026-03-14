using System;
using UnityEngine;

namespace _2_Scripts
{
    [Serializable]
    public class Tire
    {
        public Transform tireTransform;
        public Transform visualTransform;

        private Vector3 _visualStartPosition;

        [HideInInspector] public bool isGroundedExact;
        public RaycastHit exactGroundHit;

        [HideInInspector] public bool isGroundedExtended;
        public RaycastHit extendedGroundHit;

        [HideInInspector] public bool isMoving;

        public Vector3 VisualStartPosition => _visualStartPosition;

        public void Initialize()
        {
            if (visualTransform != null)
                _visualStartPosition = visualTransform.localPosition;

            isGroundedExact = false;
            isGroundedExtended = false;
            isMoving = false;
        }
    }
}