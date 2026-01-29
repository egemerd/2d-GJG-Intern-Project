using UnityEngine;
using System.Collections.Generic;


public class GroupDetector
{
    private readonly GridData gridData;
    private readonly LevelConfig config;
    private readonly int minGroupSize;

    //Reusable collections to avoid GC
    private readonly Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>(100);
    private readonly HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private readonly List<Block> currentGroup = new List<Block>(100);

    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   
        new Vector2Int(0, -1),  
        new Vector2Int(-1, 0),  
        new Vector2Int(1, 0)    
    };

    public GroupDetector(GridData gridData, LevelConfig config, int minGroupSize)
    {
        this.gridData = gridData;
        this.config = config;
        this.minGroupSize = minGroupSize;
    }

    public List<Block> FindConnectedGroup(int startX, int startY)
    {
        Block startBlock = gridData.GetBlock(startX, startY);
        if (startBlock == null) return null;

        if (!startBlock.CanBeGrouped())
        {
            Debug.Log($"[GroupDetector] Block at ({startX},{startY}) cannot be grouped - State: {startBlock.State}");
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
            Block block = gridData.GetBlock(pos.x, pos.y);

            if (block != null && block.ColorID == targetColorID && block.CanBeGrouped())
            {
                currentGroup.Add(block);

                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int neighborPos = pos + dir;

                    if (visitedCells.Contains(neighborPos)) continue;
                    if (!gridData.IsValidPosition(neighborPos.x, neighborPos.y)) continue;

                    Block neighbor = gridData.GetBlock(neighborPos.x, neighborPos.y);
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


    public void UpdateAllGroupIcons()
    {
        visitedCells.Clear();

        // Reset all blocks
        gridData.ForEachBlock((block, x, y) =>
        {
            if (block.CanBeGrouped())
            {
                block.GroupSize = 1;
                block.IconType = BlockIconType.Default;
            }
        });

        // Find and update groups
        for (int y = 0; y < gridData.Rows; y++)
        {
            for (int x = 0; x < gridData.Columns; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (visitedCells.Contains(pos)) continue;

                Block block = gridData.GetBlock(x, y);
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
        gridData.ForEachBlock((block, x, y) =>
        {
            if (block.VisualObject == null) return;

            SpriteRenderer sr = block.VisualObject.GetComponent<SpriteRenderer>();
            BlockColorData colorData = config.GetColorData(block.ColorID);

            if (colorData != null && sr != null)
            {
                sr.sprite = colorData.GetIconForType(block.IconType);
            }
        });
    }
}