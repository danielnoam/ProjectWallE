using UnityEngine;

[RequireComponent(typeof(ResourceGenerator))]
public class Base : Structure
{

    [Header("Base Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float moveSpeed = 0.3f;
    [SerializeField] private Transform obelisk;
    [SerializeField] private ResourceGenerator resourceGenerator;


    private void Start()
    {
        EnemyManager.Instance?.RegisterBase(this);
    }

    private void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterBase(this);
    }
    
    private void Update()
    {
        obelisk.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float sine = Mathf.Sin(Time.time * 2f) * moveSpeed;
        obelisk.localPosition = new Vector3(0f, sine, 0f);
        
        StateInfo = $"Health: {currentHealth}/{startHealth}";
    }


    protected override void OnBuild()
    {
        resourceGenerator.StartGenerating();
    }

    protected override void OnUpgrade()
    {

    }

    protected override void OnBreak()
    {
        resourceGenerator.StopGenerating();
        LevelManager.Instance?.FailLevel();
    }
}
