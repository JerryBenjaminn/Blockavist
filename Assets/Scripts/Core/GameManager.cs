using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────

    public enum GameState { Playing, GameOver, LevelComplete }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ── Level catalogue ───────────────────────────────────────────────────────

    [Header("Levels — drag assets in order (all worlds)")]
    [SerializeField] private LevelData[] levels;

    private int currentLevelIndex = 0;

    public LevelData CurrentLevel =>
        (levels != null && currentLevelIndex < levels.Length)
            ? levels[currentLevelIndex]
            : null;

    public int  CurrentLevelNumber => CurrentLevel != null ? CurrentLevel.levelNumber : 0;
    public int  TotalLevels        => levels != null ? levels.Length : 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    // ── Countdown gate ────────────────────────────────────────────────────────

    public bool IsCountingDown { get; private set; }
    public void BeginCountdown() => IsCountingDown = true;
    public void EndCountdown()   => IsCountingDown = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    private void Start()
    {
        // Auto-load first level only in TestLevelSetup scenes (no UIManager present).
        // In GameScene, UIManager drives level loading via GoToGame().
        if (UIManager.Instance == null && LevelManager.Instance != null && CurrentLevel != null)
            LevelManager.Instance.LoadLevel(CurrentLevel);
    }

    // ── Called by PlayerController ────────────────────────────────────────────

    public void OnPlayerDied()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;
        Debug.Log($"[Cubby's Blocks] Game Over — level {CurrentLevelNumber}.");
        UIManager.Instance?.ShowGameOver();
    }

    public void OnLevelComplete()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.LevelComplete;
        Debug.Log($"[Cubby's Blocks] Level {CurrentLevelNumber} complete!");

        ProgressManager.Instance?.UnlockNextLevel(CurrentLevelNumber);
        AdsManager.Instance?.OnLevelComplete();
        UIManager.Instance?.ShowLevelComplete();
        // No Invoke / auto-advance: LevelCompleteUI drives progression via LoadNextLevel()
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Restart current level.  In GameScene, UIManager.StartLevelSequence() shows
    /// the countdown.  In TestLevelSetup scenes, falls back to scene reload.
    /// </summary>
    public void RestartLevel()
    {
        CurrentState = GameState.Playing;

        if (LevelManager.Instance != null)
        {
            if (CurrentLevel == null)
            {
                Debug.LogWarning("[GameManager] RestartLevel called but CurrentLevel is null — returning to World Select.");
                UIManager.Instance?.GoToWorldSelect();
                return;
            }
            LevelManager.Instance.LoadLevel(CurrentLevel);
            UIManager.Instance?.StartLevelSequence();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// Advance to the next level.  Called by LevelCompleteUI "Next Level" button.
    /// UIManager.StartLevelSequence() handles hiding overlays and countdown.
    /// </summary>
    public void LoadNextLevel()
    {
        if (CurrentState != GameState.LevelComplete) return;

        currentLevelIndex++;

        if (currentLevelIndex >= TotalLevels)
        {
            Debug.Log("[Cubby's Blocks] All levels complete — world finished!");
            UIManager.Instance?.GoToWorldSelect();
            return;
        }

        CurrentState = GameState.Playing;
        if (CurrentLevel == null)
        {
            Debug.LogWarning("[GameManager] LoadNextLevel: CurrentLevel is null — returning to World Select.");
            UIManager.Instance?.GoToWorldSelect();
            return;
        }
        LevelManager.Instance?.LoadLevel(CurrentLevel);
        UIManager.Instance?.StartLevelSequence();
        Debug.Log($"[Cubby's Blocks] Loading level {CurrentLevelNumber}…");
    }

    /// <summary>Jump to a specific level by 0-based index (called from UIManager.GoToGame).</summary>
    public void LoadLevelAt(int index)
    {
        if (index < 0 || index >= TotalLevels) return;
        currentLevelIndex = index;
        CurrentState      = GameState.Playing;
        LevelManager.Instance?.LoadLevel(CurrentLevel);
    }

    /// <summary>Find the 0-based index in the levels array for a given levelNumber + world.</summary>
    public int FindLevelIndex(int levelNumber, int worldNumber)
    {
        if (levels == null) return -1;
        for (int i = 0; i < levels.Length; i++)
            if (levels[i] != null &&
                levels[i].levelNumber == levelNumber &&
                levels[i].worldNumber == worldNumber)
                return i;
        return -1;
    }
}
