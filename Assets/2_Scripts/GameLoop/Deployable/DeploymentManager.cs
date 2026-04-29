using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{
    public enum PodCameraMode { None, LookAt, Follow }
    
    public struct DeploymentRequest
    {
        public Vector3 TargetPosition;
        public Vector3 TargetForward;
        public Vector3 TargetSurfaceNormal;
        public Transform Parent;
        public StructureNode Node;
        public bool DamageOnImpact;
        public bool InstantiateOnLand;
        public PodCameraMode CameraMode;

        public DeploymentRequest(Vector3 targetPosition, Vector3 targetForward, Transform parent)
        {
            TargetPosition = targetPosition;
            TargetForward = targetForward;
            TargetSurfaceNormal = Vector3.up;
            Parent = parent;
            Node = null;
            DamageOnImpact = true;
            InstantiateOnLand = true;
            CameraMode = PodCameraMode.None;
        }
    }
    
    
    [DefaultExecutionOrder(-100)]
    public class DeploymentManager : MonoBehaviour
    {
        public static DeploymentManager Instance { get; private set; }
        public static event Action<DeploymentRequest, Pod> OnPodDeployed;

        [Header("Settings")]
        [SerializeField] private float skyDropHeight = 300f;
        [SerializeField] private Pod podPrefab;
        
        private readonly List<ShipPosition> _shipSpawnPositions = new List<ShipPosition>();

        private Transform _podHolder;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _podHolder = new GameObject("PodHolder").transform;
        }
        
        private void SpawnPod<T>(T deployable, DeploymentRequest request, Vector3 spawnPosition, bool moveInArc) where T : MonoBehaviour, IDeployable
        {
            Pod pod = Instantiate(podPrefab, spawnPosition, Quaternion.identity, _podHolder);
            pod.Initialize(deployable, request, moveInArc);
            OnPodDeployed?.Invoke(request, pod);
        }
        
        public void DeployFromShip<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            if (_shipSpawnPositions.Count == 0)
            {
                DeployFromSky(deployable, request);
                return;
            }
            
            var shipPositionIndex = Random.Range(0, _shipSpawnPositions.Count);
            var shipPosition = _shipSpawnPositions[shipPositionIndex];

            shipPosition.PlayEffects();
            SpawnPod(deployable, request, shipPosition.transform.position, true);
        }

        public void DeployFromSky<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            Vector3 origin = request.TargetPosition + Vector3.up * skyDropHeight;
            SpawnPod(deployable, request, origin, false);
        }
        
        public void RegisterShipPosition(ShipPosition position)
        {
            if (_shipSpawnPositions.Contains(position)) return;
            
            _shipSpawnPositions.Add(position);
        }

        public void UnregisterShipPosition(ShipPosition position)
        {
            if (!_shipSpawnPositions.Contains(position)) return;
            
            _shipSpawnPositions.Remove(position);
        }
    }
}