using System;
using System.Collections.Generic;
using _2_Scripts;
using UnityEngine;

namespace ProjectWallE
{
    [Serializable]
    public class CarControllerVisuals
    {
        [Tooltip("Make sure this array and the corresponding steeringTires array in CarController are in the same order")]
        [SerializeField] private TireVisual[] steeringTires;
        [Tooltip("Make sure this array and the corresponding staticTires array in CarController are in the same order")]
        [SerializeField] private TireVisual[] staticTires;
        [SerializeField] private float suspensionReturnSpeed = 5f;

        private readonly List<TireVisual> _allTireVisuals = new List<TireVisual>();
        private Transform _carTransform;

        public void UpdateTireSuspensionVisuals(bool isTireGrounded, int index, float offset)
        {
            if (!IsValidIndex(index)) return;
            if (_allTireVisuals[index].visTransform == null) return;

            if (!isTireGrounded)
            {
                _allTireVisuals[index].visTransform.localPosition = Vector3.Lerp(
                    _allTireVisuals[index].visTransform.localPosition,
                    _allTireVisuals[index].StartLocalPosition,
                    suspensionReturnSpeed * Time.fixedDeltaTime);

                return;
            }

            TireVisual tireVisual = _allTireVisuals[index];

            Vector3 localSuspensionDir =
                Quaternion.Inverse(tireVisual.StartLocalRotation) * (-_carTransform.up);

            tireVisual.visTransform.localPosition =
                tireVisual.StartLocalPosition + localSuspensionDir * offset;
        }

        public void RotateWheels(float carSpeed, float wheelRadius, bool isTireGrounded, int index)
        {
            if (!IsValidIndex(index)) return;
            if (!isTireGrounded) return;
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
            foreach (var tireVisual in _allTireVisuals)
            {
                tireVisual.ResetVisual();
            }
        }

        private void ApplyVisualRotation(TireVisual tireVisual)
        {
            Quaternion steerRotation = Quaternion.AngleAxis(tireVisual.steerY, Vector3.up);
            Quaternion spinRotation = Quaternion.AngleAxis(tireVisual.spinX, Vector3.right);

            tireVisual.visTransform.localRotation =
                tireVisual.StartLocalRotation * steerRotation * spinRotation;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _allTireVisuals.Count;
        }

        #endregion
    }
}