#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class GridVisualizerTool : EditorWindow
{
    [MenuItem("Tools/Blast Game/Grid Visualizer")]
    public static void ShowWindow()
    {
        GetWindow<GridVisualizerTool>("Grid Visualizer");
    }

    private LevelConfig levelConfig;
    private Sprite gridSprite;
    private float cellSize = 1f;
    private float cellPadding = 0.1f;
    private bool showColorInfo = true;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🎮 GRID VISUALIZER TOOL", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Configuration Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

        levelConfig = (LevelConfig)EditorGUILayout.ObjectField("Level Config", levelConfig, typeof(LevelConfig), false);
        gridSprite = (Sprite)EditorGUILayout.ObjectField("Grid Sprite", gridSprite, typeof(Sprite), false);
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, 0.5f, 3f);
        cellPadding = EditorGUILayout.Slider("Cell Padding", cellPadding, 0f, 0.5f);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // Level Info Section
        if (levelConfig != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 Level Information", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Grid Size: {levelConfig.rows} rows × {levelConfig.columns} columns");
            EditorGUILayout.LabelField($"Total Cells: {levelConfig.rows * levelConfig.columns}");
            EditorGUILayout.LabelField($"Available Colors: {levelConfig.AvailableColors.Count}");
            EditorGUILayout.LabelField($"Thresholds: A={levelConfig.thresholdA}, B={levelConfig.thresholdB}, C={levelConfig.thresholdC}");

            EditorGUILayout.Space(5);

            // Color Distribution
            showColorInfo = EditorGUILayout.Foldout(showColorInfo, "Color Distribution");
            if (showColorInfo && levelConfig.InitialGridData != null)
            {
                DrawColorDistribution();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // Action Buttons
        GUI.enabled = levelConfig != null && gridSprite != null;

        if (GUILayout.Button("🎯 Generate Grid Visualization", GUILayout.Height(45)))
        {
            GenerateGrid();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 Refresh Colors", GUILayout.Height(30)))
        {
            RefreshGridColors();
        }

        if (GUILayout.Button("🧹 Clear Grid", GUILayout.Height(30)))
        {
            ClearGrid();
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (levelConfig == null || gridSprite == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign both Level Config and Grid Sprite to generate the grid.", MessageType.Warning);
        }
    }

    private void DrawColorDistribution()
    {
        if (levelConfig.AvailableColors.Count == 0) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Count each color
        int[] colorCounts = new int[levelConfig.AvailableColors.Count];

        for (int i = 0; i < levelConfig.InitialGridData.Length; i++)
        {
            int colorID = levelConfig.InitialGridData[i];
            for (int c = 0; c < levelConfig.AvailableColors.Count; c++)
            {
                if (levelConfig.AvailableColors[c] != null &&
                    levelConfig.AvailableColors[c].ColorID == colorID)
                {
                    colorCounts[c]++;
                    break;
                }
            }
        }

        // Display distribution
        for (int i = 0; i < levelConfig.AvailableColors.Count; i++)
        {
            if (levelConfig.AvailableColors[i] == null) continue;

            BlockColorData colorData = levelConfig.AvailableColors[i];
            float percentage = (colorCounts[i] / (float)levelConfig.InitialGridData.Length) * 100f;

            EditorGUILayout.BeginHorizontal();

            // Color preview
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.backgroundColor = prevColor;

            EditorGUILayout.LabelField($"{colorData.ColorName}: {colorCounts[i]} cells ({percentage:F1}%)", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void GenerateGrid()
    {
        ClearGrid();

        // Initialize grid data if needed
        if (levelConfig.InitialGridData == null || levelConfig.InitialGridData.Length == 0)
        {
            levelConfig.InitializeGridData();
        }

        // Validate grid data
        if (levelConfig.AvailableColors.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No colors available in Level Config! Add BlockColorData assets first.", "OK");
            return;
        }

        // Create root grid object
        GameObject gridRoot = new GameObject($"LevelGrid_{levelConfig.name}");
        Grid unityGrid = gridRoot.AddComponent<Grid>();
        unityGrid.cellSize = new Vector3(cellSize + cellPadding, cellSize + cellPadding, 0);
        unityGrid.cellLayout = GridLayout.CellLayout.Rectangle;

        // Create cells container
        GameObject cellsParent = new GameObject("Cells");
        cellsParent.transform.SetParent(gridRoot.transform);
        cellsParent.transform.localPosition = Vector3.zero;

        int cellCount = 0;

        // Create grid cells (bottom to top for proper blast game layout)
        for (int y = 0; y < levelConfig.rows; y++)
        {
            for (int x = 0; x < levelConfig.columns; x++)
            {
                GameObject cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.SetParent(cellsParent.transform);

                // Calculate position with padding
                float xPos = x * (cellSize + cellPadding);
                float yPos = y * (cellSize + cellPadding);
                cell.transform.position = new Vector3(xPos, yPos, 0);

                // Add sprite renderer
                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = gridSprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 0;

                // Get color from level config based on grid data
                int colorID = levelConfig.GetColorIDAt(x, y);
                BlockColorData colorData = levelConfig.GetColorData(colorID);

                if (colorData != null)
                {
                    // Apply color from BlockColorData
                    sr.color = Color.white;

                    // Optional: Add icon overlay if default icon exists
                    if (colorData.DefaultIcon != null)
                    {
                        GameObject icon = new GameObject("Icon");
                        icon.transform.SetParent(cell.transform);
                        icon.transform.localPosition = Vector3.zero;

                        SpriteRenderer iconSr = icon.AddComponent<SpriteRenderer>();
                        iconSr.sprite = colorData.DefaultIcon;
                        iconSr.sortingOrder = 1;

                        // Scale icon to fit cell
                        Vector2 iconSize = iconSr.sprite.bounds.size;
                        icon.transform.localScale = new Vector3(
                            (cellSize * 0.7f) / iconSize.x,
                            (cellSize * 0.7f) / iconSize.y,
                            1f
                        );
                    }
                }
                else
                {
                    // Fallback color if no color data found
                    sr.color = Color.gray;
                    Debug.LogWarning($"No color data found for ColorID {colorID} at position ({x}, {y})");
                }

                // Scale sprite to fit cell size
                if (sr.sprite != null)
                {
                    Vector2 spriteSize = sr.sprite.bounds.size;
                    cell.transform.localScale = new Vector3(
                        cellSize / spriteSize.x,
                        cellSize / spriteSize.y,
                        1f
                    );
                }

                // Add collider for future interaction (optional)
                BoxCollider2D collider = cell.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(cellSize, cellSize);

                cellCount++;
            }
        }

        // Center the grid
        Vector3 centerOffset = new Vector3(
            -(levelConfig.columns * (cellSize + cellPadding)) / 2f + (cellSize / 2f),
            -(levelConfig.rows * (cellSize + cellPadding)) / 2f + (cellSize / 2f),
            0
        );
        gridRoot.transform.position = centerOffset;

        // Register undo and select
        Undo.RegisterCreatedObjectUndo(gridRoot, "Generate Grid");
        Selection.activeGameObject = gridRoot;

        Debug.Log($"✅ Grid generated: {cellCount} cells ({levelConfig.rows}×{levelConfig.columns}) with {levelConfig.AvailableColors.Count} colors");

        EditorUtility.DisplayDialog(
            "Grid Generated",
            $"Successfully created grid visualization!\n\n" +
            $"• Size: {levelConfig.rows} × {levelConfig.columns}\n" +
            $"• Total Cells: {cellCount}\n" +
            $"• Colors: {levelConfig.AvailableColors.Count}\n" +
            $"• Object: {gridRoot.name}",
            "OK"
        );
    }

    private void RefreshGridColors()
    {
        GameObject gridRoot = GameObject.Find($"LevelGrid_{levelConfig.name}");
        if (gridRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "No grid found! Generate a grid first.", "OK");
            return;
        }

        Transform cellsParent = gridRoot.transform.Find("Cells");
        if (cellsParent == null) return;

        int updatedCount = 0;

        for (int y = 0; y < levelConfig.rows; y++)
        {
            for (int x = 0; x < levelConfig.columns; x++)
            {
                Transform cell = cellsParent.Find($"Cell_{x}_{y}");
                if (cell == null) continue;

                SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                int colorID = levelConfig.GetColorIDAt(x, y);
                BlockColorData colorData = levelConfig.GetColorData(colorID);

                if (colorData != null)
                {
                    sr.color = Color.white;
                    updatedCount++;
                }
            }
        }

        Debug.Log($"✅ Refreshed {updatedCount} cells with updated colors");
    }

    private void ClearGrid()
    {
        // Try to find by config name
        if (levelConfig != null)
        {
            GameObject existing = GameObject.Find($"LevelGrid_{levelConfig.name}");
            if (existing != null)
            {
                DestroyImmediate(existing);
                return;
            }
        }

        // Fallback: find any LevelGrid
        GameObject[] allGrids = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGrids)
        {
            if (obj.name.StartsWith("LevelGrid"))
            {
                DestroyImmediate(obj);
            }
        }
    }
}
#endif