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
        var sr    = GetComponent<SpriteRenderer>();
        var tint  = sr != null ? sr.color : Color.white;
        VFXManager.Instance?.SpawnTileDestroy(transform.position, tint);
        AudioManager.Instance?.PlayPopTile();
        Destroy(gameObject);
    }

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.82f, 0.1f);
    }
}
