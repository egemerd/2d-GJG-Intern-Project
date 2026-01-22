using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Component that stores information about a single grid cell.
/// Attached to each cell GameObject in the grid.
/// </summary>
public class GridCellInfo : MonoBehaviour
{
    [Header("Grid Position")]
    [Tooltip("X coordinate in the grid (column)")]
    public int X;

    [Tooltip("Y coordinate in the grid (row)")]
    public int Y;

    [Header("Cell Data")]
    [Tooltip("Color ID assigned to this cell from LevelConfig")]
    public int ColorID;

    [Tooltip("Size of this cell (usually matches LevelConfig.CellSize)")]
    public float CellSize = 1f;

    [Header("Visualization")]
    [Tooltip("Color used for gizmo visualization in Scene view")]
    public Color GizmoColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Tooltip("Show/hide gizmo in Scene view")]
    public bool ShowGizmo = true;

    // NEW: Hide sprite at runtime
    private void Start()
    {
        // Hide the sprite renderer at runtime (keep only editor visualization)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
        }
    }

    /// <summary>
    /// Get the world position of this cell's center
    /// </summary>
    public Vector3 GetCenterPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Check if this cell is at a specific grid position
    /// </summary>
    public bool IsAt(int x, int y)
    {
        return X == x && Y == y;
    }

    /// <summary>
    /// Get grid coordinates as Vector2Int
    /// </summary>
    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(X, Y);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draw gizmos in Scene view to visualize the cell
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!ShowGizmo) return;

        // Draw filled cube with semi-transparent color
        Gizmos.color = GizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(CellSize * 0.85f, CellSize * 0.85f, 0.01f));

        // Draw wireframe border
        Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(CellSize * 0.9f, CellSize * 0.9f, 0.01f));

        // Draw coordinate label
        GUIStyle style = new GUIStyle
        {
            normal = new GUIStyleState { textColor = Color.white },
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        Handles.Label(transform.position, $"{ColorID}", style);
    }

    /// <summary>
    /// Draw additional info when selected
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(CellSize, CellSize, 0.01f));

        // Draw detailed info label
        GUIStyle style = new GUIStyle
        {
            normal = new GUIStyleState { textColor = Color.yellow },
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        Vector3 labelPos = transform.position + Vector3.up * CellSize * 0.6f;
        Handles.Label(labelPos, $"Cell [{X}, {Y}]\nColor: {ColorID}", style);
    }
#endif
}