using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instantiates and tears down level content from a LevelData asset.
/// Lives in GameScene alongside GameManager.  Assign tile/player prefabs
/// in the inspector (or run Cubby's Blocks ▸ 1. Build All Prefabs first, then
/// Cubby's Blocks ▸ 2. Create GameScene to get them wired up automatically).
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject indestructiblePrefab;
    [SerializeField] private GameObject destructiblePrefab;
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject jumpPadPrefab;
    [SerializeField] private GameObject fallingHazardPrefab;
    [SerializeField] private GameObject explosivePrefab;
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private GameObject timedHazardPrefab;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Camera")]
    [SerializeField] private Camera gameCamera;

    // The root GameObject that holds every spawned tile and the player.
    // Destroying it cleanly unloads the entire level.
    private GameObject levelRoot;

    public PlayerController ActivePlayer { get; private set; }
    public LevelData         CurrentData  { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LoadLevel(LevelData data)
    {
        if (data == null)
        {
            // No level data wired up — fall back to full scene reload so
            // TestLevelSetup scenes continue to work unchanged.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        UnloadCurrentLevel();
        CurrentData = data;
        SpawnLevel(data);
    }

    public void UnloadCurrentLevel()
    {
        if (levelRoot != null)
            Destroy(levelRoot);

        levelRoot    = null;
        ActivePlayer = null;
        CurrentData  = null;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SpawnLevel(LevelData data)
    {
        levelRoot = new GameObject($"Level_{data.worldNumber}_{data.levelNumber:00}");

        // DEBUG — confirm what the ScriptableObject actually contains before spawning.
        int tileCount = data.tiles?.Length ?? 0;
        var typeList  = new System.Text.StringBuilder();
        if (data.tiles != null)
            foreach (var e in data.tiles) typeList.Append($"{e.type}({e.gridPosition}) ");
        Debug.Log($"[LevelManager] SpawnLevel '{data.levelName}' — {tileCount} tiles: {typeList}");

        foreach (TileEntry entry in data.tiles)
        {
            // DEBUG — log TimedHazard entries before the null-prefab guard so they're
            // always visible regardless of whether the prefab reference is assigned.
            if (entry.type == TileType.TimedHazard)
                Debug.Log($"[LevelManager] TimedHazard entry found at {entry.gridPosition} — " +
                          $"prefab={(timedHazardPrefab != null ? timedHazardPrefab.name : "NULL — run '1. Build All Prefabs' then '2. Create GameScene', or assign manually in inspector")}");

            GameObject prefab = PrefabForType(entry.type);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelManager] No prefab assigned for TileType.{entry.type} — skipping.");
                continue;
            }

            Vector3    worldPos = new Vector3(entry.gridPosition.x, entry.gridPosition.y, 0f);
            GameObject go       = Instantiate(prefab, worldPos, Quaternion.identity, levelRoot.transform);

            // Guarantee physics tiles start kinematic regardless of saved prefab state.
            var tileRb = go.GetComponent<Rigidbody2D>();
            if (tileRb != null)
            {
                tileRb.bodyType       = RigidbodyType2D.Kinematic;
                tileRb.linearVelocity = Vector2.zero;
            }

            if (entry.type == TileType.Portal)
            {
                var portal = go.GetComponent<PortalTile>();
                // DEBUG — log the id being assigned so we can verify pairs match
                Debug.Log($"[LevelManager] Assigning portalId={entry.extraData} to portal " +
                          $"at {entry.gridPosition} (component {(portal != null ? "found" : "MISSING")})");
                if (portal != null) portal.PortalId = entry.extraData;
            }
        }

        SpawnPlayer(data.playerSpawnPosition);
        PositionCamera(data);
    }

    private void SpawnPlayer(Vector2 spawnPos)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("[LevelManager] Player prefab not assigned.");
            return;
        }

        GameObject obj = Instantiate(
            playerPrefab,
            new Vector3(spawnPos.x, spawnPos.y, 0f),
            Quaternion.identity,
            levelRoot.transform);

        ActivePlayer = obj.GetComponent<PlayerController>();
    }

    private void PositionCamera(LevelData data)
    {
        if (gameCamera == null || data.cameraOrthographicSize <= 0f) return;

        gameCamera.transform.position  = new Vector3(data.cameraCenter.x, data.cameraCenter.y, -10f);
        gameCamera.orthographicSize    = data.cameraOrthographicSize;
    }

    private GameObject PrefabForType(TileType type) => type switch
    {
        TileType.Indestructible => indestructiblePrefab,
        TileType.Destructible   => destructiblePrefab,
        TileType.Spike          => spikePrefab,
        TileType.Goal           => goalPrefab,
        TileType.JumpPad        => jumpPadPrefab,
        TileType.FallingHazard  => fallingHazardPrefab,
        TileType.Explosive      => explosivePrefab,
        TileType.Portal         => portalPrefab,
        TileType.TimedHazard    => timedHazardPrefab,
        _                       => null
    };
}
