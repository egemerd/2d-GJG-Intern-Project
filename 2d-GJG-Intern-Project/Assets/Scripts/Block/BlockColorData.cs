using UnityEngine;

[CreateAssetMenu(fileName = "BlockColorData", menuName = "BlockColor/BlockColorData", order = 1)]
public class BlockColorData : ScriptableObject
{
    [Header("Block Colors")]
    public int ColorID;
    public string ColorName;

    [Header("Display Color")]
    [Tooltip("Color used for editor gizmos and previews")]
    public Color DisplayColor = Color.white;

    [Header("Icon Sprites")]
    public Sprite DefaultIcon;
    public Sprite FirstIcon;
    public Sprite SecondIcon;
    public Sprite ThirdIcon;

    public Sprite GetIconForType(BlockIconType iconType)
    {
        return iconType switch
        {
            BlockIconType.First => FirstIcon != null ? FirstIcon : DefaultIcon,
            BlockIconType.Second => SecondIcon != null ? SecondIcon : DefaultIcon,
            BlockIconType.Third => ThirdIcon != null ? ThirdIcon : DefaultIcon,
            _ => DefaultIcon
        };
    }

    /// <summary>
    /// Get the display color with optional alpha override
    /// </summary>
    public Color GetGizmoColor(float alpha = 0.6f)
    {
        return new Color(DisplayColor.r, DisplayColor.g, DisplayColor.b, alpha);
    }
}