using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Get reference to Board script
        Board board = (Board)target;

        // Add custom buttons
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid Generation (Edit Mode)", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Grid in Scene", GUILayout.Height(35)))
        {
            Undo.RecordObject(board.gameObject, "Generate Grid");
            board.ResetPosition();
            board.GenerateGridInEditor();
            EditorUtility.SetDirty(board.gameObject);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Grid",
                "Are you sure you want to clear the grid?",
                "Yes", "No"))
            {
                Undo.RecordObject(board.gameObject, "Clear Grid");
                board.ClearGrid();
                EditorUtility.SetDirty(board.gameObject);
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "1. Assign LevelConfig\n" +
            "2. Assign Cell Prefab (sprite with SpriteRenderer)\n" +
            "3. Click 'Generate Grid in Scene'\n" +
            "4. Grid will appear in Scene view instantly!",
            MessageType.Info
        );
    }
}