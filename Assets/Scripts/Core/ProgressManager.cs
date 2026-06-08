using UnityEngine;

/// <summary>
/// Persists level unlock progress via PlayerPrefs.
/// Level numbers are 1-based.  World 2 unlocks when Level 10 is complete.
/// </summary>
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    private const string KeyUnlocked = "unlockedLevel";

    /// <summary>Highest unlocked level number (always >= 1).</summary>
    public int UnlockedLevel => PlayerPrefs.GetInt(KeyUnlocked, 1);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsLevelUnlocked(int levelNumber) => levelNumber <= UnlockedLevel;

    /// <summary>World 1 always unlocked; World 2 requires Level 10 completed; World 3 requires Level 20 completed.</summary>
    public bool IsWorldUnlocked(int worldNumber) =>
        worldNumber == 1 ||
        (worldNumber == 2 && UnlockedLevel > 10) ||
        (worldNumber == 3 && UnlockedLevel > 20);

    public void UnlockNextLevel(int completedLevelNumber)
    {
        int next = completedLevelNumber + 1;
        if (next > UnlockedLevel)
        {
            PlayerPrefs.SetInt(KeyUnlocked, next);
            PlayerPrefs.Save();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        PlayerPrefs.SetInt(KeyUnlocked, 1);
        PlayerPrefs.Save();
        Debug.Log("[ProgressManager] Progress reset.");
    }
#endif
}
