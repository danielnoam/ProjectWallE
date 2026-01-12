using System.Collections.Generic;
using DNExtensions;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StructureManager : MonoBehaviour
{
    public static StructureManager Instance { get; private set; }
    
    [Header("Deployment Settings")]
    [SerializeField] private AudioClip deploySfx;
    [SerializeField] private StructurePod podPrefab;
    [SerializeField] private ChanceList<Transform> podSpawnPositions = new ChanceList<Transform>();
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Tracked Structures")]
    [SerializeField] private List<Structure> allStructures = new List<Structure>();
    [SerializeField] private List<Generator> generators = new List<Generator>();
    [SerializeField] private List<Turret> turrets = new List<Turret>();
    [SerializeField] private List<Base> bases = new List<Base>();
    
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    
    #region Deployment 
    
    public void DeployPod(Structure structure, Vector3 targetPosition, Vector3 surfaceNormal)
    {
        if (deploySfx)
        {
            audioSource?.PlayOneShot(deploySfx);
        }
        
        var spawnPosition = podSpawnPositions.GetRandomItem();
        StructurePod pod = Instantiate(podPrefab, spawnPosition.position, Quaternion.LookRotation(spawnPosition.forward));
        pod.Initialize(structure, targetPosition, surfaceNormal);
    }
    
    #endregion
    
    
    
    #region Structure Registration
    
    public void RegisterStructure(Structure structure)
    {
        if (allStructures.Contains(structure)) return;
        
        allStructures.Add(structure);
        
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
    }
    
    public void UnregisterStructure(Structure structure)
    {
        if (!allStructures.Contains(structure)) return;
        
        allStructures.Remove(structure);
        
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
    }
    
    private void RegisterTurret(Turret turret)
    {
        if (turrets.Contains(turret)) return;
        turrets.Add(turret);
    }
    
    private void UnregisterTurret(Turret turret)
    {
        if (!turrets.Contains(turret)) return;
        turrets.Remove(turret);
    }

    private void RegisterGenerator(Generator generator)
    {
        if (generators.Contains(generator)) return;
        generators.Add(generator);
    }

    private void UnregisterGenerator(Generator generator)
    {
        if (!generators.Contains(generator)) return;
        generators.Remove(generator);
    }
    
    private void RegisterBase(Base baseStructure)
    {
        if (bases.Contains(baseStructure)) return;
        bases.Add(baseStructure);
    }
    
    private void UnregisterBase(Base baseStructure)
    {
        if (!bases.Contains(baseStructure)) return;
        bases.Remove(baseStructure);
    }
    
    #endregion
    
    
    
    #region Query Methods

    public Turret GetNearestTurret(Vector3 position)
    {
        Turret nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var turret in turrets)
        {
            if (!turret) continue;
            
            float dist = Vector3.Distance(position, turret.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = turret;
            }
        }
        
        return nearest;
    }
    
    
    public Turret GetNearestTurretInRange(Vector3 position, float  maxRange)
    {
        Turret nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var turret in turrets)
        {
            if (!turret) continue;
            
            float dist = Vector3.Distance(position, turret.transform.position);
            if (dist > maxRange) continue;
            
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = turret;
            }
        }
        
        return nearest;
    }

    public Generator GetNearestGeneratorInRange(Vector3 position, float maxRange)
    {
        
        Generator nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var generator in generators)
        {
            if (!generator) continue;
            
            float dist = Vector3.Distance(position, generator.transform.position);
            if (dist > maxRange) continue;
            
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = generator;
            }
        }
        
        return nearest;
    }
    
    public Base GetNearestBase(Vector3 position)
    {
        Base nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var baseStructure in bases)
        {
            if (!baseStructure) continue;
            
            float dist = Vector3.Distance(position, baseStructure.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = baseStructure;
            }
        }
        
        return nearest;
    }
    
    public Structure GetNearestStructure(Vector3 position)
    {
        Structure nearest = null;
        float closestDist = float.MaxValue;
        
        foreach (var structure in allStructures)
        {
            if (!structure) continue;
            
            float dist = Vector3.Distance(position, structure.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = structure;
            }
        }
        
        return nearest;
    }
    

    public IDamageable GetWeakestStructureInRange(Vector3 position, float maxRange)
    {
        IDamageable weakest = null;
        float lowestHealthPercent = float.MaxValue;
        
        foreach (var structure in allStructures)
        {
            if (!(structure is IDamageable damageable)) continue;
            if (!structure) continue;
            
            float dist = Vector3.Distance(position, structure.transform.position);
            if (dist > maxRange) continue;
            
            float healthPercent = structure.CurrentHealth / structure.MaxHealth;
            if (healthPercent < lowestHealthPercent)
            {
                lowestHealthPercent = healthPercent;
                weakest = damageable;
            }
        }
        
        return weakest;
    }
    
    public Base GetWeakestBase()
    {
        Base weakest = null;
        float lowestHealthPercent = float.MaxValue;
        
        foreach (var baseStructure in bases)
        {
            if (!baseStructure) continue;
            
            float healthPercent = baseStructure.CurrentHealth / baseStructure.MaxHealth;
            if (healthPercent < lowestHealthPercent)
            {
                lowestHealthPercent = healthPercent;
                weakest = baseStructure;
            }
        }
        
        return weakest;
    }
    

    
    
    public List<Structure> GetStructuresInRange(Vector3 position, float range)
    {
        List<Structure> inRange = new List<Structure>();
        
        foreach (var structure in allStructures)
        {
            if (!structure) continue;
            
            float dist = Vector3.Distance(position, structure.transform.position);
            if (dist <= range)
            {
                inRange.Add(structure);
            }
        }
        
        return inRange;
    }
    
    public List<Turret> GetTurretsInRange(Vector3 position, float range)
    {
        List<Turret> inRange = new List<Turret>();
        
        foreach (var turret in turrets)
        {
            if (!turret) continue;
            
            float dist = Vector3.Distance(position, turret.transform.position);
            if (dist <= range)
            {
                inRange.Add(turret);
            }
        }
        
        return inRange;
    }
    
    #endregion
}