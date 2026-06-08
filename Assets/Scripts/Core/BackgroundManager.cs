using System.Collections;
using UnityEngine;

/// <summary>
/// Creates and animates background particles for all three worlds.
///
/// World 1 — Grassland:   white clouds drifting left across a pastel-blue sky.
/// World 2 — Stone Dungeon: gray-lilac dust motes falling slowly on a dark purple sky.
/// World 3 — Volcano:      orange embers rising upward on a dark warm-orange sky.
///
/// All particles are built at runtime — no art assets required.
/// World is detected from GameManager.CurrentLevel.worldNumber each time
/// ResetClouds() is called (just before a level starts).  The camera background
/// fades smoothly whenever the world changes.
///
/// Public API (called by UIManager):
///   SetVisible(bool)  — show/hide the current world's particles.
///   ResetClouds()     — detect world, scatter particles, fade sky if world changed.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    // ── World 1: Cloud definitions ────────────────────────────────────────────
    // Clouds scroll left and loop from right edge back to left edge.
    private const float CloudSpawnX = 16f;
    private const float CloudKillX  = -13f;

    private static readonly (float y, float sx, float sy, float speed)[] CloudDefs =
    {
        (  3.0f, 3.4f, 1.2f, 0.35f ),
        (  1.5f, 2.6f, 1.0f, 0.48f ),
        (  0.1f, 3.9f, 1.3f, 0.30f ),
        ( -1.5f, 2.3f, 0.9f, 0.44f ),
    };

    private struct CloudData
    {
        public Transform root;
        public float     speed;
    }

    private CloudData[] _clouds;

    // ── World 2 / 3: Particle definitions ────────────────────────────────────
    // Screen bounds used for particle spawn / kill regions.
    private const float PX_MIN = -9f;
    private const float PX_MAX =  9f;
    private const float PY_BOT = -6f;
    private const float PY_TOP =  6f;

    private struct ParticleData
    {
        public Transform root;
        public float     speedY;      // world-units/sec — negative = falls, positive = rises
        public float     wobbleAmp;   // horizontal sway amplitude (world units)
        public float     wobbleFreq;  // sway cycles per second
        public float     phase;       // per-particle sway phase offset
    }

    private ParticleData[] _dungeonParticles;  // World 2 falling dust
    private ParticleData[] _volcanoParticles;  // World 3 rising embers

    // ── Sky colours per world (index = worldNumber - 1) ──────────────────────
    private static readonly Color[] WorldSkyColor =
    {
        new Color(0.659f, 0.847f, 0.918f),   // World 1 – #A8D8EA pastel blue
        new Color(0.176f, 0.149f, 0.251f),   // World 2 – #2D2640 dark gray-purple
        new Color(0.239f, 0.102f, 0.039f),   // World 3 – #3D1A0A dark warm orange
    };

    private const float SkyFadeDuration = 0.6f;

    // ── State ─────────────────────────────────────────────────────────────────
    private int       _activeWorld = 1;   // default World 1 for test scenes
    private bool      _visible;
    private Sprite    _puffSprite;
    private Coroutine _skyFade;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (Camera.main != null)
            Camera.main.backgroundColor = WorldSkyColor[0];

        _puffSprite = BuildPuffSprite();
        BuildClouds();
        BuildDungeonParticles();
        BuildVolcanoParticles();
        SetVisible(false);
    }

    private void Update()
    {
        if (!_visible) return;

        float dt   = Time.deltaTime;
        float time = Time.time;

        // World 1 — clouds scroll left and wrap around the right edge
        if (_activeWorld == 1 && _clouds != null)
        {
            for (int i = 0; i < _clouds.Length; i++)
            {
                Vector3 pos = _clouds[i].root.position;
                pos.x -= _clouds[i].speed * dt;
                if (pos.x < CloudKillX)
                    pos.x = CloudSpawnX + Random.Range(0f, 6f);
                _clouds[i].root.position = pos;
            }
        }

        // World 2 — dust motes drift downward with a gentle sway
        if (_activeWorld == 2 && _dungeonParticles != null)
        {
            for (int i = 0; i < _dungeonParticles.Length; i++)
            {
                var     p   = _dungeonParticles[i];
                Vector3 pos = p.root.position;
                pos.y += p.speedY * dt;
                pos.x += Mathf.Sin(time * p.wobbleFreq + p.phase) * p.wobbleAmp * dt;
                if (pos.y < PY_BOT)
                {
                    pos.y = PY_TOP;
                    pos.x = Random.Range(PX_MIN, PX_MAX);
                }
                p.root.position = pos;
            }
        }

        // World 3 — embers float upward with an irregular sway
        if (_activeWorld == 3 && _volcanoParticles != null)
        {
            for (int i = 0; i < _volcanoParticles.Length; i++)
            {
                var     p   = _volcanoParticles[i];
                Vector3 pos = p.root.position;
                pos.y += p.speedY * dt;
                pos.x += Mathf.Sin(time * p.wobbleFreq + p.phase) * p.wobbleAmp * dt;
                if (pos.y > PY_TOP)
                {
                    pos.y = PY_BOT;
                    pos.x = Random.Range(PX_MIN, PX_MAX);
                }
                p.root.position = pos;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show or hide the current world's particles. Called by UIManager.</summary>
    public void SetVisible(bool visible)
    {
        _visible = visible;

        if (!visible)
        {
            SetCloudsActive(false);
            SetDungeonActive(false);
            SetVolcanoActive(false);
            return;
        }

        SetCloudsActive(_activeWorld == 1);
        SetDungeonActive(_activeWorld == 2);
        SetVolcanoActive(_activeWorld == 3);
    }

    /// <summary>
    /// Detect the current world from GameManager, switch sky colour if it changed,
    /// then scatter all particles.  UIManager calls this just before each level starts.
    /// </summary>
    public void ResetClouds()
    {
        int world = GetCurrentWorld();

        if (world != _activeWorld)
        {
            _activeWorld = world;
            if (_skyFade != null) StopCoroutine(_skyFade);
            _skyFade = StartCoroutine(FadeSky(WorldSkyColor[world - 1]));
        }

        ScatterClouds();
        ScatterDungeonParticles();
        ScatterVolcanoParticles();
    }

    // ── World detection ───────────────────────────────────────────────────────

    private static int GetCurrentWorld()
    {
        int w = GameManager.Instance?.CurrentLevel?.worldNumber ?? 1;
        return Mathf.Clamp(w, 1, 3);
    }

    // ── Particle activation ───────────────────────────────────────────────────

    private void SetCloudsActive(bool v)
    {
        if (_clouds == null) return;
        foreach (var c in _clouds) c.root.gameObject.SetActive(v);
    }

    private void SetDungeonActive(bool v)
    {
        if (_dungeonParticles == null) return;
        foreach (var p in _dungeonParticles) p.root.gameObject.SetActive(v);
    }

    private void SetVolcanoActive(bool v)
    {
        if (_volcanoParticles == null) return;
        foreach (var p in _volcanoParticles) p.root.gameObject.SetActive(v);
    }

    // ── Particle scatter ──────────────────────────────────────────────────────

    private void ScatterClouds()
    {
        if (_clouds == null) return;
        for (int i = 0; i < _clouds.Length; i++)
        {
            Vector3 pos = _clouds[i].root.position;
            pos.x = Random.Range(CloudKillX + 1f, CloudSpawnX);
            _clouds[i].root.position = pos;
        }
    }

    private void ScatterDungeonParticles()
    {
        if (_dungeonParticles == null) return;
        foreach (var p in _dungeonParticles)
            p.root.position = new Vector3(
                Random.Range(PX_MIN, PX_MAX),
                Random.Range(PY_BOT, PY_TOP),
                0f);
    }

    private void ScatterVolcanoParticles()
    {
        if (_volcanoParticles == null) return;
        foreach (var p in _volcanoParticles)
            p.root.position = new Vector3(
                Random.Range(PX_MIN, PX_MAX),
                Random.Range(PY_BOT, PY_TOP),
                0f);
    }

    // ── Sky colour transition ─────────────────────────────────────────────────

    private IEnumerator FadeSky(Color target)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Color start = cam.backgroundColor;
        float t     = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / SkyFadeDuration;
            cam.backgroundColor = Color.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        cam.backgroundColor = target;
    }

    // ── Construction: World 1 clouds ──────────────────────────────────────────

    private void BuildClouds()
    {
        _clouds = new CloudData[CloudDefs.Length];

        for (int i = 0; i < CloudDefs.Length; i++)
        {
            var (y, sx, sy, speed) = CloudDefs[i];

            var root = new GameObject($"Cloud_{i + 1}");
            root.transform.SetParent(transform, false);

            // Three overlapping soft ellipses — centre + upper-left + upper-right puffs
            AddPuff(root.transform, Vector3.zero,
                    new Vector3(sx, sy, 1f),
                    new Color(1f, 1f, 1f, 0.82f));

            AddPuff(root.transform,
                    new Vector3(-sx * 0.28f, sy * 0.55f, 0f),
                    new Vector3(sx * 0.65f, sy * 0.80f, 1f),
                    new Color(1f, 1f, 1f, 0.82f));

            AddPuff(root.transform,
                    new Vector3(sx * 0.25f, sy * 0.50f, 0f),
                    new Vector3(sx * 0.55f, sy * 0.75f, 1f),
                    new Color(1f, 1f, 1f, 0.82f));

            root.transform.position = new Vector3(
                Random.Range(CloudKillX + 1f, CloudSpawnX), y, 0f);

            _clouds[i] = new CloudData { root = root.transform, speed = speed };
        }
    }

    // ── Construction: World 2 dungeon dust ────────────────────────────────────

    private void BuildDungeonParticles()
    {
        const int Count = 14;
        _dungeonParticles = new ParticleData[Count];

        for (int i = 0; i < Count; i++)
        {
            float size  = Random.Range(0.12f, 0.32f);
            float alpha = Random.Range(0.30f, 0.60f);
            // Cool gray-lilac — matches the #2D2640 dungeon sky but lighter
            var   col   = new Color(0.72f, 0.70f, 0.82f, alpha);

            var root = new GameObject($"DungeonDust_{i + 1}");
            root.transform.SetParent(transform, false);
            AddPuff(root.transform, Vector3.zero, Vector3.one * size, col);

            _dungeonParticles[i] = new ParticleData
            {
                root       = root.transform,
                speedY     = -Random.Range(0.28f, 0.65f),   // negative = falling
                wobbleAmp  = Random.Range(0.18f, 0.55f),
                wobbleFreq = Random.Range(0.35f, 0.90f),
                phase      = Random.Range(0f, Mathf.PI * 2f),
            };
        }

        ScatterDungeonParticles();
    }

    // ── Construction: World 3 volcano embers ──────────────────────────────────

    private void BuildVolcanoParticles()
    {
        const int Count = 18;
        _volcanoParticles = new ParticleData[Count];

        for (int i = 0; i < Count; i++)
        {
            float size = Random.Range(0.07f, 0.22f);
            // Blend from deep orange to bright yellow-orange for a glowing heat shimmer
            float heat = Random.Range(0f, 1f);
            var   col  = new Color(1f, Mathf.Lerp(0.20f, 0.72f, heat), 0f,
                                   Random.Range(0.55f, 0.90f));

            var root = new GameObject($"VolcanoEmber_{i + 1}");
            root.transform.SetParent(transform, false);
            AddPuff(root.transform, Vector3.zero, Vector3.one * size, col);

            _volcanoParticles[i] = new ParticleData
            {
                root       = root.transform,
                speedY     = Random.Range(0.40f, 1.05f),    // positive = rising
                wobbleAmp  = Random.Range(0.40f, 1.00f),
                wobbleFreq = Random.Range(0.60f, 1.50f),
                phase      = Random.Range(0f, Mathf.PI * 2f),
            };
        }

        ScatterVolcanoParticles();
    }

    // ── Shared sprite helpers ─────────────────────────────────────────────────

    private void AddPuff(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = new GameObject("Puff");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _puffSprite;
        sr.color        = color;
        sr.sortingOrder = -5;
    }

    /// <summary>
    /// Generates a 64×64 soft-edged white disc at runtime.
    /// Scaled on each particle's Transform to produce circles of any size.
    /// The white tint is overridden per-particle by SpriteRenderer.color.
    /// </summary>
    private static Sprite BuildPuffSprite()
    {
        const int Size = 64;
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        float half = Size * 0.5f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx   = (x - half) / half;
                float dy   = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Opaque white core that fades smoothly to transparent at the rim
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.30f, 1f, dist));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0, 0, Size, Size),
            pivot:         new Vector2(0.5f, 0.5f),
            pixelsPerUnit: Size);
    }
}
