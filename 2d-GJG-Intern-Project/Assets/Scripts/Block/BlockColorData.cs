using UnityEngine;

[CreateAssetMenu(fileName = "BlockColorData", menuName = "BlockColor/BlockColorData", order = 1)]
public class BlockColorData : ScriptableObject
{
    [Header("Block Colors")]
    public int ColorID;
    public string ColorName;

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

}
