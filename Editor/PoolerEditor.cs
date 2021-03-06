﻿#if UNITY_EDITOR
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

    private void OnEnable()
    {
        minPoolIdsProp = serializedObject.FindProperty("minPooledIds");
        dontSaveProp = serializedObject.FindProperty("dontSaveSet");
        list =
            new ReorderableList(serializedObject, serializedObject.FindProperty("poolablePrefabs"),
                                true, true, true, true);
        list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element,
                GUIContent.none);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(minPoolIdsProp);
        list.DoLayoutList();
        EditorGUILayout.PropertyField(dontSaveProp, true);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
