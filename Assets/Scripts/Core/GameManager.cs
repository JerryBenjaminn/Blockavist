using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Playing, GameOver, LevelComplete }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ── Level catalogue ───────────────────────────────────────────────────────

    [Header("Levels — drag assets in order")]
    [SerializeField] private LevelData[] levels;

    // How long to wait after level complete before loading the next level.
    // Set to 0 for instant advance.  Phase 3 UI will replace this with a button.
    [SerializeField] private float autoAdvanceDelay = 1.5f;

    private int currentLevelIndex = 0;

    public LevelData CurrentLevel =>
        (levels != null && currentLevelIndex < levels.Length)
            ? levels[currentLevelIndex]
            : null;

    public int  CurrentLevelNumber => CurrentLevel != null ? CurrentLevel.levelNumber : 0;
    public int  TotalLevels        => levels != null ? levels.Length : 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Auto-load first level when LevelManager is present.
        // TestLevelSetup scenes work because LevelManager won't be in those scenes.
        if (LevelManager.Instance != null && CurrentLevel != null)
            LevelManager.Instance.LoadLevel(CurrentLevel);
    }

    // ── Called by PlayerController ────────────────────────────────────────────

    public void OnPlayerDied()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;
        Debug.Log($"[Blockavist] Game Over — level {CurrentLevelNumber}. Tap to retry.");
        // TODO Phase 3: show Game Over UI
    }

    public void OnLevelComplete()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.LevelComplete;
        Debug.Log($"[Blockavist] Level {CurrentLevelNumber} complete!");
        // TODO Phase 3: show Level Complete UI before advancing (remove the Invoke then)
        // TODO Phase 5: trigger AdMob interstitial every 3 completions
        Invoke(nameof(LoadNextLevel), autoAdvanceDelay);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>Restart the current level (called on tap-after-game-over).</summary>
    public void RestartLevel()
    {
        CurrentState = GameState.Playing;

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadLevel(CurrentLevel);   // null → scene reload inside LevelManager
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Advance to the next level.
    /// Called automatically after autoAdvanceDelay, or immediately when the
    /// player taps during the LevelComplete state (InputHandler skips the wait).
    /// Phase 3 UI will call this from a "Next Level" button instead.
    /// </summary>
    public void LoadNextLevel()
    {
        // Cancel the Invoke in case this was called early (tap-to-skip),
        // preventing a second call from the original timer.
        CancelInvoke(nameof(LoadNextLevel));

        // Guard: only advance from LevelComplete state.
        if (CurrentState != GameState.LevelComplete) return;

        currentLevelIndex++;

        if (currentLevelIndex >= TotalLevels)
        {
            Debug.Log("[Blockavist] All levels complete — world finished!");
            // TODO Phase 3: return to World Select
            return;
        }

        CurrentState = GameState.Playing;
        LevelManager.Instance?.LoadLevel(CurrentLevel);
        Debug.Log($"[Blockavist] Loading level {CurrentLevelNumber}…");
    }

    /// <summary>Jump to a specific level by 0-based index (used by Level Select in Phase 3).</summary>
    public void LoadLevelAt(int index)
    {
        if (index < 0 || index >= TotalLevels) return;
        currentLevelIndex = index;
        CurrentState      = GameState.Playing;
        LevelManager.Instance?.LoadLevel(CurrentLevel);
    }
}
