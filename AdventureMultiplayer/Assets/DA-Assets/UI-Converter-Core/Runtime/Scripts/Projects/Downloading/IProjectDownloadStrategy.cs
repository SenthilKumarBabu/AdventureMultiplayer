#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public interface IProjectDownloadStrategy
    {
        ImportMode Mode { get; }

        Task DownloadProjectAsync(CancellationToken token);

        Task<List<Node>> DownloadAllNodes(string[] selectedIds, CancellationToken token);
    }
}
#endif