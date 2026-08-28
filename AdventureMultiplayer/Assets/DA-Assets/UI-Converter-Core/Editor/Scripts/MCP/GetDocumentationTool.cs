using DA_Assets.Shared;
using DA_Assets.Shared.MCP;
using DA_Assets.FCU.MCP;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class GetDocumentationTool : FcuMcpToolBase
    {
        private readonly DocsGetterWrapper _getter;

        public GetDocumentationTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
            _getter = new DocsGetterWrapper(FcuConfig.DocsBaseUrl, SharedConfig.DocsGetterSettings);
        }

        protected override bool RequiresFcuContext => false;

        protected override async Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            args.TryGetValue("url", out var urlObj);
            var url = Convert.ToString(urlObj, CultureInfo.InvariantCulture);

            string result = await _getter.GetDocumentationAsync(url);

            return new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = result
                }
            };
        }
    }
}