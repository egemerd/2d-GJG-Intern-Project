using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages runtime block spawning, gameplay mechanics, and collapse/blast system.
/// Uses Board for grid structure and BlockPool for optimization.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private LevelConfig config;

    [Header("Gameplay Settings")]
    [SerializeField] private int minGroupSize = 2;

    // Grid data (runtime blocks)
    private Block[,] blocks;

    // Optimized collections (reused to avoid GC)
    private Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>(100);
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private List<Block> currentGroup = new List<Block>(100);

    // Neighbor directions
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0)    // Right
    };

    public int Rows => board.Height;
    public int Columns => board.Width;
    public LevelConfig Config => config;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (board == null)
            board = FindObjectOfType<Board>();

        if (blockPool == null)
            blockPool = GetComponent<BlockPool>();

        if (config == null && board != null)
            config = board.Config;

        ValidateSetup();
    }

    private void Start()
    {
        InitializeGrid();
        SpawnAllBlocks();
        UpdateAllGroupIcons();
    }

    private void ValidateSetup()
    {
        if (board == null)
        {
            Debug.LogError("GridManager: Board not found! Please create a Board in the scene.");
            enabled = false;
            return;
        }

        if (blockPool == null)
        {
            Debug.LogError("GridManager: BlockPool not found! Please add BlockPool component.");
            enabled = false;
            return;
        }

        if (config == null)
        {
            Debug.LogError("GridManager: LevelConfig not assigned!");
            enabled = false;
            return;
        }

        Debug.Log($"✓ GridManager initialized: {Columns}x{Rows} grid with {config.AvailableColors.Count} colors");
    }

    private void InitializeGrid()
    {
        blocks = new Block[Columns, Rows];
    }

    /// <summary>
    /// Spawn all blocks at game start
    /// </summary>
    private void SpawnAllBlocks()
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                GridCellInfo cell = board.GetCell(x, y);
                if (cell != null)
                {
                    SpawnBlock(x, y, cell.ColorID);
                }
            }
        }

        Debug.Log($"✓ Spawned {Columns * Rows} blocks");
    }

    /// <summary>
    /// Spawn a single block at grid position using object pool
    /// </summary>
    private void SpawnBlock(int x, int y, int colorID)
    {
        // Get block from pool
        GameObject blockObj = blockPool.GetBlock();
        if (blockObj == null)
        {
            Debug.LogError($"Failed to get block from pool at ({x}, {y})");
            return;
        }

        // Create Block data
        Block block = new Block(x, y, colorID);
        blocks[x, y] = block;
        block.VisualObject = blockObj;

        // Position block at cell center
        Vector3 worldPos = board.GetCellWorldPosition(x, y);
        blockObj.transform.position = worldPos;
        blockObj.name = $"Block_{x}_{y}";

        // Setup visual
        SpriteRenderer sr = blockObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            BlockColorData colorData = config.GetColorData(colorID);
            if (colorData != null)
            {
                sr.sprite = colorData.DefaultIcon;
                sr.sortingOrder = 10; // Above grid cells
                ScaleBlockToFit(blockObj, sr);
            }
        }
    }

    private void ScaleBlockToFit(GameObject blockObj, SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        float targetSize = config.CellSize;
        blockObj.transform.localScale = Vector3.one * targetSize;
    }

    /// <summary>
    /// Find connected group using iterative flood fill
    /// </summary>
    public List<Block> FindConnectedGroup(int startX, int startY)
    {
        Block startBlock = GetBlock(startX, startY);
        if (startBlock == null) return null;

        floodFillQueue.Clear();
        visitedCells.Clear();
        currentGroup.Clear();

        int targetColorID = startBlock.ColorID;
        Vector2Int startPos = new Vector2Int(startX, startY);

        floodFillQueue.Enqueue(startPos);
        visitedCells.Add(startPos);

        while (floodFillQueue.Count > 0)
        {
            Vector2Int pos = floodFillQueue.Dequeue();
            Block block = GetBlock(pos.x, pos.y);

            if (block != null && block.ColorID == targetColorID)
            {
                currentGroup.Add(block);

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

        return currentGroup.Count >= minGroupSize ? new List<Block>(currentGroup) : null;
    }

    /// <summary>
    /// Update all group icons based on sizes
    /// </summary>
    public void UpdateAllGroupIcons()
    {
        visitedCells.Clear();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (visitedCells.Contains(pos)) continue;

                List<Block> group = FindConnectedGroup(x, y);
                if (group != null && group.Count >= minGroupSize)
                {
                    int groupSize = group.Count;
                    BlockIconType iconType = config.GetIconType(groupSize);

                    foreach (Block block in group)
                    {
                        block.GroupSize = groupSize;
                        block.IconType = iconType;
                        UpdateBlockVisual(block);
                        visitedCells.Add(new Vector2Int(block.x, block.y));
                    }
                }
            }
        }
    }

    private void UpdateBlockVisual(Block block)
    {
        if (block?.VisualObject == null) return;

        SpriteRenderer sr = block.VisualObject.GetComponent<SpriteRenderer>();
        BlockColorData colorData = config.GetColorData(block.ColorID);

        if (colorData != null && sr != null)
        {
            sr.sprite = colorData.GetIconForType(block.IconType);
        }
    }

    /// <summary>
    /// Blast a group of blocks
    /// </summary>
    public void BlastGroup(List<Block> group)
    {
        if (group == null || group.Count < minGroupSize)
        {
            Debug.Log($"Group too small: {group?.Count ?? 0} (min: {minGroupSize})");
            return;
        }

        StartCoroutine(BlastGroupRoutine(group));
    }

    private IEnumerator BlastGroupRoutine(List<Block> group)
    {
        // Destroy blocks
        foreach (Block block in group)
        {
            if (block.VisualObject != null)
            {
                blockPool.ReturnBlock(block.VisualObject);
            }
            blocks[block.x, block.y] = null;
        }

        yield return new WaitForSeconds(config.blastDelay);

        // Apply gravity and refill
        yield return StartCoroutine(ApplyGravityAndRefill());

        // Update icons
        UpdateAllGroupIcons();

        // Check for deadlock
        if (IsDeadlock())
        {
            Debug.LogWarning("Deadlock detected!");
            yield return new WaitForSeconds(0.5f);
            ShuffleGrid();
        }
    }

    /// <summary>
    /// Apply gravity and refill empty spaces
    /// </summary>
    private IEnumerator ApplyGravityAndRefill()
    {
        for (int x = 0; x < Columns; x++)
        {
            int writeY = 0;

            // Compact column (gravity)
            for (int y = 0; y < Rows; y++)
            {
                if (blocks[x, y] != null)
                {
                    if (writeY != y)
                    {
                        blocks[x, writeY] = blocks[x, y];
                        blocks[x, y] = null;
                        blocks[x, writeY].y = writeY;

                        Vector3 targetPos = board.GetCellWorldPosition(x, writeY);
                        StartCoroutine(AnimateBlockToPosition(blocks[x, writeY], targetPos));
                    }
                    writeY++;
                }
            }

            // Fill from top - FIXED VERSION
            int blocksToSpawn = Rows - writeY;
            for (int i = 0; i < blocksToSpawn; i++)
            {
                int y = writeY + i;

                BlockColorData randomColor = config.GetRandomColorData();
                if (randomColor != null)
                {
                    SpawnBlock(x, y, randomColor.ColorID);

                    // FIXED: Calculate spawn position ABOVE the grid
                    Vector3 spawnPos = board.GetSpawnPosition(x, i + 1);
                    Vector3 targetPos = board.GetCellWorldPosition(x, y);

                    blocks[x, y].VisualObject.transform.position = spawnPos;
                    StartCoroutine(AnimateBlockToPosition(blocks[x, y], targetPos));
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator AnimateBlockToPosition(Block block, Vector3 targetPos)
    {
        if (block?.VisualObject == null) yield break;

        Transform blockTransform = block.VisualObject.transform;
        Vector3 startPos = blockTransform.position;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            blockTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        blockTransform.position = targetPos;
    }

    /// <summary>
    /// Check for deadlock
    /// </summary>
    public bool IsDeadlock()
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                List<Block> group = FindConnectedGroup(x, y);
                if (group != null && group.Count >= minGroupSize)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Intelligent shuffle
    /// </summary>
    public void ShuffleGrid()
    {
        Debug.Log("Shuffling grid...");

        int maxAttempts = 100;
        int attempt = 0;

        do
        {
            List<int> colorIDs = new List<int>();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (blocks[x, y] != null)
                        colorIDs.Add(blocks[x, y].ColorID);
                }
            }

            // Fisher-Yates shuffle
            for (int i = colorIDs.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (colorIDs[i], colorIDs[j]) = (colorIDs[j], colorIDs[i]);
            }

            int index = 0;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (blocks[x, y] != null)
                    {
                        blocks[x, y].ColorID = colorIDs[index++];
                        UpdateBlockVisual(blocks[x, y]);
                    }
                }
            }

            attempt++;
        } while (IsDeadlock() && attempt < maxAttempts);

        UpdateAllGroupIcons();
        Debug.Log($"Shuffle complete after {attempt} attempts");
    }

    public Block GetBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return blocks[x, y];
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }

    /// <summary>
    /// Convert world position to grid coordinates
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float minDist = float.MaxValue;
        Vector2Int closest = new Vector2Int(-1, -1);

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Vector3 cellPos = board.GetCellWorldPosition(x, y);
                float dist = Vector3.Distance(worldPos, cellPos);

                if (dist < minDist)
                {
                    minDist = dist;
                    closest = new Vector2Int(x, y);
                }
            }
        }

        return closest;
    }
}