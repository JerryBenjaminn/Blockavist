using UnityEngine;

/// <summary>
/// Solid tile. Cannot be tapped away. Player turns on collision.
/// Turning is handled by PlayerController contact-normal check — no extra code needed here.
/// </summary>
public class IndestructibleTile : TileElement
{
    public override bool IsDestructible => false;

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.45f, 0.45f, 0.5f);
    }
}
