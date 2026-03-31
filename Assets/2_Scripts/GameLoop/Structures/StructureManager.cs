using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;

public struct StructuresData
{
    public int BasesCount;
    public int TurretsCount;
    public int GeneratorsCount;
    public int TotalStructures => BasesCount + TurretsCount + GeneratorsCount;
}

public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance { get; private set; }
    public static event Action<StructuresData> OnStructureCreated;
    public static event Action<StructuresData> OnStructureDestroyed;
    
    [Header("Settings")]
    [SerializeField] private Pod podPrefab;
    [SerializeField] private ChanceList<Transform> podSpawnPositions = new ChanceList<Transform>();

    private Transform _structureHolder;
    private readonly List<Structure> _structures = new List<Structure>();
    private readonly List<Generator> _generators = new List<Generator>();
    private readonly List<Turret> _turrets = new List<Turret>();
    private readonly List<Base> _bases = new List<Base>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        _structureHolder = new GameObject("StructureHolder").transform;
    }
    
    public void DeployPod(Structure structure, Vector3 targetPosition, Vector3 forward)
    {
        var spawnPosition = podSpawnPositions.GetRandomItem();
        Pod pod = Instantiate(podPrefab, spawnPosition.position, Quaternion.LookRotation(spawnPosition.forward));
        pod.Initialize(structure, targetPosition, forward, _structureHolder);
    }
    
    #region Structure Registration
    
    public void RegisterStructure(Structure structure)
    {
        if (_structures.Contains(structure)) return;
        
        _structures.Add(structure);
        
        switch (structure)
        {
            case Turret turret:
                RegisterTurret(turret);
                break;
            case Base baseStructure:
                RegisterBase(baseStructure);
                break;
            case Generator generator:
                RegisterGenerator(generator);
                break;
        }
        
        OnStructureCreated?.Invoke(new StructuresData
        {
            BasesCount = _bases.Count,
            TurretsCount = _turrets.Count,
            GeneratorsCount = _generators.Count
        });
    }
    
    public void UnregisterStructure(Structure structure)
    {
        if (!_structures.Contains(structure)) return;
        
        _structures.Remove(structure);
        
        switch (structure)
        {
            case Turret turret:
                UnregisterTurret(turret);
                break;
            case Base baseStructure:
                UnregisterBase(baseStructure);
                break;
                case Generator generator:
            UnregisterGenerator(generator);
                    break;
            
        }
        
        OnStructureDestroyed?.Invoke(new StructuresData
        {
            BasesCount = _bases.Count,
            TurretsCount = _turrets.Count,
            GeneratorsCount = _generators.Count
        });
    }
    
    private void RegisterTurret(Turret turret)
    {
        if (_turrets.Contains(turret)) return;
        _turrets.Add(turret);
    }
    
    private void UnregisterTurret(Turret turret)
    {
        if (!_turrets.Contains(turret)) return;
        _turrets.Remove(turret);
    }

    private void RegisterGenerator(Generator generator)
    {
        if (_generators.Contains(generator)) return;
        _generators.Add(generator);
    }

    private void UnregisterGenerator(Generator generator)
    {
        if (!_generators.Contains(generator)) return;
        _generators.Remove(generator);
    }
    
    private void RegisterBase(Base baseStructure)
    {
        if (_bases.Contains(baseStructure)) return;
        _bases.Add(baseStructure);
    }
    
    private void UnregisterBase(Base baseStructure)
    {
        if (!_bases.Contains(baseStructure)) return;
        _bases.Remove(baseStructure);
    }
    
    #endregion

    #region Helpers
    
    private T GetNearest<T>(List<T> list, Vector3 position, float maxRange = float.MaxValue) where T : MonoBehaviour
    {
        T nearest = null;
        float closestDist = float.MaxValue;

        foreach (var item in list)
        {
            if (!item) continue;

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
    
    private T GetWeakest<T>(List<T> list, float maxRange = float.MaxValue, Vector3? position = null) where T : Structure
    {
        T weakest = null;
        float lowestHealthPercent = float.MaxValue;

        foreach (var item in list)
        {
            if (!item) continue;

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
    
    public Base GetNearestBase(Vector3 position) => GetNearest(_bases, position);
    public Base GetNearestBaseInRange(Vector3 position, float range) => GetNearest(_bases, position, range);
    public Base GetWeakestBase(Vector3 position) => GetWeakest(_bases, float.MaxValue, position);
    public Base GetWeakestBaseInRange(Vector3 position, float range) => GetWeakest(_bases, range, position);
    public Structure GetNearestStructure(Vector3 position) => GetNearest(_structures, position);
    public Structure GetNearestStructureInRange(Vector3 position, float range) => GetNearest(_structures, position, range);
    public Structure GetWeakestStructure(Vector3 position) => GetWeakest(_structures, float.MaxValue, position);
    public Structure GetWeakestStructureInRange(Vector3 position, float range) => GetWeakest(_structures, range, position);
    public Turret GetNearestTurret(Vector3 position) => GetNearest(_turrets, position);
    public Turret GetNearestTurretInRange(Vector3 position, float range) => GetNearest(_turrets, position, range);

    #endregion
}