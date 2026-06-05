using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Visual level editor.  Open via Blockavist ▸ Level Editor.
///
/// USAGE
///   1. Open the window (dockable next to the Scene view is ideal).
///   2. Drag a LevelData asset into the "Level Data" field.
///   3. Select a paint tool from the palette (or press keys 1–5 / E).
///   4. Left-click or drag in the Scene view to place / erase tiles.
///   5. Click Save Asset when done.
///
/// GRID
///   Yellow border = safe play area  x[-7..7]  y[-4..4]
///   Each cell = 1 world unit.  Camera orthographic size 5 shows all of it.
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    // ── Grid bounds (must match camera safe area) ─────────────────────────────
    private const int XMin = -7, XMax = 7;
    private const int YMin = -4, YMax = 4;

    // Spawn is placed this many units ABOVE the clicked cell so the player
    // falls cleanly onto the floor below it.  Click one row above a floor tile.
    private const float SpawnYOffset = 0.5f;

    // ── Paint tools ───────────────────────────────────────────────────────────

    private enum Tool { Indestructible, Destructible, Spike, Goal, Spawn, Erase }

    private static readonly Color[] ToolColor = {
        new Color(0.50f, 0.50f, 0.55f, 0.85f),  // Indestructible
        new Color(1.00f, 0.82f, 0.10f, 0.85f),  // Destructible
        new Color(0.90f, 0.15f, 0.15f, 0.85f),  // Spike
        new Color(0.10f, 0.85f, 0.25f, 0.85f),  // Goal
        new Color(0.25f, 0.50f, 1.00f, 0.85f),  // Spawn
        new Color(0.80f, 0.20f, 0.20f, 0.50f),  // Erase
    };

    private static readonly string[] ToolLabel = {
        "Floor / Wall", "Destructible", "Spike", "Goal", "Spawn", "Erase"
    };

    private static readonly KeyCode[] HotKey = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.E
    };

    // ── Window state ──────────────────────────────────────────────────────────

    private LevelData   level;
    private Tool        activeTool  = Tool.Indestructible;
    private Vector2Int  lastPainted = new Vector2Int(int.MinValue, int.MinValue);
    private Vector3     mouseWorld;
    private bool        mouseInBounds;

    // ── Open / lifecycle ──────────────────────────────────────────────────────

    [MenuItem("Blockavist/Level Editor")]
    public static void Open()
    {
        var win = GetWindow<LevelEditorWindow>("Level Editor");
        win.minSize = new Vector2(270, 460);
        win.Show();
        SceneView.lastActiveSceneView?.Focus();   // focus scene so keys work right away
    }

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    // ═════════════════════════════════════════════════════════════════════════
    // EDITOR WINDOW  (left panel)
    // ═════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        // ── Header ───────────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("▦  Blockavist Level Editor", EditorStyles.boldLabel);
        Separator();

        // ── Level asset selector ──────────────────────────────────────────────
        EditorGUI.BeginChangeCheck();
        level = (LevelData)EditorGUILayout.ObjectField("Level Data", level, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        if (level != null)
        {
            EditorGUI.indentLevel = 1;
            EditorGUI.BeginChangeCheck();
            level.levelName   = EditorGUILayout.TextField("Name",   level.levelName);
            level.levelNumber = EditorGUILayout.IntField( "Number", level.levelNumber);
            level.worldNumber = EditorGUILayout.IntField( "World",  level.worldNumber);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(level);
            EditorGUI.indentLevel = 0;
        }

        Separator();

        if (level == null)
        {
            EditorGUILayout.HelpBox(
                "Drag a LevelData asset here, or right-click in the Project window\n" +
                "and choose Create ▸ Blockavist ▸ Level Data.",
                MessageType.Info);
            return;
        }

        // ── Palette ───────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Tool  (keys: 1-5 / E)", EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        // Two rows of 3 buttons
        for (int row = 0; row < 2; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                if (idx >= ToolLabel.Length) break;

                bool active = (int)activeTool == idx;
                var  fill   = ToolColor[idx];

                GUI.backgroundColor = active
                    ? new Color(fill.r, fill.g, fill.b, 1.0f)
                    : new Color(fill.r * 0.5f, fill.g * 0.5f, fill.b * 0.5f, 0.9f);

                string key = HotKey[idx] == KeyCode.E ? "E" : (idx + 1).ToString();
                if (GUILayout.Button($"{key}: {ToolLabel[idx]}", GUILayout.Height(26)))
                {
                    activeTool = (Tool)idx;
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        Separator();

        // ── Stats ─────────────────────────────────────────────────────────────
        int  tileCount = level.tiles?.Length ?? 0;
        bool hasSpawn  = level.playerSpawnPosition != Vector2.zero;
        bool hasGoal   = System.Array.Exists(level.tiles ?? new TileEntry[0],
                             e => e.type == TileType.Goal);

        EditorGUILayout.LabelField("Level Stats", EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        EditorGUILayout.LabelField("Tiles",   tileCount.ToString());
        EditorGUILayout.LabelField("Spawn",   hasSpawn ? "✓ set" : "⚠ missing");
        EditorGUILayout.LabelField("Goal",    hasGoal  ? "✓ present" : "⚠ missing");
        EditorGUI.indentLevel = 0;

        if (!hasSpawn || !hasGoal)
            EditorGUILayout.HelpBox(
                (hasSpawn ? "" : "• Place a Spawn (tool 5).\n") +
                (hasGoal  ? "" : "• Place exactly one Goal tile (tool 4)."),
                MessageType.Warning);

        Separator();

        // ── Actions ───────────────────────────────────────────────────────────
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Asset", GUILayout.Height(24)))
        {
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelEditor] Saved '{level.levelName}'");
        }

        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        if (GUILayout.Button("Clear", GUILayout.Height(24)) &&
            EditorUtility.DisplayDialog("Clear Level",
                $"Remove all content from '{level.levelName}'?", "Clear", "Cancel"))
        {
            Undo.RecordObject(level, "Clear Level");
            level.tiles               = new TileEntry[0];
            level.playerSpawnPosition = Vector2.zero;
            EditorUtility.SetDirty(level);
            SceneView.RepaintAll();
            Repaint();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        Separator();

        // ── Help ──────────────────────────────────────────────────────────────
        EditorGUILayout.HelpBox(
            "Left-click to place, drag to paint a row.\n" +
            "Right-click or E to erase.\n" +
            "Spawn: click ONE ROW ABOVE the floor tile.\n" +
            "Yellow border = safe camera area.",
            MessageType.None);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SCENE GUI  (grid + tiles drawn on top of the Scene view)
    // ═════════════════════════════════════════════════════════════════════════

    private void OnSceneGUI(SceneView sv)
    {
        if (level == null) return;

        DrawGrid();
        DrawBoundary();
        DrawExistingTiles();
        DrawSpawn();
        DrawHoverCell(sv);
        HandleSceneInput(sv);
    }

    // ── Scene drawing ─────────────────────────────────────────────────────────

    private static void DrawGrid()
    {
        Handles.color = new Color(1f, 1f, 1f, 0.07f);
        for (int x = XMin; x <= XMax + 1; x++)
        {
            float wx = x - 0.5f;
            Handles.DrawLine(new Vector3(wx, YMin - 0.5f), new Vector3(wx, YMax + 0.5f));
        }
        for (int y = YMin; y <= YMax + 1; y++)
        {
            float wy = y - 0.5f;
            Handles.DrawLine(new Vector3(XMin - 0.5f, wy), new Vector3(XMax + 0.5f, wy));
        }
    }

    private static void DrawBoundary()
    {
        Handles.color = new Color(1f, 0.95f, 0.2f, 0.7f);
        float x0 = XMin - 0.5f, y0 = YMin - 0.5f;
        float w  = XMax - XMin + 1, h = YMax - YMin + 1;
        Handles.DrawLine(new Vector3(x0,   y0),   new Vector3(x0+w, y0));
        Handles.DrawLine(new Vector3(x0+w, y0),   new Vector3(x0+w, y0+h));
        Handles.DrawLine(new Vector3(x0+w, y0+h), new Vector3(x0,   y0+h));
        Handles.DrawLine(new Vector3(x0,   y0+h), new Vector3(x0,   y0));
    }

    private void DrawExistingTiles()
    {
        if (level.tiles == null) return;
        foreach (var e in level.tiles)
            FillCell(e.gridPosition.x, e.gridPosition.y,
                     TypeToColor(e.type), new Color(0, 0, 0, 0.5f));
    }

    private void DrawSpawn()
    {
        if (level.playerSpawnPosition == Vector2.zero) return;
        var p = level.playerSpawnPosition;
        Handles.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Handles.DrawWireDisc(new Vector3(p.x, p.y, 0), Vector3.forward, 0.38f);
        Handles.DrawLine(new Vector3(p.x, p.y - 0.38f, 0), new Vector3(p.x, p.y + 0.38f, 0));
        Handles.Label(new Vector3(p.x, p.y + 0.55f, 0), "SPAWN",
            new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.4f, 0.75f, 1f) } });
    }

    private void DrawHoverCell(SceneView sv)
    {
        if (!mouseInBounds) return;
        var cell = SnapToGrid(mouseWorld);
        if (!InBounds(cell)) return;

        Color fill = activeTool == Tool.Erase
            ? new Color(1f, 0.3f, 0.3f, 0.4f)
            : WithAlpha(ToolColor[(int)activeTool], 0.45f);

        FillCell(cell.x, cell.y, fill, new Color(1f, 1f, 1f, 0.55f));

        // Coord label in the cell
        Handles.Label(
            new Vector3(cell.x - 0.46f, cell.y - 0.40f, 0),
            $"{cell.x},{cell.y}",
            new GUIStyle { fontSize = 8, normal = { textColor = new Color(1, 1, 1, 0.65f) } });
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleSceneInput(SceneView sv)
    {
        Event e = Event.current;
        if (e == null) return;

        // Keyboard shortcuts (scene view must have focus)
        if (e.type == EventType.KeyDown)
        {
            for (int i = 0; i < HotKey.Length; i++)
            {
                if (e.keyCode == HotKey[i])
                {
                    activeTool = (Tool)i;
                    Repaint();
                    e.Use();
                    return;
                }
            }
        }

        // Track mouse world position for hover preview
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            mouseWorld    = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            mouseInBounds = InBounds(SnapToGrid(mouseWorld));
            sv.Repaint();
        }
        if (e.type == EventType.MouseLeaveWindow)
        {
            mouseInBounds = false;
            sv.Repaint();
        }

        // Right-click = quick erase (doesn't change the active palette tool)
        bool rightErase = (e.button == 1);

        int cid = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        switch (e.GetTypeForControl(cid))
        {
            case EventType.MouseDown when e.button == 0 || e.button == 1:
            {
                var cell = SnapToGrid(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin);
                if (!InBounds(cell)) break;
                GUIUtility.hotControl = cid;
                Paint(cell, rightErase ? Tool.Erase : activeTool);
                lastPainted = cell;
                e.Use();
                sv.Repaint();
                break;
            }
            case EventType.MouseDrag when GUIUtility.hotControl == cid:
            {
                var cell = SnapToGrid(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin);
                if (InBounds(cell) && cell != lastPainted)
                {
                    Paint(cell, rightErase ? Tool.Erase : activeTool);
                    lastPainted = cell;
                    sv.Repaint();
                }
                e.Use();
                break;
            }
            case EventType.MouseUp when GUIUtility.hotControl == cid:
                GUIUtility.hotControl = 0;
                lastPainted = new Vector2Int(int.MinValue, int.MinValue);
                e.Use();
                break;
        }
    }

    // ── Tile painting ─────────────────────────────────────────────────────────

    private void Paint(Vector2Int cell, Tool tool)
    {
        if (level == null) return;

        if (tool == Tool.Spawn)
        {
            Undo.RecordObject(level, "Set Spawn");
            level.playerSpawnPosition = new Vector2(cell.x, cell.y + SpawnYOffset);
            EditorUtility.SetDirty(level);
            Repaint();
            return;
        }

        bool erasing = tool == Tool.Erase;
        var  list    = new List<TileEntry>(level.tiles ?? new TileEntry[0]);
        int  idx     = list.FindIndex(t => t.gridPosition == cell);

        // Early no-ops
        if (erasing && idx < 0)  return;
        TileType newType = erasing ? default : ToolToTileType(tool);
        if (!erasing && idx >= 0 && list[idx].type == newType) return;

        Undo.RecordObject(level, erasing ? "Erase Tile" : $"Place {tool}");

        if (erasing)
        {
            list.RemoveAt(idx);
        }
        else
        {
            var entry = new TileEntry { type = newType, gridPosition = cell };
            if (idx >= 0) list[idx] = entry;
            else          list.Add(entry);
        }

        level.tiles = list.ToArray();
        EditorUtility.SetDirty(level);
        Repaint();
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static void FillCell(int x, int y, Color fill, Color outline)
    {
        Vector3[] v = {
            new Vector3(x - 0.5f, y - 0.5f, 0),
            new Vector3(x + 0.5f, y - 0.5f, 0),
            new Vector3(x + 0.5f, y + 0.5f, 0),
            new Vector3(x - 0.5f, y + 0.5f, 0),
        };
        Handles.DrawSolidRectangleWithOutline(v, fill, outline);
    }

    private static Color TypeToColor(TileType t) => t switch
    {
        TileType.Indestructible => ToolColor[0],
        TileType.Destructible   => ToolColor[1],
        TileType.Spike          => ToolColor[2],
        TileType.Goal           => ToolColor[3],
        _                       => Color.magenta
    };

    private static TileType ToolToTileType(Tool t) => t switch
    {
        Tool.Indestructible => TileType.Indestructible,
        Tool.Destructible   => TileType.Destructible,
        Tool.Spike          => TileType.Spike,
        Tool.Goal           => TileType.Goal,
        _                   => TileType.Indestructible
    };

    private static Vector2Int SnapToGrid(Vector3 w) =>
        new Vector2Int(Mathf.RoundToInt(w.x), Mathf.RoundToInt(w.y));

    private static bool InBounds(Vector2Int c) =>
        c.x >= XMin && c.x <= XMax && c.y >= YMin && c.y <= YMax;

    private static Color WithAlpha(Color c, float a) =>
        new Color(c.r, c.g, c.b, a);

    private static void Separator()
    {
        EditorGUILayout.Space(3);
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.35f));
        EditorGUILayout.Space(3);
    }
}
