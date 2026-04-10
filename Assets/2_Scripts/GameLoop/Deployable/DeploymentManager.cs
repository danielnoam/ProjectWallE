using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectWallE.GameLoop
{
    public struct DeploymentRequest
    {
        public Vector3 TargetPosition;
        public Vector3 TargetForward;
        public Vector3 TargetSurfaceNormal;
        public Transform Parent;
        public StructureNode Node;
        public bool DamageOnImpact;
        public bool UseCamera;

        public DeploymentRequest(Vector3 targetPosition, Vector3 targetForward, Transform parent)
        {
            TargetPosition = targetPosition;
            TargetForward = targetForward;
            TargetSurfaceNormal = Vector3.up;
            Parent = parent;
            Node = null;
            DamageOnImpact = true;
            UseCamera = false;
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
        
        private readonly List<Transform> _shipSpawnPositions = new List<Transform>();

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
        
        private void SpawnPod<T>(T deployable, DeploymentRequest request, Vector3 spawnPosition) where T : MonoBehaviour, IDeployable
        {
            Pod pod = Instantiate(podPrefab, spawnPosition, Quaternion.identity, _podHolder);
            pod.Initialize(deployable, request);
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
            
            SpawnPod(deployable, request, shipPosition.position);
        }

        public void DeployFromSky<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            Vector3 origin = request.TargetPosition + Vector3.up * skyDropHeight;
            SpawnPod(deployable, request, origin);
        }
        
        public void RegisterShipPosition(Transform ship)
        {
            if (_shipSpawnPositions.Contains(ship)) return;
            
            _shipSpawnPositions.Add(ship);
        }

        public void UnregisterShipPosition(Transform ship)
        {
            if (!_shipSpawnPositions.Contains(ship)) return;
            
            _shipSpawnPositions.Remove(ship);
        }
    }
}