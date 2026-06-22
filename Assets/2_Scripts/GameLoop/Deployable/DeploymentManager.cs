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
        public Transform DeployableParent;
        public StructureNode Node;
        public Team Team;
        public bool InstantiateOnLand;
        public PodCameraMode CameraMode;

        public DeploymentRequest(Vector3 targetPosition, Vector3 targetForward, Transform deployableParent)
        {
            TargetPosition = targetPosition;
            TargetForward = targetForward;
            TargetSurfaceNormal = Vector3.up;
            DeployableParent = deployableParent;
            Node = null;
            Team = Team.Neutral;
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

        private readonly List<PlayerShipCannon> _shipSpawnCannons = new List<PlayerShipCannon>();
        private readonly Dictionary<EnemyBase, List<EnemyBaseCannon>> _baseCannonMap = new Dictionary<EnemyBase, List<EnemyBaseCannon>>();

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


        public void RegisterShipCannon(PlayerShipCannon cannon)
        {
            if (_shipSpawnCannons.Contains(cannon)) return;
            _shipSpawnCannons.Add(cannon);
        }

        public void UnregisterShipCannon(PlayerShipCannon cannon)
        {
            _shipSpawnCannons.Remove(cannon);
        }

        public void RegisterBaseCannon(EnemyBase ownerBase, EnemyBaseCannon cannon)
        {
            if (!ownerBase) return;

            if (!_baseCannonMap.TryGetValue(ownerBase, out var list))
            {
                list = new List<EnemyBaseCannon>();
                _baseCannonMap[ownerBase] = list;
            }

            if (!list.Contains(cannon)) list.Add(cannon);
        }

        public void UnregisterBaseCannon(EnemyBase ownerBase, EnemyBaseCannon cannon)
        {
            if (!ownerBase) return;
            if (_baseCannonMap.TryGetValue(ownerBase, out var list))
                list.Remove(cannon);
        }

        public EnemyBase GetNearestBase(Vector3 position)
        {
            EnemyBase nearest = null;
            float closestDist = float.MaxValue;

            foreach (var kvp in _baseCannonMap)
            {
                if (!kvp.Key || kvp.Value.Count == 0) continue;

                float dist = Vector3.Distance(position, kvp.Key.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = kvp.Key;
                }
            }

            return nearest;
        }

        private EnemyBaseCannon GetBaseCannon(Vector3 targetPosition)
        {
            EnemyBase nearest = GetNearestBase(targetPosition);
            if (!nearest) return null;

            var cannons = _baseCannonMap[nearest];
            return cannons[Random.Range(0, cannons.Count)];
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

        public void LaunchFromShip<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            if (_shipSpawnCannons.Count == 0)
            {
                LaunchFromSky(deployable, request);
                return;
            }

            var cannon = _shipSpawnCannons[Random.Range(0, _shipSpawnCannons.Count)];
            cannon.PlayEffects();
            SpawnPod(deployable, request, cannon.transform.position, true);
        }

        public void LaunchFromShip(ScriptableObject deployable, DeploymentRequest request)
        {
            if (_shipSpawnCannons.Count == 0)
            {
                LaunchFromSky(deployable, request);
                return;
            }

            var cannon = _shipSpawnCannons[Random.Range(0, _shipSpawnCannons.Count)];
            cannon.PlayEffects();
            SpawnPod(deployable, request, cannon.transform.position, true);
        }

        public void LaunchFromBaseCannon<T>(T deployable, DeploymentRequest request) where T : MonoBehaviour, IDeployable
        {
            var cannon = GetBaseCannon(request.TargetPosition);
            if (!cannon)
            {
                LaunchFromSky(deployable, request);
                return;
            }

            cannon.PlayEffects();
            SpawnPod(deployable, request, cannon.transform.position, true);
        }

        public void LaunchFromBaseCannon(ScriptableObject deployable, DeploymentRequest request)
        {
            var cannon = GetBaseCannon(request.TargetPosition);
            if (!cannon)
            {
                LaunchFromSky(deployable, request);
                return;
            }

            cannon.PlayEffects();
            SpawnPod(deployable, request, cannon.transform.position, true);
        }
    }
}