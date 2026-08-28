using System.IO;
using NUnit.Framework;

namespace DA_Assets.Shared.MCP.Tests
{
    [TestFixture]
    public sealed class McpConfigEditModeTests
    {
        [Test]
        public void ServerRootPath_UsesConfiguredRelativePathFromAssetsFolder()
        {
            McpConfig config = McpConfig.Instance;
            string assetsPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Project", "Assets"));

            string serverRoot = config.GetServerRootPath(assetsPath);

            Assert.AreEqual("DA-Assets/DA-Mcp-Server/Server", config.ServerRootRelativePath);
            Assert.AreEqual(
                Path.Combine(assetsPath, "DA-Assets", "DA-Mcp-Server", "Server"),
                serverRoot);
        }
    }
}
