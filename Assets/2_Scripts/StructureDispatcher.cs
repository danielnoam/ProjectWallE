using DNExtensions;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class StructureDispatcher : MonoBehaviour
{
    public static StructureDispatcher Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private AudioClip deploySfx;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private StructurePod podPrefab;
    [SerializeField] private ChanceList<Transform> podSpawnPositions = new ChanceList<Transform>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    public void DeployPod(Structure structure, Vector3 targetPosition, Vector3 surfaceNormal)
    {
        
        if (deploySfx)
        {
            audioSource.PlayOneShot(deploySfx);
        }
        var spawnPosition = podSpawnPositions.GetRandomItem();
        StructurePod pod = Instantiate(podPrefab, spawnPosition.position, Quaternion.LookRotation(spawnPosition.forward));
        pod.Initialize(structure, targetPosition, surfaceNormal);
    }
}