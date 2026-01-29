using UnityEngine;


public class SortingOrderManager : MonoBehaviour
{
    [Header("Sorting Configuration")]
    [Tooltip("Base sorting order for runtime blocks")]
    [SerializeField] private int baseSortingOrder = 10;

    [Tooltip("Sorting order for editor grid cells (should be lower than base)")]
    [SerializeField] private int editorGridSortingOrder = 5;

    [Tooltip("Name of the sorting layer to use")]
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Grid Reference")]
    [SerializeField] private Board board;

    private int totalRows;

    private void Awake()
    {
        board = FindObjectOfType<Board>();
        totalRows = board.Height;
    }

    
    public int GetSortingOrder(int y)
    {
        return baseSortingOrder + y;
    }

    
    public int GetEditorGridSortingOrder()
    {
        return editorGridSortingOrder;
    }

   
    public void ApplySortingOrder(SpriteRenderer spriteRenderer, int y)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning("SortingOrderManager: SpriteRenderer is null!");
            return;
        }

        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = GetSortingOrder(y);
    }

    
    public void ApplyEditorGridSortingOrder(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning("SortingOrderManager: SpriteRenderer is null!");
            return;
        }

        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = editorGridSortingOrder;
    }

    
    public void UpdateSortingOrder(GameObject blockObject, int newY)
    {
        if (blockObject == null) return;

        SpriteRenderer sr = blockObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            ApplySortingOrder(sr, newY);
        }
    }

    
    public int GetMaxBlockSortingOrder()
    {
        return baseSortingOrder + totalRows - 1;
    }

    
    public int GetMinBlockSortingOrder()
    {
        return baseSortingOrder;
    }

    
    
}