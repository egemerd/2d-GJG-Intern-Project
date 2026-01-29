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

    
    [ContextMenu("Debug Sorting Order Info")]
    public void DebugSortingOrderInfo()
    {
        Debug.Log("=== SORTING ORDER INFO ===");
        Debug.Log($"Total Rows: {totalRows}");
        Debug.Log($"Base Sorting Order: {baseSortingOrder}");
        Debug.Log($"Editor Grid Order: {editorGridSortingOrder}");
        Debug.Log($"Sorting Layer: {sortingLayerName}");
        Debug.Log($"\nBlock Sorting Orders (TOP rows in FRONT):");
        Debug.Log($"  TOP Row (Y={totalRows - 1}): {GetSortingOrder(totalRows - 1)} ← FRONTMOST ✅");
        for (int i = totalRows - 2; i > 0; i--)
        {
            Debug.Log($"  Row (Y={i}): {GetSortingOrder(i)}");
        }
        Debug.Log($"  BOTTOM Row (Y=0): {GetSortingOrder(0)}  BACKMOST");
        Debug.Log($"\nRange: {GetMinBlockSortingOrder()} to {GetMaxBlockSortingOrder()}");
    }
}