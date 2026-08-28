using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA_Assets.Shared.MCP
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        InputSchema InputSchema { get; }
        Task<IReadOnlyList<ContentItem>> ExecuteAsync(Dictionary<string, object> args);
    }
}