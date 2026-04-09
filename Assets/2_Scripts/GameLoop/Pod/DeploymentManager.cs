using DNExtensions.Utilities;
using UnityEngine;

namespace ProjectWallE.GameLoop
{
    public class DeploymentManager : MonoBehaviour
    {
        public static DeploymentManager Instance { get; private set; }

        [Header("Pod")]
        [SerializeField] private Pod podPrefab;

        [Header("Deployment")]
        [SerializeField] private float skyDropHeight = 150f;
        [SerializeField] private ChanceList<Transform> shipSpawnPositions = new ChanceList<Transform>();

        private Transform _structureHolder;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _structureHolder = new GameObject("StructureHolder").transform;
        }

        public void DeployStructureOnGround(Structure structure, Vector3 targetPosition, Vector3 forward, Vector3 surfaceNormal)
        {
            var spawnPosition = shipSpawnPositions.GetRandomItem();
            Pod pod = Instantiate(podPrefab, spawnPosition.position, Quaternion.LookRotation(spawnPosition.forward));
            pod.Initialize(structure, targetPosition, forward, _structureHolder, surfaceNormal: surfaceNormal);
        }

        public void DeployStructureOnNode(Structure structure, StructureNode node)
        {
            var spawnPosition = shipSpawnPositions.GetRandomItem();
            Pod pod = Instantiate(podPrefab, spawnPosition.position, Quaternion.LookRotation(spawnPosition.forward));
            pod.Initialize(structure, node.SnapPoint, node.transform.forward, _structureHolder, node, node.transform.transform.up);
        }

        public void DeployEnemy(Enemy enemy, Vector3 targetPosition, Transform parent)
        {
            Vector3 origin = targetPosition + Vector3.up * skyDropHeight;
            Pod pod = Instantiate(podPrefab, origin, Quaternion.LookRotation(Vector3.down));
            pod.Initialize(enemy, targetPosition, Vector3.forward, parent, damageOnImpact: false);
        }
    }
}