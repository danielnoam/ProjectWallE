using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public struct StructuresData
    {
        public int BasesCount;
        public int TurretsCount;
        public int GeneratorsCount;
        public int RampsCount;
        public int TotalStructures => BasesCount + TurretsCount + GeneratorsCount + RampsCount;
    }

    public class StructureManager : MonoBehaviour
    {
        public static StructureManager Instance { get; private set; }
        public static event Action<StructuresData> OnStructureCountChanged;

        [Header("Structures")]
        [SerializeField] private Structure[] allStructures;

        private Transform _structureHolder;
        private Transform _ghostHolder;
        private readonly List<Structure> _structures = new();
        private readonly Dictionary<Type, List<Structure>> _structuresByType = new();
        private readonly Dictionary<Structure, GhostStructure> _ghostInstances = new();
        private GhostStructure _activeGhost;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _ghostHolder = new GameObject("GhostHolder").transform;
            _structureHolder = new GameObject("StructureHolder").transform;
            InitializeGhosts();
        }

        private void OnEnable()
        {
            Structure.OnStructureBroken += HandleStructureStateChanged;
            Structure.OnStructureRevived += HandleStructureStateChanged;
        }

        private void OnDisable()
        {
            Structure.OnStructureBroken -= HandleStructureStateChanged;
            Structure.OnStructureRevived -= HandleStructureStateChanged;
        }

        public void DeployStructureOnGround(Structure structure, Vector3 targetPosition, Vector3 forward, Vector3 surfaceNormal, PodCameraMode podCameraMode)
        {
            var request = new DeploymentRequest(targetPosition, forward, _structureHolder)
            {
                TargetSurfaceNormal = surfaceNormal,
                Team = Team.Player,
                CameraMode = podCameraMode,
            };
            DeploymentManager.Instance?.LaunchFromShip(structure, request);
        }

        public void DeployStructureOnNode(Structure structure, StructureNode node)
        {
            var request = new DeploymentRequest(node.SnapPoint, node.transform.forward, _structureHolder)
            {
                TargetSurfaceNormal = node.transform.up,
                Team = Team.Player,
                Node = node
            };
            DeploymentManager.Instance?.LaunchFromShip(structure, request);
        }

        #region Ghost

        private void InitializeGhosts()
        {
            foreach (var structure in allStructures)
            {
                var ghostPrefab = structure.StructureUIData.GhostPrefab;
                if (!ghostPrefab) continue;

                var ghost = Instantiate(ghostPrefab, _ghostHolder);
                ghost.gameObject.SetActive(false);
                _ghostInstances[structure] = ghost;
            }
        }

        public void SetGhost(Structure structure)
        {
            if (_activeGhost) _activeGhost.gameObject.SetActive(false);

            if (!_ghostInstances.TryGetValue(structure, out GhostStructure ghost))
            {
                _activeGhost = null;
                return;
            }

            _activeGhost = ghost;
        }

        public void HideGhost()
        {
            if (!_activeGhost) return;

            _activeGhost.gameObject.SetActive(false);
            _activeGhost = null;
        }

        public void ShowGhost(Vector3 position, Quaternion rotation, bool canBuild)
        {
            if (!_activeGhost) return;

            _activeGhost.gameObject.SetActive(true);
            _activeGhost.transform.SetPositionAndRotation(position, rotation);
            _activeGhost.SetCanBuild(canBuild);
        }

        #endregion

        #region Structure Registration

        public void RegisterStructure(Structure structure)
        {
            if (_structures.Contains(structure)) return;

            _structures.Add(structure);

            var type = structure.GetType();
            while (type != null && type != typeof(Structure))
            {
                GetOrCreateList(type).Add(structure);
                type = type.BaseType;
            }

            OnStructureCountChanged?.Invoke(BuildCounts());
        }

        public void UnregisterStructure(Structure structure)
        {
            if (!_structures.Remove(structure)) return;

            var type = structure.GetType();
            while (type != null && type != typeof(Structure))
            {
                if (_structuresByType.TryGetValue(type, out var list))
                {
                    list.Remove(structure);
                }
                type = type.BaseType;
            }

            OnStructureCountChanged?.Invoke(BuildCounts());
        }

        private void HandleStructureStateChanged(Structure structure)
        {
            OnStructureCountChanged?.Invoke(BuildCounts());
        }

        private List<Structure> GetOrCreateList(Type type)
        {
            if (!_structuresByType.TryGetValue(type, out var list))
            {
                list = new List<Structure>();
                _structuresByType[type] = list;
            }
            return list;
        }

        private List<Structure> GetList(Type type)
        {
            return _structuresByType.TryGetValue(type, out var list) ? list : null;
        }

        // Turrets and Ramps include subclasses; Bases and Generators are exact type only.
        private StructuresData BuildCounts()
        {
            return new StructuresData
            {
                BasesCount = CountAliveExact<Base>(),
                TurretsCount = CountAlive<Turret>(),
                GeneratorsCount = CountAliveExact<Generator>(),
                RampsCount = CountAlive<Ramp>()
            };
        }

        private int CountAlive<T>() where T : Structure
        {
            var list = GetList(typeof(T));
            if (list == null) return 0;

            int count = 0;
            foreach (var item in list)
            {
                if (item && item.IsAlive) count++;
            }
            return count;
        }

        private int CountAliveExact<T>() where T : Structure
        {
            int count = 0;
            foreach (var item in _structures)
            {
                if (item && item.IsAlive && item.GetType() == typeof(T)) count++;
            }
            return count;
        }

        #endregion

        #region Helpers

        private T GetNearest<T>(Vector3 position, float maxRange = float.MaxValue) where T : Structure
        {
            var list = GetList(typeof(T));
            if (list == null) return null;

            T nearest = null;
            float closestDist = float.MaxValue;

            foreach (var item in list)
            {
                if (!item || !item.IsAlive) continue;

                float dist = Vector3.Distance(position, item.transform.position);
                if (dist > maxRange) continue;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = (T)item;
                }
            }

            return nearest;
        }

        private T GetWeakest<T>(float maxRange = float.MaxValue, Vector3? position = null) where T : Structure
        {
            var list = GetList(typeof(T));
            if (list == null) return null;

            T weakest = null;
            float lowestHealthPercent = float.MaxValue;

            foreach (var item in list)
            {
                if (!item || !item.IsAlive) continue;

                if (position.HasValue && Vector3.Distance(position.Value, item.transform.position) > maxRange) continue;

                float healthPercent = item.CurrentHealth / item.MaxHealth;
                if (healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    weakest = (T)item;
                }
            }

            return weakest;
        }

        private Structure GetNearestAny(Vector3 position, float maxRange = float.MaxValue)
        {
            Structure nearest = null;
            float closestDist = float.MaxValue;

            foreach (var item in _structures)
            {
                if (!item || !item.IsAlive) continue;

                float dist = Vector3.Distance(position, item.transform.position);
                if (dist > maxRange) continue;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = item;
                }
            }

            return nearest;
        }

        private Structure GetWeakestAny(float maxRange = float.MaxValue, Vector3? position = null)
        {
            Structure weakest = null;
            float lowestHealthPercent = float.MaxValue;

            foreach (var item in _structures)
            {
                if (!item || !item.IsAlive) continue;

                if (position.HasValue && Vector3.Distance(position.Value, item.transform.position) > maxRange) continue;

                float healthPercent = item.CurrentHealth / item.MaxHealth;
                if (healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    weakest = item;
                }
            }

            return weakest;
        }

        public Base GetNearestBase(Vector3 position) => GetNearest<Base>(position);
        public Base GetNearestBaseInRange(Vector3 position, float range) => GetNearest<Base>(position, range);
        public Base GetWeakestBase(Vector3 position) => GetWeakest<Base>(float.MaxValue, position);
        public Base GetWeakestBaseInRange(Vector3 position, float range) => GetWeakest<Base>(range, position);
        public Structure GetNearestStructure(Vector3 position) => GetNearestAny(position);
        public Structure GetNearestStructureInRange(Vector3 position, float range) => GetNearestAny(position, range);
        public Structure GetWeakestStructure(Vector3 position) => GetWeakestAny(float.MaxValue, position);
        public Structure GetWeakestStructureInRange(Vector3 position, float range) => GetWeakestAny(range, position);
        public Turret GetNearestTurret(Vector3 position) => GetNearest<Turret>(position);
        public Turret GetNearestTurretInRange(Vector3 position, float range) => GetNearest<Turret>(position, range);

        #endregion
    }
}