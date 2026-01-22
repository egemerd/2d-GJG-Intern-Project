using UnityEngine;

[System.Serializable]
public class Block
{
    public int x;
    public int y;
    public int ColorID;
    public int GroupSize;
    public BlockIconType IconType;

    // Object pooling reference
    public GameObject VisualObject;

    public Block(int x, int y, int colorID)
    {
        this.x = x;
        this.y = y;
        this.ColorID = colorID;
        this.GroupSize = 1;
        this.IconType = BlockIconType.Default;
    }

    public void Reset(int x, int y, int colorID)
    {
        this.x = x;
        this.y = y;
        this.ColorID = colorID;
        this.GroupSize = 1;
        this.IconType = BlockIconType.Default;
    }
}

public enum BlockIconType
{
    Default = 0,
    First = 1,
    Second = 2,
    Third = 3
}
