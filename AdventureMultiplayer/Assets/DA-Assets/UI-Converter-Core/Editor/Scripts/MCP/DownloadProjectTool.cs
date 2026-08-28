using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class DownloadProjectTool : FcuMcpToolBase
    {
        public DownloadProjectTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override async Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            ImportResult result = await monoBeh.ProjectDownloader.DownloadProject();

            string message = result.Status == ImportStatus.ProjectDownloadSuccess
                ? FormatTemplate("success", result.Message)
                : FormatTemplate("error", result.Message);

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = message
                }
            };

            return response;
        }
    }
}