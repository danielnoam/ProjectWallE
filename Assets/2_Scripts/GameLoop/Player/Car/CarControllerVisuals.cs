using System;
using System.Collections.Generic;
using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    [Serializable]
    public class CarControllerVisuals
    {
        [Header("References")]
        [Tooltip("Make sure this array and the corresponding steeringTires array in CarController are in the same order")]
        [SerializeField] private TireVisual[] steeringTires;
        [Tooltip("Make sure this array and the corresponding staticTires array in CarController are in the same order")]
        [SerializeField] private TireVisual[] staticTires;
        [Tooltip("Make sure this array and the corresponding _allTires array in CarController are in the same order")]
        [SerializeField] private TrailRenderer[] tiresSkidTrails;
        
        [Header("Suspension")]
        [SerializeField] private float suspensionReturnSpeed = 5f;
        [SerializeField] private float suspensionSmoothness = 10f;
        [SerializeField] private float wheelRadius = 1.5f;
        
        [Header("Skid marks settings")]
        [SerializeField, Range(-0.2f,0.2f)] private float skidGroundOffset = 0;
        [SerializeField, Range(0f,1f)] private float skidSlippingAmount = 0;

        private readonly List<TireVisual> _allTireVisuals = new List<TireVisual>();
        private Transform _carTransform;

        public void UpdateTireSuspensionVisuals(bool isTireGrounded, int index, float offset)
        {
            if (!IsValidIndex(index)) return;

            TireVisual tireVisual = _allTireVisuals[index];
            if (tireVisual.visTransform == null) return;

            if (!isTireGrounded)
            {
                tireVisual.visTransform.localPosition = Vector3.Lerp(
                    tireVisual.visTransform.localPosition,
                    tireVisual.StartLocalPosition,
                    suspensionReturnSpeed * Time.fixedDeltaTime);

                return;
            }

            Transform parent = tireVisual.visTransform.parent;

            Vector3 worldPos = parent.TransformPoint(tireVisual.StartLocalPosition);
            worldPos -= _carTransform.up * offset;
            Vector3 difference = worldPos - tireVisual.visTransform.position;
            
            tireVisual.visTransform.position = difference.magnitude > 0.25f ?
                Vector3.Lerp(tireVisual.visTransform.position, worldPos, suspensionSmoothness * Time.fixedDeltaTime) :
                worldPos;
        }

        public void RotateWheels(float carSpeed, int index)
        {
            if (!IsValidIndex(index)) return;
            if (_allTireVisuals[index].visTransform == null) return;
            if (wheelRadius <= 0.0001f) return;

            float wheelSpeedRad = carSpeed / wheelRadius;
            float wheelSpeedDeg = wheelSpeedRad * Mathf.Rad2Deg;

            _allTireVisuals[index].spinX += wheelSpeedDeg * Time.fixedDeltaTime;
            ApplyVisualRotation(_allTireVisuals[index]);
        }

        public void SteerWheels(float rotation)
        {
            foreach (var visual in steeringTires)
            {
                if (visual.visTransform == null) continue;

                visual.steerY = rotation;
                ApplyVisualRotation(visual);
            }
        }

        public void UpdateSkidTrailHeight(float groundHitDistance,int index, Tire tire)
        {
            var vector3 = tire.tireTransform.position;
            vector3.y -= groundHitDistance - skidGroundOffset;
            _allTireVisuals[index].skidTrailTransform.position = vector3;
        }

        public void ActivateSkidTrails(float slippingAmount, int index)
        {
            tiresSkidTrails[index].emitting = slippingAmount >= skidSlippingAmount;
        }

        #region Helpers

        public void Initialize(Transform carTransform)
        {
            GetAllTireVisuals();
            _carTransform = carTransform;
            foreach (var visual in _allTireVisuals)
            {
                visual.Initialize();
            }
        }

        public void GetAllTireVisuals()
        {
            _allTireVisuals.Clear();

            foreach (var tire in steeringTires)
                _allTireVisuals.Add(tire);

            foreach (var tire in staticTires)
                _allTireVisuals.Add(tire);
        }

        public void ResetVisuals()
        {
            for (var i = 0; i < _allTireVisuals.Count; i++)
            {
                var tireVisual = _allTireVisuals[i];
                tireVisual.ResetVisual();
                ActivateSkidTrails(-1f, i);
            }
        }

        private void ApplyVisualRotation(TireVisual tireVisual)
        {
            Transform parent = tireVisual.visTransform.parent;
            if (parent == null)
            {
                tireVisual.visTransform.localRotation = tireVisual.StartLocalRotation;
                return;
            }

            Vector3 steerAxisInParentSpace = parent.InverseTransformDirection(_carTransform.up);
            Quaternion steerRotation = Quaternion.AngleAxis(tireVisual.steerY, steerAxisInParentSpace);

            Quaternion spinRotation = Quaternion.AngleAxis(tireVisual.spinX, Vector3.right);

            tireVisual.visTransform.localRotation =
                steerRotation * tireVisual.StartLocalRotation * spinRotation;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _allTireVisuals.Count;
        }

        #endregion
    }
}