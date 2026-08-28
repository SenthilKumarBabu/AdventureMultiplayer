using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.Shared.MCP
{
    public interface IMcpResource
    {
        string Uri { get; }
        string Name { get; }
        string Description { get; }
        string MimeType { get; }
        Task<IReadOnlyList<ResourceContentItem>> ReadAsync();
    }
}
