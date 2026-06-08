using UnityEngine;

/// <summary>
/// Launches Cubby upward when the player taps it.
/// Cubby can walk over it freely — the launch only fires on a deliberate tap.
/// Jump force is tunable in the inspector.
/// </summary>
public class JumpPadTile : TileElement
{
    [SerializeField] private float jumpForce = 12f;

    // True so InputHandler calls OnPlayerTouch when the tile is tapped.
    public override bool IsDestructible => true;

    public override void OnPlayerTouch()
    {
        var player = LevelManager.Instance?.ActivePlayer;
        if (player == null) return;

        // Only launch if Cubby is physically on or adjacent to this pad at tap time.
        // Threshold 1.2 covers any on-pad position (player centre ~0.925u above pad
        // centre) while excluding tiles one step away (~1.36u diagonal).
        if (Vector2.Distance(player.transform.position, transform.position) > 1.2f) return;

        player.ApplyLaunch(jumpForce);
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.5f, 0f); // orange
    }
}
