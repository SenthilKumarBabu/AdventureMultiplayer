using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class FcuFrameListResource : FcuMcpResourceBase
    {
        public FcuFrameListResource(ConverterBase monoBeh, McpResourceSO resourceSO) : base(monoBeh, resourceSO)
        {
        }

        protected override Task<IReadOnlyList<ResourceContentItem>> ReadWithContextAsync(ConverterBase monoBeh)
        {
            string text = FcuMcpReadContent.GetFrameListText(
                monoBeh,
                "No selectable frames found. Download the project first.",
                "Frame selection data. Use select_frames with page IDs or frame keys from the table.",
                "Hierarchy:",
                "Agent data:");

            IReadOnlyList<ResourceContentItem> response = new[]
            {
                new ResourceContentItem
                {
                    Uri = Uri,
                    MimeType = MimeType,
                    Text = text
                }
            };

            return Task.FromResult(response);
        }
    }
}