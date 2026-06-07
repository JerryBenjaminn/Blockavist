using UnityEngine;

/// <summary>
/// Launches Cubby upward on contact.  Jump force is tunable in the inspector.
/// Placed on the floor; player walks over it and is catapulted into the air
/// while horizontal movement continues at normal speed.
/// </summary>
public class JumpPadTile : TileElement
{
    [SerializeField] private float jumpForce = 12f;

    public override bool IsDestructible => false;

    public override void OnPlayerCollide(PlayerController player)
    {
        player.ApplyLaunch(jumpForce);
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.5f, 0f); // orange
    }
}
