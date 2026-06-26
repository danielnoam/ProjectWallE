using DNExtensions.Systems.ObjectPooling;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [SerializeField] private DamageNumberPopup popupPrefab;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Spawn(DamageInfo info)
    {
        if (!popupPrefab) return;

        DamageNumberPopup popup = ObjectPooler.GetObjectFromPool(popupPrefab, info.Position);
        if (!popup) return;

        popup.transform.SetParent(transform, true);
        popup.Show(info);
    }
}
