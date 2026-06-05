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

    // ── Fade ──────────────────────────────────────────────────────────────────
    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float       fadeDuration = 0.35f;

    // ── State ─────────────────────────────────────────────────────────────────
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
        LevelManager.Instance?.UnloadCurrentLevel();
        Transition(mainMenu?.gameObject);
    }

    public void GoToWorldSelect()
    {
        HideAllOverlays();
        LevelManager.Instance?.UnloadCurrentLevel();
        Transition(worldSelect?.gameObject, () => worldSelect?.Refresh());
    }

    public void GoToLevelSelect(int world)
    {
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
        countdownPanel?.Begin();
    }

    // ── Overlay API ───────────────────────────────────────────────────────────

    public void ShowSettings() => settingsPanel?.gameObject.SetActive(true);
    public void HideSettings() => settingsPanel?.gameObject.SetActive(false);

    public void ShowPause()
    {
        if (pausePanel == null) return;
        pausePanel.gameObject.SetActive(true);
        LevelManager.Instance?.ActivePlayer?.Freeze();
    }

    public void HidePause()
    {
        if (pausePanel == null) return;
        pausePanel.gameObject.SetActive(false);
        LevelManager.Instance?.ActivePlayer?.Unfreeze();
    }

    public void ShowLevelComplete()
    {
        levelCompletePanel?.gameObject.SetActive(true);
    }

    public void ShowGameOver()
    {
        gameOverPanel?.gameObject.SetActive(true);
    }

    public void HideAllOverlays()
    {
        settingsPanel      ?.gameObject.SetActive(false);
        pausePanel         ?.gameObject.SetActive(false);
        levelCompletePanel ?.gameObject.SetActive(false);
        gameOverPanel      ?.gameObject.SetActive(false);
        countdownPanel     ?.gameObject.SetActive(false);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator GoToGameCo(int levelIndex)
    {
        yield return Fade(0f, 1f);

        GameManager.Instance?.LoadLevelAt(levelIndex);
        ShowOnlyScreen(gameHUD);

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
