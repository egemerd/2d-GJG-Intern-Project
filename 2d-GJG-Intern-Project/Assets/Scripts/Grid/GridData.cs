using UnityEngine;


public class GridData
{
    private Block[,] blocks;

    public int Columns { get; private set; }
    public int Rows { get; private set; }

    public GridData(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
        blocks = new Block[columns, rows];
    }

    public Block GetBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return null;
        return blocks[x, y];
    }

    public void SetBlock(int x, int y, Block block)
    {
        if (!IsValidPosition(x, y)) return;
        blocks[x, y] = block;
    }

    public void ClearBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        blocks[x, y] = null;
    }

    public void ClearAll()
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                blocks[x, y] = null;
            }
        }
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }

    /// Iterate all blocks (for operations that need to process entire grid)
    public void ForEachBlock(System.Action<Block, int, int> action)
    {
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Block block = blocks[x, y];
                if (block != null)
                {
                    action(block, x, y);
                }
            }
        }
    }

    /// Get all blocks as a list
    public System.Collections.Generic.List<Block> GetAllBlocks()
    {
        var list = new System.Collections.Generic.List<Block>();
        ForEachBlock((block, x, y) => list.Add(block));
        return list;
    }
}