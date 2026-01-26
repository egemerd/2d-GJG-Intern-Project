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
            yield return StartCoroutine(ShuffleGridWithGuaranteeRoutine());
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
            animations.Add(StartCoroutine(AnimateBlockFall(block, targetPos)));
        }

        foreach (Block block in spawningBlocks)
        {
            Vector3 targetPos = board.GetCellWorldPosition(block.x, block.y);
            animations.Add(StartCoroutine(AnimateBlockFall(block, targetPos)));
        }

        foreach (var anim in animations)
        {
            yield return anim;
        }

        Debug.Log("[GridManager] === GRAVITY COMPLETE ===");
    }

    private IEnumerator AnimateBlockFall(Block block, Vector3 targetPos)
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

    #region Shuffle With Real Movement Animation

    /// <summary>
    /// Intelligent shuffle that guarantees at least N color groups with real movement animation.
    /// Blocks physically move to their new positions.
    /// </summary>
    public void ShuffleGridWithGuarantee()
    {
        StartCoroutine(ShuffleGridWithGuaranteeRoutine());
    }

    private IEnumerator ShuffleGridWithGuaranteeRoutine()
    {
        Debug.Log("[Shuffle] ========================================");
        Debug.Log($"[Shuffle] STARTING REAL SHUFFLE WITH {guaranteedColorCount} GUARANTEED COLORS");
        Debug.Log("[Shuffle] ========================================");

        // Step 1: Collect all blocks and their current positions
        List<Block> allBlocks = new List<Block>();
        List<Vector2Int> allPositions = new List<Vector2Int>();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block != null)
                {
                    allBlocks.Add(block);
                    allPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        Debug.Log($"[Shuffle] Total blocks to shuffle: {allBlocks.Count}");

        // Step 2: Count colors for guaranteed group selection
        Dictionary<int, List<Block>> blocksByColor = new Dictionary<int, List<Block>>();
        foreach (Block block in allBlocks)
        {
            if (!blocksByColor.ContainsKey(block.ColorID))
                blocksByColor[block.ColorID] = new List<Block>();
            blocksByColor[block.ColorID].Add(block);
        }

        PrintColorCounts(blocksByColor);

        // Step 3: Select colors to guarantee
        List<int> availableColors = blocksByColor
            .Where(kvp => kvp.Value.Count >= minGroupSize)
            .Select(kvp => kvp.Key)
            .ToList();

        int colorsToGuarantee = Mathf.Min(guaranteedColorCount, availableColors.Count);
        List<int> selectedColors = SelectRandomColors(availableColors, colorsToGuarantee);

        Debug.Log($"[Shuffle] Selected {selectedColors.Count} colors to guarantee:");
        foreach (int colorID in selectedColors)
        {
            Debug.Log($"[Shuffle]   - {GetColorName(colorID)} (ID: {colorID})");
        }

        // Step 4: Create target position assignments
        // First, reserve adjacent positions for guaranteed colors
        Dictionary<Block, Vector2Int> blockTargetPositions = new Dictionary<Block, Vector2Int>();
        HashSet<Vector2Int> reservedPositions = new HashSet<Vector2Int>();

        foreach (int colorID in selectedColors)
        {
            List<Block> colorBlocks = blocksByColor[colorID];
            int groupSize = Random.Range(minGroupSize, Mathf.Min(colorBlocks.Count, 5) + 1);

            Debug.Log($"[Shuffle] Finding {groupSize} adjacent positions for {GetColorName(colorID)}...");

            List<Vector2Int> adjacentPositions = FindRandomAdjacentPositions(groupSize, reservedPositions);

            if (adjacentPositions.Count >= minGroupSize)
            {
                Debug.Log($"[Shuffle] ✓ Reserved {adjacentPositions.Count} positions for {GetColorName(colorID)}");

                // Assign blocks of this color to these positions
                for (int i = 0; i < adjacentPositions.Count && i < colorBlocks.Count; i++)
                {
                    Block block = colorBlocks[i];
                    Vector2Int targetPos = adjacentPositions[i];

                    blockTargetPositions[block] = targetPos;
                    reservedPositions.Add(targetPos);

                    Debug.Log($"[Shuffle]   Block at ({block.x},{block.y}) → ({targetPos.x},{targetPos.y})");
                }
            }
            else
            {
                Debug.LogWarning($"[Shuffle] ✗ Could not find enough adjacent positions for {GetColorName(colorID)}");
            }
        }

        // Step 5: Assign remaining blocks to remaining positions
        List<Vector2Int> remainingPositions = allPositions
            .Where(pos => !reservedPositions.Contains(pos))
            .ToList();

        List<Block> remainingBlocks = allBlocks
            .Where(block => !blockTargetPositions.ContainsKey(block))
            .ToList();

        // Shuffle remaining positions
        ShuffleList(remainingPositions);

        Debug.Log($"[Shuffle] Assigning {remainingBlocks.Count} remaining blocks to {remainingPositions.Count} positions");

        for (int i = 0; i < remainingBlocks.Count && i < remainingPositions.Count; i++)
        {
            blockTargetPositions[remainingBlocks[i]] = remainingPositions[i];
        }

        // Step 6: Set all blocks to shuffling state
        foreach (Block block in allBlocks)
        {
            block.SetState(BlockState.Shuffling);
        }

        // Step 7: Animate all blocks moving to their new positions
        Debug.Log("[Shuffle] === STARTING SHUFFLE ANIMATIONS ===");

        float shuffleDuration = 0.5f;
        BlockAnimator sampleAnimator = allBlocks[0].VisualObject?.GetComponent<BlockAnimator>();
        if (sampleAnimator != null)
        {
            shuffleDuration = sampleAnimator.ShuffleDuration;
        }

        List<Coroutine> animations = new List<Coroutine>();

        foreach (var kvp in blockTargetPositions)
        {
            Block block = kvp.Key;
            Vector2Int targetGridPos = kvp.Value;
            Vector3 targetWorldPos = board.GetCellWorldPosition(targetGridPos.x, targetGridPos.y);

            // Set shuffle target for animator
            BlockAnimator animator = block.VisualObject?.GetComponent<BlockAnimator>();
            if (animator != null)
            {
                animator.SetShuffleTarget(targetWorldPos);
            }

            animations.Add(StartCoroutine(AnimateBlockShuffle(block, targetWorldPos, targetGridPos, shuffleDuration)));
        }

        // Step 8: Wait for all animations to complete
        foreach (var anim in animations)
        {
            yield return anim;
        }

        // Step 9: Update the grid array with new positions
        Debug.Log("[Shuffle] === UPDATING GRID DATA ===");

        // Clear grid
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                blocks[x, y] = null;
            }
        }

        // Place blocks in new positions
        foreach (var kvp in blockTargetPositions)
        {
            Block block = kvp.Key;
            Vector2Int newPos = kvp.Value;

            block.x = newPos.x;
            block.y = newPos.y;
            blocks[newPos.x, newPos.y] = block;

            // Update metadata
            BlockMetadata metadata = block.VisualObject?.GetComponent<BlockMetadata>();
            if (metadata != null)
            {
                metadata.GridX = newPos.x;
                metadata.GridY = newPos.y;
            }

            // Update sorting order
            sortingOrderManager?.UpdateSortingOrder(block.VisualObject, newPos.y);

            // Update block name
            if (block.VisualObject != null)
            {
                block.VisualObject.name = $"Block_{newPos.x}_{newPos.y}";
            }

            // Set to idle
            block.SetState(BlockState.Idle);
        }

        // Step 10: Update visuals
        UpdateAllGroupIcons();

        // Step 11: Verify and print result
        PrintGridState();

        if (IsDeadlock())
        {
            Debug.LogError("[Shuffle] ✗ SHUFFLE FAILED - Still deadlocked! Applying emergency fix...");
            ApplyEmergencyFix();
            UpdateAllGroupIcons();
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
    /// Animate a block moving to its new position during shuffle.
    /// </summary>
    private IEnumerator AnimateBlockShuffle(Block block, Vector3 targetWorldPos, Vector2Int targetGridPos, float duration)
    {
        if (block?.VisualObject == null) yield break;

        Transform t = block.VisualObject.transform;
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        Vector3 punchScale = startScale * 1.15f;

        // Calculate arc height based on distance
        float distance = Vector3.Distance(startPos, targetWorldPos);
        float arcHeight = Mathf.Max(0.3f, distance * 0.3f);

        float elapsed = 0f;

        Debug.Log($"[Shuffle] Animating block from ({block.x},{block.y}) to ({targetGridPos.x},{targetGridPos.y})");

        while (elapsed < duration)
        {
            if (block.VisualObject == null) yield break;

            elapsed += Time.deltaTime;
            float t_progress = elapsed / duration;

            // Smooth movement
            float moveT = Mathf.SmoothStep(0f, 1f, t_progress);

            // Position with arc
            Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, moveT);

            // Add arc (parabola)
            float arcT = 4f * t_progress * (1f - t_progress); // Parabola peaking at 0.5
            currentPos.y += arcT * arcHeight;

            t.position = currentPos;

            // Scale punch effect
            float scaleT;
            if (t_progress < 0.15f)
            {
                // Scale up
                scaleT = t_progress / 0.15f;
                t.localScale = Vector3.Lerp(startScale, punchScale, scaleT);
            }
            else if (t_progress > 0.85f)
            {
                // Scale down
                scaleT = (t_progress - 0.85f) / 0.15f;
                t.localScale = Vector3.Lerp(punchScale, startScale, scaleT);
            }
            else
            {
                // Hold punch scale
                t.localScale = punchScale;
            }

            yield return null;
        }

        // Ensure final position and scale
        if (block.VisualObject != null)
        {
            t.position = targetWorldPos;
            t.localScale = startScale;
        }
    }

    /// <summary>
    /// Count how many of each color exists on the grid.
    /// </summary>
    private void PrintColorCounts(Dictionary<int, List<Block>> blocksByColor)
    {
        Debug.Log("[Shuffle] === COLOR COUNTS ===");
        foreach (var kvp in blocksByColor)
        {
            Debug.Log($"[Shuffle]   {GetColorName(kvp.Key)}: {kvp.Value.Count} blocks");
        }
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
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int startX = Random.Range(0, Columns);
            int startY = Random.Range(0, Rows);
            Vector2Int start = new Vector2Int(startX, startY);

            if (excludePositions.Contains(start) || blocks[startX, startY] == null)
                continue;

            List<Vector2Int> result = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && result.Count < count)
            {
                Vector2Int current = queue.Dequeue();

                if (excludePositions.Contains(current))
                    continue;

                if (blocks[current.x, current.y] == null)
                    continue;

                result.Add(current);

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
        return colorData != null ? colorData.ColorName : $"Color_{colorID}";
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