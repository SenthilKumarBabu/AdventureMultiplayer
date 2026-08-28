using DA_Assets.Shared.MCP;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.UCC.MCP
{
    public abstract class FcuMcpResourceBase : IMcpResource
    {
        protected readonly ConverterBase monoBeh;
        protected readonly McpResourceSO resourceSO;

        protected FcuMcpResourceBase(ConverterBase monoBeh, McpResourceSO resourceSO)
        {
            this.monoBeh = monoBeh;
            this.resourceSO = resourceSO;
        }

        protected virtual bool RequiresFcuContext => true;

        public string Uri => resourceSO.ResourceUri;
        public string Name => resourceSO.ResourceName;
        public string Description => resourceSO.ResourceDescription;
        public string MimeType => resourceSO.MimeType;

        public Task<IReadOnlyList<ResourceContentItem>> ReadAsync()
        {
            ConverterBase context = monoBeh;
            if (RequiresFcuContext)
            {
                context ??= FcuMcpContext.GetSelectedOrThrow(resourceSO.InstanceTypeName, resourceSO.InstanceLabel);
            }

            return ReadWithContextAsync(context);
        }

        protected abstract Task<IReadOnlyList<ResourceContentItem>> ReadWithContextAsync(ConverterBase monoBeh);
    }
}