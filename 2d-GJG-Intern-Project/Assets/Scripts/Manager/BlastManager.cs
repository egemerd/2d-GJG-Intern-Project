using UnityEngine;
using System.Collections.Generic;


public class BlastManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;


    private static BlastManager instance;
    public static BlastManager Instance => instance;


    private bool isInputEnabled = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void OnBlockClicked(int gridX, int gridY)
    {
        if (!isInputEnabled)
        {
            if (enableDebugLogs)
                Debug.Log("[BlastManager] Input is disabled globally");
            return;
        }

        if (gridManager == null || !gridManager.CanProcessInput())
        {
            if (enableDebugLogs)
                Debug.Log("[BlastManager] Grid is processing, click ignored");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[BlastManager] Block clicked at ({gridX}, {gridY})");


        Block block = gridManager.GetBlock(gridX, gridY);

        if (block == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[BlastManager] No block at ({gridX}, {gridY})");
            return;
        }


        if (!block.CanInteract())
        {
            if (enableDebugLogs)
                Debug.Log($"[BlastManager] Block at ({gridX},{gridY}) cannot interact - State: {block.State}");
            return;
        }

       
        List<Block> group = gridManager.FindConnectedGroup(gridX, gridY);

        if (group != null && group.Count >= 2)
        {
            if (enableDebugLogs)
                Debug.Log($"[BlastManager] Found valid group of {group.Count} blocks - Blasting!");

            // Trigger blast through GridManager
            gridManager.BlastGroup(group);
        }
        else
        {
            int count = group != null ? group.Count : 0;
            if (enableDebugLogs)
                Debug.Log($"[BlastManager] Group too small ({count} blocks) - minimum is 2");
        }
    }

    

    
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        Debug.Log($"[BlastManager] Input globally {(enabled ? "enabled" : "disabled")}");
    }

    
    public bool IsInputEnabled()
    {
        return isInputEnabled && (gridManager != null && gridManager.CanProcessInput());
    }

    
    public GridManager GetGridManager()
    {
        return gridManager;
    }
   
}