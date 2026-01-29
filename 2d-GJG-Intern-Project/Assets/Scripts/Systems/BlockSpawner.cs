using UnityEngine;


public class BlockSpawner
{
    private readonly GridData gridData;
    private readonly Board board;
    private readonly BlockPool blockPool;
    private readonly LevelConfig config;
    private readonly SortingOrderManager sortingOrderManager;

    public BlockSpawner(
        GridData gridData,
        Board board,
        BlockPool blockPool,
        LevelConfig config,
        SortingOrderManager sortingOrderManager)
    {
        this.gridData = gridData;
        this.board = board;
        this.blockPool = blockPool;
        this.config = config;
        this.sortingOrderManager = sortingOrderManager;
    }


    public void SpawnAllBlocks()
    {
        Debug.Log("[BlockSpawner] Spawning all blocks...");

        for (int y = 0; y < gridData.Rows; y++)
        {
            for (int x = 0; x < gridData.Columns; x++)
            {
                GridCellInfo cell = board.GetCell(x, y);
                if (cell != null)
                {
                    SpawnBlock(x, y, cell.ColorID, setIdleImmediately: true);
                }
            }
        }

        Debug.Log($"[BlockSpawner] Spawned {gridData.Columns * gridData.Rows} blocks");
    }


    public Block SpawnBlock(int x, int y, int colorID, bool setIdleImmediately = false)
    {
        GameObject blockObj = blockPool.GetBlock();
        if (blockObj == null) return null;

        Block block = new Block(x, y, colorID);
        gridData.SetBlock(x, y, block);
        block.VisualObject = blockObj;

        Vector3 worldPos = board.GetCellWorldPosition(x, y);
        blockObj.transform.position = worldPos;
        blockObj.name = $"Block_{x}_{y}";

        SetupBlockVisual(block, blockObj, colorID, y);
        SetupBlockComponents(block, blockObj, x, y, colorID);

        if (setIdleImmediately)
        {
            block.SetState(BlockState.Idle);
        }

        return block;
    }


    public Block SpawnBlockForRefill(int x, int y, int spawnIndex)
    {
        BlockColorData randomColor = config.GetRandomColorData();
        if (randomColor == null) return null;

        Block block = SpawnBlock(x, y, randomColor.ColorID, setIdleImmediately: false);
        if (block == null) return null;

        // Position above grid
        Vector3 spawnPos = board.GetSpawnPosition(x, spawnIndex + 1);
        block.VisualObject.transform.position = spawnPos;

        return block;
    }

    private void SetupBlockVisual(Block block, GameObject blockObj, int colorID, int y)
    {
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
    }

    private void SetupBlockComponents(Block block, GameObject blockObj, int x, int y, int colorID)
    {
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
    }


    public void UpdateBlockMetadata(Block block, int newX, int newY)
    {
        if (block?.VisualObject == null) return;

        block.x = newX;
        block.y = newY;

        BlockMetadata metadata = block.VisualObject.GetComponent<BlockMetadata>();
        if (metadata != null)
        {
            metadata.GridX = newX;
            metadata.GridY = newY;
        }

        sortingOrderManager?.UpdateSortingOrder(block.VisualObject, newY);
        block.VisualObject.name = $"Block_{newX}_{newY}";
    }
}