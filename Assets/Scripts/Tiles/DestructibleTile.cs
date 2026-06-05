using UnityEngine;

/// <summary>
/// Tile that disappears when the player taps it.
/// Acts as a solid wall until removed — player turns on collision just like an indestructible tile.
/// </summary>
public class DestructibleTile : TileElement
{
    public override bool IsDestructible => true;

    public override void OnPlayerTouch()
    {
        Destroy(gameObject);
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.82f, 0.1f);
    }
}
