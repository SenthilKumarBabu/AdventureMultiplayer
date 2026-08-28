using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class DestroySyncHelpersTool : FcuMcpToolBase
    {
        public DestroySyncHelpersTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override async Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            int destroyedCount = await monoBeh.SyncHelpers.DestroySyncHelpersAsync();

            return new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = FormatTemplate("success", destroyedCount, monoBeh.GetInstanceID())
                }
            };
        }
    }
}