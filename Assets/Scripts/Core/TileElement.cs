using UnityEngine;

/// <summary>
/// Abstract base class for all level elements.
/// New element types: subclass this, override IsDestructible + Render().
/// No changes to existing code needed.
/// </summary>
public abstract class TileElement : MonoBehaviour
{
    public abstract bool IsDestructible { get; }

    protected virtual void Start()
    {
        Render();
    }

    /// <summary>Called when the player taps this tile on screen.</summary>
    public virtual void OnPlayerTouch() { }

    /// <summary>Called when the player's collider physically enters this tile.</summary>
    public virtual void OnPlayerCollide(PlayerController player) { }

    /// <summary>Called when the player's trigger enters this tile (for trigger colliders).</summary>
    public virtual void OnPlayerTrigger(PlayerController player) { }

    /// <summary>Set up visual appearance. Called once from Start().</summary>
    protected virtual void Render() { }

#if UNITY_EDITOR
    // Refresh tile color in the editor whenever the inspector changes or scripts recompile.
    // Deferred so it doesn't run during serialization (which causes warnings).
    protected virtual void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Render();
        };
    }
#endif
}
