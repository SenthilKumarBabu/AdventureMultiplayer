using DA_Assets.UCC;
using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class GetFrameListTool : FcuMcpToolBase
    {
        public GetFrameListTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            string text = FcuMcpReadContent.GetFrameListText(
                monoBeh,
                GetTemplate("empty"),
                GetTemplate("instruction"),
                GetTemplate("hierarchy_prefix"),
                GetTemplate("agent_data_prefix"));

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = text
                }
            };

            return Task.FromResult(response);
        }
    }
}