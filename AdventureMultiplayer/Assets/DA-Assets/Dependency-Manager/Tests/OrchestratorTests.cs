using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;

namespace DA_Assets.DM.Tests
{
    public class OrchestratorTests
    {
        [Test]
        public void ProcessSingleItem_KnownPrecompiled_EnablesItem()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "orch_enable", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_ORCH_ENABLE");

            bool changed = DependencyManager.ProcessSingleItem(item);

            Assert.IsTrue(changed);
            Assert.IsTrue(item.IsEnabled);
            Assert.AreEqual(DetectionStrategy.PrecompiledAssembly, item.LastMatchedSource);
            Assert.AreNotEqual(DependencyManager.PathNotFound, item.LastFoundPath);
        }

        [Test]
        public void ProcessSingleItem_UnknownType_DisablesItem()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "orch_disable", "Fake.Type.Name123, Fake.Assembly", "TEST_ORCH_DISABLE");
            item.IsEnabled = true;
            EditorUtility.SetDirty(item);

            bool changed = DependencyManager.ProcessSingleItem(item);

            Assert.IsTrue(changed);
            Assert.IsFalse(item.IsEnabled);
            Assert.AreEqual(DetectionStrategy.None, item.LastMatchedSource);
            Assert.AreEqual(DependencyManager.PathNotFound, item.LastFoundPath);
        }

        [Test]
        public void ProcessSingleItem_DisabledManually_StaysDisabled()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "orch_manual", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_ORCH_MANUAL");
            item.DisabledManually = true;
            EditorUtility.SetDirty(item);

            DependencyManager.ProcessSingleItem(item);

            Assert.IsFalse(item.IsEnabled, "Manual disable must override successful detection.");
        }

        [Test]
        public void ProcessSingleItem_NoChange_ReturnsFalse()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "orch_stable", "Fake.Type.Stable999, Fake.Assembly", "TEST_ORCH_STABLE");

            DependencyManager.ProcessSingleItem(item);
            bool secondPass = DependencyManager.ProcessSingleItem(item);

            Assert.IsFalse(secondPass, "Second pass with identical state must report no change.");
        }

        [Test]
        public void ProcessSingleItem_AsmdefDependency_AddsReferenceToTargetAsmdef()
        {
            string dependencyPath = TestHelper.CreateTempAsmdef("orch_ref_dep", "Test.Dependency");
            string targetPath = TestHelper.CreateTempAsmdef("orch_ref_target", "Test.Target");
            AssemblyDefinitionAsset dependencyAsmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(dependencyPath);
            AssemblyDefinitionAsset targetAsmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(targetPath);
            DependencyItem item = TestHelper.CreateAsmdefItem("orch_ref_add", dependencyAsmdef, "TEST_ORCH_REF_ADD");
            item.ReferencingAsmdef = targetAsmdef;
            EditorUtility.SetDirty(item);

            DependencyManager.ProcessSingleItem(item);

            string dependencyGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
            AsmdefData targetData = AsmdefRepository.LoadData(targetPath);
            CollectionAssert.Contains(targetData.references, "GUID:" + dependencyGuid);
        }

        [Test]
        public void ForceDisableDependency_AsmdefDependency_RemovesReferenceFromTargetAsmdef()
        {
            string dependencyPath = TestHelper.CreateTempAsmdef("orch_ref_disable_dep", "Test.DisableDependency");
            string targetPath = TestHelper.CreateTempAsmdef("orch_ref_disable_target", "Test.DisableTarget");
            AssemblyDefinitionAsset dependencyAsmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(dependencyPath);
            AssemblyDefinitionAsset targetAsmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(targetPath);
            DependencyItem item = TestHelper.CreateAsmdefItem("orch_ref_disable", dependencyAsmdef, "TEST_ORCH_REF_DISABLE");
            item.ReferencingAsmdef = targetAsmdef;
            EditorUtility.SetDirty(item);
            DependencyManager.ProcessSingleItem(item);

            DependencyManager.ForceDisableDependency(item);

            string dependencyGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
            AsmdefData targetData = AsmdefRepository.LoadData(targetPath);
            CollectionAssert.DoesNotContain(targetData.references, "GUID:" + dependencyGuid);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelper.CleanupTempAssets();
        }
    }
}
