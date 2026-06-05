using UnityEngine;

/// <summary>
/// Level exit. Set as a trigger (isTrigger = true) so the player walks through it.
/// PlayerController.OnTriggerEnter2D calls ReachGoal() when this is touched.
/// </summary>
public class GoalTile : TileElement
{
    public override bool IsDestructible => false;

    protected override void Render()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.1f, 0.85f, 0.25f);
    }
}
