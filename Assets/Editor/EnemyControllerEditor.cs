using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomEditor(typeof(enemyController))]
public class EnemyControllerEditor : Editor
{
    SerializedProperty enemyTypeProperty;
    SerializedProperty moveSetProperty;
    ReorderableList reorderableList;

    void OnEnable() {
        enemyTypeProperty = serializedObject.FindProperty("enemyType");
        moveSetProperty = serializedObject.FindProperty("moveSet");

        reorderableList = new ReorderableList(serializedObject, moveSetProperty, true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Move Set");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);

            string currentMove = element.stringValue;

            var moves = ((enemyController)target).GetMovesForEnemyType(((enemyController)target).enemyType);
            int currentIndex = Mathf.Max(0, moves.IndexOf(currentMove));

            currentIndex = EditorGUI.Popup(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                currentIndex,
                moves.ToArray());

            if (currentIndex >= 0 && currentIndex < moves.Count) {
                element.stringValue = moves[currentIndex];
            }
        };

        reorderableList.onAddCallback = (ReorderableList list) => {
            moveSetProperty.InsertArrayElementAtIndex(moveSetProperty.arraySize);
        };

        reorderableList.onRemoveCallback = (ReorderableList list) => {
            moveSetProperty.DeleteArrayElementAtIndex(list.index);
        };
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.PropertyField(enemyTypeProperty);

        enemyController enemyController = (enemyController)target;
        var moves = enemyController.GetMovesForEnemyType(enemyController.enemyType);

        reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
