using NUnit.Framework;
using UnityEditor;
using UnityEditorInternal;

namespace DA_Assets.DM.Tests
{
    public class ResolverTests
    {

        [Test]
        public void PrecompiledAssembly_KnownType_ReturnsFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "precompiled_known", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_PRECOMPILED_KNOWN");

            ResolutionResult r = new PrecompiledAssemblyResolver().Resolve(item);

            Assert.IsTrue(r.Found);
            Assert.AreEqual(DetectionStrategy.PrecompiledAssembly, r.MatchedSource);
        }

        [Test]
        public void PrecompiledAssembly_UnknownType_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "precompiled_unknown", "Fake.NonExistent.Type12345, FakeAssembly", "TEST_PRECOMPILED_UNKNOWN");

            ResolutionResult r = new PrecompiledAssemblyResolver().Resolve(item);

            Assert.IsFalse(r.Found);
            Assert.AreEqual(DetectionStrategy.None, r.MatchedSource);
        }

        [Test]
        public void PrecompiledAssembly_EmptyTypeName_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "precompiled_empty", "", "TEST_PRECOMPILED_EMPTY");

            ResolutionResult r = new PrecompiledAssemblyResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void PrecompiledAssembly_UnexpectedPathSuffix_Rejects()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "precompiled_unexpected", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_PRECOMPILED_REJECT",
                unexpected: new[] { "UnityEngine.UI.dll" });

            ResolutionResult r = new PrecompiledAssemblyResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void AssemblyDefinition_NullAsmdef_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreateAsmdefItem("asmdef_null", null, "TEST_ASMDEF_NULL");

            ResolutionResult r = new AssemblyDefinitionResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void AssemblyDefinition_ExistingAsmdef_ReturnsFound()
        {
            string asmdefPath = TestHelper.CreateTempAsmdef("asmdef_existing", "Test.Existing");
            AssemblyDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmdefPath);
            Assert.IsNotNull(asset, "Asmdef asset must load.");

            DependencyItem item = TestHelper.CreateAsmdefItem("asmdef_existing_item", asset, "TEST_ASMDEF_EXISTING");

            ResolutionResult r = new AssemblyDefinitionResolver().Resolve(item);

            Assert.IsTrue(r.Found);
            Assert.AreEqual(DetectionStrategy.AssemblyDefinition, r.MatchedSource);
            Assert.IsTrue(r.DisplayPath.EndsWith(".asmdef"));
        }

        [Test]
        public void MonoScript_Null_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreateAsmdefItem("monoscript_null", null, "TEST_MONO_NULL");
            item.Strategy = DetectionStrategy.MonoScript;
            item.Script = null;

            ResolutionResult r = new MonoScriptResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void UpmPackage_KnownPackage_ReturnsFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "upm_known", "ignored", "TEST_UPM_KNOWN");
            item.Strategy = DetectionStrategy.UpmPackage;
            item.UpmPackageId = "com.unity.test-framework";

            ResolutionResult r = new UpmPackageResolver().Resolve(item);

            Assert.IsTrue(r.Found);
            Assert.AreEqual(DetectionStrategy.UpmPackage, r.MatchedSource);
        }

        [Test]
        public void UpmPackage_UnknownPackage_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "upm_unknown", "ignored", "TEST_UPM_UNKNOWN");
            item.Strategy = DetectionStrategy.UpmPackage;
            item.UpmPackageId = "com.fake.nonexistent.package.xyz";

            ResolutionResult r = new UpmPackageResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void UpmPackage_EmptyId_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "upm_empty", "ignored", "TEST_UPM_EMPTY");
            item.Strategy = DetectionStrategy.UpmPackage;
            item.UpmPackageId = "";

            ResolutionResult r = new UpmPackageResolver().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [Test]
        public void Resolution_FlagsCombination_FirstMatchWins()
        {

            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "combined_match", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_COMBINED");
            item.Strategy = DetectionStrategy.PrecompiledAssembly | DetectionStrategy.UpmPackage;
            item.UpmPackageId = "com.unity.test-framework";

            ResolutionResult r = new DependencyResolutionService().Resolve(item);

            Assert.IsTrue(r.Found);

            Assert.AreEqual(DetectionStrategy.PrecompiledAssembly, r.MatchedSource);
        }

        [Test]
        public void Resolution_NoFlags_ReturnsNotFound()
        {
            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "no_flags", "UnityEngine.UI.Image, UnityEngine.UI", "TEST_NOFLAGS");
            item.Strategy = DetectionStrategy.None;

            ResolutionResult r = new DependencyResolutionService().Resolve(item);

            Assert.IsFalse(r.Found);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelper.CleanupTempAssets();
        }
    }
}
