using UnityEngine;

/// <summary>
/// Creates and animates background clouds for the gameplay scene.
/// Lives on the "Background" GameObject; cloud children are built at runtime in Awake.
///
/// Visibility is driven by UIManager (SetVisible).  Cloud positions are randomised
/// by ResetClouds(), which UIManager calls just before each level starts.
///
/// Camera background colour is also applied here so it works on existing scenes
/// without re-running "2. Create GameScene".
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    // World-space X limits for cloud looping (camera half-width ≈ 8.9 at ortho size 5)
    private const float SpawnX = 16f;   // clouds appear here after looping off left edge
    private const float KillX  = -13f;  // clouds are recycled when they cross this

    // #A8D8EA pastel blue sky
    private static readonly Color SkyColor = new Color(0.659f, 0.847f, 0.918f);

    // Per-cloud vertical Y positions, horizontal scales, vertical scales, drift speeds
    // Four clouds at different depths/sizes give natural-looking parallax variety
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
    private bool        _visible;
    private Sprite      _puffSprite;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Apply pastel sky to the camera — safe to call on existing scenes
        if (Camera.main != null)
            Camera.main.backgroundColor = SkyColor;

        _puffSprite = BuildPuffSprite();
        BuildClouds();
        SetVisible(false); // hidden until UIManager activates gameplay
    }

    private void Update()
    {
        if (!_visible || _clouds == null) return;

        float dt = Time.deltaTime;
        for (int i = 0; i < _clouds.Length; i++)
        {
            Vector3 pos = _clouds[i].root.position;
            pos.x -= _clouds[i].speed * dt;
            // Loop off left → respawn right, with a small random extra offset
            // so clouds don't all reappear at the same time
            if (pos.x < KillX)
                pos.x = SpawnX + Random.Range(0f, 6f);
            _clouds[i].root.position = pos;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show or hide all clouds.  Called by UIManager at every screen transition.</summary>
    public void SetVisible(bool visible)
    {
        _visible = visible;
        if (_clouds == null) return;
        foreach (var c in _clouds)
            c.root.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Scatter clouds to random X positions across the screen.
    /// Call just before a level starts so clouds are never bunched at one edge.
    /// </summary>
    public void ResetClouds()
    {
        if (_clouds == null) return;
        for (int i = 0; i < _clouds.Length; i++)
        {
            Vector3 pos = _clouds[i].root.position;
            pos.x = Random.Range(KillX + 1f, SpawnX);
            _clouds[i].root.position = pos;
        }
    }

    // ── Cloud construction ────────────────────────────────────────────────────

    private void BuildClouds()
    {
        _clouds = new CloudData[CloudDefs.Length];

        for (int i = 0; i < CloudDefs.Length; i++)
        {
            var (y, sx, sy, speed) = CloudDefs[i];

            var root = new GameObject($"Cloud_{i + 1}");
            root.transform.SetParent(transform, false);

            // Each cloud is three overlapping soft-edged ellipses:
            //   centre puff (largest), upper-left puff, upper-right puff
            AddPuff(root.transform,
                    localPos:   Vector3.zero,
                    localScale: new Vector3(sx,        sy,        1f));

            AddPuff(root.transform,
                    localPos:   new Vector3(-sx * 0.28f, sy * 0.55f, 0f),
                    localScale: new Vector3(sx * 0.65f,  sy * 0.80f, 1f));

            AddPuff(root.transform,
                    localPos:   new Vector3( sx * 0.25f, sy * 0.50f, 0f),
                    localScale: new Vector3(sx * 0.55f,  sy * 0.75f, 1f));

            // Scatter initial X so clouds are already spread across the screen
            root.transform.position = new Vector3(Random.Range(KillX + 1f, SpawnX), y, 0f);

            _clouds[i] = new CloudData { root = root.transform, speed = speed };
        }
    }

    private void AddPuff(Transform parent, Vector3 localPos, Vector3 localScale)
    {
        var go = new GameObject("Puff");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = _puffSprite;
        sr.color        = new Color(1f, 1f, 1f, 0.82f);
        sr.sortingOrder = -5;
    }

    // ── Procedural puff sprite ────────────────────────────────────────────────

    /// <summary>
    /// Generates a 64×64 soft-edged white disc at runtime.
    /// Scaled to non-uniform X/Y on each puff's Transform to produce an ellipse.
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
            pivot:          new Vector2(0.5f, 0.5f),
            pixelsPerUnit:  Size);
    }
}
