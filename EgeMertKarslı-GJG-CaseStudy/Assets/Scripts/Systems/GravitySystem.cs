using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class GravitySystem
{
    private readonly GridData gridData;
    private readonly Board board;
    private readonly BlockSpawner blockSpawner;
    private readonly SortingOrderManager sortingOrderManager;
    private readonly MonoBehaviour coroutineRunner;

    private readonly float fallDuration = 0.25f;

    public event System.Action OnGravityComplete;

    public GravitySystem(
        GridData gridData,
        Board board,
        BlockSpawner blockSpawner,
        SortingOrderManager sortingOrderManager,
        MonoBehaviour coroutineRunner)
    {
        this.gridData = gridData;
        this.board = board;
        this.blockSpawner = blockSpawner;
        this.sortingOrderManager = sortingOrderManager;
        this.coroutineRunner = coroutineRunner;
    }



    public void ApplyGravityAndRefill()
    {
        coroutineRunner.StartCoroutine(GravityRoutine());
    }

    private IEnumerator GravityRoutine()
    {
        Debug.Log("[GravitySystem] Starting gravity...");

        List<Block> fallingBlocks = new List<Block>();
        List<Block> spawningBlocks = new List<Block>();

        // Process each column
        for (int x = 0; x < gridData.Columns; x++)
        {
            ProcessColumn(x, fallingBlocks, spawningBlocks);
        }

        Debug.Log($"[GravitySystem] Falling: {fallingBlocks.Count}, Spawning: {spawningBlocks.Count}");

  
        List<Coroutine> animations = new List<Coroutine>();

        foreach (Block block in fallingBlocks)
        {
            Vector3 targetPos = board.GetCellWorldPosition(block.x, block.y);
            animations.Add(coroutineRunner.StartCoroutine(AnimateBlockFall(block, targetPos)));
        }

        foreach (Block block in spawningBlocks)
        {
            Vector3 targetPos = board.GetCellWorldPosition(block.x, block.y);
            animations.Add(coroutineRunner.StartCoroutine(AnimateBlockFall(block, targetPos)));
        }

       
        foreach (var anim in animations)
        {
            yield return anim;
        }

        Debug.Log("[GravitySystem] Gravity complete");
        OnGravityComplete?.Invoke();
    }

    private void ProcessColumn(int x, List<Block> fallingBlocks, List<Block> spawningBlocks)
    {
        int writeY = 0;

     
        for (int y = 0; y < gridData.Rows; y++)
        {
            Block block = gridData.GetBlock(x, y);
            if (block != null)
            {
                if (writeY != y)
                {
                    gridData.SetBlock(x, writeY, block);
                    gridData.ClearBlock(x, y);

                    block.y = writeY;
                    block.SetState(BlockState.Falling);
                    fallingBlocks.Add(block);

                    blockSpawner.UpdateBlockMetadata(block, x, writeY);
                }
                writeY++;
            }
        }

        int blocksToSpawn = gridData.Rows - writeY;
        for (int i = 0; i < blocksToSpawn; i++)
        {
            int y = writeY + i;
            Block newBlock = blockSpawner.SpawnBlockForRefill(x, y, i);
            if (newBlock != null)
            {
                spawningBlocks.Add(newBlock);
            }
        }
    }

    private IEnumerator AnimateBlockFall(Block block, Vector3 targetPos)
    {
        if (block?.VisualObject == null) yield break;

        Transform t = block.VisualObject.transform;
        Vector3 startPos = t.position;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            if (block.VisualObject == null) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / fallDuration);
            t.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        if (block.VisualObject != null)
        {
            t.position = targetPos;
        }

        block.SetState(BlockState.Idle);
    }
}