using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Menu: Blockavist ▸ 5. Build UI
///
/// Creates the complete UI Canvas hierarchy in the active scene and wires
/// all UIManager/screen script references automatically.
///
/// PREREQUISITES
///   • Run "1. Build All Prefabs" and "2. Create GameScene" first.
///   • TextMeshPro Essential Resources must be imported
///     (Window ▸ TextMeshPro ▸ Import TMP Essential Resources).
///
/// SAFE TO RE-RUN: reuses the existing UIRoot and any screens already inside it.
/// Only missing screens are created — existing screens (and their button listeners)
/// are left intact.  UIManager references are always re-wired.
/// GameManager is never touched, so the Levels array is preserved.
/// </summary>
public static class UIBuilder
{
    // Reference resolution for Canvas Scaler (landscape 16:9)
    private static readonly Vector2 Ref = new Vector2(1920, 1080);

    // Colour palette
    private static readonly Color BgDark        = new Color(0.08f, 0.08f, 0.13f);
    private static readonly Color BgMid         = new Color(0.13f, 0.13f, 0.20f);
    private static readonly Color CardBg         = new Color(0.18f, 0.18f, 0.27f);
    private static readonly Color AccentBlue     = new Color(0.20f, 0.55f, 1.00f);
    private static readonly Color AccentGreen    = new Color(0.15f, 0.78f, 0.35f);
    private static readonly Color AccentRed      = new Color(0.85f, 0.22f, 0.22f);
    private static readonly Color AccentYellow   = new Color(1.00f, 0.82f, 0.10f);
    private static readonly Color AccentOrange   = new Color(1.00f, 0.55f, 0.10f);
    private static readonly Color White          = Color.white;
    private static readonly Color DimOverlay     = new Color(0f, 0f, 0f, 0.72f);

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Blockavist/5. Build UI")]
    public static void BuildUI()
    {
        // ── EventSystem ───────────────────────────────────────────────────────
        EnsureEventSystem();

        // ── Service singletons ────────────────────────────────────────────────
        // UIManager is resolved before screen builders so BuildGameHUD can
        // reference the real component for a persistent listener.
        EnsureComponent<ProgressManager>("ProgressManager");
        EnsureComponent<AudioManager>   ("AudioManager");
        EnsureComponent<AdsManager>     ("AdsManager");
        var uiManagerGO   = EnsureComponent<UIManager>("UIManager");
        var uiManagerComp = uiManagerGO.GetComponent<UIManager>();

        // ── Root Canvas (reuse if present) ────────────────────────────────────
        var existingRoot = GameObject.Find("UIRoot");
        var root  = existingRoot != null ? existingRoot : CreateCanvas("UIRoot", sortOrder: 10);
        var rootT = root.transform;

        // ── Fade overlay ──────────────────────────────────────────────────────
        // Reuse if present; only add components on first creation.
        var fadeT  = rootT.Find("FadeOverlay");
        CanvasGroup fadeCG;
        if (fadeT != null)
        {
            fadeCG = fadeT.GetComponent<CanvasGroup>();
        }
        else
        {
            var fadeRT = PanelFull(rootT, "FadeOverlay", Color.black);
            fadeCG            = fadeRT.gameObject.AddComponent<CanvasGroup>();
            fadeCG.alpha      = 0f;
            fadeCG.blocksRaycasts = false;
            var fadeCanvas    = fadeRT.gameObject.AddComponent<Canvas>();
            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder    = 99;
            fadeRT.gameObject.AddComponent<GraphicRaycaster>();
        }

        // ── Screens — find existing or build missing ──────────────────────────
        // Existing screens keep their GameObjects, components, and button
        // listeners intact.  Only absent screens are built from scratch.
        var loadingGO     = FindOrCreate(rootT, "LoadingScreen",      () => BuildLoadingScreen(rootT));
        var mainMenuGO    = FindOrCreate(rootT, "MainMenuScreen",     () => BuildMainMenu(rootT));
        var wsGO          = FindOrCreate(rootT, "WorldSelectScreen",  () => BuildWorldSelect(rootT));
        var lsGO          = FindOrCreate(rootT, "LevelSelectScreen",  () => BuildLevelSelect(rootT));
        RewireLevelSelectButtons(lsGO); // always refreshed — repairs lost refs on existing screens
        var gameHUD       = FindOrCreate(rootT, "GameHUD",            () => BuildGameHUD(rootT, uiManagerComp));
        var settingsGO    = FindOrCreate(rootT, "SettingsPanel",      () => BuildSettingsPanel(rootT));
        var pauseGO       = FindOrCreate(rootT, "PausePanel",         () => BuildPausePanel(rootT));
        var countdownGO   = FindOrCreate(rootT, "CountdownPanel",     () => BuildCountdownPanel(rootT));
        var lvlCompleteGO = FindOrCreate(rootT, "LevelCompletePanel", () => BuildLevelCompletePanel(rootT));
        var gameOverGO    = FindOrCreate(rootT, "GameOverPanel",      () => BuildGameOverPanel(rootT));
        var tutorialGO    = FindOrCreate(rootT, "TutorialPanel",      () => BuildTutorialPanel(rootT));

        // ── Initial active states ─────────────────────────────────────────────
        loadingGO    .SetActive(true);
        mainMenuGO   .SetActive(false);
        wsGO         .SetActive(false);
        lsGO         .SetActive(false);
        gameHUD      .SetActive(false);
        settingsGO   .SetActive(false);
        pauseGO      .SetActive(false);
        countdownGO  .SetActive(false);
        lvlCompleteGO.SetActive(false);
        gameOverGO   .SetActive(false);
        tutorialGO   .SetActive(false);

        // ── UIManager wiring (always refreshed) ───────────────────────────────
        var so = new SerializedObject(uiManagerComp);
        SetObjRef(so, "loadingScreen",      loadingGO    .GetComponent<LoadingScreenUI>());
        SetObjRef(so, "mainMenu",           mainMenuGO   .GetComponent<MainMenuUI>());
        SetObjRef(so, "worldSelect",        wsGO         .GetComponent<WorldSelectUI>());
        SetObjRef(so, "levelSelect",        lsGO         .GetComponent<LevelSelectUI>());
        SetObjRef(so, "gameHUD",            gameHUD);
        SetObjRef(so, "settingsPanel",      settingsGO   .GetComponent<SettingsUI>());
        SetObjRef(so, "pausePanel",         pauseGO      .GetComponent<PauseUI>());
        SetObjRef(so, "levelCompletePanel", lvlCompleteGO.GetComponent<LevelCompleteUI>());
        SetObjRef(so, "gameOverPanel",      gameOverGO   .GetComponent<GameOverUI>());
        SetObjRef(so, "countdownPanel",     countdownGO  .GetComponent<CountdownUI>());
        SetObjRef(so, "tutorialPanel",      tutorialGO   .GetComponent<TutorialUI>());
        SetObjRef(so, "fadeOverlay",        fadeCG);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(uiManagerGO);

        // ── TutorialManager — wire panel reference ────────────────────────────
        // Only TutorialManager is marked dirty — NOT the GameManager GO — so
        // the Levels array and other GameManager fields are never disturbed.
        var gmGO        = EnsureComponent<GameManager>("GameManager");
        var tutorialMgr = gmGO.GetComponent<TutorialManager>();
        if (tutorialMgr == null) tutorialMgr = gmGO.AddComponent<TutorialManager>();
        var soTM = new SerializedObject(tutorialMgr);
        SetObjRef(soTM, "tutorialPanel", tutorialGO.GetComponent<TutorialUI>());
        soTM.ApplyModifiedProperties();
        EditorUtility.SetDirty(tutorialMgr); // component-level dirty, not the whole GO

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Blockavist] UI built successfully. Save the scene (Ctrl+S).");
    }

    /// <summary>
    /// Returns the existing child named <paramref name="name"/> under
    /// <paramref name="parent"/>, or runs <paramref name="create"/> and returns
    /// its result if no such child exists.
    /// </summary>
    static GameObject FindOrCreate(Transform parent, string name, System.Func<GameObject> create)
    {
        var existing = parent.Find(name);
        return existing != null ? existing.gameObject : create();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SCREEN BUILDERS
    // ═════════════════════════════════════════════════════════════════════════

    // ── Loading Screen ────────────────────────────────────────────────────────
    static GameObject BuildLoadingScreen(Transform parent)
    {
        var root = PanelFull(parent, "LoadingScreen", BgDark).gameObject;
        var ui   = root.AddComponent<LoadingScreenUI>();

        // Title
        var title = TMP(root.transform, "BLOCKAVIST", 110, AccentBlue);
        Anchored(title.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(900, 130));

        // Progress bar background
        var barBG = PanelAnchored(root.transform, "BarBG", new Vector2(0.5f, 0.32f),
                                  new Vector2(700, 22), new Color(0.25f, 0.25f, 0.35f));
        // Progress fill
        var fillRT  = PanelAnchored(barBG, "BarFill", new Vector2(0f, 0.5f),
                                    new Vector2(700, 22), AccentBlue);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot     = new Vector2(0f, 0.5f);
        fillRT.sizeDelta = new Vector2(0f, 0f);
        var fillImg  = fillRT.gameObject.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        // Status text
        var status = TMP(root.transform, "Loading...  0%", 38, new Color(0.7f, 0.7f, 0.8f));
        Anchored(status.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(700, 50));

        // Wire
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "progressFill", fillImg);
        SetObjRef(soUI, "statusText",   status);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── Main Menu ─────────────────────────────────────────────────────────────
    static GameObject BuildMainMenu(Transform parent)
    {
        var root = PanelFull(parent, "MainMenuScreen", BgDark).gameObject;
        var ui   = root.AddComponent<MainMenuUI>();

        TMP(root.transform, "BLOCKAVIST", 120, AccentBlue)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.70f), new Vector2(1000, 140)));

        // Play button
        var playBtn = Btn(root.transform, "PLAY", new Vector2(0.5f, 0.45f),
                          new Vector2(420, 110), AccentGreen, White, 56);
        UnityEventTools.AddPersistentListener(playBtn.onClick, ui.OnPlayClicked);

        // Settings button (top-right corner) — text label avoids missing-glyph warning
        var settingsBtn = Btn(root.transform, "Settings", new Vector2(0.89f, 0.93f),
                              new Vector2(200, 70), CardBg, White, 36);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, ui.OnSettingsClicked);

        return root;
    }

    // ── Settings Panel (overlay) ──────────────────────────────────────────────
    static GameObject BuildSettingsPanel(Transform parent)
    {
        var root = PanelFull(parent, "SettingsPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<SettingsUI>();

        // Card
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(640, 480), CardBg);

        TMP(card, "Settings", 64, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.82f), new Vector2(500, 80)));

        // Music row
        var musicLabel  = TMP(card, "Music", 44, White);
        Anchored(musicLabel.rectTransform, new Vector2(0.3f, 0.58f), new Vector2(260, 60));

        var musicToggle = CreateToggle(card, new Vector2(0.72f, 0.58f));
        UnityEventTools.AddBoolPersistentListener(musicToggle.onValueChanged, ui.OnMusicToggled, true);

        // SFX row
        var sfxLabel    = TMP(card, "Sound Effects", 44, White);
        Anchored(sfxLabel.rectTransform, new Vector2(0.3f, 0.40f), new Vector2(260, 60));

        var sfxToggle   = CreateToggle(card, new Vector2(0.72f, 0.40f));
        UnityEventTools.AddBoolPersistentListener(sfxToggle.onValueChanged, ui.OnSFXToggled, true);

        // Close button
        var closeBtn = Btn(card, "X  Close", new Vector2(0.5f, 0.14f),
                           new Vector2(260, 72), AccentRed, White, 42);
        UnityEventTools.AddPersistentListener(closeBtn.onClick, ui.OnCloseClicked);

        // Wire toggles
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "musicToggle", musicToggle);
        SetObjRef(soUI, "sfxToggle",   sfxToggle);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── World Select ──────────────────────────────────────────────────────────
    static GameObject BuildWorldSelect(Transform parent)
    {
        var root = PanelFull(parent, "WorldSelectScreen", BgDark).gameObject;
        var ui   = root.AddComponent<WorldSelectUI>();

        TMP(root.transform, "Select World", 72, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.88f), new Vector2(800, 90)));

        // World 1 card
        var w1Card = PanelAnchored(root.transform, "World1Card", new Vector2(0.5f, 0.64f),
                                   new Vector2(800, 180), AccentBlue);
        TMP(w1Card, "World 1  -  Basic Blocks", 50, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.60f), new Vector2(700, 65)));
        TMP(w1Card, "Levels 1 - 10", 36, new Color(0.85f, 0.95f, 1f))
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.28f), new Vector2(700, 50)));
        var w1Btn = w1Card.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(w1Btn.onClick, ui.OnWorld1Clicked);
        if (w1Card.gameObject.GetComponent<Image>() == null)
            w1Card.gameObject.AddComponent<Image>().color = AccentBlue;

        // World 2 card
        var w2Card = PanelAnchored(root.transform, "World2Card", new Vector2(0.5f, 0.38f),
                                   new Vector2(800, 180), new Color(0.30f, 0.30f, 0.42f));
        TMP(w2Card, "World 2  -  ???", 50, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.60f), new Vector2(700, 65)));
        var w2Sub = TMP(w2Card, "Complete World 1 to unlock", 34, new Color(0.7f, 0.7f, 0.85f));
        Anchored(w2Sub.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(700, 50));

        // Padlock label (text-based)
        var lockTxt = TMP(w2Card, "[locked]", 36, new Color(0.9f, 0.8f, 0.2f));
        Anchored(lockTxt.rectTransform, new Vector2(0.88f, 0.55f), new Vector2(120, 50));

        var w2Btn = w2Card.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(w2Btn.onClick, ui.OnWorld2Clicked);
        if (w2Card.gameObject.GetComponent<Image>() == null)
            w2Card.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.30f, 0.42f);

        // Back button
        var backBtn = Btn(root.transform, "Back", new Vector2(0.12f, 0.92f),
                          new Vector2(200, 72), CardBg, White, 38);
        UnityEventTools.AddPersistentListener(backBtn.onClick, ui.OnBackClicked);

        // Wire references
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "world1Button",   w1Btn);
        SetObjRef(soUI, "world2Button",   w2Btn);
        SetObjRef(soUI, "world2LockIcon", lockTxt.gameObject);
        SetObjRef(soUI, "world2SubText",  w2Sub);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── Level Select ──────────────────────────────────────────────────────────
    static GameObject BuildLevelSelect(Transform parent)
    {
        var root = PanelFull(parent, "LevelSelectScreen", BgDark).gameObject;
        var ui   = root.AddComponent<LevelSelectUI>();

        var titleTmp = TMP(root.transform, "World 1", 72, White);
        Anchored(titleTmp.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(800, 90));

        var backBtn = Btn(root.transform, "Back", new Vector2(0.10f, 0.92f),
                          new Vector2(200, 72), CardBg, White, 38);
        UnityEventTools.AddPersistentListener(backBtn.onClick, ui.OnBackClicked);

        // Grid — 5 columns × 2 rows
        var gridGO = new GameObject("LevelGrid");
        gridGO.transform.SetParent(root.transform, false);
        var gridImg = gridGO.AddComponent<Image>();
        gridImg.color = Color.clear;
        var gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.05f, 0.15f);
        gridRT.anchorMax = new Vector2(0.95f, 0.82f);
        gridRT.offsetMin = gridRT.offsetMax = Vector2.zero;

        var glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.spacing         = new Vector2(24, 24);
        glg.cellSize        = new Vector2(280, 200);
        glg.childAlignment  = TextAnchor.MiddleCenter;

        var buttonRefs = new LevelButtonUI[10];
        for (int i = 0; i < 10; i++)
        {
            int lvlNum = i + 1;
            var cell   = new GameObject($"Level_{lvlNum:00}");
            cell.transform.SetParent(gridGO.transform, false);

            var cellImg = cell.AddComponent<Image>();
            cellImg.color = CardBg;
            cell.AddComponent<RectTransform>();

            var btn    = cell.AddComponent<Button>();
            var btnUI  = cell.AddComponent<LevelButtonUI>();
            buttonRefs[i] = btnUI;

            // Number label
            var numTmp = TMP(cell.transform, lvlNum.ToString(), 56, White);
            Anchored(numTmp.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(200, 80));

            // Lock icon (hidden by default for first level)
            var lockTmp = TMP(cell.transform, "[L]", 44, AccentYellow);
            Anchored(lockTmp.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(80, 60));
            lockTmp.gameObject.SetActive(i > 0);

            // Wire LevelButtonUI
            var soBtn = new SerializedObject(btnUI);
            SetObjRef(soBtn, "button",     btn);
            SetObjRef(soBtn, "numberText", numTmp);
            SetObjRef(soBtn, "lockIcon",   lockTmp.gameObject);
            soBtn.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(btn.onClick, btnUI.OnClicked);
        }

        // Wire LevelSelectUI
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "titleText", titleTmp);
        var btnsProp = soUI.FindProperty("levelButtons");
        btnsProp.arraySize = 10;
        for (int i = 0; i < 10; i++)
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttonRefs[i];
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── Game HUD ──────────────────────────────────────────────────────────────
    // Receives uiManager directly so a persistent (serialized) listener can be
    // added — lambdas cannot be persistent listeners.
    static GameObject BuildGameHUD(Transform parent, UIManager uiManager)
    {
        var root  = new GameObject("GameHUD");
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = Color.clear;
        var rt  = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Pause button — top-left corner
        var pauseBtn = Btn(root.transform, "II", new Vector2(0.06f, 0.92f),
                           new Vector2(90, 90), new Color(0.2f, 0.2f, 0.3f, 0.85f), White, 44);
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, uiManager.ShowPause);

        return root;
    }

    // ── Pause Panel (overlay) ─────────────────────────────────────────────────
    static GameObject BuildPausePanel(Transform parent)
    {
        var root = PanelFull(parent, "PausePanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<PauseUI>();

        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(560, 400), CardBg);

        TMP(card, "Paused", 72, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.78f), new Vector2(440, 90)));

        var resume = Btn(card, "Resume", new Vector2(0.5f, 0.52f),
                         new Vector2(400, 96), AccentGreen, White, 46);
        UnityEventTools.AddPersistentListener(resume.onClick, ui.OnResumeClicked);

        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.22f),
                        new Vector2(400, 80), CardBg, White, 40);
        UnityEventTools.AddPersistentListener(wsBtn.onClick, ui.OnWorldSelectClicked);

        return root;
    }

    // ── Countdown Panel (overlay) ─────────────────────────────────────────────
    static GameObject BuildCountdownPanel(Transform parent)
    {
        var root = PanelFull(parent, "CountdownPanel",
                             new Color(0f, 0f, 0f, 0.45f)).gameObject;
        var ui   = root.AddComponent<CountdownUI>();

        var numTmp = TMP(root.transform, "3", 220, White);
        numTmp.fontStyle = FontStyles.Bold;
        Anchored(numTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(400, 300));

        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "countdownText", numTmp);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── Level Complete Panel (overlay) ────────────────────────────────────────
    static GameObject BuildLevelCompletePanel(Transform parent)
    {
        var root = PanelFull(parent, "LevelCompletePanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<LevelCompleteUI>();

        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(640, 420), CardBg);

        TMP(card, "Level Complete!", 68, AccentGreen)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.80f), new Vector2(580, 85)));

        // Stars row (placeholder)
        TMP(card, "* * *", 72, AccentYellow)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.60f), new Vector2(460, 80)));

        var nextBtn = Btn(card, "Next Level", new Vector2(0.5f, 0.38f),
                          new Vector2(440, 96), AccentGreen, White, 48);
        UnityEventTools.AddPersistentListener(nextBtn.onClick, ui.OnNextLevelClicked);

        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.14f),
                        new Vector2(440, 80), CardBg, White, 40);
        UnityEventTools.AddPersistentListener(wsBtn.onClick, ui.OnWorldSelectClicked);

        return root;
    }

    // ── Game Over Panel (overlay) ─────────────────────────────────────────────
    static GameObject BuildGameOverPanel(Transform parent)
    {
        var root = PanelFull(parent, "GameOverPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<GameOverUI>();

        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(600, 400), CardBg);

        TMP(card, "Game Over", 72, AccentRed)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.80f), new Vector2(520, 90)));

        var retryBtn = Btn(card, "Retry", new Vector2(0.5f, 0.50f),
                           new Vector2(440, 96), AccentOrange, White, 48);
        UnityEventTools.AddPersistentListener(retryBtn.onClick, ui.OnRetryClicked);

        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.20f),
                        new Vector2(440, 80), CardBg, White, 40);
        UnityEventTools.AddPersistentListener(wsBtn.onClick, ui.OnWorldSelectClicked);

        return root;
    }

    // ── Tutorial Panel (overlay) ──────────────────────────────────────────────
    static GameObject BuildTutorialPanel(Transform parent)
    {
        var root = PanelFull(parent, "TutorialPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<TutorialUI>();

        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(680, 380), CardBg);

        var msgTmp = TMP(card, "Tap yellow blocks to destroy them", 52, White);
        Anchored(msgTmp.rectTransform, new Vector2(0.5f, 0.64f), new Vector2(580, 160));
        msgTmp.enableWordWrapping = true;

        var confirmBtn = Btn(card, "Got it!", new Vector2(0.5f, 0.22f),
                             new Vector2(380, 96), AccentGreen, White, 48);
        UnityEventTools.AddPersistentListener(confirmBtn.onClick, ui.OnConfirmClicked);

        var btnLabelTmp = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();

        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "messageText",   msgTmp);
        SetObjRef(soUI, "buttonLabel",   btnLabelTmp);
        SetObjRef(soUI, "confirmButton", confirmBtn);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOW-LEVEL HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    // Creates a root Canvas with CanvasScaler
    static GameObject CreateCanvas(string name, int sortOrder)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Ref;
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // Full-screen panel stretched to parent
    static RectTransform PanelFull(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Centered, fixed-size panel
    static RectTransform PanelAnchored(Transform parent, string name,
        Vector2 anchorPos, Vector2 size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPos;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    // TMP text label (fills parent by default)
    static TextMeshProUGUI TMP(Transform parent, string text, float size,
        Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go  = new GameObject("TMP_" + text.Replace(" ", "_").Substring(0, Mathf.Min(text.Length, 12)));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // Button with label, placed at anchorPos with given size
    static Button Btn(Transform parent, string label, Vector2 anchorPos,
        Vector2 size, Color bg, Color fg, float fontSize)
    {
        var go  = new GameObject("Btn_" + label.Replace(" ", "_").Trim());
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(anchorPos.x, anchorPos.y);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var btn    = go.AddComponent<Button>();
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor      = bg;
        colors.highlightedColor = bg * 1.18f;
        colors.pressedColor     = bg * 0.80f;
        colors.selectedColor    = bg;
        btn.colors = colors;

        TMP(go.transform, label, fontSize, fg);
        return btn;
    }

    // Toggle with background + checkmark images
    static Toggle CreateToggle(Transform parent, Vector2 anchorPos)
    {
        var go  = new GameObject("Toggle");
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPos;
        rt.sizeDelta = new Vector2(120, 60);
        rt.anchoredPosition = Vector2.zero;

        var toggle = go.AddComponent<Toggle>();

        // Background
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.38f);
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Checkmark (coloured green when on)
        var ckGO  = new GameObject("Checkmark");
        ckGO.transform.SetParent(bgGO.transform, false);
        var ckImg = ckGO.AddComponent<Image>();
        ckImg.color = AccentGreen;
        var ckRT  = ckGO.GetComponent<RectTransform>();
        ckRT.anchorMin = ckRT.anchorMax = ckRT.pivot = Vector2.one * 0.5f;
        ckRT.sizeDelta = new Vector2(84, 44);

        toggle.targetGraphic = bgImg;
        toggle.graphic       = ckImg;
        toggle.isOn          = true;

        return toggle;
    }

    // Set anchored position + size on an existing RectTransform
    static void Anchored(RectTransform rt, Vector2 anchorPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPos;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    // Serialized property helper
    static void SetObjRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    // Re-wires the levelButtons array and OnClicked listeners for every Level_XX
    // cell inside LevelGrid.  Called on every Build UI run so that a screen that
    // survived FindOrCreate always has correct serialized refs, even if a prior
    // destroy-and-rebuild run left the array empty.
    static void RewireLevelSelectButtons(GameObject lsGO)
    {
        var ui = lsGO.GetComponent<LevelSelectUI>();
        if (ui == null) return;

        var gridT = lsGO.transform.Find("LevelGrid");
        if (gridT == null) return;

        var buttonRefs = new LevelButtonUI[10];
        for (int i = 0; i < 10; i++)
        {
            int lvlNum = i + 1;
            var cellT  = gridT.Find($"Level_{lvlNum:00}");
            if (cellT == null) continue;

            var btn   = cellT.GetComponent<Button>();
            var btnUI = cellT.GetComponent<LevelButtonUI>();
            if (btn == null || btnUI == null) continue;

            buttonRefs[i] = btnUI;

            // Re-wire LevelButtonUI's serialized fields from its own children
            var numTmpGO  = cellT.Find($"TMP_{lvlNum}");
            var lockTmpGO = cellT.Find("TMP_[L]");
            var soBtn = new SerializedObject(btnUI);
            SetObjRef(soBtn, "button",     btn);
            if (numTmpGO  != null) SetObjRef(soBtn, "numberText", numTmpGO .GetComponent<TextMeshProUGUI>());
            if (lockTmpGO != null) SetObjRef(soBtn, "lockIcon",   lockTmpGO.gameObject);
            soBtn.ApplyModifiedProperties();

            // Ensure exactly one OnClicked persistent listener (remove stale, re-add)
            UnityEventTools.RemovePersistentListener(btn.onClick, btnUI.OnClicked);
            UnityEventTools.AddPersistentListener   (btn.onClick, btnUI.OnClicked);
        }

        // Re-wire the LevelSelectUI.levelButtons array
        var soUI     = new SerializedObject(ui);
        var btnsProp = soUI.FindProperty("levelButtons");
        btnsProp.arraySize = 10;
        for (int i = 0; i < 10; i++)
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttonRefs[i];
        soUI.ApplyModifiedProperties();
    }

    // Creates an EventSystem with InputSystemUIInputModule if none exists.
    // If one already exists, replaces any legacy StandaloneInputModule with
    // InputSystemUIInputModule so it works with the new Input System package.
    static void EnsureEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            es = go.GetComponent<EventSystem>();
        }

        // Remove legacy module if present — it conflicts with the new Input System
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) Object.DestroyImmediate(legacy);

        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    // Ensures a singleton component exists in the scene; returns the GameObject.
    static GameObject EnsureComponent<T>(string name) where T : Component
    {
        var existing = Object.FindAnyObjectByType<T>();
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name);
        go.AddComponent<T>();
        return go;
    }
}

// Extension to allow fluent .Do() call on RectTransform without breaking up expressions
internal static class RectTransformExt
{
    internal static RectTransform Do(this RectTransform rt, System.Action<RectTransform> action)
    {
        action(rt);
        return rt;
    }
}
