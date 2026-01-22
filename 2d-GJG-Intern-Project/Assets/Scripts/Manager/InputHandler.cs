using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LevelConfig levelConfig;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (mainCamera == null || gridManager == null || levelConfig == null)
        {
            Debug.LogWarning("InputHandler: Missing references!");
            return;
        }

        // Get mouse position in world space
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        mouseWorldPos.z = 0;

        Debug.Log($"Screen: {mouseScreenPos}, World: {mouseWorldPos}");

        // Get grid parent position (this is where the grid is centered)
        Transform gridTransform = gridManager.transform.GetComponentInChildren<Grid>()?.transform;
        if (gridTransform == null)
        {
            Debug.LogError("Grid component not found!");
            return;
        }

        Vector3 gridOrigin = gridTransform.position;
        Debug.Log($"Grid Origin: {gridOrigin}");

        // Convert world position to local position relative to grid
        Vector3 localPos = mouseWorldPos - gridOrigin;

        // Calculate grid coordinates
        float totalSpacing = levelConfig.TotalCellSpacing;

        // Account for cell centering (cells are centered at their position)
        // We need to add half spacing to align with the cell center offset
        int x = Mathf.FloorToInt((localPos.x + (gridManager.Columns * totalSpacing) / 2f) / totalSpacing);
        int y = Mathf.FloorToInt((localPos.y + (gridManager.Rows * totalSpacing) / 2f) / totalSpacing);

        Debug.Log($"Local: {localPos}, Grid Coords: ({x}, {y})");

        // Validate position
        if (!gridManager.IsValidPosition(x, y))
        {
            Debug.Log($"❌ Clicked outside grid: ({x}, {y}) - Valid: (0-{gridManager.Columns - 1}, 0-{gridManager.Rows - 1})");
            return;
        }

        Block block = gridManager.GetBlock(x, y);
        if (block == null)
        {
            Debug.Log($"❌ No block at ({x}, {y})");
            return;
        }

        Debug.Log($"✅ Found block at ({x}, {y}), ColorID: {block.ColorID}");

        // Find and blast group
        List<Block> group = gridManager.FindConnectedGroup(x, y);
        if (group != null && group.Count >= 2)
        {
            Debug.Log($"💥 Blasting group of {group.Count} blocks at ({x}, {y})!");
            gridManager.BlastGroup(group);
        }
        else
        {
            Debug.Log($"⚠️ Block at ({x}, {y}) has no valid group (minimum 2 blocks required)");
        }
    }
}