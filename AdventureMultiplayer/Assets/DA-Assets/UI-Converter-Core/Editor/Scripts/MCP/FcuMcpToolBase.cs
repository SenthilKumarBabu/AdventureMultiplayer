using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public abstract class FcuMcpToolBase : IMcpTool
    {
        protected readonly ConverterBase monoBeh;
        protected readonly McpToolSO toolSO;

        protected FcuMcpToolBase(ConverterBase monoBeh, McpToolSO toolSO)
        {
            this.monoBeh = monoBeh;
            this.toolSO = toolSO;
        }

        protected virtual bool RequiresFcuContext => true;

        public string Name => toolSO.ToolName;
        public string Description => toolSO.ToolDescription;
        public InputSchema InputSchema => toolSO.Schema;

        public Task<IReadOnlyList<ContentItem>> ExecuteAsync(Dictionary<string, object> args)
        {
            args ??= new Dictionary<string, object>();

            ConverterBase context = monoBeh;
            if (RequiresFcuContext)
            {
                context ??= FcuMcpContext.GetSelectedOrThrow(toolSO.InstanceTypeName, toolSO.InstanceLabel);
            }

            return ExecuteWithContextAsync(context, args);
        }

        protected abstract Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args);

        protected string GetTemplate(string key) => toolSO.GetTemplate(key);

        protected string FormatTemplate(string key, params object[] args) => toolSO.FormatTemplate(key, args);
    }
}