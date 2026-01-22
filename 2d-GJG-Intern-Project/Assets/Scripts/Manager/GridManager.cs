using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform gridParent;

    // Grid data structure
    private Block[,] grid;
    private GameObject[,] blockVisuals;

    // Unity Grid component
    private Grid unityGrid;

    // Optimized collections (reused to avoid GC)
    private Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>(100);
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private List<Block> currentGroup = new List<Block>(100);
    private List<List<Block>> allGroups = new List<List<Block>>(50);

    // Neighbor directions (up, down, left, right)
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0)    // Right
    };

    public int Rows => levelConfig.Rows;
    public int Columns => levelConfig.Columns;

    private void Awake()
    {
        InitializeGrid();
    }

    private void Start()
    {
        GenerateInitialGrid();
        UpdateAllGroupIcons();
    }

    /// <summary>
    /// Initialize grid data structures
    /// </summary>
    private void InitializeGrid()
    {
        grid = new Block[levelConfig.Rows, levelConfig.Columns];
        blockVisuals = new GameObject[levelConfig.Rows, levelConfig.Columns];

        // Create Unity Grid for positioning
        if (gridParent == null)
        {
            GameObject gridObj = new GameObject("Grid");
            gridParent = gridObj.transform;
        }

        unityGrid = gridParent.GetComponent<Grid>();
        if (unityGrid == null)
        {
            unityGrid = gridParent.gameObject.AddComponent<Grid>();
        }

        // Set cell size to total spacing (cell size + padding)
        unityGrid.cellSize = new Vector3(
            levelConfig.TotalCellSpacing,
            levelConfig.TotalCellSpacing,
            0
        );

        // Initialize level config grid data
        if (levelConfig.InitialGridData == null || levelConfig.InitialGridData.Length == 0)
        {
            levelConfig.InitializeGridData();
        }
    }

    /// <summary>
    /// Generate initial grid from LevelConfig data
    /// </summary>
    private void GenerateInitialGrid()
    {
        for (int y = 0; y < levelConfig.Rows; y++)
        {
            for (int x = 0; x < levelConfig.Columns; x++)
            {
                int colorID = levelConfig.GetColorIDAt(x, y);
                CreateBlock(x, y, colorID);
            }
        }

        // Center grid
        CenterGrid();
    }

    /// <summary>
    /// Create a block at specified position
    /// </summary>
    private void CreateBlock(int x, int y, int colorID)
    {
        // Create block data
        Block block = new Block(x, y, colorID);
        grid[y, x] = block;

        // Create visual
        GameObject blockObj = Instantiate(blockPrefab, gridParent);
        blockObj.name = $"Block_{x}_{y}";

        // Position block (center of cell)
        Vector3 worldPos = GetWorldPosition(x, y);
        blockObj.transform.position = worldPos;

        // Setup visual
        SpriteRenderer sr = blockObj.GetComponent<SpriteRenderer>();
        BlockColorData colorData = levelConfig.GetColorData(colorID);

        if (colorData != null && sr != null)
        {
            sr.sprite = colorData.DefaultIcon;
            sr.color = Color.white;
        }

        // Scale block to fit cell size (accounting for padding)
        ScaleBlockToFit(blockObj, sr);

        // Store reference
        block.VisualObject = blockObj;
        blockVisuals[y, x] = blockObj;
    }

    /// <summary>
    /// Scale block sprite to fit cell size with padding
    /// </summary>
    private void ScaleBlockToFit(GameObject blockObj, SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        // Get sprite's natural size
        Vector2 spriteSize = sr.sprite.bounds.size;

        // Calculate scale to fit cell size (not total spacing, just the cell)
        float scaleX = levelConfig.CellSize / spriteSize.x;
        float scaleY = levelConfig.CellSize / spriteSize.y;

        // Apply uniform scale (use smaller value to fit within cell)
        float uniformScale = Mathf.Min(scaleX, scaleY);
        blockObj.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    /// <summary>
    /// Get world position for grid coordinates (center of cell)
    /// </summary>
    private Vector3 GetWorldPosition(int x, int y)
    {
        // Position at grid cell using total spacing
        float totalSpacing = levelConfig.TotalCellSpacing;
        return new Vector3(
            x * totalSpacing + (totalSpacing / 2f),
            y * totalSpacing + (totalSpacing / 2f),
            0
        );
    }

    /// <summary>
    /// Center the grid in view
    /// </summary>
    private void CenterGrid()
    {
        float totalSpacing = levelConfig.TotalCellSpacing;
        Vector3 centerOffset = new Vector3(
            -(levelConfig.Columns * totalSpacing) / 2f,
            -(levelConfig.Rows * totalSpacing) / 2f,
            0
        );
        gridParent.position = centerOffset;
    }

    /// <summary>
    /// Find connected group using iterative flood fill (mobile optimized)
    /// </summary>
    public List<Block> FindConnectedGroup(int startX, int startY)
    {
        Block startBlock = GetBlock(startX, startY);
        if (startBlock == null) return null;

        // Clear reusable collections
        floodFillQueue.Clear();
        visitedCells.Clear();
        currentGroup.Clear();

        int targetColorID = startBlock.ColorID;
        Vector2Int startPos = new Vector2Int(startX, startY);

        floodFillQueue.Enqueue(startPos);
        visitedCells.Add(startPos);

        // Iterative flood fill (no recursion)
        while (floodFillQueue.Count > 0)
        {
            Vector2Int pos = floodFillQueue.Dequeue();
            Block block = GetBlock(pos.x, pos.y);

            if (block != null && block.ColorID == targetColorID)
            {
                currentGroup.Add(block);

                // Check all 4 neighbors
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int neighborPos = pos + dir;

                    if (visitedCells.Contains(neighborPos))
                        continue;

                    if (!IsValidPosition(neighborPos.x, neighborPos.y))
                        continue;

                    Block neighbor = GetBlock(neighborPos.x, neighborPos.y);
                    if (neighbor != null && neighbor.ColorID == targetColorID)
                    {
                        floodFillQueue.Enqueue(neighborPos);
                        visitedCells.Add(neighborPos);
                    }
                }
            }
        }

        // Return group only if it has 2+ blocks
        return currentGroup.Count >= 2 ? new List<Block>(currentGroup) : null;
    }

    /// <summary>
    /// Update all group icons based on group sizes and thresholds
    /// </summary>
    public void UpdateAllGroupIcons()
    {
        allGroups.Clear();
        visitedCells.Clear();

        // Find all groups
        for (int y = 0; y < levelConfig.Rows; y++)
        {
            for (int x = 0; x < levelConfig.Columns; x++)
            {
                Block block = GetBlock(x, y);
                if (block == null) continue;

                Vector2Int pos = new Vector2Int(x, y);
                if (visitedCells.Contains(pos)) continue;

                List<Block> group = FindConnectedGroup(x, y);
                if (group != null && group.Count >= 2)
                {
                    allGroups.Add(group);
                    foreach (Block b in group)
                    {
                        visitedCells.Add(new Vector2Int(b.x, b.y));
                    }
                }
            }
        }

        // Update icons based on group sizes
        foreach (List<Block> group in allGroups)
        {
            int groupSize = group.Count;
            BlockIconType iconType = levelConfig.GetIconType(groupSize);

            foreach (Block block in group)
            {
                block.GroupSize = groupSize;
                block.IconType = iconType;
                UpdateBlockVisual(block);
            }
        }
    }

    /// <summary>
    /// Update block visual based on current state
    /// </summary>
    private void UpdateBlockVisual(Block block)
    {
        if (block.VisualObject == null) return;

        SpriteRenderer sr = block.VisualObject.GetComponent<SpriteRenderer>();
        BlockColorData colorData = levelConfig.GetColorData(block.ColorID);

        if (colorData != null && sr != null)
        {
            sr.sprite = colorData.GetIconForType(block.IconType);
            sr.color = Color.white;
        }
    }

    /// <summary>
    /// Blast a group of blocks
    /// </summary>
    public void BlastGroup(List<Block> group)
    {
        if (group == null || group.Count < 2) return;

        // Destroy blocks
        foreach (Block block in group)
        {
            if (block.VisualObject != null)
            {
                Destroy(block.VisualObject);
            }
            grid[block.y, block.x] = null;
            blockVisuals[block.y, block.x] = null;
        }

        StartCoroutine(ApplyGravityAndRefill());
    }

    /// <summary>
    /// Apply gravity and refill empty spaces
    /// </summary>
    private IEnumerator ApplyGravityAndRefill()
    {
        yield return new WaitForSeconds(levelConfig.BlastDelay);

        // Apply gravity column by column
        for (int x = 0; x < levelConfig.Columns; x++)
        {
            int writeIndex = 0;

            // Compact column (move blocks down)
            for (int y = 0; y < levelConfig.Rows; y++)
            {
                if (grid[y, x] != null)
                {
                    if (writeIndex != y)
                    {
                        // Move block down
                        grid[writeIndex, x] = grid[y, x];
                        grid[y, x] = null;

                        blockVisuals[writeIndex, x] = blockVisuals[y, x];
                        blockVisuals[y, x] = null;

                        grid[writeIndex, x].y = writeIndex;

                        // Animate block to new position
                        StartCoroutine(AnimateBlockToPosition(grid[writeIndex, x], writeIndex));
                    }
                    writeIndex++;
                }
            }

            // Fill empty spaces from top
            for (int y = writeIndex; y < levelConfig.Rows; y++)
            {
                BlockColorData randomColor = levelConfig.GetRandomColorData();
                if (randomColor != null)
                {
                    CreateBlock(x, y, randomColor.ColorID);

                    // Animate block dropping from above
                    Block newBlock = grid[y, x];
                    newBlock.VisualObject.transform.position = GetWorldPosition(x, levelConfig.Rows + (y - writeIndex));
                    StartCoroutine(AnimateBlockToPosition(newBlock, y));
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Update group icons
        UpdateAllGroupIcons();

        // Check for deadlock
        if (IsDeadlock())
        {
            ShuffleGrid();
        }
    }

    /// <summary>
    /// Animate block to target position
    /// </summary>
    private IEnumerator AnimateBlockToPosition(Block block, int targetY)
    {
        if (block.VisualObject == null) yield break;

        Vector3 targetPos = GetWorldPosition(block.x, targetY);
        Transform blockTransform = block.VisualObject.transform;

        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = blockTransform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            blockTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        blockTransform.position = targetPos;
    }

    /// <summary>
    /// Check if grid is in deadlock (no valid moves)
    /// </summary>
    public bool IsDeadlock()
    {
        for (int y = 0; y < levelConfig.Rows; y++)
        {
            for (int x = 0; x < levelConfig.Columns; x++)
            {
                List<Block> group = FindConnectedGroup(x, y);
                if (group != null && group.Count >= 2)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Intelligent shuffle - guarantees at least one valid group
    /// </summary>
    public void ShuffleGrid()
    {
        Debug.Log("Deadlock detected! Shuffling grid...");

        int maxAttempts = 100;
        int attempt = 0;

        do
        {
            // Collect all color IDs
            List<int> colorIDs = new List<int>();
            for (int y = 0; y < levelConfig.Rows; y++)
            {
                for (int x = 0; x < levelConfig.Columns; x++)
                {
                    if (grid[y, x] != null)
                    {
                        colorIDs.Add(grid[y, x].ColorID);
                    }
                }
            }

            // Fisher-Yates shuffle
            for (int i = colorIDs.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = colorIDs[i];
                colorIDs[i] = colorIDs[j];
                colorIDs[j] = temp;
            }

            // Reassign colors
            int index = 0;
            for (int y = 0; y < levelConfig.Rows; y++)
            {
                for (int x = 0; x < levelConfig.Columns; x++)
                {
                    if (grid[y, x] != null)
                    {
                        grid[y, x].ColorID = colorIDs[index++];
                        UpdateBlockVisual(grid[y, x]);
                    }
                }
            }

            attempt++;

        } while (IsDeadlock() && attempt < maxAttempts);

        UpdateAllGroupIcons();
    }

    /// <summary>
    /// Get block at position
    /// </summary>
    public Block GetBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return grid[y, x];
    }

    /// <summary>
    /// Check if position is valid
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < levelConfig.Columns && y >= 0 && y < levelConfig.Rows;
    }

    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (grid == null || levelConfig == null) return;

        float totalSpacing = levelConfig.TotalCellSpacing;
        float cellSize = levelConfig.CellSize;

        for (int y = 0; y < levelConfig.Rows; y++)
        {
            for (int x = 0; x < levelConfig.Columns; x++)
            {
                Vector3 pos = GetWorldPosition(x, y) + gridParent.position;

                // Draw cell boundary (total spacing)
                Gizmos.color = Color.gray;
                Gizmos.DrawWireCube(pos, Vector3.one * totalSpacing * 0.95f);

                // Draw actual block size (excluding padding)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.95f);
            }
        }
    }
}