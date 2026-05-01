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
        public Team ImpactTeam;
        public bool InstantiateOnLand;
        public PodCameraMode CameraMode;

        public DeploymentRequest(Vector3 targetPosition, Vector3 targetForward, Transform parent)
        {
            TargetPosition = targetPosition;
            TargetForward = targetForward;
            TargetSurfaceNormal = Vector3.up;
            Parent = parent;
            Node = null;
            ImpactTeam = Team.Neutral;
            InstantiateOnLand = true;
            CameraMode = PodCameraMode.None;
        }
    }
    
    
    [DefaultExecutionOrder(-100)]
    public class DeploymentManager : MonoBehaviour
    {
        public static DeploymentManager Instance { get; private set; }
        public static event Action<DeploymentRequest, Pod> OnPodLaunched;

        [Header("Settings")]
        [SerializeField] private float skyDropHeight = 300f;
        [SerializeField] private Pod podPrefab;
        
        private readonly List<ShipCannon> _shipSpawnCannons = new List<ShipCannon>();

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
            Pod prefab = deployable is IDeployableWithPod withPod && withPod.PodPrefab ? withPod.PodPrefab : podPrefab;
            Pod pod = Instantiate(prefab, spawnPosition, Quaternion.identity, _podHolder);
            pod.Initialize(deployable, request, moveInArc);
            OnPodLaunched?.Invoke(request, pod);
        }

        private void SpawnPod(ScriptableObject deployable, DeploymentRequest request, Vector3 spawnPosition, bool moveInArc)
        {
            Pod prefab = deployable is IDeployableWithPod withPod && withPod.PodPrefab ? withPod.PodPrefab : podPrefab;
            Pod pod = Instantiate(prefab, spawnPosition, Quaternion.identity, _podHolder);
            pod.Initialize(deployable, request, moveInArc);
            OnPodLaunched?.Invoke(request, pod);
        }
        
                
        public void RegisterShipCannon(ShipCannon cannon)
        {
            if (_shipSpawnCannons.Contains(cannon)) return;
            
            _shipSpawnCannons.Add(cannon);
        }

        public void UnregisterShipCannon(ShipCannon cannon)
        {
            if (!_shipSpawnCannons.Contains(cannon)) return;
            
            _shipSpawnCannons.Remove(cannon);
        }
        
        public void LaunchFromShip<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            if (_shipSpawnCannons.Count == 0)
            {
                LaunchFromSky(deployable, request);
                return;
            }
            
            var shipPositionIndex = Random.Range(0, _shipSpawnCannons.Count);
            var shipPosition = _shipSpawnCannons[shipPositionIndex];

            shipPosition.PlayEffects();
            SpawnPod(deployable, request, shipPosition.transform.position, true);
        }

        public void LaunchFromSky<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            Vector3 origin = request.TargetPosition + Vector3.up * skyDropHeight;
            SpawnPod(deployable, request, origin, false);
        }

        public void LaunchFromSky(ScriptableObject deployable, DeploymentRequest request)
        {
            Vector3 origin = request.TargetPosition + Vector3.up * skyDropHeight;
            SpawnPod(deployable, request, origin, false);
        }

        public void LaunchFromShip(ScriptableObject deployable, DeploymentRequest request)
        {
            if (_shipSpawnCannons.Count == 0)
            {
                LaunchFromSky(deployable, request);
                return;
            }
            
            var shipPositionIndex = Random.Range(0, _shipSpawnCannons.Count);
            var shipPosition = _shipSpawnCannons[shipPositionIndex];
            shipPosition.PlayEffects();
            SpawnPod(deployable, request, shipPosition.transform.position, true);
        }
    }
}