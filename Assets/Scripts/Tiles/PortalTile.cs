using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Portal tile — placed in pairs that share the same PortalId.
/// When Cubby walks through a portal, it teleports to the partner portal
/// and continues moving in the same direction.
///
/// The collider is a trigger so Cubby passes through instead of being blocked.
/// The tile has a Rigidbody2D (kinematic) so it can fall when support below is removed.
///
/// To create a portal pair in the Level Editor: place Portal A, then Portal B.
/// The editor automatically assigns matching pair IDs.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PortalTile : TileElement
{
    // Inspector-visible only; set at runtime by LevelManager from TileEntry.extraData.
    [SerializeField] private int portalId = 0;

    private Rigidbody2D rb;
    private bool        onCooldown = false;

    // ── Global registry so FindPartner() avoids FindObjectsByType every frame ─
    private static readonly List<PortalTile> s_all = new List<PortalTile>();

    public int PortalId
    {
        get => portalId;
        set => portalId = value;
    }

    public override bool IsDestructible => false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        s_all.Add(this);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        s_all.Remove(this);
    }

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (rb == null || rb.bodyType == RigidbodyType2D.Dynamic) return;

        // Fall if the tile directly below is gone (same support logic as FallingHazardTile)
        Vector2    checkPos = (Vector2)transform.position + Vector2.down * 0.6f;
        Collider2D below    = Physics2D.OverlapPoint(checkPos);

        if (below == null || below.gameObject == gameObject)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // ── Portal logic ──────────────────────────────────────────────────────────

    public override void OnPlayerTrigger(PlayerController player)
    {
        if (onCooldown) return;

        PortalTile partner = FindPartner();
        if (partner == null) return;

        // Block the exit portal so the player doesn't immediately teleport back
        partner.ActivateCooldown();

        // Teleport: place Cubby 0.5 units above the partner portal center
        player.transform.position = partner.transform.position + Vector3.up * 0.5f;
    }

    public void ActivateCooldown()
    {
        onCooldown = true;
        StartCoroutine(ClearCooldown());
    }

    private IEnumerator ClearCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        onCooldown = false;
    }

    private PortalTile FindPartner()
    {
        foreach (PortalTile p in s_all)
        {
            if (p != this && p.portalId == portalId)
                return p;
        }
        return null;
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0f, 0.85f, 0.85f); // cyan
    }
}
