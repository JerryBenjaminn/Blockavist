using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Hazard tile with a visible countdown that begins once gameplay starts.
/// After the timer expires the tile falls under gravity and kills Cubby on contact.
/// The countdown is paused during the pre-level countdown so the two timers
/// never overlap and confuse the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TimedHazardTile : TileElement
{
    [SerializeField] private float fallDelay = 3f;

    private Rigidbody2D rb;
    private TextMeshPro label;
    private bool        started = false;
    private bool        falling = false;

    public override bool IsDestructible => false;

    // Awake runs immediately at Instantiate time — before any physics step or Update.
    // Resetting here guarantees kinematic state and clean flag state on every load.
    private void Awake()
    {
        rb                = GetComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = true;
        started           = false;
        falling           = false;
    }

    protected override void Start()
    {
        base.Start();
        label = CreateLabel();
        UpdateLabel(fallDelay); // show initial count before gameplay starts
    }

    private void Update()
    {
        if (started || falling) return;

        var gm = GameManager.Instance;
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing || gm.IsCountingDown) return;

        started = true;
        StartCoroutine(CountdownThenFall());
    }

    private IEnumerator CountdownThenFall()
    {
        float remaining = fallDelay;
        while (remaining > 0f)
        {
            // Re-check the guard every frame. The coroutine may have been started
            // during the brief fade window before IsCountingDown is set to true, so
            // pause the timer while the level pre-countdown is still running.
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameManager.GameState.Playing || gm.IsCountingDown)
            {
                yield return null;
                continue;
            }

            UpdateLabel(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }
        falling = true;
        if (label != null) label.text = "";
        if (this != null) rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void UpdateLabel(float remaining)
    {
        if (label == null) return;
        // CeilToInt: shows 3 → 2 → 1 as each full second expires
        label.text = Mathf.CeilToInt(Mathf.Max(remaining, 0f)).ToString();
    }

    private TextMeshPro CreateLabel()
    {
        var go = new GameObject("CountdownLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);

        var tmp               = go.AddComponent<TextMeshPro>();
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.fontSize          = 3.5f;
        tmp.fontStyle         = FontStyles.Bold;
        tmp.color             = Color.white;
        tmp.enableWordWrapping = false;

        var rt       = tmp.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1.2f, 1.2f);

        // Render in front of the tile sprite
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 2;

        return tmp;
    }

    public override void OnPlayerCollide(PlayerController player)
    {
        player.Die();
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.80f, 0.35f, 0.05f); // dark orange
    }
}
