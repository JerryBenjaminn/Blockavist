using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility that creates all tile/player prefabs and the clean GameScene.
///
/// Menu: Cubby's Blocks ▸ 1. Build All Prefabs
///       Cubby's Blocks ▸ 2. Create GameScene
///
/// Run (1) first so the prefabs exist before you open (2) and start placing tiles.
/// </summary>
public static class PrefabBuilder
{
    private const string PrefabDir     = "Assets/Prefabs";
    private const string AnimationsDir = "Assets/Animations";
    private const string ScenePath     = "Assets/Scenes/GameScene.unity";
    private const string ArtDir        = "Assets/Art/Cubby";

    // ── 1. Build All Prefabs ──────────────────────────────────────────────────

    [MenuItem("Cubby's Blocks/1. Build All Prefabs")]
    public static void BuildAllPrefabs()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Animations"));
        AssetDatabase.Refresh();

        // Load all real sprites
        Sprite sprDestructible   = LoadSprite("tile_destructible");
        Sprite sprIndestructible = LoadSprite("tile_indestructible");
        Sprite sprHazard         = LoadSprite("tile_hazard");
        Sprite sprHazardFace     = LoadSprite("hazard_face_angry");
        Sprite sprGoal           = LoadSprite("tile_goal");
        Sprite sprGoalFace       = LoadSprite("cubby_face_smile_big");
        Sprite sprBody           = LoadSprite("cubby_body");
        Sprite sprFaceHappy      = LoadSprite("cubby_face_happy");
        Sprite sprFaceShocked    = LoadSprite("cubby_face_shocked");
        Sprite sprFaceSmile      = LoadSprite("cubby_face_smile_big");
        Sprite sprPeaceSign      = LoadSprite("cubby_hand_peace");

        CreateSimpleTilePrefab<IndestructibleTile>("IndestructibleTile", sprIndestructible, isTrigger: false, scale: 1f);
        CreateSimpleTilePrefab<DestructibleTile>  ("DestructibleTile",   sprDestructible,   isTrigger: false, scale: 1f);
        CreateFacedTilePrefab<SpikeTile>          ("SpikeTile",  sprHazard, sprHazardFace, isTrigger: false, scale: 0.8f);
        CreateFacedTilePrefab<GoalTile>           ("GoalTile",   sprGoal,   sprGoalFace,   isTrigger: true,  scale: 1f);
        CreatePlayerPrefab(sprBody, sprFaceHappy, sprFaceShocked, sprFaceSmile, sprPeaceSign);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Cubby's Blocks] Prefabs built in Assets/Prefabs/");
    }

    // ── 2. Create GameScene ───────────────────────────────────────────────────

    [MenuItem("Cubby's Blocks/2. Create GameScene")]
    public static void CreateGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic       = true;
            cam.orthographicSize   = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor    = new Color(0.08f, 0.08f, 0.12f);
            cam.clearFlags         = CameraClearFlags.SolidColor;
        }

        var light = Object.FindAnyObjectByType<Light>();
        if (light != null) Object.DestroyImmediate(light.gameObject);

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("InputHandler").AddComponent<InputHandler>();

        var lmGO = new GameObject("LevelManager");
        var lm   = lmGO.AddComponent<LevelManager>();
        WireLevelManagerPrefabs(lm, cam);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);

        Debug.Log($"[Cubby's Blocks] GameScene saved to {ScenePath}");
    }

    // ── Tile Helpers ──────────────────────────────────────────────────────────

    private static void CreateSimpleTilePrefab<T>(
        string name, Sprite sprite, bool isTrigger, float scale) where T : TileElement
    {
        string path = $"{PrefabDir}/{name}.prefab";
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = Color.white;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = isTrigger;

        go.AddComponent<T>();

        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);
        if (!ok) Debug.LogError($"[Cubby's Blocks] Failed to save: {path}");
    }

    private static void CreateFacedTilePrefab<T>(
        string name, Sprite bodySprite, Sprite faceSprite,
        bool isTrigger, float scale) where T : TileElement
    {
        string path = $"{PrefabDir}/{name}.prefab";
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = bodySprite;
        sr.color        = Color.white;
        sr.sortingOrder = 0;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = isTrigger;
        go.AddComponent<T>();

        // Face child layered on top
        var faceGO = new GameObject("Face");
        faceGO.transform.SetParent(go.transform, false);
        var faceSR = faceGO.AddComponent<SpriteRenderer>();
        faceSR.sprite       = faceSprite;
        faceSR.sortingOrder = 1;

        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);
        if (!ok) Debug.LogError($"[Cubby's Blocks] Failed to save: {path}");
    }

    // ── Player Helper ─────────────────────────────────────────────────────────

    private static void CreatePlayerPrefab(
        Sprite body, Sprite faceHappy, Sprite faceShocked,
        Sprite faceSmile, Sprite peaceSign)
    {
        string path = $"{PrefabDir}/Player.prefab";
        var go = new GameObject("Player");

        // Physics
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 3f;
        rb.freezeRotation         = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<CapsuleCollider2D>(); // size set to (0.85, 0.85) in PlayerController.Awake

        // PlayerController — wire face sprites via SerializedObject so they're saved into the prefab
        var pc = go.AddComponent<PlayerController>();
        var so = new SerializedObject(pc);
        so.FindProperty("faceHappy")   .objectReferenceValue = faceHappy;
        so.FindProperty("faceShocked") .objectReferenceValue = faceShocked;
        so.FindProperty("faceSmileBig").objectReferenceValue = faceSmile;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Visual child — Animator bob lives here so physics root is unaffected
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);

        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = EnsureBobAnimator();

        // Body
        var bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(visual.transform, false);
        var bodySR = bodyGO.AddComponent<SpriteRenderer>();
        bodySR.sprite       = body;
        bodySR.sortingOrder = 2;

        // Face
        var faceGO = new GameObject("Face");
        faceGO.transform.SetParent(visual.transform, false);
        var faceSR = faceGO.AddComponent<SpriteRenderer>();
        faceSR.sprite       = faceHappy;
        faceSR.sortingOrder = 3;

        // PeaceSign (raised hand during countdown — starts hidden)
        var peaceGO = new GameObject("PeaceSign");
        peaceGO.transform.SetParent(visual.transform, false);
        peaceGO.transform.localPosition = new Vector3(0.4f, 0.3f, 0f);
        peaceGO.transform.localScale    = new Vector3(0.6f, 0.6f, 1f);
        var peaceSR = peaceGO.AddComponent<SpriteRenderer>();
        peaceSR.sprite       = peaceSign;
        peaceSR.sortingOrder = 3;
        peaceGO.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);
        if (!ok) Debug.LogError("[Cubby's Blocks] Failed to save Player prefab.");
    }

    /// <summary>Creates (or loads) Assets/Animations/CubbyBob.controller with a looping Y-position bob clip.</summary>
    private static AnimatorController EnsureBobAnimator()
    {
        string ctrlPath = $"{AnimationsDir}/CubbyBob.controller";
        string clipPath = $"{AnimationsDir}/CubbyBob.anim";

        // Clip
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "CubbyBob" };

            // Smooth 0 → peak → 0 over ~0.55 seconds, looping at ~1.8 Hz
            var curve = new AnimationCurve(
                new Keyframe(0f,     0f,    0f, 0f),
                new Keyframe(0.275f, 0.07f, 0f, 0f),
                new Keyframe(0.55f,  0f,    0f, 0f)
            );
            // Set tangent mode to clamped-auto for smooth sine-like shape
            for (int i = 0; i < curve.length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);

            clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", curve);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, clipPath);
        }

        // Controller
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm    = ctrl.layers[0].stateMachine;
            var state = sm.AddState("Bob");
            state.motion = clip;
        }

        return ctrl;
    }

    // ── Scene Wiring ──────────────────────────────────────────────────────────

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
        if (prefab == null) return;
        so.FindProperty(propName).objectReferenceValue = prefab;
    }

    private static Sprite LoadSprite(string name)
    {
        string path = $"{ArtDir}/{name}.asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[PrefabBuilder] Sprite not found: {path}");
        return sprite;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
            if (s.path == scenePath) return;

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        existing.CopyTo(updated, 0);
        updated[existing.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
