using UnityEngine;

/// <summary>
/// Builds the Phase 1 test level at runtime using only primitives.
///
/// HOW TO USE:
///   1. Open SampleScene (or any empty scene).
///   2. Create an empty GameObject, name it "TestLevelSetup".
///   3. Attach this script to it.
///   4. Press Play.
///
/// TEST LEVEL LAYOUT:
///   Floor  : y=0, x=-4..4  (gray indestructible tiles)
///   Blocker: x=2,  y=1     (yellow destructible tile — tap it to remove)
///   Goal   : x=3,  y=1     (green trigger — walk into it to win)
///   Spike  : x=-2, y=1     (red spike — player dies if not avoided)
///   Player : starts at x=-3, walking RIGHT
///
/// EXPECTED PLAY THROUGH:
///   - Player walks right, hits yellow blocker, turns LEFT.
///   - Player walks left off the floor edge, falls → Game Over.
///   - Restart: tap the yellow tile before the player reaches it.
///   - Player walks through to the green goal → Level Complete.
///   - The red spike is on the LEFT return path — don't restart into it.
/// </summary>
public class TestLevelSetup : MonoBehaviour
{
    private static Sprite sharedSquare;

    private void Awake()
    {
        sharedSquare = MakeSquareSprite();
        EnsureManagers();
        BuildLevel();
        PositionCamera();
    }

    // Creates managers only when they are absent — safe to use in scenes that
    // already have GameManager/InputHandler placed via the editor (e.g. GameScene).
    private void EnsureManagers()
    {
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        if (FindAnyObjectByType<InputHandler>() == null)
            new GameObject("InputHandler").AddComponent<InputHandler>();
    }

    private void BuildLevel()
    {
        // Floor row
        for (int x = -4; x <= 4; x++)
            MakeTile<IndestructibleTile>(x, 0);

        // Obstacles / interactive elements at y=1 (player walk height)
        MakeTile<SpikeTile>(-2, 1);                     // red hazard on left return path
        MakeTile<DestructibleTile>(2, 1);               // yellow blocker

        // Goal — must be a trigger so player walks through rather than bouncing off
        GameObject goalObj = MakeTile<GoalTile>(3, 3);
        goalObj.GetComponent<BoxCollider2D>().isTrigger = true;

        // Player
        MakePlayer(-1f, 1.5f);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private GameObject MakeTile<T>(float x, float y) where T : TileElement
    {
        GameObject obj = new GameObject(typeof(T).Name);
        obj.transform.position = new Vector3(x, y, 0f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSquare;

        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<T>();

        return obj;
    }

    private void MakePlayer(float x, float y)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(x, y, 0f);

        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSquare;
        sr.color = new Color(0.2f, 0.45f, 1f);
        sr.sortingOrder = 2;

        // Rigidbody2D and BoxCollider2D are configured inside PlayerController.Awake()
        player.AddComponent<BoxCollider2D>();
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<PlayerController>();
    }

    private void PositionCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0f, 1.5f, -10f);
        cam.orthographicSize = 7f;
    }

    /// <summary>Creates a 1×1 world-unit white square sprite from a tiny texture.</summary>
    private static Sprite MakeSquareSprite()
    {
        const int size = 4;
        Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        // pixelsPerUnit = size → sprite is exactly 1 world unit wide/tall
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
