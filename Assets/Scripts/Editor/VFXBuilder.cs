using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Cubby's Blocks ▸ 6. Build VFX Prefabs
///
/// Creates three particle-system prefabs in Assets/Prefabs/VFX/ using only
/// Unity's built-in Particle System modules — no external assets needed.
/// After creation, wires the prefab references on any VFXManager found in the
/// active scene.
///
/// Run after "5. Build UI" so VFXManager is already in the scene.
/// </summary>
public static class VFXBuilder
{
    private const string VFXDir = "Assets/Prefabs/VFX";

    [MenuItem("Cubby's Blocks/6. Build VFX Prefabs")]
    public static void BuildVFXPrefabs()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/VFX"));
        AssetDatabase.Refresh();

        CreateTileDestroyVFX();
        CreateCubbyDeathVFX();
        CreateGoalReachedVFX();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WireVFXManager();

        Debug.Log("[Cubby's Blocks] VFX prefabs built in Assets/Prefabs/VFX/. Save the scene (Ctrl+S).");
    }

    // ── Tile Destroy ──────────────────────────────────────────────────────────
    // Small debris burst — yellow-to-orange fixed palette.

    static void CreateTileDestroyVFX()
    {
        var go = new GameObject("TileDestroy_VFX");
        var ps = go.AddComponent<ParticleSystem>();

        var main              = ps.main;
        main.duration         = 0.5f;
        main.loop             = false;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(2f,   5f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        // Yellow to orange — matches the destructible tile's appearance
        main.startColor       = new ParticleSystem.MinMaxGradient(
                                    new Color(1.00f, 0.78f, 0.05f),   // bright yellow
                                    new Color(1.00f, 0.40f, 0.05f));  // deep orange
        main.gravityModifier  = 1.8f;
        main.maxParticles     = 30;
        main.stopAction       = ParticleSystemStopAction.Destroy;
        main.playOnAwake      = false;

        var emission          = ps.emission;
        emission.rateOverTime = 0f;
        var burst             = new ParticleSystem.Burst(0f, 15);
        burst.minCount        = 12;
        burst.maxCount        = 18;
        emission.SetBursts(new[] { burst });

        var shape             = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Sphere;
        shape.radius          = 0.15f;

        var col               = ps.colorOverLifetime;
        col.enabled           = true;
        col.color             = FadeOutGradient();

        var sol               = ps.sizeOverLifetime;
        sol.enabled           = true;
        sol.size              = ShrinkCurve(0f);

        var rend              = go.GetComponent<ParticleSystemRenderer>();
        rend.sortingOrder     = 10;

        SavePrefab(go, $"{VFXDir}/TileDestroy_VFX.prefab");
    }

    // ── Cubby Death ───────────────────────────────────────────────────────────
    // Bigger, faster burst — light blue to dark blue per particle.

    static void CreateCubbyDeathVFX()
    {
        var go = new GameObject("CubbyDeath_VFX");
        var ps = go.AddComponent<ParticleSystem>();

        var main              = ps.main;
        main.duration         = 1.0f;
        main.loop             = false;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(3f,   8f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
        // Each particle gets a random colour between light blue and dark blue
        main.startColor       = new ParticleSystem.MinMaxGradient(
                                    new Color(0.45f, 0.85f, 1.00f),  // light sky-blue
                                    new Color(0.05f, 0.15f, 0.75f)); // deep navy-blue
        main.gravityModifier  = 2.0f;
        main.maxParticles     = 60;
        main.stopAction       = ParticleSystemStopAction.Destroy;
        main.playOnAwake      = false;

        var emission          = ps.emission;
        emission.rateOverTime = 0f;
        var burst             = new ParticleSystem.Burst(0f, 35);
        burst.minCount        = 30;
        burst.maxCount        = 40;
        emission.SetBursts(new[] { burst });

        var shape             = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Sphere;
        shape.radius          = 0.25f;

        var col               = ps.colorOverLifetime;
        col.enabled           = true;
        col.color             = FadeOutGradient();

        // Shrinks to 15 % of original size rather than zero — pieces feel physical
        var sol               = ps.sizeOverLifetime;
        sol.enabled           = true;
        sol.size              = ShrinkCurve(0.15f);

        var rend              = go.GetComponent<ParticleSystemRenderer>();
        rend.sortingOrder     = 10;

        SavePrefab(go, $"{VFXDir}/CubbyDeath_VFX.prefab");
    }

    // ── Goal Reached ──────────────────────────────────────────────────────────
    // Celebratory burst: bright-to-dark green random start colour, green gradient fade.

    static void CreateGoalReachedVFX()
    {
        var go = new GameObject("GoalReached_VFX");
        var ps = go.AddComponent<ParticleSystem>();

        var main              = ps.main;
        main.duration         = 1.5f;
        main.loop             = false;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(2f,   6f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        // Bright lime to deep forest green
        main.startColor       = new ParticleSystem.MinMaxGradient(
                                    new Color(0.40f, 1.00f, 0.25f),  // bright lime-green
                                    new Color(0.03f, 0.50f, 0.10f)); // deep forest-green
        main.gravityModifier  = 1.2f;
        main.maxParticles     = 80;
        main.stopAction       = ParticleSystemStopAction.Destroy;
        main.playOnAwake      = false;

        var emission          = ps.emission;
        emission.rateOverTime = 0f;
        var burst             = new ParticleSystem.Burst(0f, 50);
        burst.minCount        = 45;
        burst.maxCount        = 60;
        emission.SetBursts(new[] { burst });

        var shape             = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Sphere;
        shape.radius          = 0.3f;

        // Colour shifts through gold → green → blue → white as particles age, then fades out
        var col               = ps.colorOverLifetime;
        col.enabled           = true;
        col.color             = CelebrationGradient();

        var sol               = ps.sizeOverLifetime;
        sol.enabled           = true;
        sol.size              = ShrinkCurve(0f);

        var rend              = go.GetComponent<ParticleSystemRenderer>();
        rend.sortingOrder     = 10;

        SavePrefab(go, $"{VFXDir}/GoalReached_VFX.prefab");
    }

    // ── VFXManager wiring ─────────────────────────────────────────────────────

    static void WireVFXManager()
    {
        var vfxMgr = Object.FindAnyObjectByType<VFXManager>();
        if (vfxMgr == null)
        {
            Debug.Log("[Cubby's Blocks] VFXManager not found — run '5. Build UI' first, then re-run '6. Build VFX Prefabs'.");
            return;
        }

        var so = new SerializedObject(vfxMgr);
        so.FindProperty("tileDestroyPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{VFXDir}/TileDestroy_VFX.prefab");
        so.FindProperty("cubbyDeathPrefab").objectReferenceValue  =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{VFXDir}/CubbyDeath_VFX.prefab");
        so.FindProperty("goalReachedPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>($"{VFXDir}/GoalReached_VFX.prefab");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(vfxMgr);
    }

    // ── Gradient helpers ──────────────────────────────────────────────────────

    /// White colour, alpha fades 1 → 0 over the particle lifetime.
    static ParticleSystem.MinMaxGradient FadeOutGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),          new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    /// Lime → mint → forest green, alpha fades 1 → 0.
    static ParticleSystem.MinMaxGradient CelebrationGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.40f, 1.00f, 0.25f), 0f),    // lime-green
                new GradientColorKey(new Color(0.20f, 0.90f, 0.55f), 0.40f), // mint
                new GradientColorKey(new Color(0.05f, 0.60f, 0.20f), 0.75f), // forest-green
                new GradientColorKey(new Color(0.40f, 1.00f, 0.25f), 1f),    // lime back out
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        return new ParticleSystem.MinMaxGradient(g);
    }

    // ── Curve helpers ─────────────────────────────────────────────────────────

    /// Size starts at 1 and eases to <paramref name="endSize"/> over lifetime.
    static ParticleSystem.MinMaxCurve ShrinkCurve(float endSize)
    {
        var curve = AnimationCurve.EaseInOut(0f, 1f, 1f, endSize);
        return new ParticleSystem.MinMaxCurve(1f, curve);
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);
        if (!ok) Debug.LogError($"[Cubby's Blocks] Failed to save: {path}");
    }
}
