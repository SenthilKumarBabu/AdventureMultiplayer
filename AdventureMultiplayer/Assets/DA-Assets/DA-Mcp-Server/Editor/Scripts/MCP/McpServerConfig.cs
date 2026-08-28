using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    [CreateAssetMenu(fileName = "McpServerConfig", menuName = "D.A. Assets/MCP/Server Config")]
    public class McpServerConfig : ScriptableObject
    {
        [Header("Server Identity")]
        public string DisplayName;

        [Tooltip("URL path segment, e.g. 'fcu'. Used ONLY in endpoint URL: /{EndpointPath}/mcp")]
        public string EndpointPath;

        [TextArea(4, 12)]
        public string Instructions;

        [Header("Serialized MCP Declarations")]
        public List<McpToolSO> McpTools = new();
        public List<McpResourceSO> McpResources = new();

        [SerializeField] bool _debug;

        public bool DebugLogging => _debug;
    }
}
