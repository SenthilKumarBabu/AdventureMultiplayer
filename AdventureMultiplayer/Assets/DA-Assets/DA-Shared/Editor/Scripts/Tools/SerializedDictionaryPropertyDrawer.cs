using DA_Assets.Tools;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace DA_Assets.Shared
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    public class SerializedDictionaryPropertyDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();
        private Dictionary<string, int> cachedrowCount = new Dictionary<string, int>();

        private int GetRowCount(SerializedProperty property)
        {
            var rowCountProp = property.FindPropertyRelative("rowCount");
            return rowCountProp != null ? Mathf.Max(1, rowCountProp.intValue) : 1;
        }

        private ReorderableList GetList(SerializedProperty property)
        {
            string key = property.propertyPath;
            int rowCount = GetRowCount(property);

            if (cachedrowCount.ContainsKey(key) && cachedrowCount[key] != rowCount)
            {
                reorderableLists.Remove(key);
            }
            cachedrowCount[key] = rowCount;

            if (!reorderableLists.ContainsKey(key))
            {
                var keys = property.FindPropertyRelative("m_Keys");
                var values = property.FindPropertyRelative("m_Values");

                if (keys == null || values == null)
                    return null;

                ReorderableList list = new ReorderableList(property.serializedObject, keys, true, true, true, true);

                list.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, property.displayName);
                };

                list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (index >= keys.arraySize || index >= values.arraySize)
                        return;

                    var keyProp = keys.GetArrayElementAtIndex(index);
                    var valueProp = values.GetArrayElementAtIndex(index);

                    float keyWidth = rect.width * 0.3f;
                    float valueWidth = rect.width * 0.7f - 5f;

                    bool isStringKey = keyProp.propertyType == SerializedPropertyType.String;
                    bool isStringValue = valueProp.propertyType == SerializedPropertyType.String;

                    float keyHeight = isStringKey ? EditorGUIUtility.singleLineHeight * rowCount : EditorGUIUtility.singleLineHeight;
                    float valueHeight = isStringValue
                        ? EditorGUIUtility.singleLineHeight * rowCount
                        : EditorGUI.GetPropertyHeight(valueProp, GUIContent.none, true);

                    var keyRect = new Rect(rect.x, rect.y + 2, keyWidth, keyHeight);
                    var valueRect = new Rect(rect.x + keyWidth + 5f, rect.y + 2, valueWidth, valueHeight);

                    if (isStringKey)
                    {
                        keyProp.stringValue = EditorGUI.TextArea(keyRect, keyProp.stringValue);
                    }
                    else
                    {
                        EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                    }

                    if (isStringValue)
                    {
                        valueProp.stringValue = EditorGUI.TextArea(valueRect, valueProp.stringValue);
                    }
                    else
                    {
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                    }
                };

                list.elementHeightCallback = (int index) =>
                {
                    if (index >= keys.arraySize || index >= values.arraySize)
                        return EditorGUIUtility.singleLineHeight + 4f;

                    var keyProp = keys.GetArrayElementAtIndex(index);
                    var valueProp = values.GetArrayElementAtIndex(index);

                    bool isStringKey = keyProp.propertyType == SerializedPropertyType.String;
                    bool isStringValue = valueProp.propertyType == SerializedPropertyType.String;

                    float keyHeight = isStringKey ? EditorGUIUtility.singleLineHeight * rowCount : EditorGUIUtility.singleLineHeight;
                    float valueHeight = isStringValue
                        ? EditorGUIUtility.singleLineHeight * rowCount
                        : EditorGUI.GetPropertyHeight(valueProp, GUIContent.none, true);

                    return Mathf.Max(keyHeight, valueHeight) + 4f;
                };

                list.onAddCallback = (ReorderableList l) =>
                {
                    keys.InsertArrayElementAtIndex(keys.arraySize);
                    values.InsertArrayElementAtIndex(values.arraySize);

                    var newKey = keys.GetArrayElementAtIndex(keys.arraySize - 1);
                    var newValue = values.GetArrayElementAtIndex(values.arraySize - 1);

                    if (newKey.propertyType == SerializedPropertyType.String)
                        newKey.stringValue = "";
                    if (newValue.propertyType == SerializedPropertyType.String)
                        newValue.stringValue = "";

                    property.serializedObject.ApplyModifiedProperties();
                };

                list.onRemoveCallback = (ReorderableList l) =>
                {
                    if (l.index >= 0 && l.index < keys.arraySize && l.index < values.arraySize)
                    {
                        int prevKeySize = keys.arraySize;
                        int prevValSize = values.arraySize;

                        keys.DeleteArrayElementAtIndex(l.index);
                        // Unity requires a second call for reference/object/enum elements
                        // that are first set to null/default before actual removal.
                        if (keys.arraySize == prevKeySize)
                            keys.DeleteArrayElementAtIndex(l.index);

                        values.DeleteArrayElementAtIndex(l.index);
                        if (values.arraySize == prevValSize)
                            values.DeleteArrayElementAtIndex(l.index);

                        property.serializedObject.ApplyModifiedProperties();
                    }
                };


                list.onReorderCallbackWithDetails = (ReorderableList l, int oldIndex, int newIndex) =>
                {
                    values.MoveArrayElement(oldIndex, newIndex);
                    property.serializedObject.ApplyModifiedProperties();
                };

                reorderableLists[key] = list;
            }

            return reorderableLists[key];
        }

        private bool HasStringKeyOrValue(SerializedProperty property)
        {
            var keys = property.FindPropertyRelative("m_Keys");
            var values = property.FindPropertyRelative("m_Values");

            if (keys != null && keys.arraySize > 0)
            {
                var firstKey = keys.GetArrayElementAtIndex(0);
                if (firstKey.propertyType == SerializedPropertyType.String)
                    return true;
            }

            if (values != null && values.arraySize > 0)
            {
                var firstValue = values.GetArrayElementAtIndex(0);
                if (firstValue.propertyType == SerializedPropertyType.String)
                    return true;
            }

            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // When collapsed — only the foldout header line.
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var list = GetList(property);
            if (list != null)
            {
                float height = EditorGUIUtility.singleLineHeight; // foldout header

                if (HasStringKeyOrValue(property))
                    height += EditorGUIUtility.singleLineHeight + 2f; // Row Count field

                height += list.GetHeight();
                return height;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw foldout header.
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            float yOffset = EditorGUIUtility.singleLineHeight;

            var list = GetList(property);
            if (list != null)
            {
                bool showRowCount = HasStringKeyOrValue(property);
                var rowCountProp = property.FindPropertyRelative("rowCount");

                if (showRowCount && rowCountProp != null)
                {
                    float rowCountHeight = EditorGUIUtility.singleLineHeight;
                    Rect rowCountRect = new Rect(position.x, position.y + yOffset, position.width, rowCountHeight);
                    EditorGUI.PropertyField(rowCountRect, rowCountProp, new GUIContent("Row Count"));
                    yOffset += rowCountHeight + 2f;
                }

                Rect listRect = new Rect(position.x, position.y + yOffset, position.width, position.height - yOffset);
                list.DoList(listRect);
            }
            else
            {
                Rect errorRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(errorRect, label.text, "Error: Invalid SerializedDictionary");
            }
        }
    }
}