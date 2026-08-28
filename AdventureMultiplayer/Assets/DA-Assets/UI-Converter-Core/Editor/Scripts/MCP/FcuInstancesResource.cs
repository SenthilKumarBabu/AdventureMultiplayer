using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public class FcuInstancesResource : FcuMcpResourceBase
    {
        public FcuInstancesResource(ConverterBase monoBeh, McpResourceSO resourceSO) : base(monoBeh, resourceSO)
        {
        }

        protected override bool RequiresFcuContext => false;

        protected override Task<IReadOnlyList<ResourceContentItem>> ReadWithContextAsync(ConverterBase monoBeh)
        {
            IReadOnlyList<ResourceContentItem> response = new[]
            {
                new ResourceContentItem
                {
                    Uri = Uri,
                    MimeType = MimeType,
                    Text = FcuMcpReadContent.GetInstancesJson(resourceSO.InstanceTypeName)
                }
            };

            return Task.FromResult(response);
        }
    }
}