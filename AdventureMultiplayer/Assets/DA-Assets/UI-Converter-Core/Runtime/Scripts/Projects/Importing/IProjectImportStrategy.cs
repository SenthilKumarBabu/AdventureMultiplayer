#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public interface IProjectImportStrategy
    {
        UIFramework Framework { get; }

        Task<List<Node>> ShowLayoutUpdaterWindow(
            SyncHelper[] syncHelpers,
            List<Node> currentPage,
            CancellationToken token);

        Task LoadPrefabs(CancellationToken token);

        Task DrawGameObjects(Node virtualPage, CancellationToken token);

        Task FinalSteps(Node virtualPage, List<Node> currentPage, CancellationToken token);
    }
}
#endif