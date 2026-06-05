using UnityEngine;

/// <summary>
/// Hazard tile. Kills the player on any physical contact.
/// </summary>
public class SpikeTile : TileElement
{
    public override bool IsDestructible => false;

    public override void OnPlayerCollide(PlayerController player)
    {
        player.Die();
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.9f, 0.15f, 0.15f);
    }
}
