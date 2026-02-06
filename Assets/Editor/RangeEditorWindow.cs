using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class RangeEditorWindow : EditorWindow
{
    private int range = 0;
    private bool[,] grid;

    [MenuItem("Window/Range Editor")]
    public static void ShowWindow()
    {
        GetWindow<RangeEditorWindow>("Range Editor");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Attack Range Editor", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        range = EditorGUILayout.IntField("Range (tiles from player)", range);
        range = Mathf.Max(0, range);

        if (EditorGUI.EndChangeCheck())
        {
            CreateGrid();
        }

        EditorGUILayout.Space();
        DrawGrid();
        EditorGUILayout.Space();

        if (GUILayout.Button("Save"))
        {
            SaveGridToJson();

            range = 0;
            CreateGrid();
            Repaint();
        }
    }

    private void CreateGrid()
    {
        int size = range * 2 + 1;
        grid = new bool[size, size];
    }

    private void DrawGrid()
    {
        if (grid == null) return;

        int size = range * 2 + 1;
        int center = range;
        Color defaultColor = GUI.backgroundColor;

        for (int y = 0; y < size; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < size; x++)
            {
                bool isCenter = x == center && y == center;
                if (isCenter)
                {
                    GUI.backgroundColor = Color.cyan;
                    EditorGUI.BeginDisabledGroup(true);
                    grid[x, y] = true;
                }

                grid[x, y] = GUILayout.Toggle(grid[x, y], GUIContent.none, GUILayout.Width(20), GUILayout.Height(20));

                if (isCenter)
                {
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = defaultColor;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // Serializable class for saving coordinates
    [System.Serializable]
    private class GridData
    {
        public List<Vector2Int> tiles = new List<Vector2Int>();
    }

    private void SaveGridToJson()
    {
        if (grid == null) return;

        int size = range * 2 + 1;
        int center = range;

        GridData data = new GridData();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (grid[x, y])
                {
                    int relX = x - center;
                    int relY = y - center;
                    data.tiles.Add(new Vector2Int(relX, relY));
                }
            }
        }

        string json = JsonUtility.ToJson(data, true);

        string folderPath = Path.Combine(Application.dataPath, "Ability Range Templates");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        int fileIndex = 1;
        string filePath;
        do
        {
            filePath = Path.Combine(folderPath, $"grid_{fileIndex}.json");
            fileIndex++;
        } while (File.Exists(filePath));

        File.WriteAllText(filePath, json);
        Debug.Log($"Grid saved to {filePath}");
    }
}
