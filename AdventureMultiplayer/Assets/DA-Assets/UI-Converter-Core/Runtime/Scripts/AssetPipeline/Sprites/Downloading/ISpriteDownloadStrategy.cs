#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public interface ISpriteDownloadStrategy
    {
        ImportMode Mode { get; }

        Task DownloadSprites(List<Node> fobjects, CancellationToken token);
    }
}
#endif