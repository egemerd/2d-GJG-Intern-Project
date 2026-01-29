using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(GridCellInfo))]
[CanEditMultipleObjects]
public class GridCellInfoEditor : Editor
{
    private GridCellInfo cell;
    private LevelConfig config;

    private void OnEnable()
    {
        cell = (GridCellInfo)target;
        FindConfig();
    }

    private void FindConfig()
    {
        Board board = cell.GetComponentInParent<Board>();
        if (board != null)
        {
            config = board.Config;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (config == null)
        {
            FindConfig();
            if (config == null)
            {
                EditorGUILayout.HelpBox("Could not find LevelConfig.", MessageType.Warning);
                return;
            }
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quick Color Selection", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        foreach (var colorData in config.AvailableColors)
        {
            if (colorData == null) continue;

            bool isSelected = cell.ColorID == colorData.ColorID;
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            GUIContent content = new GUIContent();
            if (colorData.DefaultIcon != null)
            {
                content.image = AssetPreview.GetAssetPreview(colorData.DefaultIcon);
            }
            content.tooltip = colorData.ColorName;

            if (GUILayout.Button(content, GUILayout.Width(50), GUILayout.Height(50)))
            {
                Undo.RecordObject(cell, "Change Cell Color");
                cell.ColorID = colorData.ColorID;
                cell.GizmoColor = GetGizmoColor(colorData.ColorID);

                // Update LevelConfig data
                config.SetColorIDAt(cell.X, cell.Y, colorData.ColorID);
                EditorUtility.SetDirty(cell);
                EditorUtility.SetDirty(config);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = oldBg;
        }

        EditorGUILayout.EndHorizontal();

        // Show current color info
        BlockColorData currentColor = config.GetColorData(cell.ColorID);
        if (currentColor != null)
        {
            EditorGUILayout.LabelField($"Current: {currentColor.ColorName}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }


    
    private Color GetGizmoColor(int colorID)
    {
        // Try to get actual color from BlockColorData
        if (config != null)
        {
            BlockColorData colorData = config.GetColorData(colorID);
            if (colorData != null)
            {
                return colorData.GetGizmoColor(0.6f);
            }
        }

        // Fallback colors
        Color[] fallbackColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f, 0.6f),
            new Color(0.3f, 0.6f, 1f, 0.6f),
            new Color(0.3f, 1f, 0.3f, 0.6f),
            new Color(1f, 1f, 0.3f, 0.6f),
            new Color(1f, 0.5f, 0.9f, 0.6f),
            new Color(0.6f, 0.3f, 1f, 0.6f),
            new Color(1f, 0.6f, 0.3f, 0.6f),
            new Color(0.3f, 1f, 1f, 0.6f),
        };
        return fallbackColors[colorID % fallbackColors.Length];
    }
}