using UnityEngine;

/// <summary>
/// Shows a two-step tutorial overlay on Level 1 the first time it is played.
/// Attach to a persistent GameObject in GameScene alongside GameManager.
/// Wire tutorialPanel in the Inspector.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string PrefsKey = "Tutorial_L1_Seen";

    [SerializeField] private TutorialUI tutorialPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => CountdownUI.OnComplete += HandleCountdownComplete;
    private void OnDisable() => CountdownUI.OnComplete -= HandleCountdownComplete;

    private void HandleCountdownComplete()
    {
        int  levelNum = GameManager.Instance?.CurrentLevel?.levelNumber ?? -1;
        bool seen     = PlayerPrefs.GetInt(PrefsKey, 0) != 0;
        Debug.Log($"[TutorialManager] 1. OnComplete received — levelNumber={levelNum}  tutorialSeen={seen}  tutorialPanel={(tutorialPanel != null ? tutorialPanel.name : "NULL")}");

        int  levelNumCheck = GameManager.Instance?.CurrentLevel?.levelNumber ?? -1;
        bool seenCheck     = PlayerPrefs.GetInt(PrefsKey, 0) != 0;
        bool shouldShow    = levelNumCheck == 1 && !seenCheck;
        Debug.Log($"[TutorialManager] 2. ShouldShowTutorial — levelNumber={levelNumCheck}  seen={seenCheck}  result={shouldShow}");

        if (!shouldShow) return;

        var player = LevelManager.Instance?.ActivePlayer;
        Debug.Log($"[TutorialManager] 3. Calling tutorialPanel.Show() — player={(player != null ? player.name : "NULL")}  IsFrozen={player?.IsFrozen}");
        player?.Freeze();
        tutorialPanel?.Show(OnTutorialDone);
    }

    private void OnTutorialDone()
    {
        Debug.Log("[TutorialManager] 4. OnTutorialDone called — writing Tutorial_L1_Seen=1 and unfreezing player.");
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();
        LevelManager.Instance?.ActivePlayer?.Unfreeze();
    }

    private bool ShouldShowTutorial() =>
        GameManager.Instance?.CurrentLevel?.levelNumber == 1 &&
        PlayerPrefs.GetInt(PrefsKey, 0) == 0;
}
