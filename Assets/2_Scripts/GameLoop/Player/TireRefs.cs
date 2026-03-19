using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        public Transform visTransform;

        [NonSerialized] public float spinX;
        [NonSerialized] public float steerY;

        private Vector3 _startLocalPosition;
        private Quaternion _startLocalRotation;
        private Vector3 _startWorldPosition;

        public Vector3 StartLocalPosition => _startLocalPosition;
        public Quaternion StartLocalRotation => _startLocalRotation;
        public Vector3 StartWorldPosition => _startWorldPosition;

        public void Initialize()
        {
            if (visTransform == null) return;

            _startLocalPosition = visTransform.localPosition;
            _startLocalRotation = visTransform.localRotation;
            _startWorldPosition = visTransform.position;

            spinX = 0f;
            steerY = 0f;
        }

        public void ResetVisual()
        {
            if (visTransform == null) return;

            visTransform.localPosition = _startLocalPosition;
            visTransform.localRotation = _startLocalRotation;

            spinX = 0f;
            steerY = 0f;
        }
    }
}