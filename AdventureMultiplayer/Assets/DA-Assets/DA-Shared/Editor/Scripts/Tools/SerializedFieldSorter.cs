using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace DA_Assets.Tools
{
    public class SerializedFieldSorter
    {
        /// <summary>
        /// Draws all <see cref="SerializedProperty"/> fields of the given <paramref name="serializedObject"/>
        /// grouped alphabetically by their referenced object type.
        /// Properties whose referenced objects share the same type are collapsed under a foldout;
        /// single-instance properties are drawn directly without a group header.
        /// The <paramref name="foldoutStates"/> dictionary is updated in-place to persist expand/collapse state across repaints.
        /// </summary>
        /// <param name="serializedObject">The serialized object whose properties are rendered.</param>
        /// <param name="foldoutStates">Persistent foldout states keyed by type name. Modified in-place.</param>
        public static void DrawSorted(SerializedObject serializedObject, Dictionary<string, bool> foldoutStates)
        {
            serializedObject.Update();

            Dictionary<string, List<SerializedProperty>> groupedProperties = new Dictionary<string, List<SerializedProperty>>();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script")
                    continue;

                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                {
                    Type propType = property.objectReferenceValue.GetType();
                    string typeName = propType.Name;

                    if (!groupedProperties.ContainsKey(typeName))
                    {
                        groupedProperties[typeName] = new List<SerializedProperty>();
                    }

                    groupedProperties[typeName].Add(property.Copy());
                }
            }

            foreach (var group in groupedProperties.OrderBy(g => g.Key))
            {
                string typeName = group.Key;
                List<SerializedProperty> properties = group.Value;

                if (properties.Count > 1)
                {
                    if (!foldoutStates.ContainsKey(typeName))
                    {
                        foldoutStates[typeName] = false;
                    }

                    foldoutStates[typeName] = EditorGUILayout.Foldout(foldoutStates[typeName], $"{typeName}s");

                    if (foldoutStates[typeName])
                    {
                        EditorGUI.indentLevel++;
                        foreach (var prop in properties)
                        {
                            EditorGUILayout.PropertyField(prop, true);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    SerializedProperty singleProp = properties.First();
                    EditorGUILayout.PropertyField(singleProp, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
