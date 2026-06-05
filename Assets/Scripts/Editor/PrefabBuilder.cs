using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility that creates all tile/player prefabs and the clean GameScene.
///
/// Menu: Blockavist ▸ 1. Build All Prefabs
///       Blockavist ▸ 2. Create GameScene
///
/// Run (1) first so the prefabs exist before you open (2) and start placing tiles.
/// </summary>
public static class PrefabBuilder
{
    private const string PrefabDir  = "Assets/Prefabs";
    private const string SpriteDir  = "Assets/Prefabs/Sprites";
    private const string SpritePath = "Assets/Prefabs/Sprites/WhiteSquare.png";
    private const string ScenePath  = "Assets/Scenes/GameScene.unity";

    // ── 1. Build All Prefabs ──────────────────────────────────────────────────

    [MenuItem("Blockavist/1. Build All Prefabs")]
    public static void BuildAllPrefabs()
    {
        Sprite square = EnsureSquareSprite();

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs"));
        AssetDatabase.Refresh();

        CreateTilePrefab<IndestructibleTile>("IndestructibleTile", new Color(0.45f, 0.45f, 0.5f),  square, isTrigger: false);
        CreateTilePrefab<DestructibleTile>  ("DestructibleTile",   new Color(1f,    0.82f, 0.1f),  square, isTrigger: false);
        CreateTilePrefab<SpikeTile>         ("SpikeTile",          new Color(0.9f,  0.15f, 0.15f), square, isTrigger: false);
        CreateTilePrefab<GoalTile>          ("GoalTile",           new Color(0.1f,  0.85f, 0.25f), square, isTrigger: true);
        CreatePlayerPrefab(square);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Blockavist] Prefabs created in Assets/Prefabs/");
    }

    // ── 2. Create GameScene ───────────────────────────────────────────────────

    [MenuItem("Blockavist/2. Create GameScene")]
    public static void CreateGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Configure camera for 2D
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic        = true;
            cam.orthographicSize    = 5f;
            cam.transform.position  = new Vector3(0f, 0f, -10f);
            cam.backgroundColor     = new Color(0.08f, 0.08f, 0.12f);
            cam.clearFlags          = CameraClearFlags.SolidColor;
        }

        // Remove directional light — 2D sprites render without it
        var light = Object.FindAnyObjectByType<Light>();
        if (light != null) Object.DestroyImmediate(light.gameObject);

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("InputHandler").AddComponent<InputHandler>();

        // Add LevelManager and wire all prefab references automatically
        // if the prefabs have already been built via menu item (1).
        var lmGO = new GameObject("LevelManager");
        var lm   = lmGO.AddComponent<LevelManager>();
        WireLevelManagerPrefabs(lm, cam);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        Debug.Log($"[Blockavist] GameScene saved to {ScenePath}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns tile + player prefab references on LevelManager via SerializedObject
    /// so the values are saved into the scene asset.  Also assigns the camera.
    /// Silently skips any prefab that hasn't been built yet.
    /// </summary>
    private static void WireLevelManagerPrefabs(LevelManager lm, Camera cam)
    {
        var so = new SerializedObject(lm);

        AssignPrefab(so, "indestructiblePrefab", $"{PrefabDir}/IndestructibleTile.prefab");
        AssignPrefab(so, "destructiblePrefab",   $"{PrefabDir}/DestructibleTile.prefab");
        AssignPrefab(so, "spikePrefab",          $"{PrefabDir}/SpikeTile.prefab");
        AssignPrefab(so, "goalPrefab",           $"{PrefabDir}/GoalTile.prefab");
        AssignPrefab(so, "playerPrefab",         $"{PrefabDir}/Player.prefab");

        if (cam != null)
            so.FindProperty("gameCamera").objectReferenceValue = cam;

        so.ApplyModifiedProperties();
    }

    private static void AssignPrefab(SerializedObject so, string propName, string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return; // prefabs not built yet — user can assign manually
        so.FindProperty(propName).objectReferenceValue = prefab;
    }

    private static void CreateTilePrefab<T>(
        string name, Color color, Sprite sprite, bool isTrigger) where T : TileElement
    {
        string path = $"{PrefabDir}/{name}.prefab";

        GameObject go = new GameObject(name);

        var sr        = go.AddComponent<SpriteRenderer>();
        sr.sprite     = sprite;
        sr.color      = color;

        var col       = go.AddComponent<BoxCollider2D>();
        col.isTrigger = isTrigger;

        go.AddComponent<T>();

        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);

        if (!ok) Debug.LogError($"[Blockavist] Failed to save prefab: {path}");
    }

    private static void CreatePlayerPrefab(Sprite sprite)
    {
        string path = $"{PrefabDir}/Player.prefab";

        GameObject go = new GameObject("Player");

        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = sprite;
        sr.color     = new Color(0.2f, 0.45f, 1f);
        sr.sortingOrder = 2;

        var col      = go.AddComponent<BoxCollider2D>();
        col.size     = new Vector2(0.85f, 0.85f);

        var rb                      = go.AddComponent<Rigidbody2D>();
        rb.gravityScale             = 3f;
        rb.freezeRotation           = true;
        rb.collisionDetectionMode   = CollisionDetectionMode2D.Continuous;

        go.AddComponent<PlayerController>();

        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);

        if (!ok) Debug.LogError($"[Blockavist] Failed to save Player prefab.");
    }

    /// <summary>
    /// Creates a 4×4 white PNG sprite asset (1 world unit at pixelsPerUnit=4).
    /// Skips creation if the asset already exists.
    /// </summary>
    private static Sprite EnsureSquareSprite()
    {
        if (File.Exists(Path.Combine(Application.dataPath, "Prefabs/Sprites/WhiteSquare.png")))
        {
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/Sprites"));

        Texture2D tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(
            Path.Combine(Application.dataPath, "Prefabs/Sprites/WhiteSquare.png"),
            tex.EncodeToPNG());

        AssetDatabase.Refresh();

        // Configure import settings: 4 ppu → exactly 1 world unit
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode          = FilterMode.Point;
            importer.mipmapEnabled       = false;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
            if (s.path == scenePath) return; // already present

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        existing.CopyTo(updated, 0);
        updated[existing.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
