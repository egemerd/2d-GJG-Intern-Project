using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Board board;
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private LevelConfig config;
    [SerializeField] private SortingOrderManager sortingOrderManager;

    [Header("Gameplay Settings")]
    [SerializeField] private int minGroupSize = 2;

    private Block[,] blocks;

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

        // Setup visual
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

        // Setup metadata
        BlockMetadata metadata = blockObj.GetComponent<BlockMetadata>();
        if (metadata != null)
        {
            metadata.GridX = x;
            metadata.GridY = y;
            metadata.ColorID = colorID;
        }

        // Bind animator to block
        BlockAnimator animator = blockObj.GetComponent<BlockAnimator>();
        if (animator != null)
        {
            animator.SetOriginalScale(blockObj.transform.localScale);
            animator.BindToBlock(block);
        }

        // Set to idle immediately for initial spawn
        if (setIdleImmediately)
        {
            block.SetState(BlockState.Idle);
        }
    }

    public List<Block> FindConnectedGroup(int startX, int startY)
    {
        Block startBlock = GetBlock(startX, startY);
        if (startBlock == null) return null;

        // Only group idle blocks
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

        Debug.Log($"[GridManager] === BLAST START === ({group.Count} blocks)");

        // Set all to Blasting state
        foreach (Block block in group)
        {
            block.SetState(BlockState.Blasting);
        }

        StartCoroutine(BlastGroupRoutine(group));
    }

    private IEnumerator BlastGroupRoutine(List<Block> group)
    {
        // Get blast duration from first block's animator
        float blastDuration = 0.2f; // Default
        if (group.Count > 0 && group[0].VisualObject != null)
        {
            BlockAnimator animator = group[0].VisualObject.GetComponent<BlockAnimator>();
            if (animator != null)
            {
                blastDuration = animator.BlastDuration;
            }
        }

        // Set all to Blasting state (triggers animations)
        foreach (Block block in group)
        {
            block.SetState(BlockState.Blasting);
        }

        // Wait for blast animations to complete
        yield return new WaitForSeconds(blastDuration);

        // Return blocks to pool
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
            ShuffleGrid();
        }

        Debug.Log("[GridManager] === READY FOR INPUT ===");
    }

    private IEnumerator ApplyGravityAndRefill()
    {
        List<Block> fallingBlocks = new List<Block>();
        List<Block> spawningBlocks = new List<Block>();

        for (int x = 0; x < Columns; x++)
        {
            int writeY = 0;

            // Gravity - move blocks down
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

                        // Update metadata
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

            // Spawn new blocks from top
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

                    // Position above grid
                    Vector3 spawnPos = board.GetSpawnPosition(x, i + 1);
                    newBlock.VisualObject.transform.position = spawnPos;
                }
            }
        }

        Debug.Log($"[GridManager] Falling: {fallingBlocks.Count}, Spawning: {spawningBlocks.Count}");

        // Animate all blocks
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

        // Wait for all animations
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

        // Animation complete - set to Idle
        block.SetState(BlockState.Idle);
    }

    public void UpdateAllGroupIcons()
    {
        visitedCells.Clear();

        // Reset all idle blocks
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

        // Find and update groups
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

        // Update visuals
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

    public void ShuffleGrid()
    {
        Debug.Log("[GridManager] === SHUFFLE START ===");

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

                    SpriteRenderer sr = blocks[x, y].VisualObject?.GetComponent<SpriteRenderer>();
                    BlockColorData colorData = config.GetColorData(blocks[x, y].ColorID);
                    if (sr != null && colorData != null)
                    {
                        sr.sprite = colorData.DefaultIcon;
                    }
                }
            }
        }

        UpdateAllGroupIcons();
        Debug.Log("[GridManager] === SHUFFLE COMPLETE ===");
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