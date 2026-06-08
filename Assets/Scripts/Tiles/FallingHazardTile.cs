using System.Collections;
using UnityEngine;

/// <summary>
/// Dangerous tile that falls when the tile directly below it is removed.
/// Starts kinematic (frozen in place). Each frame, checks for a supporting tile
/// 0.6 units below center; if support is gone, waits fallDelay seconds then goes
/// dynamic and drops under gravity.  Kills Cubby on any contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FallingHazardTile : TileElement
{
    [SerializeField] private float fallDelay = 0.2f;

    private Rigidbody2D rb;
    private bool        falling = false;

    public override bool IsDestructible => false;

    // Awake runs immediately at Instantiate time — before any physics step or Update.
    // Resetting here guarantees kinematic state regardless of prefab body-type or restart order.
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

        // Only monitor for missing support once gameplay is live.
        var gm = GameManager.Instance;

        // DEBUG — log state every frame until fall begins so we can see exactly when
        // the guard passes on first load. Remove once root cause is confirmed.
        Debug.Log($"[FallingHazard {name}] frame={Time.frameCount} " +
                  $"State={gm?.CurrentState} IsCountingDown={gm?.IsCountingDown} " +
                  $"pos={transform.position}");

        if (gm == null || gm.CurrentState != GameManager.GameState.Playing || gm.IsCountingDown) return;

        // Check 0.6 units below center — safely outside the tile's own 1×1 collider.
        Vector2    checkPos = (Vector2)transform.position + Vector2.down * 0.6f;
        Collider2D below    = Physics2D.OverlapPoint(checkPos);

        if (below == null || below.gameObject == gameObject)
        {
            // DEBUG — capture the exact state values at the moment falling triggers.
            Debug.Log($"[FallingHazard {name}] FALL TRIGGERED at frame={Time.frameCount} " +
                      $"State={gm.CurrentState} IsCountingDown={gm.IsCountingDown} " +
                      $"TimeSinceStartup={Time.realtimeSinceStartup:F3}");
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

    public override void OnPlayerCollide(PlayerController player)
    {
        player.Die();
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.50f, 0.05f, 0.05f); // dark red
    }
}
