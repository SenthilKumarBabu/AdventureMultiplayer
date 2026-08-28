using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class ListFcuInstancesTool : IMcpTool
    {
        private readonly McpToolSO _toolSO;

        public ListFcuInstancesTool(McpToolSO toolSO)
        {
            _toolSO = toolSO;
        }

        public string Name => _toolSO.ToolName;
        public string Description => _toolSO.ToolDescription;
        public InputSchema InputSchema => _toolSO.Schema;

        public Task<IReadOnlyList<ContentItem>> ExecuteAsync(Dictionary<string, object> args)
        {
            string json = FcuMcpReadContent.GetInstancesJson(_toolSO.InstanceTypeName);

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = json
                }
            };

            return Task.FromResult(response);
        }

    }
}