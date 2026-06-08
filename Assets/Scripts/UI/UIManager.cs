using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central screen navigator.  All screens are children of the same Canvas;
/// only one is active at a time.  Overlays are shown additively on top.
///
/// Screen flow:
///   Loading → MainMenu → WorldSelect → LevelSelect → Game
///   Game overlays: Pause | LevelComplete | GameOver
///   MainMenu overlay: Settings
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── Screens (mutually exclusive) ──────────────────────────────────────────
    [Header("Screens")]
    [SerializeField] private LoadingScreenUI  loadingScreen;
    [SerializeField] private MainMenuUI       mainMenu;
    [SerializeField] private WorldSelectUI    worldSelect;
    [SerializeField] private LevelSelectUI    levelSelect;
    [SerializeField] private GameObject       gameHUD;

    // ── Overlays (additive, shown on top of game) ─────────────────────────────
    [Header("Overlays")]
    [SerializeField] private SettingsUI      settingsPanel;
    [SerializeField] private PauseUI         pausePanel;
    [SerializeField] private LevelCompleteUI levelCompletePanel;
    [SerializeField] private GameOverUI      gameOverPanel;
    [SerializeField] private CountdownUI     countdownPanel;
    [SerializeField] private TutorialUI      tutorialPanel;
    [SerializeField] private HintUI          hintPanel;

    // ── Fade ──────────────────────────────────────────────────────────────────
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float       fadeDuration = 0.35f;

    // ── State ─────────────────────────────────────────────────────────────────
    public bool IsPauseOpen { get; private set; }
    public bool IsHintOpen  { get; private set; }

    private GameObject[] allScreens;
    private Coroutine    _activeNav;   // only one navigation coroutine at a time

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        allScreens = new GameObject[]
        {
            loadingScreen?.gameObject,
            mainMenu?.gameObject,
            worldSelect?.gameObject,
            levelSelect?.gameObject,
            gameHUD
        };

        HideAllOverlays();
        ShowOnlyScreen(loadingScreen?.gameObject);

        loadingScreen?.Begin(OnLoadingDone);
    }

    private void OnLoadingDone() => Transition(mainMenu?.gameObject);

    // ── Public navigation API ─────────────────────────────────────────────────

    public void GoToMainMenu()
    {
        HideAllOverlays();
        BackgroundManager.Instance?.SetVisible(false);
        LevelManager.Instance?.UnloadCurrentLevel();
        Transition(mainMenu?.gameObject);
    }

    public void GoToWorldSelect()
    {
        HideAllOverlays();
        BackgroundManager.Instance?.SetVisible(false);
        LevelManager.Instance?.UnloadCurrentLevel();
        Transition(worldSelect?.gameObject, () => worldSelect?.Refresh());
    }

    public void GoToLevelSelect(int world)
    {
        BackgroundManager.Instance?.SetVisible(false);
        Transition(levelSelect?.gameObject, () => levelSelect?.Setup(world));
    }

    /// <summary>
    /// Load level at index, fade to game HUD, then start countdown.
    /// Called from LevelButtonUI and (internally) RestartLevel / LoadNextLevel.
    /// </summary>
    public void GoToGame(int levelIndex)
    {
        if (_activeNav != null) StopCoroutine(_activeNav);
        _activeNav = StartCoroutine(GoToGameCo(levelIndex));
    }

    /// <summary>
    /// Show game HUD + start countdown without a screen transition.
    /// Used when re-starting or advancing within the same scene (overlays already open).
    /// </summary>
    public void StartLevelSequence()
    {
        HideAllOverlays();
        ShowOnlyScreen(gameHUD);
        BackgroundManager.Instance?.ResetClouds();
        BackgroundManager.Instance?.SetVisible(true);
        countdownPanel?.Begin();
    }

    // ── Overlay API ───────────────────────────────────────────────────────────

    public void ShowSettings() => settingsPanel?.gameObject.SetActive(true);
    public void HideSettings() => settingsPanel?.gameObject.SetActive(false);

    public void ShowPause()
    {
        if (pausePanel == null) return;
        IsPauseOpen = true;
        pausePanel.gameObject.SetActive(true);
        BackgroundManager.Instance?.SetVisible(false);
        LevelManager.Instance?.ActivePlayer?.Freeze();
    }

    public void HidePause()
    {
        if (pausePanel == null) return;
        IsPauseOpen = false;
        pausePanel.gameObject.SetActive(false);
        LevelManager.Instance?.ActivePlayer?.Unfreeze();
        // Only restore clouds if the player is still mid-game (not headed to a menu)
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
            BackgroundManager.Instance?.SetVisible(true);
    }

    public void ShowLevelComplete()
    {
        levelCompletePanel?.gameObject.SetActive(true);
        BackgroundManager.Instance?.SetVisible(false);
    }

    public void ShowGameOver()
    {
        gameOverPanel?.gameObject.SetActive(true);
        BackgroundManager.Instance?.SetVisible(false);
    }

    public void ShowHint()
    {
        if (hintPanel == null) return;
        IsHintOpen = true;
        hintPanel.gameObject.SetActive(true);
        LevelManager.Instance?.ActivePlayer?.Freeze();
    }

    public void HideHint()
    {
        if (hintPanel == null) return;
        IsHintOpen = false;
        hintPanel.gameObject.SetActive(false);
        LevelManager.Instance?.ActivePlayer?.Unfreeze();
    }

    public void HideAllOverlays()
    {
        settingsPanel      ?.gameObject.SetActive(false);
        pausePanel         ?.gameObject.SetActive(false);
        levelCompletePanel ?.gameObject.SetActive(false);
        gameOverPanel      ?.gameObject.SetActive(false);
        countdownPanel     ?.gameObject.SetActive(false);
        tutorialPanel      ?.gameObject.SetActive(false);
        hintPanel          ?.gameObject.SetActive(false);
        IsPauseOpen = false;
        IsHintOpen  = false;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator GoToGameCo(int levelIndex)
    {
        yield return Fade(0f, 1f);

        GameManager.Instance?.LoadLevelAt(levelIndex);
        ShowOnlyScreen(gameHUD);
        BackgroundManager.Instance?.ResetClouds();
        BackgroundManager.Instance?.SetVisible(true);

        yield return Fade(1f, 0f);

        countdownPanel?.Begin();   // player freezes inside Begin(); unfreezes on GO!
    }

    private void Transition(GameObject target, Action midpoint = null)
    {
        if (_activeNav != null) StopCoroutine(_activeNav);
        _activeNav = StartCoroutine(TransitionCo(target, midpoint));
    }

    private IEnumerator TransitionCo(GameObject target, Action midpoint = null)
    {
        yield return Fade(0f, 1f);
        ShowOnlyScreen(target);
        midpoint?.Invoke();
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null) yield break;
        float t = 0f;
        fadeOverlay.alpha = from;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        fadeOverlay.alpha = to;
    }

    private void ShowOnlyScreen(GameObject target)
    {
        if (allScreens == null) return;
        foreach (var s in allScreens)
            if (s != null) s.SetActive(s == target);
    }
}
