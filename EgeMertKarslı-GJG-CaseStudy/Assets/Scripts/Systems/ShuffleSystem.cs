using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles intelligent grid shuffling with guaranteed matches.
/// </summary>
public class ShuffleSystem
{
    private readonly GridData gridData;
    private readonly Board board;
    private readonly LevelConfig config;
    private readonly SortingOrderManager sortingOrderManager;
    private readonly MonoBehaviour coroutineRunner;

    private readonly int minGroupSize;
    private readonly int guaranteedColorCount;
    private readonly float shuffleDuration = 0.5f;

    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    public event System.Action OnShuffleComplete;

    public ShuffleSystem(
        GridData gridData,
        Board board,
        LevelConfig config,
        SortingOrderManager sortingOrderManager,
        MonoBehaviour coroutineRunner,
        int minGroupSize,
        int guaranteedColorCount)
    {
        this.gridData = gridData;
        this.board = board;
        this.config = config;
        this.sortingOrderManager = sortingOrderManager;
        this.coroutineRunner = coroutineRunner;
        this.minGroupSize = minGroupSize;
        this.guaranteedColorCount = guaranteedColorCount;
    }

    /// <summary>
    /// Start the shuffle with guaranteed matches.
    /// </summary>
    public void Shuffle()
    {
        coroutineRunner.StartCoroutine(ShuffleRoutine());
    }

    private IEnumerator ShuffleRoutine()
    {
        Debug.Log("[ShuffleSystem] ========================================");
        Debug.Log($"[ShuffleSystem] STARTING SHUFFLE WITH {guaranteedColorCount} GUARANTEED COLORS");
        Debug.Log("[ShuffleSystem] ========================================");

        // Collect all blocks
        List<Block> allBlocks = gridData.GetAllBlocks();
        List<Vector2Int> allPositions = new List<Vector2Int>();

        gridData.ForEachBlock((block, x, y) => allPositions.Add(new Vector2Int(x, y)));

        // Group blocks by color
        Dictionary<int, List<Block>> blocksByColor = GroupBlocksByColor(allBlocks);

        // Select colors to guarantee
        List<int> selectedColors = SelectColorsToGuarantee(blocksByColor);

        // Calculate target positions
        Dictionary<Block, Vector2Int> blockTargetPositions = CalculateTargetPositions(
            allBlocks, allPositions, blocksByColor, selectedColors);

        // Set all blocks to shuffling state
        foreach (Block block in allBlocks)
        {
            block.SetState(BlockState.Shuffling);
        }

        // Animate shuffle
        yield return AnimateAllBlocks(blockTargetPositions);

        // Update grid data
        UpdateGridAfterShuffle(blockTargetPositions);

        Debug.Log("[ShuffleSystem] ========================================");
        Debug.Log("[ShuffleSystem] SHUFFLE COMPLETE");
        Debug.Log("[ShuffleSystem] ========================================");

        OnShuffleComplete?.Invoke();
    }

    private Dictionary<int, List<Block>> GroupBlocksByColor(List<Block> blocks)
    {
        Dictionary<int, List<Block>> result = new Dictionary<int, List<Block>>();

        foreach (Block block in blocks)
        {
            if (!result.ContainsKey(block.ColorID))
                result[block.ColorID] = new List<Block>();
            result[block.ColorID].Add(block);
        }

        return result;
    }

    private List<int> SelectColorsToGuarantee(Dictionary<int, List<Block>> blocksByColor)
    {
        List<int> availableColors = blocksByColor
            .Where(kvp => kvp.Value.Count >= minGroupSize)
            .Select(kvp => kvp.Key)
            .ToList();

        int colorsToGuarantee = Mathf.Min(guaranteedColorCount, availableColors.Count);

        ShuffleList(availableColors);
        return availableColors.Take(colorsToGuarantee).ToList();
    }

    private Dictionary<Block, Vector2Int> CalculateTargetPositions(
        List<Block> allBlocks,
        List<Vector2Int> allPositions,
        Dictionary<int, List<Block>> blocksByColor,
        List<int> selectedColors)
    {
        Dictionary<Block, Vector2Int> result = new Dictionary<Block, Vector2Int>();
        HashSet<Vector2Int> reservedPositions = new HashSet<Vector2Int>();

        // Reserve positions for guaranteed colors
        foreach (int colorID in selectedColors)
        {
            List<Block> colorBlocks = blocksByColor[colorID];
            int groupSize = Random.Range(minGroupSize, Mathf.Min(colorBlocks.Count, 5) + 1);

            List<Vector2Int> adjacentPositions = FindRandomAdjacentPositions(groupSize, reservedPositions);

            if (adjacentPositions.Count >= minGroupSize)
            {
                for (int i = 0; i < adjacentPositions.Count && i < colorBlocks.Count; i++)
                {
                    result[colorBlocks[i]] = adjacentPositions[i];
                    reservedPositions.Add(adjacentPositions[i]);
                }
            }
        }

        // Assign remaining blocks
        List<Vector2Int> remainingPositions = allPositions
            .Where(pos => !reservedPositions.Contains(pos))
            .ToList();

        List<Block> remainingBlocks = allBlocks
            .Where(block => !result.ContainsKey(block))
            .ToList();

        ShuffleList(remainingPositions);

        for (int i = 0; i < remainingBlocks.Count && i < remainingPositions.Count; i++)
        {
            result[remainingBlocks[i]] = remainingPositions[i];
        }

        return result;
    }

    private List<Vector2Int> FindRandomAdjacentPositions(int count, HashSet<Vector2Int> excludePositions)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int startX = Random.Range(0, gridData.Columns);
            int startY = Random.Range(0, gridData.Rows);
            Vector2Int start = new Vector2Int(startX, startY);

            if (excludePositions.Contains(start) || gridData.GetBlock(startX, startY) == null)
                continue;

            List<Vector2Int> result = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && result.Count < count)
            {
                Vector2Int current = queue.Dequeue();

                if (excludePositions.Contains(current)) continue;
                if (gridData.GetBlock(current.x, current.y) == null) continue;

                result.Add(current);

                List<Vector2Int> shuffledDirs = new List<Vector2Int>(Directions);
                ShuffleList(shuffledDirs);

                foreach (Vector2Int dir in shuffledDirs)
                {
                    Vector2Int neighbor = current + dir;

                    if (!gridData.IsValidPosition(neighbor.x, neighbor.y)) continue;
                    if (visited.Contains(neighbor)) continue;
                    if (excludePositions.Contains(neighbor)) continue;
                    if (gridData.GetBlock(neighbor.x, neighbor.y) == null) continue;

                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }

            if (result.Count >= minGroupSize)
                return result;
        }

        return new List<Vector2Int>();
    }

    private IEnumerator AnimateAllBlocks(Dictionary<Block, Vector2Int> blockTargetPositions)
    {
        List<Coroutine> animations = new List<Coroutine>();

        foreach (var kvp in blockTargetPositions)
        {
            Block block = kvp.Key;
            Vector2Int targetGridPos = kvp.Value;
            Vector3 targetWorldPos = board.GetCellWorldPosition(targetGridPos.x, targetGridPos.y);

            animations.Add(coroutineRunner.StartCoroutine(
                AnimateBlockShuffle(block, targetWorldPos, shuffleDuration)));
        }

        foreach (var anim in animations)
        {
            yield return anim;
        }
    }

    private IEnumerator AnimateBlockShuffle(Block block, Vector3 targetPos, float duration)
    {
        if (block?.VisualObject == null) yield break;

        Transform t = block.VisualObject.transform;
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        Vector3 punchScale = startScale * 1.15f;

        float distance = Vector3.Distance(startPos, targetPos);
        float arcHeight = Mathf.Max(0.3f, distance * 0.3f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (block.VisualObject == null) yield break;

            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float moveT = Mathf.SmoothStep(0f, 1f, progress);

            // Position with arc
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, moveT);
            float arcT = 4f * progress * (1f - progress);
            currentPos.y += arcT * arcHeight;
            t.position = currentPos;

            // Scale punch
            if (progress < 0.15f)
                t.localScale = Vector3.Lerp(startScale, punchScale, progress / 0.15f);
            else if (progress > 0.85f)
                t.localScale = Vector3.Lerp(punchScale, startScale, (progress - 0.85f) / 0.15f);
            else
                t.localScale = punchScale;

            yield return null;
        }

        if (block.VisualObject != null)
        {
            t.position = targetPos;
            t.localScale = startScale;
        }
    }

    private void UpdateGridAfterShuffle(Dictionary<Block, Vector2Int> blockTargetPositions)
    {
        gridData.ClearAll();

        foreach (var kvp in blockTargetPositions)
        {
            Block block = kvp.Key;
            Vector2Int newPos = kvp.Value;

            block.x = newPos.x;
            block.y = newPos.y;
            gridData.SetBlock(newPos.x, newPos.y, block);

            // Update metadata
            if (block.VisualObject != null)
            {
                BlockMetadata metadata = block.VisualObject.GetComponent<BlockMetadata>();
                if (metadata != null)
                {
                    metadata.GridX = newPos.x;
                    metadata.GridY = newPos.y;
                }

                sortingOrderManager?.UpdateSortingOrder(block.VisualObject, newPos.y);
                block.VisualObject.name = $"Block_{newPos.x}_{newPos.y}";
            }

            block.SetState(BlockState.Idle);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}