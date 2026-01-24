using UnityEngine;
using UnityEngine.EventSystems;

public class Blastable : MonoBehaviour, IPointerClickHandler
{
    private GridManager grid;
    private BlockMetadata blockData;

    private void Awake()
    {
        grid = FindObjectOfType<GridManager>();
        blockData = GetComponent<BlockMetadata>();

        if (grid == null)
        {
            Debug.LogError("[Blastable] GridManager not found!");
        }
    }

    private void OnEnable()
    {
        if (blockData == null)
        {
            blockData = GetComponent<BlockMetadata>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[Blastable] Clicked at ({blockData.GridX}, {blockData.GridY})");

        if (grid == null || blockData == null)
        {
            Debug.LogWarning("[Blastable] Missing references!");
            return;
        }

        // Get block and check state
        Block block = grid.GetBlock(blockData.GridX, blockData.GridY);
        if (block == null)
        {
            Debug.LogWarning($"[Blastable] No block at ({blockData.GridX}, {blockData.GridY})");
            return;
        }

        // Check if block can be interacted with
        if (!block.CanInteract())
        {
            Debug.Log($"[Blastable] Block rejected click - State: {block.State}");
            return;
        }

        // Find group and blast
        var group = grid.FindConnectedGroup(blockData.GridX, blockData.GridY);

        if (group != null && group.Count >= 2)
        {
            Debug.Log($"[Blastable] Found group of {group.Count} blocks - Blasting!");
            grid.BlastGroup(group);
        }
        else
        {
            int count = group != null ? group.Count : 0;
            Debug.Log($"[Blastable] Group too small: {count} blocks");
        }
    }
}