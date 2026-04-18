using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using TMPro;
using UnityEngine;

namespace ProjectWallE.GameLoop.UI
{
    public class ObjectiveGameMarker : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showDistance;

        [Header("References")]
        [SerializeField, AutoGetSelf] private Note note;
        [SerializeField, AutoGetSelf] private RadarTarget radarTarget;
        [SerializeField] private GameObject inGameMarker;
        [SerializeField] private TextMeshProUGUI distanceLabel;

        private Transform _player;
        private bool _active;

        private void Awake()
        {
            inGameMarker?.SetActive(false);
        }

        private void Update()
        {
            if (!_active || !showDistance || !distanceLabel || !_player) return;

            float dist = Vector3.Distance(_player.position, transform.position);
            distanceLabel.text = $"{dist:F0}m";
        }

        public void OnObjectiveStarted(bool showInGame, bool showOnRadar)
        {
            _active = true;

            inGameMarker?.SetActive(showInGame);
            if (distanceLabel) distanceLabel.gameObject.SetActive(showInGame && showDistance);

            if (showOnRadar && radarTarget)
            {
                radarTarget.EnableBlip();
                radarTarget.PingBlip();
            }

            if (!_player)
            {
                _player = LevelManager.Instance ? LevelManager.Instance.Player?.transform : null;
            }
        }

        public void OnObjectiveCompleted(bool showInGame, bool showOnRadar)
        {
            _active = false;
            inGameMarker?.SetActive(false);
            if (showOnRadar && radarTarget) radarTarget.DisableBlip();
        }
    }
}