using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fires after the countdown completes and shows any tutorial pop-ups needed
/// for the current level:
///
///   1. Level 1 basic tutorial (two steps, shown once ever).
///   2. One pop-up per new tile type (JumpPad / FallingHazard / Explosive / Portal),
///      shown the first time that tile type appears in any level.
///
/// All steps are shown sequentially; the player is frozen for the whole sequence.
/// Each step's PlayerPrefs key is written only after the player confirms that step.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string L1PrefsKey = "Tutorial_L1_Seen";

    // Tile-type tutorials in the order they should appear.
    // message/key/btnLabel are all owned here so TutorialUI stays a dumb display widget.
    private static readonly (TileType type, string prefsKey, string message)[] TileTutorials =
    {
        (TileType.JumpPad,       "Tutorial_JumpPad_Seen",       "Tap the orange pad to launch Cubby!"),
        (TileType.FallingHazard, "Tutorial_FallingHazard_Seen", "Red blocks fall when their support is removed!"),
        (TileType.Explosive,     "Tutorial_Explosive_Seen",     "Tap the purple block to start a countdown — stand back!"),
        (TileType.Portal,        "Tutorial_Portal_Seen",        "Guide Cubby into a cyan portal to teleport to its twin!"),
    };

    [SerializeField] private TutorialUI tutorialPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => CountdownUI.OnComplete += HandleCountdownComplete;
    private void OnDisable() => CountdownUI.OnComplete -= HandleCountdownComplete;

    // ── Main flow ─────────────────────────────────────────────────────────────

    private void HandleCountdownComplete()
    {
        var queue = BuildQueue();
        if (queue.Count == 0) return;

        LevelManager.Instance?.ActivePlayer?.Freeze();
        ShowNext(queue);
    }

    // Builds the ordered list of tutorial steps needed for this level session.
    private Queue<(string msg, string btn, string key)> BuildQueue()
    {
        var queue = new Queue<(string, string, string)>();

        // ── Level 1 basic tutorial ────────────────────────────────────────────
        if (GameManager.Instance?.CurrentLevel?.levelNumber == 1 &&
            PlayerPrefs.GetInt(L1PrefsKey, 0) == 0)
        {
            // Two steps; prefs key is only written when the second (final) step is confirmed.
            queue.Enqueue(("Tap yellow blocks to destroy them", "Got it!", null));
            queue.Enqueue(("Guide Cubby to the green goal!",   "Let's go!", L1PrefsKey));
        }

        // ── Per-tile-type tutorials ───────────────────────────────────────────
        LevelData data = LevelManager.Instance?.CurrentData;
        if (data?.tiles == null) return queue;

        // Collect every tile type present in this level
        var presentTypes = new HashSet<TileType>();
        foreach (TileEntry entry in data.tiles)
            presentTypes.Add(entry.type);

        // Enqueue a tutorial for each unseen tile type that appears in this level
        foreach (var (type, key, msg) in TileTutorials)
        {
            if (presentTypes.Contains(type) && PlayerPrefs.GetInt(key, 0) == 0)
                queue.Enqueue((msg, "Got it!", key));
        }

        return queue;
    }

    // Shows the next step; unfreezes the player after the last step.
    private void ShowNext(Queue<(string msg, string btn, string key)> queue)
    {
        if (queue.Count == 0)
        {
            LevelManager.Instance?.ActivePlayer?.Unfreeze();
            return;
        }

        var (msg, btn, key) = queue.Dequeue();
        tutorialPanel?.Show(msg, btn, () =>
        {
            if (key != null)
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            ShowNext(queue);
        });
    }
}
