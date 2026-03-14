using System;
using UnityEngine;

namespace _2_Scripts
{
    [Serializable]
    public class Tire
    {
        public Transform tireTransform;

        [HideInInspector] public bool isGroundedExact;
        public RaycastHit exactGroundHit;

        [HideInInspector] public bool isGroundedExtended;
        public RaycastHit extendedGroundHit;

        [HideInInspector] public bool isMoving;

        public void Initialize()
        {
            isGroundedExact = false;
            isGroundedExtended = false;
            isMoving = false;
        }
    }

    [Serializable]
    public class TireVisual
    {
        public Transform visualTransform;

        [NonSerialized] public float spinX;
        [NonSerialized] public float steerY;

        private Vector3 _visualStartPosition;
        private Vector3 _visualStartRotation;

        public Vector3 VisualStartPosition => _visualStartPosition;
        public Vector3 VisualStartRotation => _visualStartRotation;

        public void Initialize()
        {
            if (visualTransform == null) return;

            _visualStartPosition = visualTransform.localPosition;
            _visualStartRotation = visualTransform.localEulerAngles;

            spinX = 0f;
            steerY = 0f;
        }

        public void ResetVisual()
        {
            if (visualTransform == null) return;

            visualTransform.localPosition = _visualStartPosition;
            visualTransform.localEulerAngles = _visualStartRotation;

            spinX = 0f;
            steerY = 0f;
        }
    }
}