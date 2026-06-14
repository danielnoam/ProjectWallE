using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace ProjectWallE.GameLoop
{
    public class ShipMovement : MonoBehaviour
    {
        public enum FollowMode { Loop, Once, FollowPlayer }
        public enum EntryPoint { ClosestPoint, FirstKnot }

        public static ShipMovement Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private SplineContainer startingSpline;
        [SerializeField, Min(0f)] private float startingSpeed = 15f;
        [SerializeField] private FollowMode startingMode = FollowMode.Loop;
        [SerializeField] private bool playOnAwake = true;

        [Header("Alignment")]
        [SerializeField] private bool alignToSpline = true;
        [Tooltip("How fast the ship rotates to face the spline tangent. 0 aligns instantly")]
        [SerializeField, Min(0f)] private float rotationSpeed = 5f;

        [Header("Transition")]
        [Tooltip("Default seconds to ease onto a newly assigned spline when a marker does not specify one")]
        [SerializeField, Min(0f)] private float defaultTransitionDuration = 3f;
        [Tooltip("How wide the ship banks when merging onto a new spline. 0 is a near-straight approach, higher arcs out along the current heading")]
        [SerializeField, Range(0f, 1f)] private float mergeStrength = 0.4f;

        private SplineContainer _activeSpline;
        private float _speed;
        private FollowMode _mode;
        private bool _closed;
        private float _splineLength;
        private float _distance;
        private bool _moving;

        private bool _transitioning;
        private float _transitionTime;
        private float _transitionDuration;
        private Vector3 _mergeP0;
        private Vector3 _mergeP1;
        private Vector3 _mergeP2;
        private Vector3 _mergeP3;
        private Vector3 _mergeUp;
        private float _mergeStartSlope;
        private float _mergeEndSlope;
        private SplineContainer _pendingSpline;
        private float _pendingSpeed;
        private FollowMode _pendingMode;
        private EntryPoint _pendingEntry;


        private void OnValidate()
        {
            if (startingSpline)
            {
                transform.position = startingSpline.transform.position;
                transform.rotation = startingSpline.transform.rotation;
            }
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playOnAwake && startingSpline) BeginFollow(startingSpline, startingSpeed, startingMode, EntryPoint.ClosestPoint);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_transitioning) UpdateTransition();
            else if (_moving) UpdateFollow();
        }

        /// <summary>
        /// Banks the ship onto a new spline along a smooth merge curve, then follows it. Pass a transition duration of 0 to switch instantly.
        /// </summary>
        public void SetSpline(SplineContainer spline, float speed, float transitionDuration, FollowMode mode = FollowMode.Loop, EntryPoint entry = EntryPoint.ClosestPoint)
        {
            if (!spline || !HasKnots(spline)) return;

            if (transitionDuration <= 0f)
            {
                BeginFollow(spline, speed, mode, entry);
                return;
            }

            float entryT = GetEntryT(spline, entry);
            spline.Evaluate(Mathf.Clamp01(entryT), out float3 entryPoint, out float3 entryTangent, out float3 entryUp);

            Vector3 startPos = transform.position;
            Vector3 endPos = entryPoint;
            float chord = Vector3.Distance(startPos, endPos);

            if (chord < 0.01f)
            {
                BeginFollow(spline, speed, mode, entry);
                return;
            }

            Vector3 startDir = transform.forward.sqrMagnitude > 0.0001f ? transform.forward.normalized : (endPos - startPos).normalized;
            Vector3 endDir = ((Vector3)entryTangent).sqrMagnitude > 0.0001f ? ((Vector3)entryTangent).normalized : (endPos - startPos).normalized;

            float handle = chord * mergeStrength;
            _mergeP0 = startPos;
            _mergeP3 = endPos;
            _mergeP1 = startPos + startDir * handle;
            _mergeP2 = endPos - endDir * handle;
            _mergeUp = ((Vector3)entryUp).sqrMagnitude > 0.0001f ? ((Vector3)entryUp).normalized : Vector3.up;

            // Cubic Bezier end-derivative magnitude is 3*handle at both ends; pick the time-mapping slopes so
            // world speed leaves at the current cruise speed and arrives at the new follow speed (no stall/hitch).
            float endDerivative = 3f * handle;
            _mergeStartSlope = endDerivative > 0.001f ? Mathf.Clamp(_speed * transitionDuration / endDerivative, 0f, 2.5f) : 0f;
            _mergeEndSlope = endDerivative > 0.001f ? Mathf.Clamp(speed * transitionDuration / endDerivative, 0f, 2.5f) : 0f;

            _pendingSpline = spline;
            _pendingSpeed = speed;
            _pendingMode = mode;
            _pendingEntry = entry;
            _transitionDuration = transitionDuration;
            _transitionTime = 0f;
            _transitioning = true;
            _moving = false;
        }

        public void SetSpline(SplineContainer spline, float speed, FollowMode mode = FollowMode.Loop, EntryPoint entry = EntryPoint.ClosestPoint)
        {
            SetSpline(spline, speed, defaultTransitionDuration, mode, entry);
        }

        private void BeginFollow(SplineContainer spline, float speed, FollowMode mode, EntryPoint entry)
        {
            _activeSpline = spline;
            _speed = speed;
            _mode = mode;
            _closed = spline.Spline.Closed;
            _splineLength = spline.CalculateLength();
            _distance = GetEntryT(spline, entry) * _splineLength;
            _transitioning = false;
            _moving = true;
        }

        private void UpdateTransition()
        {
            _transitionTime += Time.deltaTime;
            float s = _transitionDuration > 0f ? Mathf.Clamp01(_transitionTime / _transitionDuration) : 1f;
            float u = HermiteEase(s, _mergeStartSlope, _mergeEndSlope);

            transform.position = EvaluateBezier(u);

            Vector3 dir = EvaluateBezierTangent(u);
            if (alignToSpline && dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, _mergeUp);

            if (s >= 1f) BeginFollow(_pendingSpline, _pendingSpeed, _pendingMode, _pendingEntry);
        }

        private static float HermiteEase(float s, float startSlope, float endSlope)
        {
            float s2 = s * s;
            float s3 = s2 * s;
            return (s3 - 2f * s2 + s) * startSlope + (3f * s2 - 2f * s3) + (s3 - s2) * endSlope;
        }

        private Vector3 EvaluateBezier(float u)
        {
            float iu = 1f - u;
            return iu * iu * iu * _mergeP0
                 + 3f * iu * iu * u * _mergeP1
                 + 3f * iu * u * u * _mergeP2
                 + u * u * u * _mergeP3;
        }

        private Vector3 EvaluateBezierTangent(float u)
        {
            float iu = 1f - u;
            return 3f * iu * iu * (_mergeP1 - _mergeP0)
                 + 6f * iu * u * (_mergeP2 - _mergeP1)
                 + 3f * u * u * (_mergeP3 - _mergeP2);
        }

        private void UpdateFollow()
        {
            float t = _mode == FollowMode.FollowPlayer ? AdvanceTowardPlayer() : AdvanceAlongSpline();

            EvaluatePose(_activeSpline, t, out Vector3 pos, out Quaternion rot);
            transform.position = pos;

            if (!alignToSpline) return;
            transform.rotation = rotationSpeed > 0f
                ? Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime)
                : rot;
        }

        private float AdvanceAlongSpline()
        {
            _distance += _speed * Time.deltaTime;
            float t = _splineLength > 0f ? _distance / _splineLength : 0f;

            if (t < 1f) return t;

            if (_mode == FollowMode.Loop)
            {
                t %= 1f;
                _distance = t * _splineLength;
                return t;
            }

            _moving = false;
            return 1f;
        }

        private float AdvanceTowardPlayer()
        {
            if (_splineLength <= 0f) return 0f;
            if (!PlayerManager.Instance) return _distance / _splineLength;

            float targetDistance = GetNearestT(_activeSpline, PlayerManager.Instance.transform.position) * _splineLength;
            float maxStep = _speed * Time.deltaTime;

            if (_closed)
            {
                float half = _splineLength * 0.5f;
                float diff = Mathf.Repeat(targetDistance - _distance + half, _splineLength) - half;
                _distance = Mathf.Repeat(_distance + Mathf.Clamp(diff, -maxStep, maxStep), _splineLength);
            }
            else
            {
                _distance = Mathf.MoveTowards(_distance, targetDistance, maxStep);
            }

            return _distance / _splineLength;
        }

        private void EvaluatePose(SplineContainer spline, float t, out Vector3 position, out Quaternion rotation)
        {
            spline.Evaluate(Mathf.Clamp01(t), out float3 p, out float3 tangent, out float3 up);
            position = p;

            if (spline.Spline.Count < 2)
            {
                rotation = spline.transform.rotation;
                return;
            }

            Vector3 forward = tangent;
            rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, ((Vector3)up).normalized)
                : transform.rotation;
        }

        private float GetEntryT(SplineContainer spline, EntryPoint entry)
        {
            return entry == EntryPoint.FirstKnot ? 0f : GetNearestT(spline, transform.position);
        }

        private float GetNearestT(SplineContainer spline, Vector3 worldPosition)
        {
            float3 localPoint = spline.transform.InverseTransformPoint(worldPosition);
            SplineUtility.GetNearestPoint(spline.Spline, localPoint, out _, out float t);
            return t;
        }

        private static bool HasKnots(SplineContainer spline)
        {
            return spline.Splines.Count > 0 && spline.Spline.Count > 0;
        }
    }
}
