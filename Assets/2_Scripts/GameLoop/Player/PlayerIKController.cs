using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ProjectWallE.GameLoop.Player
{
    public class PlayerIKController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float shoulderOffset = 0.2f;
        [SerializeField] private float targetDistance = 3f;
        [SerializeField] private float transitionSpeed = 5f;

        [Header("References")]
        [SerializeField] private Rig rig;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform shoulder;
        [SerializeField] private Transform ikTarget;

        private Camera _cam;
        private float _targetWeight = 1f;

        public void SetEnabled(bool enabled) => _targetWeight = enabled ? 1f : 0f;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (!_cam) return;

            rig.weight = Mathf.MoveTowards(rig.weight, _targetWeight, transitionSpeed * Time.deltaTime);
            if (rig.weight < 0.01f) return;

            ikTarget.position = shoulder.position + _cam.transform.forward * targetDistance + torso.right * shoulderOffset;
            ikTarget.rotation = _cam.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
        }
    }
}