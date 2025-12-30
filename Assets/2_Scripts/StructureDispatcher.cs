using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class StructureDispatcher : MonoBehaviour
{
    public static StructureDispatcher Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private AudioClip deploySfx;
    
    [Header("References")]
    [SerializeField] private StructurePod podPrefab;
    [SerializeField] private Transform podSpawnPosition;
    [SerializeField] private AudioSource audioSource;

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
        StructurePod pod = Instantiate(podPrefab, podSpawnPosition.position, Quaternion.LookRotation(podSpawnPosition.forward));
        pod.Initialize(structure, targetPosition, surfaceNormal);
    }
}