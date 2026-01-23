using UnityEngine;

/// <summary>
/// Manages sorting order for grid-based blocks to ensure proper visual layering.
/// Top rows appear in front of bottom rows (isometric/top-down view).
/// </summary>
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
        if (board == null)
            board = FindObjectOfType<Board>();

        if (board != null)
            totalRows = board.Height;
    }

    /// <summary>
    /// Calculate sorting order for a block at given Y position.
    /// TOP rows (higher Y) get HIGHER order (appear in front).
    /// BOTTOM rows (lower Y) get LOWER order (appear behind).
    /// </summary>
    public int GetSortingOrder(int y)
    {
        // ✅ FIXED FORMULA: baseSortingOrder + Y
        // Example with 8 rows (Y=0 to Y=7):
        //   Y=7 (TOP row)    → 10 + 7 = 17 (FRONT - highest)
        //   Y=6              → 10 + 6 = 16
        //   Y=5              → 10 + 5 = 15
        //   ...
        //   Y=1              → 10 + 1 = 11
        //   Y=0 (BOTTOM row) → 10 + 0 = 10 (BACK - lowest)
        return baseSortingOrder + y;
    }

    /// <summary>
    /// Get sorting order for editor grid cells (always behind runtime blocks)
    /// </summary>
    public int GetEditorGridSortingOrder()
    {
        return editorGridSortingOrder;
    }

    /// <summary>
    /// Apply sorting order to a sprite renderer based on Y position
    /// </summary>
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

    /// <summary>
    /// Apply editor grid sorting order to a sprite renderer
    /// </summary>
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

    /// <summary>
    /// Update sorting order when block moves to new Y position
    /// </summary>
    public void UpdateSortingOrder(GameObject blockObject, int newY)
    {
        if (blockObject == null) return;

        SpriteRenderer sr = blockObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            ApplySortingOrder(sr, newY);
        }
    }

    /// <summary>
    /// Get the maximum sorting order used by blocks (useful for UI layering)
    /// </summary>
    public int GetMaxBlockSortingOrder()
    {
        return baseSortingOrder + totalRows - 1;
    }

    /// <summary>
    /// Get the minimum sorting order used by blocks
    /// </summary>
    public int GetMinBlockSortingOrder()
    {
        return baseSortingOrder;
    }

    /// <summary>
    /// Debug: Print sorting order info
    /// </summary>
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
        Debug.Log($"  BOTTOM Row (Y=0): {GetSortingOrder(0)} ← BACKMOST");
        Debug.Log($"\nRange: {GetMinBlockSortingOrder()} to {GetMaxBlockSortingOrder()}");
    }
}