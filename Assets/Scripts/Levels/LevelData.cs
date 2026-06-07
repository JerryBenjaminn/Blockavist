using UnityEngine;

// ── Tile catalogue ────────────────────────────────────────────────────────────

public enum TileType
{
    Indestructible = 0,
    Destructible   = 1,
    Spike          = 2,
    Goal           = 3,
    JumpPad        = 4,
    FallingHazard  = 5,
    Explosive      = 6,
    Portal         = 7,
}

[System.Serializable]
public struct TileEntry
{
    public TileType    type;
    public Vector2Int  gridPosition; // integer grid coords; world pos = (x, y, 0)
    public int         extraData;    // Portal: pair ID (0, 1, 2…); other types: unused
}

// ── Level definition ──────────────────────────────────────────────────────────

/// <summary>
/// All data needed to build one level.  One .asset file per level.
/// Adding a new level = new asset, zero code changes.
/// </summary>
[CreateAssetMenu(fileName = "Level_01", menuName = "Cubby's Blocks/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int    levelNumber = 1;
    public string levelName   = "New Level";
    public int    worldNumber = 1;

    [Header("Layout")]
    public TileEntry[] tiles;
    public Vector2     playerSpawnPosition;

    [Header("Camera")]
    public Vector2 cameraCenter          = Vector2.zero;
    public float   cameraOrthographicSize = 7f;
}
