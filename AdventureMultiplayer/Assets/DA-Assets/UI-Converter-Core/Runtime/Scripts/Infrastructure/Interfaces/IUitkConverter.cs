#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public interface IUitkConverter
    {
        void Init(ConverterBase monoBeh);
        Task Convert(Node virtualPage, List<Node> currPage, CancellationToken token);
    }
}
#endif