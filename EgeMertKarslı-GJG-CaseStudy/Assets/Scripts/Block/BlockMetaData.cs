using UnityEngine;


public class BlockMetadata : MonoBehaviour
{
    public int GridX;
    public int GridY;
    public int ColorID;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"({GridX}, {GridY})\nID: {ColorID}"
        );
#endif
    }
}