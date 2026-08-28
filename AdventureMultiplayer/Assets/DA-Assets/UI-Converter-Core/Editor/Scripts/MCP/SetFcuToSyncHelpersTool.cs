using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class SetFcuToSyncHelpersTool : FcuMcpToolBase
    {
        public SetFcuToSyncHelpersTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override async Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            int syncHelpersCount = monoBeh.SyncHelpers.GetAllSyncHelpers().Length;

            await monoBeh.SyncHelpers.SetFcuToAllSyncHelpersAsync(CancellationToken.None);

            return new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = FormatTemplate("success", syncHelpersCount, monoBeh.GetInstanceID())
                }
            };
        }
    }
}