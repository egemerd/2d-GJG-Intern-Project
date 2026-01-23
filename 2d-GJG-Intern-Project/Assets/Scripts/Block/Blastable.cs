using UnityEngine;
using UnityEngine.EventSystems;

public class Blastable : MonoBehaviour, IPointerClickHandler
{
    private GridManager grid;

    //for matching with the secelted grid block
    public int gridX { get; set; }
    public int gridY { get; set; }

    private void Awake()
    {
        grid = FindObjectOfType<GridManager>();

        if (grid == null)
        {
            Debug.LogError("[Blastable] GridManager not found!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (grid == null)
        {
            Debug.LogWarning("GridManager is null!");
            return;
        }
  
        // get the data of the clicked block
        var blockData = gameObject.GetComponent<BlockMetadata>();
        if (blockData != null)
        {
            gridX = blockData.GridX;
            gridY = blockData.GridY;
        }

        Debug.Log($"[Blastable] Clicked {gameObject.name} at Grid({gridX}, {gridY})");

        var group = grid.FindConnectedGroup(gridX, gridY);

        //Check for null before accessing .Count
        if (group != null && group.Count >= 2)
        {
            Debug.Log($"[Blastable] Blasting {group.Count} blocks!");
            grid.BlastGroup(group);
        }
        else
        {
            int count = group != null ? group.Count : 0;
            Debug.Log($"[Blastable] Group too small: {count} blocks (need 2+)");
        }
    }
}