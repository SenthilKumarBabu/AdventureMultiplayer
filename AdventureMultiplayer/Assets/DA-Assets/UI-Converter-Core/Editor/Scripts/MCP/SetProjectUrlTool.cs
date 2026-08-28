using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class SetProjectUrlTool : FcuMcpToolBase
    {
        public SetProjectUrlTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            args.TryGetValue("project_url", out var projectUrlObj);

            var projectUrl = Convert.ToString(projectUrlObj, CultureInfo.InvariantCulture) ?? string.Empty;

            monoBeh.Settings.MainSettings.ProjectUrl = projectUrl;
            bool success = monoBeh.Settings.MainSettings.TryGetFigmaFileId(projectUrl, out _);

            var responseMessage = success
                ? FormatTemplate("success", monoBeh.Settings.MainSettings.ProjectUrl)
                : GetTemplate("error");


            if (string.IsNullOrEmpty(responseMessage) && !success)
            {
                responseMessage = FcuLocKey.log_incorrent_project_url.Localize();
            }

            IReadOnlyList<ContentItem> response = new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = responseMessage
                }
            };

            return Task.FromResult(response);
        }
    }
}