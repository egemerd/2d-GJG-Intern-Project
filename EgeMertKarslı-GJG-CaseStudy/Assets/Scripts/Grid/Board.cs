using UnityEngine;


public class Board : MonoBehaviour
{
    [SerializeField] private LevelConfig config;
    [SerializeField] private GameObject cellPrefab;

    private GridCellInfo[,] cells;
    private int width;
    private int height;

    public LevelConfig Config => config;
    public int Width => width;
    public int Height => height;

    private void Awake()
    {
        if (config != null)
        {
            width = config.columns;
            height = config.rows;
            IndexCells();
        }
    }

   
    private void IndexCells()
    {
        cells = new GridCellInfo[width, height];

        foreach (Transform child in transform)
        {
            GridCellInfo cellInfo = child.GetComponent<GridCellInfo>();
            if (cellInfo != null)
            {
                if (cellInfo.X >= 0 && cellInfo.X < width && cellInfo.Y >= 0 && cellInfo.Y < height)
                {
                    cells[cellInfo.X, cellInfo.Y] = cellInfo;
                }
            }
        }

        Debug.Log($"✓ Board indexed: {width}x{height} cells");
    }

    
    public GridCellInfo GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return cells[x, y];
    }

    
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        GridCellInfo cell = GetCell(x, y);
        return cell != null ? cell.GetCenterPosition() : Vector3.zero;
    }

    
    public Vector3 GetSpawnPosition(int x, int dropDistance)
    {
        // Get the topmost cell position
        GridCellInfo topCell = GetCell(x, height - 1);
        if (topCell == null) return Vector3.zero;

        // Calculate position above the grid
        float spacing = config.TotalCellSpacing;
        Vector3 topPos = topCell.GetCenterPosition();

        // Add extra height based on drop distance
        return topPos + Vector3.up * (spacing * (dropDistance + 1));
    }

    #region Editor Methods

    
    public void GenerateGridInEditor()
    {
        GenerateGridInEditor(randomize: true);
    }

    
    /// <param name="randomize">If true, randomizes grid data. If false, preserves existing data.</param>
    public void GenerateGridInEditor(bool randomize)
    {
        if (config == null)
        {
            Debug.LogError("Board: LevelConfig is not assigned!");
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError("Board: Cell Prefab is not assigned!");
            return;
        }

 
        if (randomize)
        {
            RegenerateGridData();
        }
        else
        {
            EnsureGridDataExists();
        }

        
        ClearGrid();

        
        width = config.columns;
        height = config.rows;
        float spacing = config.TotalCellSpacing;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, y, spacing);
            }
        }

        CenterGrid();
        Debug.Log($"✓ Grid generated: {width}x{height} cells with {config.ColorCount} colors (randomize: {randomize})");
    }

    
    private void EnsureGridDataExists()
    {
        if (config.InitialGridData == null || config.InitialGridData.Length != config.rows * config.columns)
        {
            config.InitializeGridData();
        }

        ValidateExistingColors();
    }

    
    private void ValidateExistingColors()
    {
        if (config.AvailableColors == null || config.AvailableColors.Count == 0)
        {
            Debug.LogWarning("[Board] No colors available in LevelConfig!");
            return;
        }

        for (int i = 0; i < config.InitialGridData.Length; i++)
        {
            int colorID = config.InitialGridData[i];
            if (!config.IsColorAvailable(colorID))
            {
                // Only replace invalid colors
                var randomColor = config.GetRandomColorData();
                if (randomColor != null)
                {
                    config.InitialGridData[i] = randomColor.ColorID;
                }
            }
        }
    }

    
    private void RegenerateGridData()
    {
        if (config.AvailableColors == null || config.AvailableColors.Count == 0)
        {
            Debug.LogWarning("[Board] No colors available in LevelConfig!");
            return;
        }

        config.InitializeGridData();
        config.RandomizeGrid();

        Debug.Log($"[Board] Regenerated grid data with {config.ColorCount} colors");
    }

    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    private void CreateCell(int x, int y, float spacing)
    {
        Vector3 position = new Vector3(x * spacing, y * spacing, 0);

        GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
        cell.name = $"Cell_{x}_{y}";

        GridCellInfo cellInfo = cell.GetComponent<GridCellInfo>();
        if (cellInfo == null)
        {
            cellInfo = cell.AddComponent<GridCellInfo>();
        }

        cellInfo.X = x;
        cellInfo.Y = y;

        // Get ColorID and validate it
        int colorID = GetValidColorID(x, y);
        cellInfo.ColorID = colorID;
        cellInfo.CellSize = config.CellSize;

        // Only use gizmos for visualization in editor
        cellInfo.GizmoColor = GetGizmoColorForID(colorID);


        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            DestroyImmediate(sr);
        }
    }

    
    private int GetValidColorID(int x, int y)
    {
        int colorID = config.GetColorIDAt(x, y);

        // Validate: Check if this ColorID exists in AvailableColors
        if (!config.IsColorAvailable(colorID))
        {
            Debug.LogWarning($"[Board] ColorID {colorID} at ({x},{y}) not available. Using random color.");

            var randomColor = config.GetRandomColorData();
            if (randomColor != null)
            {
                colorID = randomColor.ColorID;
                config.SetColorIDAt(x, y, colorID);
            }
            else
            {
                Debug.LogError("[Board] No valid colors available!");
                colorID = 0; 
            }
        }

        return colorID;
    }

    private void CenterGrid()
    {
        float spacing = config.TotalCellSpacing;
        float offsetX = (width - 1) * spacing * 0.5f;
        float offsetY = (height - 1) * spacing * 0.5f;
        transform.position = new Vector3(-offsetX, -offsetY, 0);
    }

    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
        Debug.Log("✓ Grid cleared");
    }

    private Color GetGizmoColorForID(int colorID)
    {
        if (config != null)
        {
            BlockColorData colorData = config.GetColorData(colorID);
            if (colorData != null)
            {
                return colorData.GetGizmoColor(0.6f);
            }
        }


        Color[] fallbackColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f, 0.6f),
            new Color(0.3f, 0.6f, 1f, 0.6f),
            new Color(0.3f, 1f, 0.3f, 0.6f),
            new Color(1f, 1f, 0.3f, 0.6f),
            new Color(1f, 0.5f, 0.9f, 0.6f),
            new Color(0.6f, 0.3f, 1f, 0.6f),
            new Color(1f, 0.6f, 0.3f, 0.6f),
            new Color(0.3f, 1f, 1f, 0.6f),
        };
        return fallbackColors[colorID % fallbackColors.Length];
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (config == null) return;

        float spacing = config.TotalCellSpacing;
        float gridWidth = (config.columns - 1) * spacing;
        float gridHeight = (config.rows - 1) * spacing;

        Vector3 center = transform.position + new Vector3(gridWidth * 0.5f, gridHeight * 0.5f, 0);
        Vector3 size = new Vector3(gridWidth + config.CellSize, gridHeight + config.CellSize, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}