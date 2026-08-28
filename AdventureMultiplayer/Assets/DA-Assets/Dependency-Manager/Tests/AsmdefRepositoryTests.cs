using NUnit.Framework;

namespace DA_Assets.DM.Tests
{
    public class AsmdefRepositoryTests
    {
        [Test]
        public void FindNearestAsmdef_InSameDir_ReturnsPath()
        {
            string asmdefPath = TestHelper.CreateTempAsmdef("nearest_same", "NearestSameTest");
            string scriptPath = $"{TestHelper.TempRoot}/nearest_same/SomeScript.cs";

            string result = AsmdefRepository.FindNearestAsmdef(scriptPath);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.EndsWith("NearestSameTest.asmdef"));
        }

        [Test]
        public void FindNearestAsmdef_InParentDir_ReturnsPath()
        {
            TestHelper.CreateTempAsmdef("nearest_parent", "NearestParentTest");
            string childDir = TestHelper.EnsureSubDirectory("nearest_parent/child");
            string scriptPath = $"{childDir}/SomeScript.cs";

            string result = AsmdefRepository.FindNearestAsmdef(scriptPath);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.EndsWith("NearestParentTest.asmdef"));
        }

        [Test]
        public void FindNearestAsmdef_NoAsmdef_ReturnsNull()
        {
            string dir = TestHelper.EnsureSubDirectory("nearest_none");
            string scriptPath = $"{dir}/SomeScript.cs";

            string result = AsmdefRepository.FindNearestAsmdef(scriptPath);

            Assert.IsNull(result);
        }

        [Test]
        public void FindNearestAsmdef_NullPath_ReturnsNull()
        {
            Assert.IsNull(AsmdefRepository.FindNearestAsmdef(null));
            Assert.IsNull(AsmdefRepository.FindNearestAsmdef(""));
        }

        [TearDown]
        public void TearDown()
        {
            TestHelper.CleanupTempAssets();
        }
    }
}
