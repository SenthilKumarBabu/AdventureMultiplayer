using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{

    [CustomEditor(typeof(DependencyItem))]
    public class DependencyItemEditor : Editor
    {
        private SerializedProperty _scriptingDefineSymbol;
        private SerializedProperty _strategy;

        private SerializedProperty _script;
        private SerializedProperty _generatedAsmdefName;
        private SerializedProperty _generatedAsmdefGuid;
        private SerializedProperty _generatedAsmdefReferences;
        private SerializedProperty _generatedEditorAsmdefName;
        private SerializedProperty _generatedEditorAsmdefGuid;
        private SerializedProperty _generatedEditorAsmdefReferences;

        private SerializedProperty _asmdef;
        private SerializedProperty _referencingAsmdef;

        private SerializedProperty _assemblyQualifiedTypeName;
        private SerializedProperty _expectedPathSuffixes;
        private SerializedProperty _unexpectedPathSuffixes;

        private SerializedProperty _upmPackageId;

        private void OnEnable()
        {
            _scriptingDefineSymbol = serializedObject.FindProperty(nameof(DependencyItem.ScriptingDefineSymbol));
            _strategy = serializedObject.FindProperty(nameof(DependencyItem.Strategy));

            _script = serializedObject.FindProperty(nameof(DependencyItem.Script));
            _generatedAsmdefName = serializedObject.FindProperty(nameof(DependencyItem.GeneratedAsmdefName));
            _generatedAsmdefGuid = serializedObject.FindProperty(nameof(DependencyItem.GeneratedAsmdefGuid));
            _generatedAsmdefReferences = serializedObject.FindProperty(nameof(DependencyItem.GeneratedAsmdefReferences));
            _generatedEditorAsmdefName = serializedObject.FindProperty(nameof(DependencyItem.GeneratedEditorAsmdefName));
            _generatedEditorAsmdefGuid = serializedObject.FindProperty(nameof(DependencyItem.GeneratedEditorAsmdefGuid));
            _generatedEditorAsmdefReferences = serializedObject.FindProperty(nameof(DependencyItem.GeneratedEditorAsmdefReferences));

            _asmdef = serializedObject.FindProperty(nameof(DependencyItem.Asmdef));
            _referencingAsmdef = serializedObject.FindProperty(nameof(DependencyItem.ReferencingAsmdef));

            _assemblyQualifiedTypeName = serializedObject.FindProperty(nameof(DependencyItem.AssemblyQualifiedTypeName));
            _expectedPathSuffixes = serializedObject.FindProperty(nameof(DependencyItem.ExpectedPathSuffixes));
            _unexpectedPathSuffixes = serializedObject.FindProperty(nameof(DependencyItem.UnexpectedPathSuffixes));

            _upmPackageId = serializedObject.FindProperty(nameof(DependencyItem.UpmPackageId));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_scriptingDefineSymbol);
            EditorGUILayout.PropertyField(_strategy);
            EditorGUILayout.PropertyField(_referencingAsmdef);

            DependencyItem item = (DependencyItem)target;
            DetectionStrategy strategy = item.Strategy;

            if ((strategy & DetectionStrategy.MonoScript) != 0)
            {
                DrawSection("MonoScript", () =>
                {
                    EditorGUILayout.PropertyField(_script);
                    EditorGUILayout.PropertyField(_generatedAsmdefName);
                    EditorGUILayout.PropertyField(_generatedAsmdefGuid);
                    EditorGUILayout.PropertyField(_generatedAsmdefReferences, includeChildren: true);
                    EditorGUILayout.PropertyField(_generatedEditorAsmdefName);
                    EditorGUILayout.PropertyField(_generatedEditorAsmdefGuid);
                    EditorGUILayout.PropertyField(_generatedEditorAsmdefReferences, includeChildren: true);
                });
            }

            if ((strategy & DetectionStrategy.AssemblyDefinition) != 0)
            {
                DrawSection("Assembly Definition", () =>
                {
                    EditorGUILayout.PropertyField(_asmdef);
                });
            }

            if ((strategy & DetectionStrategy.PrecompiledAssembly) != 0)
            {
                DrawSection("Precompiled Assembly (DLL)", () =>
                {
                    EditorGUILayout.PropertyField(_assemblyQualifiedTypeName);
                    EditorGUILayout.PropertyField(_expectedPathSuffixes, includeChildren: true);
                    EditorGUILayout.PropertyField(_unexpectedPathSuffixes, includeChildren: true);
                });
            }

            if ((strategy & DetectionStrategy.UpmPackage) != 0)
            {
                DrawSection("UPM Package", () =>
                {
                    EditorGUILayout.PropertyField(_upmPackageId);
                });
            }

            serializedObject.ApplyModifiedProperties();

            DrawRuntimeStatus(item);
        }

        private static void DrawSection(string title, System.Action body)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                body();
            }
        }

        private static void DrawRuntimeStatus(DependencyItem item)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime status", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Is Enabled", item.IsEnabled);
                EditorGUILayout.TextField("Last Found Path", string.IsNullOrEmpty(item.LastFoundPath) ? "-" : item.LastFoundPath);
                EditorGUILayout.EnumPopup("Last Matched Source", item.LastMatchedSource);
                EditorGUILayout.Toggle("Disabled Manually", item.DisabledManually);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Check now"))
            {
                DependencyManager.CheckSingleItem(item);
            }
        }
    }
}
