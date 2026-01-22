using UnityEngine;
using UnityEngine.EventSystems;

public class Blastable : MonoBehaviour, IPointerClickHandler
{
    private GridManager grid;

    public int GridX { get; set; }
    public int GridY { get; set; }

    private void Awake()
    {
        grid = FindObjectOfType<GridManager>();

        if (grid == null)
        {
            Debug.LogError("Blastable: GridManager not found!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (grid == null)
        {
            Debug.LogWarning("GridManager is null!");
            return;
        }

        Debug.Log($"Clicked block at ({GridX}, {GridY})");

        var group = grid.FindConnectedGroup(GridX, GridY);

        // FIXED: Check for null before accessing .Count
        if (group != null && group.Count >= 2)
        {
            Debug.Log($"💥 Blasting {group.Count} blocks!");
            grid.BlastGroup(group);
        }
        else
        {
            int count = group != null ? group.Count : 0;
            Debug.Log($"⚠️ Group too small: {count} blocks (need 2+)");
        }
    }
}