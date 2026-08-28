using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace DA_Assets.DM.Tests
{
    public class AsmdefGeneratorTests
    {
        private bool _originalDebug;

        [SetUp]
        public void SetUp()
        {
            _originalDebug = DependencyManagerConfig.Instance.Debug;
            DependencyManagerConfig.Instance.Debug = false;
        }

        [Test]
        public void EnsureGenerated_NewFile_Creates()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_new");

            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir,
                "Test.Generated",
                new string[0],
                new string[0],
                "11111111111111111111111111111111");

            Assert.IsNotNull(assetPath);
            Assert.IsTrue(File.Exists(Path.GetFullPath(assetPath)));
            string content = File.ReadAllText(Path.GetFullPath(assetPath));
            Assert.IsTrue(content.Contains("\"name\": \"Test.Generated\""));
        }

        [Test]
        public void EnsureGenerated_SameInputs_Idempotent()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_idempotent");
            string desired = "22222222222222222222222222222222";
            string path1 = AsmdefGenerator.EnsureGenerated(dir, "Test.Idempotent", new string[0], new string[0], desired);
            string contentBefore = File.ReadAllText(Path.GetFullPath(path1));

            string path2 = AsmdefGenerator.EnsureGenerated(dir, "Test.Idempotent", new string[0], new string[0], desired);

            Assert.AreEqual(path1, path2);
            string contentAfter = File.ReadAllText(Path.GetFullPath(path2));
            Assert.AreEqual(contentBefore, contentAfter);
        }

        [Test]
        public void EnsureGenerated_ChangedReferences_Updates()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_change");
            string desired = "33333333333333333333333333333333";
            AsmdefGenerator.EnsureGenerated(dir, "Test.Change", new string[0], new string[0], desired);

            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir, "Test.Change", new[] { "abcdef1234567890abcdef1234567890" }, new string[0], desired);

            string content = File.ReadAllText(Path.GetFullPath(assetPath));
            Assert.IsTrue(content.Contains("GUID:abcdef1234567890abcdef1234567890"));
        }

        [Test]
        public void EnsureGenerated_PackagesPath_SkipsAndWarns()
        {
            DependencyManagerConfig.Instance.Debug = true;

            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex("Test\\.Skipped"));

            string assetPath = AsmdefGenerator.EnsureGenerated(
                "Packages/com.example.fake/Runtime",
                "Test.Skipped",
                new string[0],
                new string[0],
                "55555555555555555555555555555555");

            Assert.IsNull(assetPath);
        }

        [Test]
        public void EnsureGenerated_PackagesPath_WhenDebugDisabled_SkipsWithoutWarning()
        {
            string assetPath = AsmdefGenerator.EnsureGenerated(
                "Packages/com.example.fake/Runtime",
                "Test.SkippedQuiet",
                new string[0],
                new string[0],
                "55555555555555555555555555555555");

            Assert.IsNull(assetPath);
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DeleteByName_Existing_Deletes()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_delete");
            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir,
                "Test.ToDelete",
                new string[0],
                new string[0],
                "44444444444444444444444444444444");
            Assert.IsTrue(File.Exists(Path.GetFullPath(assetPath)));

            bool deleted = AsmdefGenerator.DeleteByName("Test.ToDelete");

            Assert.IsTrue(deleted);
            Assert.IsFalse(File.Exists(Path.GetFullPath(assetPath)));
        }

        [Test]
        public void DeleteByName_Missing_ReturnsFalse()
        {
            bool deleted = AsmdefGenerator.DeleteByName("Test.NeverCreated.Xyz");
            Assert.IsFalse(deleted);
        }

        [Test]
        public void EnsureGenerated_WithDesiredGuid_AssignsStableGuid()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_guid_new");
            string desired = "abcdef0123456789abcdef0123456789";

            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir, "Test.StableGuid", new string[0], new string[0], desired);

            Assert.IsNotNull(assetPath);
            string actual = AssetDatabase.AssetPathToGUID(assetPath);
            Assert.AreEqual(desired, actual,
                "Generated asmdef must adopt the supplied stable GUID.");
        }

        [Test]
        public void EnsureGenerated_ExistingFileWithDifferentGuid_RewritesMeta()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_guid_rewrite");

            string original = "00112233445566778899aabbccddeeff";
            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir, "Test.Rewrite", new string[0], new string[0], original);
            string originalGuid = AssetDatabase.AssetPathToGUID(assetPath);

            string desired = "0123456789abcdef0123456789abcdef";
            AsmdefGenerator.EnsureGenerated(
                dir, "Test.Rewrite", new string[0], new string[0], desired);

            string finalGuid = AssetDatabase.AssetPathToGUID(assetPath);
            Assert.AreNotEqual(originalGuid, finalGuid,
                "Existing asmdef meta GUID should be rewritten when a stable GUID is requested.");
            Assert.AreEqual(desired, finalGuid);
        }

        [Test]
        public void EnsureGenerated_GuidAlreadyMatches_NoOp()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_guid_noop");
            string desired = "fedcba9876543210fedcba9876543210";

            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir, "Test.NoOp", new string[0], new string[0], desired);
            Assert.AreEqual(desired, AssetDatabase.AssetPathToGUID(assetPath));

            AsmdefGenerator.EnsureGenerated(
                dir, "Test.NoOp", new string[0], new string[0], desired);
            Assert.AreEqual(desired, AssetDatabase.AssetPathToGUID(assetPath));
        }

        [Test]
        public void NormalizeMetaGuid_AcceptsValidHex_StripsDashesAndLowercases()
        {
            Assert.AreEqual("abcdef0123456789abcdef0123456789",
                AsmdefGenerator.NormalizeMetaGuid("ABCDEF0123456789ABCDEF0123456789"));
            Assert.AreEqual("abcdef0123456789abcdef0123456789",
                AsmdefGenerator.NormalizeMetaGuid("ABCDEF01-2345-6789-ABCD-EF0123456789"));
        }

        [Test]
        public void NormalizeMetaGuid_NullOrWhitespace_ReturnsNull()
        {
            Assert.IsNull(AsmdefGenerator.NormalizeMetaGuid(null));
            Assert.IsNull(AsmdefGenerator.NormalizeMetaGuid(""));
            Assert.IsNull(AsmdefGenerator.NormalizeMetaGuid("   "));
        }

        [Test]
        public void NormalizeMetaGuid_InvalidFormat_Throws()
        {
            Assert.Throws<System.ArgumentException>(() =>
                AsmdefGenerator.NormalizeMetaGuid("not-a-guid"));
            Assert.Throws<System.ArgumentException>(() =>
                AsmdefGenerator.NormalizeMetaGuid("abc"));
            Assert.Throws<System.ArgumentException>(() =>
                AsmdefGenerator.NormalizeMetaGuid("gggggggggggggggggggggggggggggggg"));
        }

        [Test]
        public void EnsureGenerated_MissingGuid_SkipsAndLogsError()
        {
            string dir = TestHelper.EnsureSubDirectory("gen_guid_missing");
            DependencyManagerConfig.Instance.Debug = true;

            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("stable GUID is required"));

            string assetPath = AsmdefGenerator.EnsureGenerated(
                dir, "Test.MissingGuid", new string[0], new string[0]);

            Assert.IsNull(assetPath);
            Assert.IsFalse(File.Exists(Path.GetFullPath($"{dir}/Test.MissingGuid.asmdef")));
        }

        [TearDown]
        public void TearDown()
        {
            DependencyManagerConfig.Instance.Debug = _originalDebug;
            TestHelper.CleanupTempAssets();
        }
    }
}
