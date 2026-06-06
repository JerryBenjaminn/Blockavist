using UnityEngine;

/// <summary>
/// Spawns pooled particle effects at world positions.
/// Prefabs are created by VFXBuilder (Cubby's Blocks ▸ 6. Build VFX Prefabs).
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs — assigned by Build VFX Prefabs")]
    [SerializeField] private GameObject tileDestroyPrefab;
    [SerializeField] private GameObject cubbyDeathPrefab;
    [SerializeField] private GameObject goalReachedPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Burst of debris at the tile's position. Color is defined in the prefab.</summary>
    public void SpawnTileDestroy(Vector3 position, Color tint)
    {
        SpawnAndPlay(tileDestroyPrefab, position);
    }

    public void SpawnCubbyDeath(Vector3 position)  => SpawnAndPlay(cubbyDeathPrefab,  position);
    public void SpawnGoalReached(Vector3 position) => SpawnAndPlay(goalReachedPrefab, position);

    private static void SpawnAndPlay(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, position, Quaternion.identity);
        go.GetComponent<ParticleSystem>()?.Play();
    }
}
