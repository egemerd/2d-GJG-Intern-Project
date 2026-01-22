using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera mainCamera;

    private void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            HandleTouch();
        }
    }

    private void HandleClick()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        ProcessInput(screenPos);
    }

    private void HandleTouch()
    {
        Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        ProcessInput(screenPos);
    }

    private void ProcessInput(Vector2 screenPos)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        worldPos.z = 0;

        Vector2Int gridPos = gridManager.WorldToGrid(worldPos);

        if (!gridManager.IsValidPosition(gridPos.x, gridPos.y))
        {
            Debug.Log("Clicked outside grid");
            return;
        }

        Block block = gridManager.GetBlock(gridPos.x, gridPos.y);
        if (block == null)
        {
            Debug.Log("No block at position");
            return;
        }

        List<Block> group = gridManager.FindConnectedGroup(gridPos.x, gridPos.y);
        if (group != null && group.Count >= 2)
        {
            Debug.Log($"💥 Blasting {group.Count} blocks!");
            gridManager.BlastGroup(group);
        }
        else
        {
            Debug.Log("Group too small or no group found");
        }
    }
}