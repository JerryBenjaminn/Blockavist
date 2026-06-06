using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed          = 3f;
    [SerializeField] private float deathYThreshold    = -10f;

    // A contact is treated as a "wall" only when its normal points at least this
    // far sideways (|nx| > threshold).  0.5 ≈ 30° from vertical — too loose;
    // tile-seam edges produce normals around (0.6, 0.8) which falsely exceed it.
    // 0.7 ≈ 44° requires a genuinely near-horizontal surface to trigger a flip.
    [SerializeField] private float wallNormalThreshold = 0.7f;

    [Header("Face Sprites")]
    [SerializeField] private Sprite faceHappy;
    [SerializeField] private Sprite faceShocked;
    [SerializeField] private Sprite faceSmileBig;

    private Rigidbody2D rb;
    private int direction = 1; // 1 = right, -1 = left

    // Visual component refs — auto-discovered in Awake from the Visual child hierarchy
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer faceRenderer;
    private SpriteRenderer peaceSignRenderer;
    private CubbyBob       bobScript;

    public bool IsAlive  { get; private set; } = true;
    public bool IsFrozen { get; private set; } = true;

    /// <summary>Freeze horizontal movement while keeping gravity active (used during countdown).</summary>
    public void Freeze()   => IsFrozen = true;
    public void Unfreeze() => IsFrozen = false;

    public void ShowPeaceSign()
    {
        if (peaceSignRenderer != null) peaceSignRenderer.gameObject.SetActive(true);
    }

    public void HidePeaceSign()
    {
        if (peaceSignRenderer != null) peaceSignRenderer.gameObject.SetActive(false);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Slightly smaller than one tile so corners don't snag on edges
        GetComponent<CapsuleCollider2D>().size = new Vector2(0.85f, 0.85f);

        ApplyZeroFrictionMaterial();
        DiscoverVisuals();
    }

    private void DiscoverVisuals()
    {
        Transform vis = transform.Find("Visual");
        if (vis == null) return;

        bodyRenderer      = vis.Find("Body")?.GetComponent<SpriteRenderer>();
        faceRenderer      = vis.Find("Face")?.GetComponent<SpriteRenderer>();
        Transform psT     = vis.Find("PeaceSign");
        if (psT != null) peaceSignRenderer = psT.GetComponent<SpriteRenderer>();
        bobScript = vis.GetComponent<CubbyBob>();

        if (faceRenderer != null && faceHappy != null)
            faceRenderer.sprite = faceHappy;
    }

    // Zero friction stops the player's box corners from "gripping" tile seam edges.
    // Minimum combine mode ensures this wins even if tile colliders have friction set.
    private void ApplyZeroFrictionMaterial()
    {
        var mat = new PhysicsMaterial2D("PlayerNoFriction")
        {
            friction        = 0f,
            bounciness      = 0f,
            frictionCombine = PhysicsMaterialCombine2D.Minimum
        };
        GetComponent<CapsuleCollider2D>().sharedMaterial = mat;
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;
        float hSpeed = IsFrozen ? 0f : direction * moveSpeed;
        // Preserve vertical velocity (gravity/falling) while controlling horizontal
        rb.linearVelocity = new Vector2(hSpeed, rb.linearVelocity.y);
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (transform.position.y < deathYThreshold)
            Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsAlive) return;

        TileElement tile = collision.gameObject.GetComponent<TileElement>();
        if (tile != null)
            tile.OnPlayerCollide(this);

        if (!IsAlive) return;

        // Horizontal contact normal means the player hit a wall — reverse direction.
        // wallNormalThreshold filters out seam/corner contacts that have a small
        // but non-zero horizontal component.
        foreach (ContactPoint2D contact in collision.contacts)
        {
            float absNx = Mathf.Abs(contact.normal.x);
            if (absNx > wallNormalThreshold)
            {
                LogTurn(collision.gameObject.name, contact, absNx);
                FlipDirection();
                break;
            }
        }
    }

    // Goal tile sets isTrigger = true, so goal detection goes through here
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive) return;
        if (other.GetComponent<GoalTile>() != null)
            ReachGoal();
    }

    // Compiled out in release builds — zero runtime cost in production.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogTurn(string hitName, ContactPoint2D contact, float absNx)
    {
        float angleDeg = Mathf.Asin(absNx) * Mathf.Rad2Deg;
        Debug.Log(
            $"[PlayerController] TURN  hit={hitName}" +
            $"  normal=({contact.normal.x:F3}, {contact.normal.y:F3})" +
            $"  |nx|={absNx:F3}  angle_from_vertical={angleDeg:F1}°" +
            $"  contact_point={contact.point}" +
            $"  threshold={wallNormalThreshold}",
            gameObject);
    }

    public void FlipDirection()
    {
        direction *= -1;
        bool flipped = direction < 0;
        if (bodyRenderer      != null) bodyRenderer.flipX      = flipped;
        if (faceRenderer      != null) faceRenderer.flipX      = flipped;
        if (peaceSignRenderer != null) peaceSignRenderer.flipX = flipped;
    }

    public void Die()
    {
        IsAlive = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;
        if (faceRenderer != null && faceShocked != null) faceRenderer.sprite = faceShocked;
        if (bobScript    != null) bobScript.enabled = false;
        GameManager.Instance?.OnPlayerDied();
    }

    public void ReachGoal()
    {
        IsAlive = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;
        if (faceRenderer != null && faceSmileBig != null) faceRenderer.sprite = faceSmileBig;
        if (bobScript    != null) bobScript.enabled = false;
        GameManager.Instance?.OnLevelComplete();
    }
}
