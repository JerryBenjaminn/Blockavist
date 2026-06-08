using System.Collections;
using UnityEngine;

/// <summary>
/// Tap to start a 3-second countdown. Flashes faster as time runs out.
/// On explosion: destroys all tiles within explosionRadius (including indestructible ones)
/// and chain-triggers any adjacent ExplosiveTiles.
///
/// Falls when support below is removed (same physics as FallingHazardTile).
/// If already armed when support is lost, the countdown keeps running so the
/// tile detonates mid-air at whatever position it has fallen to.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ExplosiveTile : TileElement
{
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float explosionRadius   = 1.5f;
    [SerializeField] private float fallDelay         = 0.2f;

    private static readonly Color BaseColor = new Color(0.55f, 0.10f, 0.80f); // purple

    private Rigidbody2D rb;
    private bool isArmed    = false;
    private bool isExploded = false;
    private bool falling    = false;

    public override bool IsDestructible => true;

    private void Awake()
    {
        rb                = GetComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = true;
        falling           = false;
    }

    protected override void Start() => base.Start();

    private void Update()
    {
        if (falling) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing || gm.IsCountingDown) return;

        Vector2    checkPos = (Vector2)transform.position + Vector2.down * 0.6f;
        Collider2D below    = Physics2D.OverlapPoint(checkPos);

        if (below == null || below.gameObject == gameObject)
        {
            falling = true;
            StartCoroutine(FallAfterDelay());
        }
    }

    private IEnumerator FallAfterDelay()
    {
        yield return new WaitForSeconds(fallDelay);
        if (this != null)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public override void OnPlayerTouch()
    {
        Arm();
    }

    /// <summary>Start the countdown.  Safe to call multiple times — only arms once.</summary>
    public void Arm()
    {
        if (isArmed) return;
        isArmed = true;
        StartCoroutine(CountdownRoutine());
    }

    /// <summary>Detonate immediately (used by chain reaction from a neighboring explosion).</summary>
    public void ExplodeNow()
    {
        if (isExploded) return;
        isExploded = true;
        StopAllCoroutines();
        Explode();
    }

    private IEnumerator CountdownRoutine()
    {
        var   sr      = GetComponent<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < countdownDuration)
        {
            // Blink rate accelerates from 1 Hz to 10 Hz over the countdown
            float t         = elapsed / countdownDuration;
            float blinkRate = Mathf.Lerp(1f, 10f, t);
            bool  lit       = ((int)(elapsed * blinkRate * 2f)) % 2 == 0;

            if (sr != null)
                sr.color = lit ? Color.white : BaseColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        ExplodeNow();
    }

    private void Explode()
    {
        VFXManager.Instance?.SpawnTileDestroy(transform.position, BaseColor);

        // Collect hits before destroying anything so the array stays valid
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            TileElement tile = hit.GetComponent<TileElement>();
            if (tile == null) continue;

            if (tile is ExplosiveTile other)
                other.ExplodeNow(); // chain reaction
            else
                Destroy(hit.gameObject);
        }

        Destroy(gameObject);
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = BaseColor;
    }
}
