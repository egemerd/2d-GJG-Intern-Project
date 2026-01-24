using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Comprehensive Level Designer Window for creating and editing grid-based levels.
/// Provides visual tools for level designers to paint block colors on the grid.
/// </summary>
public class LevelDesignEditor : EditorWindow
{
    // References
    private LevelConfig targetConfig;
    private Board targetBoard;

    // Tool State
    private int selectedColorIndex = 0;
    private BrushMode currentBrush = BrushMode.Paint;
    private bool isEditing = false;

    // UI State
    private Vector2 scrollPosition;
    private Vector2 colorScrollPosition;
    private bool showGridPreview = true;
    private bool autoRefreshBoard = true;

    // Undo support
    private int[] undoBuffer;

    public enum BrushMode
    {
        Paint,      // Paint single cell
        Fill,       // Flood fill same color
        Bucket,     // Fill all of same color
        Random,     // Random from available colors
        Erase       // Set to first color (reset)
    }

    [MenuItem("Tools/Level Designer %#L", priority = 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelDesignEditor>("Level Designer");
        window.minSize = new Vector2(350, 500);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindReferences();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        isEditing = false;
    }

    private void FindReferences()
    {
        if (targetBoard == null)
            targetBoard = FindObjectOfType<Board>();

        if (targetBoard != null && targetConfig == null)
            targetConfig = targetBoard.Config;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);

        DrawHeader();
        EditorGUILayout.Space(5);

        DrawReferencesSection();
        EditorGUILayout.Space(5);

        if (targetConfig == null)
        {
            EditorGUILayout.HelpBox("Please assign a LevelConfig to begin designing.", MessageType.Warning);
            return;
        }

        DrawToolbar();
        EditorGUILayout.Space(5);

        DrawColorPalette();
        EditorGUILayout.Space(5);

        DrawBrushTools();
        EditorGUILayout.Space(5);

        DrawGridControls();
        EditorGUILayout.Space(5);

        DrawGridPreview();
        EditorGUILayout.Space(5);

        DrawActions();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        GUILayout.Label("🎮 Level Designer", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawReferencesSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        targetConfig = (LevelConfig)EditorGUILayout.ObjectField("Level Config", targetConfig, typeof(LevelConfig), false);
        if (EditorGUI.EndChangeCheck() && targetConfig != null)
        {
            targetConfig.InitializeGridData();
        }

        targetBoard = (Board)EditorGUILayout.ObjectField("Board", targetBoard, typeof(Board), true);

        if (GUILayout.Button("Find in Scene", GUILayout.Height(20)))
        {
            FindReferences();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // Edit Mode Toggle
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = isEditing ? Color.green : Color.gray;

        if (GUILayout.Button(isEditing ? "🎨 EDITING" : "✏️ Start Editing", GUILayout.Height(30)))
        {
            isEditing = !isEditing;
            if (isEditing)
            {
                SaveUndoState();
                Tools.current = Tool.None; // Disable Unity tools while editing
            }
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = oldColor;

        EditorGUILayout.EndHorizontal();

        if (isEditing)
        {
            EditorGUILayout.HelpBox("Click on grid cells in Scene View to paint colors.", MessageType.Info);
        }
    }

    private void DrawColorPalette()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎨 Color Palette", EditorStyles.boldLabel);

        if (targetConfig.AvailableColors == null || targetConfig.AvailableColors.Count == 0)
        {
            EditorGUILayout.HelpBox("No colors available. Add BlockColorData to LevelConfig.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        colorScrollPosition = EditorGUILayout.BeginScrollView(colorScrollPosition, GUILayout.Height(80));
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < targetConfig.AvailableColors.Count; i++)
        {
            BlockColorData colorData = targetConfig.AvailableColors[i];
            if (colorData == null) continue;

            bool isSelected = (i == selectedColorIndex);

            // Create button style
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Draw color button with sprite preview
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            GUIContent content = new GUIContent();
            if (colorData.DefaultIcon != null)
            {
                content.image = AssetPreview.GetAssetPreview(colorData.DefaultIcon);
            }
            content.tooltip = $"{colorData.ColorName} (ID: {colorData.ColorID})";

            if (GUILayout.Button(content, buttonStyle, GUILayout.Width(60), GUILayout.Height(60)))
            {
                selectedColorIndex = i;
                currentBrush = BrushMode.Paint;
            }

            GUI.backgroundColor = oldBg;

            // Draw selection indicator
            if (isSelected)
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(new Rect(lastRect.x, lastRect.y + lastRect.height - 3, lastRect.width, 3), Color.yellow);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        // Show selected color info
        if (selectedColorIndex < targetConfig.AvailableColors.Count)
        {
            BlockColorData selected = targetConfig.AvailableColors[selectedColorIndex];
            if (selected != null)
            {
                EditorGUILayout.LabelField($"Selected: {selected.ColorName} (ID: {selected.ColorID})", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBrushTools()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🖌️ Brush Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        DrawBrushButton("Paint", BrushMode.Paint, "Paint single cells");
        DrawBrushButton("Fill", BrushMode.Fill, "Flood fill connected same-color cells");
        DrawBrushButton("Bucket", BrushMode.Bucket, "Replace all cells of same color");
        DrawBrushButton("Random", BrushMode.Random, "Paint random colors");
        DrawBrushButton("Erase", BrushMode.Erase, "Reset to first color");

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawBrushButton(string label, BrushMode mode, string tooltip)
    {
        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = currentBrush == mode ? Color.cyan : Color.white;

        if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(25)))
        {
            currentBrush = mode;
        }

        GUI.backgroundColor = oldBg;
    }

    private void DrawGridControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚙️ Grid Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        targetConfig.rows = EditorGUILayout.IntSlider("Rows", targetConfig.rows, 2, 12);
        targetConfig.columns = EditorGUILayout.IntSlider("Columns", targetConfig.columns, 2, 12);

        if (EditorGUI.EndChangeCheck())
        {
            ResizeGridData();
            EditorUtility.SetDirty(targetConfig);
        }

        EditorGUILayout.Space(5);

        showGridPreview = EditorGUILayout.Toggle("Show Grid Preview", showGridPreview);
        autoRefreshBoard = EditorGUILayout.Toggle("Auto Refresh Board", autoRefreshBoard);

        EditorGUILayout.EndVertical();
    }

    private void DrawGridPreview()
    {
        if (!showGridPreview) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📋 Grid Preview", EditorStyles.boldLabel);

        if (targetConfig.InitialGridData == null || targetConfig.InitialGridData.Length == 0)
        {
            EditorGUILayout.HelpBox("Grid data not initialized.", MessageType.Info);
            if (GUILayout.Button("Initialize Grid"))
            {
                targetConfig.InitializeGridData();
                EditorUtility.SetDirty(targetConfig);
            }
            EditorGUILayout.EndVertical();
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        // Draw grid from top to bottom (visual order)
        for (int y = targetConfig.rows - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int x = 0; x < targetConfig.columns; x++)
            {
                int colorID = targetConfig.GetColorIDAt(x, y);
                BlockColorData colorData = targetConfig.GetColorData(colorID);

                Color cellColor = GetPreviewColor(colorID);
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = cellColor;

                string label = colorData != null ? colorData.ColorName.Substring(0, Mathf.Min(1, colorData.ColorName.Length)) : colorID.ToString();

                if (GUILayout.Button(label, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    // Click to paint in preview too
                    PaintCell(x, y);
                }

                GUI.backgroundColor = oldBg;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎬 Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🎲 Randomize", GUILayout.Height(30)))
        {
            SaveUndoState();
            targetConfig.RandomizeGrid();
            EditorUtility.SetDirty(targetConfig);
            RefreshBoard(randomize: true); // Explicit randomize
        }

        if (GUILayout.Button("🧹 Clear", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Grid", "Reset all cells to first color?", "Yes", "No"))
            {
                SaveUndoState();
                ClearGrid();
                RefreshBoard(randomize: false); // Don't randomize on clear
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("↩️ Undo", GUILayout.Height(25)))
        {
            RestoreUndoState();
        }

        if (GUILayout.Button("🔄 Refresh Board", GUILayout.Height(25)))
        {
            RefreshBoard(randomize: false); // Don't randomize on manual refresh
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("💾 Save Level Config", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(targetConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"✓ Level saved: {targetConfig.name}");
        }

        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isEditing || targetConfig == null || targetBoard == null)
            return;

        DrawSceneGridOverlay();
        HandleSceneInput();
    }

    private void DrawSceneGridOverlay()
    {
        Handles.BeginGUI();

        // Draw instruction panel
        GUILayout.BeginArea(new Rect(10, 10, 200, 80));
        EditorGUI.DrawRect(new Rect(0, 0, 200, 80), new Color(0, 0, 0, 0.7f));
        GUILayout.Label(" 🎨 Level Designer Active", EditorStyles.whiteLabel);
        GUILayout.Label($" Brush: {currentBrush}", EditorStyles.whiteLabel);

        if (selectedColorIndex < targetConfig.AvailableColors.Count)
        {
            var color = targetConfig.AvailableColors[selectedColorIndex];
            GUILayout.Label($" Color: {color?.ColorName ?? "None"}", EditorStyles.whiteLabel);
        }

        GUILayout.Label(" [LMB] Paint | [Esc] Exit", EditorStyles.whiteLabel);
        GUILayout.EndArea();

        Handles.EndGUI();

        // Draw cell highlights in Scene
        foreach (Transform child in targetBoard.transform)
        {
            GridCellInfo cell = child.GetComponent<GridCellInfo>();
            if (cell == null) continue;

            BlockColorData colorData = targetConfig.GetColorData(cell.ColorID);
            if (colorData != null && colorData.DefaultIcon != null)
            {
                // Draw sprite preview
                Handles.BeginGUI();
                Vector3 screenPos = HandleUtility.WorldToGUIPoint(cell.transform.position);
                float size = 40;
                Rect spriteRect = new Rect(screenPos.x - size / 2, screenPos.y - size / 2, size, size);

                if (colorData.DefaultIcon != null)
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(colorData.DefaultIcon);
                    if (tex != null)
                    {
                        GUI.DrawTexture(spriteRect, tex);
                    }
                }
                Handles.EndGUI();
            }
        }
    }

    private void HandleSceneInput()
    {
        Event e = Event.current;

        // Exit editing with Escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isEditing = false;
            Repaint();
            e.Use();
            return;
        }

        // Handle mouse input
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            GridCellInfo hitCell = FindCellAtRay(ray);

            if (hitCell != null)
            {
                SaveUndoState();
                PaintCell(hitCell.X, hitCell.Y);
                e.Use();
            }
        }

        // Handle drag painting
        if (e.type == EventType.MouseDrag && e.button == 0 && currentBrush == BrushMode.Paint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            GridCellInfo hitCell = FindCellAtRay(ray);

            if (hitCell != null)
            {
                PaintCell(hitCell.X, hitCell.Y);
                e.Use();
            }
        }

        // Force repaint for smooth updates
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        SceneView.RepaintAll();
    }

    private GridCellInfo FindCellAtRay(Ray ray)
    {
        float closestDist = float.MaxValue;
        GridCellInfo closestCell = null;

        foreach (Transform child in targetBoard.transform)
        {
            GridCellInfo cell = child.GetComponent<GridCellInfo>();
            if (cell == null) continue;

            // Simple distance check from ray to cell position
            Vector3 cellPos = cell.transform.position;
            float dist = Vector3.Cross(ray.direction, cellPos - ray.origin).magnitude;

            if (dist < closestDist && dist < targetConfig.CellSize * 0.6f)
            {
                closestDist = dist;
                closestCell = cell;
            }
        }

        return closestCell;
    }

    private void PaintCell(int x, int y)
    {
        if (targetConfig.AvailableColors.Count == 0) return;

        int newColorID;
        targetBoard.ResetPosition();
        switch (currentBrush)
        {
            case BrushMode.Paint:
                newColorID = targetConfig.AvailableColors[selectedColorIndex].ColorID;
                SetCellColor(x, y, newColorID);
                break;

            case BrushMode.Fill:
                FloodFill(x, y, targetConfig.AvailableColors[selectedColorIndex].ColorID);
                break;

            case BrushMode.Bucket:
                BucketFill(targetConfig.GetColorIDAt(x, y), targetConfig.AvailableColors[selectedColorIndex].ColorID);
                break;

            case BrushMode.Random:
                newColorID = targetConfig.GetRandomColorData().ColorID;
                SetCellColor(x, y, newColorID);
                break;

            case BrushMode.Erase:
                newColorID = targetConfig.AvailableColors[0].ColorID;
                SetCellColor(x, y, newColorID);
                break;
        }

        EditorUtility.SetDirty(targetConfig);

        if (autoRefreshBoard)
        {
            RefreshBoard();
        }

        Repaint();
    }

    private void SetCellColor(int x, int y, int colorID)
    {
        targetConfig.SetColorIDAt(x, y, colorID);
        UpdateBoardCell(x, y, colorID);
    }

    private void FloodFill(int startX, int startY, int newColorID)
    {
        int targetColorID = targetConfig.GetColorIDAt(startX, startY);
        if (targetColorID == newColorID) return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            if (visited.Contains(pos)) continue;
            visited.Add(pos);

            if (targetConfig.GetColorIDAt(pos.x, pos.y) != targetColorID)
                continue;

            SetCellColor(pos.x, pos.y, newColorID);

            // Add neighbors
            if (pos.x > 0) queue.Enqueue(new Vector2Int(pos.x - 1, pos.y));
            if (pos.x < targetConfig.columns - 1) queue.Enqueue(new Vector2Int(pos.x + 1, pos.y));
            if (pos.y > 0) queue.Enqueue(new Vector2Int(pos.x, pos.y - 1));
            if (pos.y < targetConfig.rows - 1) queue.Enqueue(new Vector2Int(pos.x, pos.y + 1));
        }
    }

    private void BucketFill(int targetColorID, int newColorID)
    {
        if (targetColorID == newColorID) return;

        for (int y = 0; y < targetConfig.rows; y++)
        {
            for (int x = 0; x < targetConfig.columns; x++)
            {
                if (targetConfig.GetColorIDAt(x, y) == targetColorID)
                {
                    SetCellColor(x, y, newColorID);
                }
            }
        }
    }

    private void UpdateBoardCell(int x, int y, int colorID)
    {
        if (targetBoard == null) return;

        foreach (Transform child in targetBoard.transform)
        {
            GridCellInfo cell = child.GetComponent<GridCellInfo>();
            if (cell != null && cell.X == x && cell.Y == y)
            {
                cell.ColorID = colorID;
                cell.GizmoColor = GetPreviewColor(colorID);
                EditorUtility.SetDirty(cell);
                break;
            }
        }
    }

    private void RefreshBoard()
    {
        RefreshBoard(randomize: false);
    }

    private void RefreshBoard(bool randomize)
    {
        if (targetBoard != null)
        {
            // Use the new overload - don't randomize when painting
            targetBoard.GenerateGridInEditor(randomize);
            SceneView.RepaintAll();
        }
    }

    private void ClearGrid()
    {
        if (targetConfig.AvailableColors.Count == 0) return;

        int firstColorID = targetConfig.AvailableColors[0].ColorID;

        for (int y = 0; y < targetConfig.rows; y++)
        {
            for (int x = 0; x < targetConfig.columns; x++)
            {
                targetConfig.SetColorIDAt(x, y, firstColorID);
            }
        }

        EditorUtility.SetDirty(targetConfig);
    }

    private void ResizeGridData()
    {
        int newSize = targetConfig.rows * targetConfig.columns;
        int[] newData = new int[newSize];

        // Preserve existing data where possible
        if (targetConfig.InitialGridData != null)
        {
            int oldRows = targetConfig.InitialGridData.Length / Mathf.Max(1, targetConfig.columns);

            for (int y = 0; y < targetConfig.rows; y++)
            {
                for (int x = 0; x < targetConfig.columns; x++)
                {
                    int newIndex = y * targetConfig.columns + x;
                    int oldIndex = y * targetConfig.columns + x;

                    if (oldIndex < targetConfig.InitialGridData.Length)
                    {
                        newData[newIndex] = targetConfig.InitialGridData[oldIndex];
                    }
                    else if (targetConfig.AvailableColors.Count > 0)
                    {
                        newData[newIndex] = targetConfig.GetRandomColorData().ColorID;
                    }
                }
            }
        }
        else if (targetConfig.AvailableColors.Count > 0)
        {
            for (int i = 0; i < newSize; i++)
            {
                newData[i] = targetConfig.GetRandomColorData().ColorID;
            }
        }

        targetConfig.InitialGridData = newData;
    }

    private void SaveUndoState()
    {
        if (targetConfig?.InitialGridData != null)
        {
            undoBuffer = (int[])targetConfig.InitialGridData.Clone();
        }
    }

    private void RestoreUndoState()
    {
        if (undoBuffer != null && targetConfig != null)
        {
            targetConfig.InitialGridData = (int[])undoBuffer.Clone();
            EditorUtility.SetDirty(targetConfig);
            RefreshBoard();
            Repaint();
        }
    }

    private Color GetPreviewColor(int colorID)
    {
        Color[] colors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f),   // Red
            new Color(0.3f, 0.6f, 1f),   // Blue
            new Color(0.3f, 1f, 0.3f),   // Green
            new Color(1f, 1f, 0.3f),     // Yellow
            new Color(1f, 0.5f, 0.9f),   // Pink
            new Color(0.6f, 0.3f, 1f),   // Purple
            new Color(1f, 0.6f, 0.3f),   // Orange
            new Color(0.3f, 1f, 1f),     // Cyan
        };
        return colors[colorID % colors.Length];
    }
}