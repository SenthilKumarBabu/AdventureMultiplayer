using DA_Assets.Constants;
using DA_Assets.Singleton;
using System.IO;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/MCP/Config")]
    [ResourcePath("")]
    public sealed class McpConfig : AssetConfig<McpConfig>
    {
        [SerializeField] string serverRootRelativePath = "DA-Assets/DA-Mcp-Server/Server";
        [SerializeField] McpRegistrySO mcpRegistry;
        [SerializeField] bool showServerConsoleWindow = true;

        public string ServerRootRelativePath => serverRootRelativePath;
        public McpRegistrySO McpRegistry => mcpRegistry;
        public bool ShowServerConsoleWindow => showServerConsoleWindow;

        public string GetServerRootPath(string assetsRootPath)
        {
            return Path.GetFullPath(Path.Combine(assetsRootPath, serverRootRelativePath));
        }
    }
}
