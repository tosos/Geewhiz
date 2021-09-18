#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

[CustomEditor(typeof(Pooler))]
public class PoolerEditor : Editor
{
    private ReorderableList list;
    private SerializedProperty minPoolIdsProp;
    private SerializedProperty dontSaveProp;
    private SerializedProperty poolablePrefabs;

    private void OnEnable()
    {
        minPoolIdsProp = serializedObject.FindProperty("minPooledIds");
        dontSaveProp = serializedObject.FindProperty("dontSaveSet");
        poolablePrefabs = serializedObject.FindProperty("poolablePrefabs");
        list =
            new ReorderableList(serializedObject, poolablePrefabs,
                                true, true, true, true);
        if (dontSaveProp.arraySize != poolablePrefabs.arraySize)
        {
            dontSaveProp.arraySize = poolablePrefabs.arraySize;
        }
        list.drawHeaderCallback = (Rect rect) =>
        {
            GUIStyle style1 = new GUIStyle();
            style1.normal.textColor = Color.white;
            style1.alignment = TextAnchor.MiddleLeft;
            EditorGUI.LabelField(rect, "Poolables", style1);

            GUIStyle style2 = new GUIStyle();
            style2.alignment = TextAnchor.MiddleRight;
            style2.normal.textColor = Color.white;
            EditorGUI.LabelField(rect, "Dont Save?", style2);
        };
        list.onAddCallback = (ReorderableList list) =>
        {
            poolablePrefabs.arraySize++;
            dontSaveProp.arraySize++;
        };
        list.onRemoveCallback = (ReorderableList list) =>
        {
            poolablePrefabs.arraySize--;
            dontSaveProp.arraySize--;
        };
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = poolablePrefabs.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width - 25, EditorGUIUtility.singleLineHeight), element,
                GUIContent.none);
            var boolel = dontSaveProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(
                new Rect(rect.x + rect.width - 20, rect.y, 20, EditorGUIUtility.singleLineHeight), boolel,
                GUIContent.none);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(minPoolIdsProp);
        list.DoLayoutList();
        // EditorGUILayout.PropertyField(dontSaveProp, true);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
