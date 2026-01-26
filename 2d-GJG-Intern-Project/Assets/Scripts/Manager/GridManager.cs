using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private LevelConfig config;
    [SerializeField] private SortingOrderManager sortingOrderManager;

    [Header("Gameplay Settings")]
    [SerializeField] private int minGroupSize = 2;

    [Header("Shuffle Settings")]
    [Tooltip("How many different colors should have guaranteed groups after shuffle")]
    [SerializeField][Range(1, 4)] private int guaranteedColorCount = 1;

    private Block[,] blocks;

    // Global input lock
    private bool isProcessing = false;
    public bool IsProcessing => isProcessing;

    // Optimized collections
    private Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>(100);
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private List<Block> currentGroup = new List<Block>(100);

    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    public int Rows => board.Height;
    public int Columns => board.Width;

    private void Awake()
    {
        if (board == null) board = FindObjectOfType<Board>();
        if (blockPool == null) blockPool = GetComponent<BlockPool>();
        if (config == null && board != null) config = board.Config;
        if (sortingOrderManager == null) sortingOrderManager = GetComponent<SortingOrderManager>();

        ValidateSetup();
    }

    private void Start()
    {
        Debug.Log("[GridManager] === GAME START ===");
        InitializeGrid();
        SpawnAllBlocks();
        UpdateAllGroupIcons();
        Debug.Log("[GridManager] === READY FOR INPUT ===");
    }

    private void ValidateSetup()
    {
        if (board == null || blockPool == null || config == null)
        {
            Debug.LogError("[GridManager] Missing required references!");
            enabled = false;
        }
    }

    private void InitializeGrid()
    {
        blocks = new Block[Columns, Rows];
        Debug.Log($"[GridManager] Grid initialized: {Columns}x{Rows}");
    }

    private void SpawnAllBlocks()
    {
        Debug.Log("[GridManager] Spawning all blocks...");

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                GridCellInfo cell = board.GetCell(x, y);
                if (cell != null)
                {
                    SpawnBlock(x, y, cell.ColorID, setIdleImmediately: true);
                }
            }
        }

        Debug.Log($"[GridManager] Spawned {Columns * Rows} blocks");
    }

    private void SpawnBlock(int x, int y, int colorID, bool setIdleImmediately = false)
    {
        GameObject blockObj = blockPool.GetBlock();
        if (blockObj == null) return;

        Block block = new Block(x, y, colorID);
        blocks[x, y] = block;
        block.VisualObject = blockObj;

        Vector3 worldPos = board.GetCellWorldPosition(x, y);
        blockObj.transform.position = worldPos;
        blockObj.name = $"Block_{x}_{y}";

        SpriteRenderer sr = blockObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            BlockColorData colorData = config.GetColorData(colorID);
            if (colorData != null)
            {
                sr.sprite = colorData.DefaultIcon;
                sortingOrderManager?.ApplySortingOrder(sr, y);
                blockObj.transform.localScale = Vector3.one * config.CellSize;
            }
        }

        BlockMetadata metadata = blockObj.GetComponent<BlockMetadata>();
        if (metadata != null)
        {
            metadata.GridX = x;
            metadata.GridY = y;
            metadata.ColorID = colorID;
        }

        BlockAnimator animator = blockObj.GetComponent<BlockAnimator>();
        if (animator != null)
        {
            animator.SetOriginalScale(blockObj.transform.localScale);
            animator.BindToBlock(block);
        }

        if (setIdleImmediately)
        {
            block.SetState(BlockState.Idle);
        }
    }

    public bool CanProcessInput()
    {
        if (isProcessing)
        {
            Debug.Log("[GridManager] Input blocked - grid is processing");
            return false;
        }
        return true;
    }

    public List<Block> FindConnectedGroup(int startX, int startY)
    {
        Block startBlock = GetBlock(startX, startY);
        if (startBlock == null) return null;

        if (!startBlock.CanBeGrouped())
        {
            Debug.Log($"[GridManager] Block at ({startX},{startY}) cannot be grouped - State: {startBlock.State}");
            return null;
        }

        floodFillQueue.Clear();
        visitedCells.Clear();
        currentGroup.Clear();

        int targetColorID = startBlock.ColorID;
        floodFillQueue.Enqueue(new Vector2Int(startX, startY));
        visitedCells.Add(new Vector2Int(startX, startY));

        while (floodFillQueue.Count > 0)
        {
            Vector2Int pos = floodFillQueue.Dequeue();
            Block block = GetBlock(pos.x, pos.y);

            if (block != null && block.ColorID == targetColorID && block.CanBeGrouped())
            {
                currentGroup.Add(block);

                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int neighborPos = pos + dir;

                    if (visitedCells.Contains(neighborPos)) continue;
                    if (!IsValidPosition(neighborPos.x, neighborPos.y)) continue;

                    Block neighbor = GetBlock(neighborPos.x, neighborPos.y);
                    if (neighbor != null && neighbor.ColorID == targetColorID && neighbor.CanBeGrouped())
                    {
                        floodFillQueue.Enqueue(neighborPos);
                        visitedCells.Add(neighborPos);
                    }
                }
            }
        }

        return currentGroup.Count >= minGroupSize ? new List<Block>(currentGroup) : null;
    }

    public void BlastGroup(List<Block> group)
    {
        if (group == null || group.Count < minGroupSize) return;

        isProcessing = true;
        Debug.Log($"[GridManager] === BLAST START === ({group.Count} blocks) | Input LOCKED");

        foreach (Block block in group)
        {
            block.SetState(BlockState.Blasting);
        }

        StartCoroutine(BlastGroupRoutine(group));
    }

    private IEnumerator BlastGroupRoutine(List<Block> group)
    {
        float blastDuration = 0.2f;
        if (group.Count > 0 && group[0].VisualObject != null)
        {
            BlockAnimator animator = group[0].VisualObject.GetComponent<BlockAnimator>();
            if (animator != null)
            {
                blastDuration = animator.BlastDuration;
            }
        }

        yield return new WaitForSeconds(blastDuration);

        foreach (Block block in group)
        {
            if (block.VisualObject != null)
            {
                blockPool.ReturnBlock(block.VisualObject);
            }
            blocks[block.x, block.y] = null;
        }

        Debug.Log("[GridManager] Blocks destroyed");
        yield return new WaitForSeconds(config.blastDelay);

        Debug.Log("[GridManager] === GRAVITY START ===");
        yield return StartCoroutine(ApplyGravityAndRefill());

        Debug.Log("[GridManager] === UPDATING ICONS ===");
        UpdateAllGroupIcons();

        if (IsDeadlock())
        {
            Debug.LogWarning("[GridManager] === DEADLOCK DETECTED ===");
            yield return new WaitForSeconds(0.5f);
            ShuffleGridWithGuarantee();
        }

        isProcessing = false;
        Debug.Log("[GridManager] === READY FOR INPUT === | Input UNLOCKED");
    }

    private IEnumerator ApplyGravityAndRefill()
    {
        List<Block> fallingBlocks = new List<Block>();
        List<Block> spawningBlocks = new List<Block>();

        for (int x = 0; x < Columns; x++)
        {
            int writeY = 0;

            for (int y = 0; y < Rows; y++)
            {
                if (blocks[x, y] != null)
                {
                    if (writeY != y)
                    {
                        Block block = blocks[x, y];
                        blocks[x, writeY] = block;
                        blocks[x, y] = null;
                        block.y = writeY;

                        block.SetState(BlockState.Falling);
                        fallingBlocks.Add(block);

                        BlockMetadata metadata = block.VisualObject.GetComponent<BlockMetadata>();
                        if (metadata != null)
                        {
                            metadata.GridX = x;
                            metadata.GridY = writeY;
                        }

                        sortingOrderManager?.UpdateSortingOrder(block.VisualObject, writeY);
                    }
                    writeY++;
                }
            }

            int blocksToSpawn = Rows - writeY;
            for (int i = 0; i < blocksToSpawn; i++)
            {
                int y = writeY + i;

                BlockColorData randomColor = config.GetRandomColorData();
                if (randomColor != null)
                {
                    SpawnBlock(x, y, randomColor.ColorID, setIdleImmediately: false);

                    Block newBlock = blocks[x, y];
                    spawningBlocks.Add(newBlock);

                    Vector3 spawnPos = board.GetSpawnPosition(x, i + 1);
                    newBlock.VisualObject.transform.position = spawnPos;
                }
            }
        }

        Debug.Log($"[GridManager] Falling: {fallingBlocks.Count}, Spawning: {spawningBlocks.Count}");

        List<Coroutine> animations = new List<Coroutine>();

        foreach (Block block in fallingBlocks)
        {
            Vector3 targetPos = board.GetCellWorldPosition(block.x, block.y);
            animations.Add(StartCoroutine(AnimateBlock(block, targetPos)));
        }

        foreach (Block block in spawningBlocks)
        {
            Vector3 targetPos = board.GetCellWorldPosition(block.x, block.y);
            animations.Add(StartCoroutine(AnimateBlock(block, targetPos)));
        }

        foreach (var anim in animations)
        {
            yield return anim;
        }

        Debug.Log("[GridManager] === GRAVITY COMPLETE ===");
    }

    private IEnumerator AnimateBlock(Block block, Vector3 targetPos)
    {
        if (block?.VisualObject == null) yield break;

        Transform t = block.VisualObject.transform;
        Vector3 startPos = t.position;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (block.VisualObject == null) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        if (block.VisualObject != null)
        {
            t.position = targetPos;
        }

        block.SetState(BlockState.Idle);
    }

    public void UpdateAllGroupIcons()
    {
        visitedCells.Clear();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = GetBlock(x, y);
                if (block != null && block.CanBeGrouped())
                {
                    block.GroupSize = 1;
                    block.IconType = BlockIconType.Default;
                }
            }
        }

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (visitedCells.Contains(pos)) continue;

                Block block = GetBlock(x, y);
                if (block == null || !block.CanBeGrouped()) continue;

                List<Block> group = FindConnectedGroup(x, y);

                if (group != null && group.Count >= minGroupSize)
                {
                    BlockIconType iconType = config.GetIconType(group.Count);

                    foreach (Block groupBlock in group)
                    {
                        groupBlock.GroupSize = group.Count;
                        groupBlock.IconType = iconType;
                        visitedCells.Add(new Vector2Int(groupBlock.x, groupBlock.y));
                    }
                }
                else
                {
                    visitedCells.Add(pos);
                }
            }
        }

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = GetBlock(x, y);
                if (block?.VisualObject == null) continue;

                SpriteRenderer sr = block.VisualObject.GetComponent<SpriteRenderer>();
                BlockColorData colorData = config.GetColorData(block.ColorID);

                if (colorData != null && sr != null)
                {
                    sr.sprite = colorData.GetIconForType(block.IconType);
                }
            }
        }
    }

    public bool IsDeadlock()
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = GetBlock(x, y);
                if (block != null && block.CanBeGrouped())
                {
                    List<Block> group = FindConnectedGroup(x, y);
                    if (group != null && group.Count >= minGroupSize)
                        return false;
                }
            }
        }
        return true;
    }

    #region Shuffle With Guarantee

    /// <summary>
    /// Intelligent shuffle that guarantees at least N color groups.
    /// </summary>
    public void ShuffleGridWithGuarantee()
    {
        Debug.Log("[Shuffle] ========================================");
        Debug.Log($"[Shuffle] STARTING SHUFFLE WITH {guaranteedColorCount} GUARANTEED COLORS");
        Debug.Log("[Shuffle] ========================================");

        // Step 1: Count all colors on the grid
        Dictionary<int, int> colorCounts = CountAllColors();
        PrintColorCounts(colorCounts);

        // Step 2: Collect all colors as a pool
        List<int> colorPool = CollectAllColorsAsList();
        Debug.Log($"[Shuffle] Total blocks in pool: {colorPool.Count}");

        // Step 3: Get unique colors that have at least minGroupSize blocks
        List<int> availableColors = colorCounts
            .Where(kvp => kvp.Value >= minGroupSize)
            .Select(kvp => kvp.Key)
            .ToList();

        Debug.Log($"[Shuffle] Colors with enough blocks for groups: {availableColors.Count}");

        // Step 4: Randomly select colors to guarantee (up to guaranteedColorCount)
        int colorsToGuarantee = Mathf.Min(guaranteedColorCount, availableColors.Count);
        List<int> selectedColors = SelectRandomColors(availableColors, colorsToGuarantee);

        Debug.Log($"[Shuffle] Selected {selectedColors.Count} colors to guarantee:");
        foreach (int colorID in selectedColors)
        {
            Debug.Log($"[Shuffle]   - {GetColorName(colorID)} (ID: {colorID})");
        }

        // Step 5: For each guaranteed color, reserve adjacent positions
        HashSet<Vector2Int> allReservedPositions = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, int> reservedColorAssignments = new Dictionary<Vector2Int, int>();

        foreach (int colorID in selectedColors)
        {
            int colorCount = colorCounts[colorID];

            // Random group size between minGroupSize and available count (max 5 for variety)
            int maxGroupSize = Mathf.Min(colorCount, 5);
            int groupSize = Random.Range(minGroupSize, maxGroupSize + 1);

            Debug.Log($"[Shuffle] Finding {groupSize} adjacent positions for {GetColorName(colorID)}...");

            // Find random adjacent positions
            List<Vector2Int> adjacentPositions = FindRandomAdjacentPositions(groupSize, allReservedPositions);

            if (adjacentPositions.Count >= minGroupSize)
            {
                Debug.Log($"[Shuffle] ✓ Reserved {adjacentPositions.Count} positions for {GetColorName(colorID)}:");

                foreach (var pos in adjacentPositions)
                {
                    allReservedPositions.Add(pos);
                    reservedColorAssignments[pos] = colorID;
                    Debug.Log($"[Shuffle]     ({pos.x}, {pos.y}) → {GetColorName(colorID)}");

                    // Remove one instance of this color from the pool
                    colorPool.Remove(colorID);
                }
            }
            else
            {
                Debug.LogWarning($"[Shuffle] ✗ Could not find enough adjacent positions for {GetColorName(colorID)}");
            }
        }

        // Step 6: Shuffle remaining colors
        ShuffleList(colorPool);
        Debug.Log($"[Shuffle] Shuffled remaining {colorPool.Count} colors");

        // Step 7: Apply colors to all positions
        Debug.Log("[Shuffle] === APPLYING COLORS ===");
        int poolIndex = 0;

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block == null) continue;

                Vector2Int pos = new Vector2Int(x, y);
                int oldColorID = block.ColorID;
                int newColorID;
                string assignType;

                if (reservedColorAssignments.TryGetValue(pos, out int reservedColor))
                {
                    newColorID = reservedColor;
                    assignType = "GUARANTEED";
                }
                else
                {
                    newColorID = colorPool[poolIndex++];
                    assignType = "RANDOM";
                }

                block.ColorID = newColorID;

                Debug.Log($"[Shuffle] ({x},{y}) [{assignType}]: {GetColorName(oldColorID)} → {GetColorName(newColorID)}");

                UpdateBlockVisual(block);
            }
        }

        // Step 8: Update visuals
        UpdateAllGroupIcons();

        // Step 9: Verify and print result
        PrintGridState();

        if (IsDeadlock())
        {
            Debug.LogError("[Shuffle] ✗ SHUFFLE FAILED - Still deadlocked! Applying emergency fix...");
            ApplyEmergencyFix();
        }
        else
        {
            Debug.Log("[Shuffle] ✓ SHUFFLE SUCCESS - Valid groups exist!");
        }

        Debug.Log("[Shuffle] ========================================");
        Debug.Log("[Shuffle] SHUFFLE COMPLETE");
        Debug.Log("[Shuffle] ========================================");
    }

    /// <summary>
    /// Count how many of each color exists on the grid.
    /// </summary>
    private Dictionary<int, int> CountAllColors()
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block != null)
                {
                    if (!counts.ContainsKey(block.ColorID))
                        counts[block.ColorID] = 0;
                    counts[block.ColorID]++;
                }
            }
        }

        return counts;
    }

    /// <summary>
    /// Collect all colors as a flat list (for shuffling).
    /// </summary>
    private List<int> CollectAllColorsAsList()
    {
        List<int> colors = new List<int>();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block != null)
                {
                    colors.Add(block.ColorID);
                }
            }
        }

        return colors;
    }

    /// <summary>
    /// Randomly select N colors from available colors.
    /// </summary>
    private List<int> SelectRandomColors(List<int> availableColors, int count)
    {
        List<int> shuffled = new List<int>(availableColors);
        ShuffleList(shuffled);
        return shuffled.Take(count).ToList();
    }

    /// <summary>
    /// Find random adjacent positions using BFS from a random starting point.
    /// </summary>
    private List<Vector2Int> FindRandomAdjacentPositions(int count, HashSet<Vector2Int> excludePositions)
    {
        // Try multiple times with different starting positions
        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Pick random starting position
            int startX = Random.Range(0, Columns);
            int startY = Random.Range(0, Rows);
            Vector2Int start = new Vector2Int(startX, startY);

            // Skip if already reserved or no block there
            if (excludePositions.Contains(start) || blocks[startX, startY] == null)
                continue;

            // BFS to find adjacent positions
            List<Vector2Int> result = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && result.Count < count)
            {
                Vector2Int current = queue.Dequeue();

                // Skip if reserved
                if (excludePositions.Contains(current))
                    continue;

                // Skip if no block
                if (blocks[current.x, current.y] == null)
                    continue;

                result.Add(current);

                // Shuffle directions for randomness
                List<Vector2Int> shuffledDirs = new List<Vector2Int>(Directions);
                ShuffleList(shuffledDirs);

                foreach (Vector2Int dir in shuffledDirs)
                {
                    Vector2Int neighbor = current + dir;

                    if (!IsValidPosition(neighbor.x, neighbor.y))
                        continue;

                    if (visited.Contains(neighbor))
                        continue;

                    if (excludePositions.Contains(neighbor))
                        continue;

                    if (blocks[neighbor.x, neighbor.y] == null)
                        continue;

                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }

            if (result.Count >= minGroupSize)
            {
                Debug.Log($"[Shuffle] Found adjacent group starting at ({startX},{startY}) on attempt {attempt + 1}");
                return result;
            }
        }

        Debug.LogWarning("[Shuffle] Could not find enough adjacent positions after 20 attempts");
        return new List<Vector2Int>();
    }

    /// <summary>
    /// Fisher-Yates shuffle for any list.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Emergency fix if shuffle still results in deadlock.
    /// </summary>
    private void ApplyEmergencyFix()
    {
        Debug.LogWarning("[Shuffle] === EMERGENCY FIX ===");

        // Find any two adjacent blocks and make them the same color
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns - 1; x++)
            {
                Block block1 = blocks[x, y];
                Block block2 = blocks[x + 1, y];

                if (block1 != null && block2 != null)
                {
                    block2.ColorID = block1.ColorID;
                    UpdateBlockVisual(block2);

                    Debug.Log($"[Shuffle] Emergency: Set ({x + 1},{y}) to {GetColorName(block1.ColorID)}");

                    UpdateAllGroupIcons();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Update a single block's visual.
    /// </summary>
    private void UpdateBlockVisual(Block block)
    {
        if (block?.VisualObject == null) return;

        SpriteRenderer sr = block.VisualObject.GetComponent<SpriteRenderer>();
        BlockColorData colorData = config.GetColorData(block.ColorID);

        if (colorData != null && sr != null)
        {
            sr.sprite = colorData.DefaultIcon;
        }

        BlockMetadata metadata = block.VisualObject.GetComponent<BlockMetadata>();
        if (metadata != null)
        {
            metadata.ColorID = block.ColorID;
        }
    }

    /// <summary>
    /// Get color name for logging.
    /// </summary>
    private string GetColorName(int colorID)
    {
        BlockColorData colorData = config.GetColorData(colorID);
        return colorData != null ? colorData.name : $"Color_{colorID}";
    }

    /// <summary>
    /// Print color counts for debugging.
    /// </summary>
    private void PrintColorCounts(Dictionary<int, int> counts)
    {
        Debug.Log("[Shuffle] === COLOR COUNTS ===");
        foreach (var kvp in counts)
        {
            Debug.Log($"[Shuffle]   {GetColorName(kvp.Key)}: {kvp.Value} blocks");
        }
    }

    /// <summary>
    /// Print current grid state.
    /// </summary>
    private void PrintGridState()
    {
        Debug.Log("[Shuffle] === FINAL GRID STATE ===");

        for (int y = Rows - 1; y >= 0; y--)
        {
            string row = $"Row {y}: ";
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block != null)
                {
                    string colorName = GetColorName(block.ColorID);
                    string initial = colorName.Length > 0 ? colorName.Substring(0, 1).ToUpper() : "?";
                    row += $"[{initial}]";
                }
                else
                {
                    row += "[X]";
                }
            }
            Debug.Log(row);
        }
    }

    #endregion

    public Block GetBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return blocks[x, y];
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }

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