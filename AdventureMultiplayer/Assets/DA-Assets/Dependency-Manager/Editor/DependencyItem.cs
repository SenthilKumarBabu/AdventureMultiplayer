using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DA_Assets.DM
{

    [CreateAssetMenu(fileName = "New Dependency", menuName = "D.A. Assets/Dependency Item")]
    public class DependencyItem : ScriptableObject
    {

        [Tooltip("Symbol added to PlayerSettings when this dependency is detected.\nExample: TextMeshPro")]
        public string ScriptingDefineSymbol;

        [Tooltip("Sources used to detect this dependency. Multiple flags can be combined (OR).")]
        public DetectionStrategy Strategy;

        [Tooltip("Reference to a .cs file that proves the dependency is present.")]
        public MonoScript Script;

        [Tooltip("If non-empty AND the script has no neighbouring asmdef, DM generates one with this name.\nLeave empty to skip generation.\nExample: DA_Generated.ProceduralUIImage")]
        public string GeneratedAsmdefName;

        [Tooltip("Stable 32-char hex GUID assigned to the generated runtime asmdef's .meta file.\n" +
                 "Required so that other asmdefs (e.g. DA_Assets.FCU) can reference this asmdef by GUID across machines/users.\n" +
                 "Must be authored manually before generation.")]
        public string GeneratedAsmdefGuid;

        [Tooltip("Asmdefs that will be written as references inside the generated asmdef (e.g. Unity.TextMeshPro).")]
        public List<AssemblyDefinitionAsset> GeneratedAsmdefReferences = new List<AssemblyDefinitionAsset>();

        [Tooltip("If non-empty AND a 'Editor' subfolder exists next to the script, DM generates an editor asmdef with this name.")]
        public string GeneratedEditorAsmdefName;

        [Tooltip("Stable 32-char hex GUID assigned to the generated editor asmdef's .meta file.\n" +
                 "Same purpose as GeneratedAsmdefGuid but for the editor asmdef. Must be authored manually before generation.")]
        public string GeneratedEditorAsmdefGuid;

        [Tooltip("Additional asmdef references for the generated editor asmdef (the runtime asmdef is included automatically).")]
        public List<AssemblyDefinitionAsset> GeneratedEditorAsmdefReferences = new List<AssemblyDefinitionAsset>();

        [Tooltip("Asmdef asset that proves the dependency is present.")]
        public AssemblyDefinitionAsset Asmdef;

        [Tooltip("Asmdef that should reference this dependency's asmdef when the dependency is enabled.\nExample: DA_Assets.FCU")]
        public AssemblyDefinitionAsset ReferencingAsmdef;

        [Tooltip("Assembly-qualified type name resolved via Type.GetType.\nExample: TMPro.TextMeshPro, Unity.TextMeshPro")]
        public string AssemblyQualifiedTypeName;

        [Tooltip("If non-empty, the assembly is accepted ONLY when its location ends with one of these suffixes.")]
        public string[] ExpectedPathSuffixes;

        [Tooltip("If non-empty, the assembly is REJECTED when its location ends with one of these suffixes.")]
        public string[] UnexpectedPathSuffixes;

        [Tooltip("UPM package id matched against UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().\nExample: com.unity.nuget.newtonsoft-json")]
        public string UpmPackageId;

        [SerializeField, HideInInspector] private bool isEnabled;
        [SerializeField, HideInInspector] private bool disabledManually;
        [SerializeField, HideInInspector] private string lastFoundPath;
        [SerializeField, HideInInspector] private DetectionStrategy lastMatchedSource;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        public bool DisabledManually { get => disabledManually; set => disabledManually = value; }
        public string LastFoundPath { get => lastFoundPath; set => lastFoundPath = value; }
        public DetectionStrategy LastMatchedSource { get => lastMatchedSource; set => lastMatchedSource = value; }
    }
}
