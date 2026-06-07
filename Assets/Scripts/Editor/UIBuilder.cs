using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Menu: Cubby's Blocks ▸ 5. Build UI
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
    private static readonly Color CardBg        = new Color(0.96f, 0.96f, 0.99f);   // light panel background
    private static readonly Color CardText      = new Color(0.10f, 0.12f, 0.18f);   // dark text for light cards
    private static readonly Color AccentBlue    = new Color(0.20f, 0.55f, 1.00f);
    private static readonly Color AccentGreen   = new Color(0.15f, 0.78f, 0.35f);
    private static readonly Color AccentRed     = new Color(0.85f, 0.22f, 0.22f);
    private static readonly Color AccentYellow  = new Color(1.00f, 0.82f, 0.10f);
    private static readonly Color AccentOrange  = new Color(1.00f, 0.55f, 0.10f);
    private static readonly Color White         = Color.white;
    private static readonly Color DimOverlay    = new Color(0f, 0f, 0f, 0.60f);

    // Sprite assets — loaded once per Build UI run
    private static Sprite _sprBtnGreen;
    private static Sprite _sprBtnBlue;
    private static Sprite _sprBtnRed;
    private static Sprite _sprCheckGreen;
    private static Sprite _sprCheckRed;
    private static Sprite _sprStarFull;
    private static Sprite _sprStarEmpty;
    private static Sprite _sprBackground; // Cubby_0 sub-sprite from Cubby.png (Multiple mode)

    // Font assets — loaded once per Build UI run
    private static TMP_FontAsset _fontBlocks; // game title / logo
    private static TMP_FontAsset _fontBold;   // all other UI text

    static void LoadAssets()
    {
        _sprBtnGreen   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/button_rectangle_green.asset");
        _sprBtnBlue    = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/button_rectangle_blue.asset");
        _sprBtnRed     = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/button_rectangle_red.asset");
        _sprCheckGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/check_square_green.asset");
        _sprCheckRed   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/check_square_red.asset");
        _sprStarFull   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/star_full.asset");
        _sprStarEmpty  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/star_empty.asset");

        _fontBlocks = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Kenney Blocks SDF.asset");
        _fontBold   = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Kenney Bold SDF.asset");

        if (_fontBlocks == null) Debug.LogWarning("[Cubby's Blocks] Kenney Blocks SDF not found in Assets/Fonts/");
        if (_fontBold   == null) Debug.LogWarning("[Cubby's Blocks] Kenney Bold SDF not found in Assets/Fonts/");

        // Cubby.png uses Multiple sprite mode — the sub-sprite is named Cubby_0
        _sprBackground = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Background/Cubby.png"))
            if (a is Sprite s && s.name == "Cubby_0") { _sprBackground = s; break; }
        if (_sprBackground == null)
            Debug.LogWarning("[Cubby's Blocks] Background sprite Cubby_0 not found in Assets/Art/Background/Cubby.png");
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Cubby's Blocks/5. Build UI")]
    public static void BuildUI()
    {
        LoadAssets();

        // ── EventSystem ───────────────────────────────────────────────────────
        EnsureEventSystem();

        // ── Service singletons ────────────────────────────────────────────────
        // UIManager is resolved before screen builders so BuildGameHUD can
        // reference the real component for a persistent listener.
        EnsureComponent<ProgressManager>  ("ProgressManager");
        EnsureComponent<AudioManager>    ("AudioManager");
        EnsureComponent<AdsManager>      ("AdsManager");
        EnsureComponent<VFXManager>      ("VFXManager");
        EnsureComponent<BackgroundManager>("Background");
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
        // SettingsPanel — force-rebuild to remove ToggleSpriteSwapper components and
        // old persistent listener wiring. Runtime listeners are added in SettingsUI.Awake.
        { var s = rootT.Find("SettingsPanel"); if (s != null) Object.DestroyImmediate(s.gameObject); }
        var settingsGO    = BuildSettingsPanel(rootT);
        var pauseGO       = FindOrCreate(rootT, "PausePanel",         () => BuildPausePanel(rootT));
        // CountdownPanel is stateless (text only, no button listeners) — always rebuild
        // so that text size, word-wrap, and outline fixes are applied to existing scenes.
        { var cd = rootT.Find("CountdownPanel"); if (cd != null) Object.DestroyImmediate(cd.gameObject); }
        var countdownGO   = BuildCountdownPanel(rootT);
        var lvlCompleteGO = FindOrCreate(rootT, "LevelCompletePanel", () => BuildLevelCompletePanel(rootT));
        var gameOverGO    = FindOrCreate(rootT, "GameOverPanel",      () => BuildGameOverPanel(rootT));
        var tutorialGO    = FindOrCreate(rootT, "TutorialPanel",      () => BuildTutorialPanel(rootT));

        // ── Audio — ButtonSFX on every standard button ────────────────────────
        // Skips LevelButtonUI buttons; those play level_select SFX in OnClicked().
        EnsureButtonSFX(root);

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

        Debug.Log("[Cubby's Blocks] UI built successfully. Save the scene (Ctrl+S).");
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
        // Transparent panel — background image sits in front of camera, all UI sits on top.
        var root = PanelFull(parent, "LoadingScreen", Color.clear).gameObject;
        var ui   = root.AddComponent<LoadingScreenUI>();

        AddScreenBackground(root.transform);

        // ── Title ─────────────────────────────────────────────────────────────
        // Upper-centre, clear of the background characters in the centre-bottom area.
        // #FFE000 bright yellow reads well against the pastel blue sky with black outline.
        var title = TMP(root.transform, "Cubby's Blocks", 76,
                        new Color(1f, 0.878f, 0f),          // #FFE000
                        TextAlignmentOptions.Center, _fontBlocks);
        Anchored(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(960, 110));

        // TMP outline
        title.outlineColor = Color.black;
        title.outlineWidth = 0.2f;

        // TMP underlay (drop shadow) — modifies an instanced copy of the font material
        var titleMat = title.fontMaterial;
        if (titleMat != null)
        {
            titleMat.EnableKeyword("UNDERLAY_ON");
            titleMat.SetColor("_UnderlayColor",   new Color(0f, 0f, 0f, 0.60f));
            titleMat.SetFloat("_UnderlayOffsetX",  1.0f);
            titleMat.SetFloat("_UnderlayOffsetY", -1.0f);
            titleMat.SetFloat("_UnderlaySoftness", 0.10f);
        }

        // ── Loading bar — bottom-centre, clear of background art ─────────────
        // Dark semi-transparent tray behind the bar gives contrast over any background.
        var barTray = PanelAnchored(root.transform, "BarTray", new Vector2(0.5f, 0.09f),
                                    new Vector2(720, 64), new Color(0f, 0f, 0f, 0.40f));

        // Inset bar background inside the tray
        var barBG = PanelAnchored(barTray, "BarBG", Vector2.one * 0.5f,
                                  new Vector2(680, 44), new Color(0f, 0f, 0f, 0.55f));

        // Fill — anchor-based: anchorMax.x grows 0→1 as progress advances.
        // Image.Type.Simple fills whatever rect it occupies; LoadingScreenUI drives
        // the width by setting rectTransform.anchorMax = new Vector2(t, 1f).
        var fillGO  = new GameObject("BarFill");
        fillGO.transform.SetParent(barBG, false);
        var fillImg             = fillGO.AddComponent<Image>();
        fillImg.color           = new Color(1f, 0.878f, 0f); // #FFE000 matches title
        fillImg.type            = Image.Type.Simple;
        var fillRT              = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin        = Vector2.zero;            // left edge anchored at 0
        fillRT.anchorMax        = new Vector2(0f, 1f);    // starts with zero width
        fillRT.pivot            = new Vector2(0f, 0.5f);
        fillRT.sizeDelta        = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;

        // Wire — statusText left unwired; null-checks in LoadingScreenUI keep it safe
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "progressFill", fillImg);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── Main Menu ─────────────────────────────────────────────────────────────
    static GameObject BuildMainMenu(Transform parent)
    {
        var root = PanelFull(parent, "MainMenuScreen", Color.clear).gameObject;
        var ui   = root.AddComponent<MainMenuUI>();

        AddScreenBackground(root.transform);

        // Title — #FFE000 yellow + black outline matches the LoadingScreen style
        var mainTitle = TMP(root.transform, "Cubby's Blocks", 120,
                            new Color(1f, 0.878f, 0f), // #FFE000
                            TextAlignmentOptions.Center, _fontBlocks);
        Anchored(mainTitle.rectTransform, new Vector2(0.5f, 0.70f), new Vector2(1000, 140));
        mainTitle.outlineColor = Color.black;
        mainTitle.outlineWidth = 0.2f;

        // Play — primary action
        var playBtn = Btn(root.transform, "PLAY", new Vector2(0.5f, 0.44f),
                          new Vector2(400, 96), AccentGreen, White, 26, _sprBtnGreen);
        UnityEventTools.AddPersistentListener(playBtn.onClick, ui.OnPlayClicked);

        // Settings — secondary
        var settingsBtn = Btn(root.transform, "Settings", new Vector2(0.87f, 0.93f),
                              new Vector2(260, 76), CardBg, White, 24, _sprBtnBlue);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, ui.OnSettingsClicked);

        return root;
    }

    // ── Settings Panel (overlay) ──────────────────────────────────────────────
    static GameObject BuildSettingsPanel(Transform parent)
    {
        var root = PanelFull(parent, "SettingsPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<SettingsUI>();

        // Card — light background, tall enough for title + 2 toggle rows + close button
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(640, 460), CardBg);

        TMP(card, "Settings", 34, CardText)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.86f), new Vector2(500, 52)));

        // Music row
        var musicLabel  = TMP(card, "Music", 26, CardText);
        Anchored(musicLabel.rectTransform, new Vector2(0.28f, 0.66f), new Vector2(260, 44));

        var musicToggle = CreateToggle(card, new Vector2(0.72f, 0.66f));

        // SFX row
        var sfxLabel    = TMP(card, "Sound Effects", 26, CardText);
        Anchored(sfxLabel.rectTransform, new Vector2(0.28f, 0.50f), new Vector2(260, 44));

        var sfxToggle   = CreateToggle(card, new Vector2(0.72f, 0.50f));

        // Close — destructive action
        var closeBtn = Btn(card, "X  Close", new Vector2(0.5f, 0.16f),
                           new Vector2(280, 80), AccentRed, White, 24, _sprBtnRed);
        UnityEventTools.AddPersistentListener(closeBtn.onClick, ui.OnCloseClicked);

        // Runtime toggle listeners are wired in SettingsUI.Awake — no persistent wiring needed here
        var soUI = new SerializedObject(ui);
        SetObjRef(soUI, "musicToggle",    musicToggle);
        SetObjRef(soUI, "sfxToggle",      sfxToggle);
        SetObjRef(soUI, "toggleOnSprite", _sprCheckGreen);
        SetObjRef(soUI, "toggleOffSprite", _sprCheckRed);
        soUI.ApplyModifiedProperties();

        return root;
    }

    // ── World Select ──────────────────────────────────────────────────────────
    static GameObject BuildWorldSelect(Transform parent)
    {
        var root = PanelFull(parent, "WorldSelectScreen", Color.clear).gameObject;
        var ui   = root.AddComponent<WorldSelectUI>();

        AddScreenBackground(root.transform);

        TMP(root.transform, "Select World", 34, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.88f), new Vector2(800, 52)));

        // World 1 card
        var w1Card = PanelAnchored(root.transform, "World1Card", new Vector2(0.5f, 0.63f),
                                   new Vector2(800, 160), AccentBlue);
        TMP(w1Card, "World 1  -  Basic Blocks", 26, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.62f), new Vector2(700, 44)));
        TMP(w1Card, "Levels 1 - 10", 22, new Color(0.85f, 0.95f, 1f))
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.28f), new Vector2(700, 36)));
        var w1Btn = w1Card.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(w1Btn.onClick, ui.OnWorld1Clicked);
        if (w1Card.gameObject.GetComponent<Image>() == null)
            w1Card.gameObject.AddComponent<Image>().color = AccentBlue;

        // World 2 card
        var w2Card = PanelAnchored(root.transform, "World2Card", new Vector2(0.5f, 0.40f),
                                   new Vector2(800, 160), new Color(0.30f, 0.30f, 0.42f));
        TMP(w2Card, "World 2  -  ???", 26, White)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.62f), new Vector2(700, 44)));
        var w2Sub = TMP(w2Card, "Complete World 1 to unlock", 22, new Color(0.7f, 0.7f, 0.85f));
        Anchored(w2Sub.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(700, 36));

        // Padlock label (text-based)
        var lockTxt = TMP(w2Card, "[locked]", 22, new Color(0.9f, 0.8f, 0.2f));
        Anchored(lockTxt.rectTransform, new Vector2(0.88f, 0.55f), new Vector2(120, 36));

        var w2Btn = w2Card.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(w2Btn.onClick, ui.OnWorld2Clicked);
        if (w2Card.gameObject.GetComponent<Image>() == null)
            w2Card.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.30f, 0.42f);

        // Back — secondary
        var backBtn = Btn(root.transform, "Back", new Vector2(0.10f, 0.92f),
                          new Vector2(240, 80), CardBg, White, 24, _sprBtnBlue);
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
        var root = PanelFull(parent, "LevelSelectScreen", Color.clear).gameObject;
        var ui   = root.AddComponent<LevelSelectUI>();

        AddScreenBackground(root.transform);

        var titleTmp = TMP(root.transform, "World 1", 34, White);
        Anchored(titleTmp.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(800, 52));

        // Back — secondary
        var backBtn = Btn(root.transform, "Back", new Vector2(0.10f, 0.92f),
                          new Vector2(240, 80), CardBg, White, 24, _sprBtnBlue);
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
            // Level select cells — blue sprite; fallback to dark card colour
            if (_sprBtnBlue != null)
            {
                cellImg.sprite = _sprBtnBlue;
                cellImg.type   = Image.Type.Simple;
                cellImg.color  = Color.white;
            }
            else
            {
                cellImg.color = new Color(0.18f, 0.18f, 0.27f);
            }
            cell.AddComponent<RectTransform>();

            var btn    = cell.AddComponent<Button>();
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor    = Color.white;
            btn.colors = colors;

            var btnUI  = cell.AddComponent<LevelButtonUI>();
            buttonRefs[i] = btnUI;

            // Number label
            var numTmp = TMP(cell.transform, lvlNum.ToString(), 34, White);
            Anchored(numTmp.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(200, 52));

            // Lock icon (hidden by default for first level)
            var lockTmp = TMP(cell.transform, "[L]", 26, AccentYellow);
            Anchored(lockTmp.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(80, 40));
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

        // Pause button — top-left corner (icon button, no sprite)
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

        // Card sized to fit title (52px) + gap + resume (88px) + gap + world select (88px) + margins
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(580, 380), CardBg);

        TMP(card, "Paused", 34, CardText)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.84f), new Vector2(440, 52)));

        // Resume — primary action
        var resume = Btn(card, "Resume", new Vector2(0.5f, 0.57f),
                         new Vector2(440, 88), AccentGreen, White, 24, _sprBtnGreen);
        UnityEventTools.AddPersistentListener(resume.onClick, ui.OnResumeClicked);

        // World Select — secondary
        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.24f),
                        new Vector2(440, 88), CardBg, White, 24, _sprBtnBlue);
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
        numTmp.fontStyle          = FontStyles.Bold;
        numTmp.enableWordWrapping = false;
        numTmp.outlineColor       = Color.black;
        numTmp.outlineWidth       = 0.25f;
        // Width 600 gives "GO!" comfortable room at font size 220; height 300 fits the digit
        Anchored(numTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600, 300));

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

        // Card sized to fit title + stars row + two buttons with clear gaps
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(680, 520), CardBg);

        TMP(card, "Level Complete!", 34, AccentGreen)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.89f), new Vector2(580, 52)));

        // Stars row — three Image components (all filled for MVP)
        var starsGO = new GameObject("StarsRow");
        starsGO.transform.SetParent(card, false);
        var starsRT = starsGO.AddComponent<RectTransform>();
        Anchored(starsRT, new Vector2(0.5f, 0.73f), new Vector2(280, 72));
        var hlg = starsGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 12f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childScaleWidth        = false;
        hlg.childScaleHeight       = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        for (int i = 0; i < 3; i++)
        {
            var starGO  = new GameObject($"Star_{i + 1}");
            starGO.transform.SetParent(starsGO.transform, false);
            var starImg = starGO.AddComponent<Image>();
            starImg.sprite         = _sprStarFull;
            starImg.color          = Color.white;
            starImg.preserveAspect = true;
            var starRT = starGO.GetComponent<RectTransform>();
            starRT.sizeDelta = new Vector2(72, 72);
        }

        // Next Level — primary action
        var nextBtn = Btn(card, "Next Level", new Vector2(0.5f, 0.50f),
                          new Vector2(460, 96), AccentGreen, White, 26, _sprBtnGreen);
        UnityEventTools.AddPersistentListener(nextBtn.onClick, ui.OnNextLevelClicked);

        // World Select — secondary
        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.22f),
                        new Vector2(440, 88), CardBg, White, 24, _sprBtnBlue);
        UnityEventTools.AddPersistentListener(wsBtn.onClick, ui.OnWorldSelectClicked);

        return root;
    }

    // ── Game Over Panel (overlay) ─────────────────────────────────────────────
    static GameObject BuildGameOverPanel(Transform parent)
    {
        var root = PanelFull(parent, "GameOverPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<GameOverUI>();

        // Card sized to fit title + two buttons with clear gaps
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(620, 400), CardBg);

        TMP(card, "Game Over", 34, AccentRed)
            .rectTransform.Do(rt => Anchored(rt, new Vector2(0.5f, 0.84f), new Vector2(520, 52)));

        // Retry — secondary (per design spec)
        var retryBtn = Btn(card, "Retry", new Vector2(0.5f, 0.56f),
                           new Vector2(440, 88), AccentOrange, White, 24, _sprBtnBlue);
        UnityEventTools.AddPersistentListener(retryBtn.onClick, ui.OnRetryClicked);

        // World Select — secondary
        var wsBtn = Btn(card, "World Select", new Vector2(0.5f, 0.24f),
                        new Vector2(440, 88), CardBg, White, 24, _sprBtnBlue);
        UnityEventTools.AddPersistentListener(wsBtn.onClick, ui.OnWorldSelectClicked);

        return root;
    }

    // ── Tutorial Panel (overlay) ──────────────────────────────────────────────
    static GameObject BuildTutorialPanel(Transform parent)
    {
        var root = PanelFull(parent, "TutorialPanel", DimOverlay).gameObject;
        var ui   = root.AddComponent<TutorialUI>();

        // Card sized to fit body text + button with clear gap
        var card = PanelAnchored(root.transform, "Card", Vector2.one * 0.5f,
                                 new Vector2(700, 380), CardBg);

        var msgTmp = TMP(card, "Tap yellow blocks to destroy them", 28, CardText);
        Anchored(msgTmp.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(600, 120));
        msgTmp.enableWordWrapping = true;

        // Got it! — primary action
        var confirmBtn = Btn(card, "Got it!", new Vector2(0.5f, 0.21f),
                             new Vector2(360, 96), AccentGreen, White, 26, _sprBtnGreen);
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

    // Full-screen background image placed as the first child of a screen root so
    // it renders behind every sibling.  A nested Canvas with sortingOrder -1 keeps
    // it in front of the camera but behind the main UIRoot canvas (sortingOrder 10).
    // raycastTarget = false ensures it never intercepts button clicks.
    static void AddScreenBackground(Transform screenRoot)
    {
        if (_sprBackground == null) return;

        var go = new GameObject("ScreenBackground");
        go.transform.SetParent(screenRoot, false);
        go.transform.SetSiblingIndex(0); // first child = behind all siblings

        var img            = go.AddComponent<Image>();
        img.sprite         = _sprBackground;
        img.color          = Color.white;
        img.raycastTarget  = false;
        img.preserveAspect = false; // stretch to fill

        var rt       = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Nested canvas gives an explicit sort order independent of hierarchy depth.
        // sortingOrder -1 < UIRoot sortingOrder 10, so this is always behind all UI.
        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder    = -1;
        // No GraphicRaycaster — background never needs to receive input events.
    }

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

    // TMP text label (fills parent by default).
    // font = null → KenneyBold SDF; pass _fontBlocks explicitly for the game title.
    static TextMeshProUGUI TMP(Transform parent, string text, float size,
        Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
        TMP_FontAsset font = null)
    {
        var go  = new GameObject("TMP_" + text.Replace(" ", "_").Substring(0, Mathf.Min(text.Length, 12)));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.font      = font ?? _fontBold;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    // Button with label; sprite overrides flat bg when provided
    static Button Btn(Transform parent, string label, Vector2 anchorPos,
        Vector2 size, Color bg, Color fg, float fontSize, Sprite sprite = null)
    {
        var go  = new GameObject("Btn_" + label.Replace(" ", "_").Trim());
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
        }
        else
        {
            img.color = bg;
        }
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(anchorPos.x, anchorPos.y);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var btn    = go.AddComponent<Button>();
        var colors = ColorBlock.defaultColorBlock;
        if (sprite != null)
        {
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor    = Color.white;
        }
        else
        {
            colors.normalColor      = bg;
            colors.highlightedColor = bg * 1.18f;
            colors.pressedColor     = bg * 0.80f;
            colors.selectedColor    = bg;
        }
        btn.colors = colors;

        var lbl = TMP(go.transform, label, fontSize, fg);
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin      = 20f;
        lbl.fontSizeMax      = fontSize;
        lbl.margin           = new Vector4(16f, 0f, 16f, 0f);
        return btn;
    }

    // Toggle using check_square sprites. Sprite swapping is handled by SettingsUI directly.
    static Toggle CreateToggle(Transform parent, Vector2 anchorPos)
    {
        var go  = new GameObject("Toggle");
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPos;
        rt.sizeDelta = new Vector2(80, 80);
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite         = _sprCheckGreen;
        img.color          = Color.white;
        img.preserveAspect = true;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = img;
        toggle.graphic       = null;
        toggle.transition    = Selectable.Transition.None;
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

    // Adds ButtonSFX to every Button in the UIRoot that doesn't already have one,
    // except LevelButtonUI buttons — those call AudioManager.PlayLevelSelect() directly.
    static void EnsureButtonSFX(GameObject uiRoot)
    {
        foreach (var btn in uiRoot.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<LevelButtonUI>() != null) continue;
            if (btn.GetComponent<ButtonSFX>()    == null)
                btn.gameObject.AddComponent<ButtonSFX>();
        }
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
