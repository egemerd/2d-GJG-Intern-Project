using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    private bool showAdvancedSettings = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        Board board = (Board)target;

        // Main Actions
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Grid in Scene", GUILayout.Height(35)))
        {
            Undo.RecordObject(board.gameObject, "Generate Grid");
            board.ResetPosition();
            board.GenerateGridInEditor();
            EditorUtility.SetDirty(board.gameObject);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Randomize", GUILayout.Height(25)))
        {
            if (board.Config != null)
            {
                Undo.RecordObject(board.Config, "Randomize Grid");
                board.Config.RandomizeGrid();
                board.GenerateGridInEditor();
                EditorUtility.SetDirty(board.Config);
                SceneView.RepaintAll();
            }
        }

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

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Level Designer Button
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Level Design", EditorStyles.boldLabel);

        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = Color.cyan;

        if (GUILayout.Button("Open Level Designer Window", GUILayout.Height(30)))
        {
            LevelDesignEditor.ShowWindow();
        }

        GUI.backgroundColor = oldBg;

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Advanced Settings
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (board.Config != null)
            {
                EditorGUILayout.LabelField($"Grid Size: {board.Config.columns}x{board.Config.rows}");
                EditorGUILayout.LabelField($"Colors: {board.Config.ColorCount}");
                EditorGUILayout.LabelField($"Cell Size: {board.Config.CellSize}");

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Select Level Config"))
                {
                    Selection.activeObject = board.Config;
                }
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1. Open Level Designer (Ctrl+Shift+L)\n" +
            "2. Select colors and paint cells\n" +
            "3. Save to persist changes\n" +
            "4. Play to test!",
            MessageType.Info
        );
    }
}