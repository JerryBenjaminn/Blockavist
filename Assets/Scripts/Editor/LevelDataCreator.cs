using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Blockavist ▸ 3. Create Level Data Assets
///
/// Creates the first five LevelData assets under Assets/Levels/World1/.
/// Run this AFTER "1. Build All Prefabs" so LevelManager prefab refs already exist.
///
/// LEVEL DESIGNS (World 1 — difficulty curve follows CLAUDE.md spec):
///
///   L1  First Steps   — tap the yellow tile; no hazards
///   L2  Turn Around   — indestructible wall; player auto-turns to reach goal
///   L3  Clear the Way — destructible blocks path on return trip
///   L4  Block and Turn — destructible on left, wall on right; tap while heading left
///   L5  The Drop      — upper floor ends mid-air; player falls to lower platform
/// </summary>
public static class LevelDataCreator
{
    private const string OutDir = "Assets/Levels/World1";

    [MenuItem("Blockavist/3. Create Level Data Assets")]
    public static void CreateAllLevels()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Levels/World1"));
        AssetDatabase.Refresh();

        Save(BuildLevel1(), "Level_01_FirstSteps");
        Save(BuildLevel2(), "Level_02_TurnAround");
        Save(BuildLevel3(), "Level_03_ClearTheWay");
        Save(BuildLevel4(), "Level_04_BlockAndTurn");
        Save(BuildLevel5(), "Level_05_TheDrop");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Blockavist] World 1 levels 1–5 created in Assets/Levels/World1/");
    }

    // ── Level definitions ─────────────────────────────────────────────────────

    // L1 — FIRST STEPS
    // Floor -4..4.  Yellow blocker at (3,1).  Green goal at (4,1).
    // Player starts at (-3, 1.5) walking RIGHT.
    // Puzzle: tap the yellow tile before the player reaches it.
    private static LevelData BuildLevel1()
    {
        var d = Make(1, "First Steps", 1);
        d.tiles = Concat(
            Floor(-4, 4, 0),
            T(TileType.Destructible,   3, 1),
            T(TileType.Goal,           4, 1));
        d.playerSpawnPosition    = new Vector2(-3f, 1.5f);
        d.cameraCenter           = new Vector2(0f, 1f);
        d.cameraOrthographicSize = 7f;
        return d;
    }

    // L2 — TURN AROUND
    // Floor -4..4.  Indestructible wall at (4,1) forces player to turn.
    // Goal at (-3,1) — player reaches it on the return trip without any tapping.
    // Player starts at (1, 1.5) walking RIGHT.
    // Puzzle: just watch and understand auto-turn.
    private static LevelData BuildLevel2()
    {
        var d = Make(2, "Turn Around", 1);
        d.tiles = Concat(
            Floor(-4, 4, 0),
            T(TileType.Indestructible, 4, 1),
            T(TileType.Goal,          -3, 1));
        d.playerSpawnPosition    = new Vector2(1f, 1.5f);
        d.cameraCenter           = new Vector2(0f, 1f);
        d.cameraOrthographicSize = 7f;
        return d;
    }

    // L3 — CLEAR THE WAY
    // Floor -4..4.  Right wall (4,1) bounces player back left.
    // Destructible at (-2,1) guards the goal at (-4,1).
    // Player starts at (2, 1.5) walking RIGHT → bounces off wall → heads toward blocker.
    // Puzzle: tap (-2,1) while player is heading left so it can reach the goal.
    private static LevelData BuildLevel3()
    {
        var d = Make(3, "Clear the Way", 1);
        d.tiles = Concat(
            Floor(-4, 4, 0),
            T(TileType.Indestructible,  4, 1),
            T(TileType.Destructible,   -2, 1),
            T(TileType.Goal,           -4, 1));
        d.playerSpawnPosition    = new Vector2(2f, 1.5f);
        d.cameraCenter           = new Vector2(0f, 1f);
        d.cameraOrthographicSize = 7f;
        return d;
    }

    // L4 — BLOCK AND TURN
    // Floor -4..4.  Right wall (4,1).  Destructible at (-1,1) guards goal (-3,1).
    // Player starts at (1, 1.5) walking RIGHT → bounces off right wall → heads left.
    // Puzzle: tap (-1,1) while player heads left, then player walks through to goal.
    private static LevelData BuildLevel4()
    {
        var d = Make(4, "Block and Turn", 1);
        d.tiles = Concat(
            Floor(-4, 4, 0),
            T(TileType.Indestructible,  4, 1),
            T(TileType.Destructible,   -1, 1),
            T(TileType.Goal,           -3, 1));
        d.playerSpawnPosition    = new Vector2(1f, 1.5f);
        d.cameraCenter           = new Vector2(0f, 1f);
        d.cameraOrthographicSize = 7f;
        return d;
    }

    // L5 — THE DROP
    // Upper floor -4..0 (y=0).  Gap at x=1.  Lower platform 1..5 (y=-4).
    // Player falls off the upper floor edge and lands on the lower platform.
    // Goal at (3, -3).  No tapping needed — introduces the falling mechanic.
    // Camera pulled back and shifted down to show both floors.
    private static LevelData BuildLevel5()
    {
        var d = Make(5, "The Drop", 1);
        d.tiles = Concat(
            Floor(-4,  0, 0),   // upper floor
            Floor( 1,  5, -4),  // lower platform
            T(TileType.Goal, 3, -3));
        d.playerSpawnPosition    = new Vector2(-2f, 1.5f);
        d.cameraCenter           = new Vector2(0f, -1f);
        d.cameraOrthographicSize = 8f;
        return d;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LevelData Make(int num, string name, int world)
    {
        var d         = ScriptableObject.CreateInstance<LevelData>();
        d.levelNumber = num;
        d.levelName   = name;
        d.worldNumber = world;
        return d;
    }

    private static void Save(LevelData d, string fileName)
    {
        string path = $"{OutDir}/{fileName}.asset";
        AssetDatabase.CreateAsset(d, path);
    }

    /// <summary>Row of indestructible floor tiles from xMin to xMax inclusive at given y.</summary>
    /// <summary>Row of indestructible floor tiles from xMin to xMax inclusive at given y.</summary>
    private static TileEntry[] Floor(int xMin, int xMax, int y)
    {
        var list = new List<TileEntry>();
        for (int x = xMin; x <= xMax; x++)
            list.Add(new TileEntry { type = TileType.Indestructible, gridPosition = new Vector2Int(x, y) });
        return list.ToArray();
    }

    /// <summary>Single TileEntry convenience factory.</summary>
    private static TileEntry T(TileType type, int x, int y) =>
        new TileEntry { type = type, gridPosition = new Vector2Int(x, y) };

    /// <summary>
    /// Merges any mix of TileEntry and TileEntry[] into one flat array.
    /// Accepts TileEntry[] from Floor() and bare TileEntry from T().
    /// </summary>
    private static TileEntry[] Concat(params object[] items)
    {
        var result = new List<TileEntry>();
        foreach (var item in items)
        {
            if      (item is TileEntry   single) result.Add(single);
            else if (item is TileEntry[] array)  result.AddRange(array);
        }
        return result.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // EXAMPLE LEVELS  (landscape mobile  16:9  ortho-size 5)
    //
    // Camera constraints:
    //   Orthographic size : 5   (shows 10 units height, ~17.8 units width at 16:9)
    //   Safe play area    : x[-7..7]  y[-4..4]
    //   Tile size         : 1×1 world unit
    //   Player spawn      : left side x ≈ -6,  y = floor_y + 1.5
    //   Goal              : right side x ≈ 6,  y = floor_y + 1  (trigger)
    //
    // Difficulty curve:
    //   EL1 – tap the yellow tile (intro to destructibles)
    //   EL2 – indestructible wall forces auto-turn (intro to turning)
    //   EL3 – tap while player heads back left (timing + destructible)
    //   EL4 – two destructibles; open the left gate at the right moment
    //   EL5 – spike on upper floor; tap the bridge tile to fall safely to goal
    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Blockavist/4. Create Example Levels")]
    public static void CreateExampleLevels()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Levels/World1"));
        AssetDatabase.Refresh();

        Save(BuildExample1(), "ExampleLevel_1");
        Save(BuildExample2(), "ExampleLevel_2");
        Save(BuildExample3(), "ExampleLevel_3");
        Save(BuildExample4(), "ExampleLevel_4");
        Save(BuildExample5(), "ExampleLevel_5");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Blockavist] Example levels 1–5 created in Assets/Levels/World1/");
    }

    // ── EL1 — FIRST TAP ──────────────────────────────────────────────────────
    // Full floor -7..7.  One yellow blocker at (3,1) — goal at (6,1) behind it.
    // Player starts at (-6,1.5) walking RIGHT.
    // Puzzle: tap the yellow tile before the player reaches it.
    private static LevelData BuildExample1()
    {
        var d = Make(1, "First Tap", 1);
        d.tiles = Concat(
            Floor(-7, 7, 0),
            T(TileType.Destructible,   3, 1),
            T(TileType.Goal,           6, 1));
        d.playerSpawnPosition    = new Vector2(-6f, 1.5f);
        d.cameraCenter           = new Vector2(0f,  1f);
        d.cameraOrthographicSize = 5f;
        return d;
    }

    // ── EL2 — WALL BOUNCE ────────────────────────────────────────────────────
    // Full floor.  Indestructible wall at (6,1) forces player to turn left.
    // Goal at (-5,1) is reached automatically on the return trip.
    // Puzzle: none — just watch and understand the auto-turn mechanic.
    private static LevelData BuildExample2()
    {
        var d = Make(2, "Wall Bounce", 1);
        d.tiles = Concat(
            Floor(-7, 7, 0),
            T(TileType.Indestructible, 6,  1),
            T(TileType.Goal,          -5,  1));
        d.playerSpawnPosition    = new Vector2(-6f, 1.5f);
        d.cameraCenter           = new Vector2(0f,  1f);
        d.cameraOrthographicSize = 5f;
        return d;
    }

    // ── EL3 — TIMED GATE ─────────────────────────────────────────────────────
    // Player starts right-of-centre, hits right wall, turns left toward blocker.
    // Goal is behind the blocker on the left.
    // Puzzle: tap (-2,1) while the player is heading left so it can reach (-5,1).
    private static LevelData BuildExample3()
    {
        var d = Make(3, "Timed Gate", 1);
        d.tiles = Concat(
            Floor(-7, 7, 0),
            T(TileType.Indestructible,  6, 1),   // right wall — bounce point
            T(TileType.Destructible,   -2, 1),   // gate blocking the goal
            T(TileType.Goal,           -5, 1));
        d.playerSpawnPosition    = new Vector2(2f, 1.5f);
        d.cameraCenter           = new Vector2(0f, 1f);
        d.cameraOrthographicSize = 5f;
        return d;
    }

    // ── EL4 — DOUBLE GATE ────────────────────────────────────────────────────
    // Two destructible blockers.  Right blocker forces an early turn; player
    // then bounces between the two.  Tap the LEFT gate while heading left.
    // Puzzle: recognise that only the left gate needs removing; time the tap.
    private static LevelData BuildExample4()
    {
        var d = Make(4, "Double Gate", 1);
        d.tiles = Concat(
            Floor(-7, 7, 0),
            T(TileType.Indestructible,  6,  1),  // right wall
            T(TileType.Destructible,    3,  1),  // right blocker — creates a short bounce
            T(TileType.Destructible,   -2,  1),  // left gate — must remove to reach goal
            T(TileType.Goal,           -5,  1));
        d.playerSpawnPosition    = new Vector2(-4f, 1.5f);
        d.cameraCenter           = new Vector2(0f,  1f);
        d.cameraOrthographicSize = 5f;
        return d;
    }

    // ── EL5 — THE BRIDGE ─────────────────────────────────────────────────────
    // Split two-floor level.  Upper floor has a ONE-TILE destructible bridge
    // at (0,0) and a spike wall at (4,1) — the player dies if they cross the
    // bridge without removing it and continue right.
    //
    // Layout:
    //   Upper floor  : indestructible  x=-7..-1  and  x=1..6  (y=0)
    //   Bridge tile  : destructible    x=0            (y=0)   ← tap this
    //   Spike wall   : spike           x=4            (y=1)   ← instant kill
    //   Lower floor  : indestructible  x=-1..7        (y=-3)
    //   Goal         : y=-2 at x=5 (one unit above lower floor)
    //
    // Solution: tap the bridge (0,0) before the player walks onto it.
    //   The player falls through the gap, lands on the lower floor, walks to goal.
    // If you miss: player crosses to x=4 → spike → game over.
    private static LevelData BuildExample5()
    {
        var d = Make(5, "The Bridge", 1);
        d.tiles = Concat(
            Floor(-7, -1, 0),               // upper floor left
            T(TileType.Destructible,   0,  0),  // bridge tile — tap to open gap
            Floor(1, 6, 0),                 // upper floor right
            T(TileType.Spike,          4,  1),  // spike wall — threatens player on upper floor
            Floor(-1, 7, -3),               // lower platform
            T(TileType.Goal,           5, -2)); // goal on lower platform
        d.playerSpawnPosition    = new Vector2(-5f, 1.5f);
        d.cameraCenter           = new Vector2(0f, -0.5f);
        d.cameraOrthographicSize = 5f;
        return d;
    }
}
