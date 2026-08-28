#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace DA_Assets.UCC
{
    public class ProjectImportUITK : IProjectImportStrategy
    {
        private readonly ConverterBase _monoBeh;

        public UIFramework Framework => UIFramework.UITK;

        public ProjectImportUITK(ConverterBase monoBeh)
        {
            _monoBeh = monoBeh;
        }

        public async Task<List<Node>> ShowLayoutUpdaterWindow(
            SyncHelper[] syncHelpers,
            List<Node> currentPage,
            CancellationToken token)
        {
            return currentPage;
        }

        public Task LoadPrefabs(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task DrawGameObjects(Node virtualPage, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public async Task FinalSteps(Node virtualPage, List<Node> currentPage, CancellationToken token)
        {
            await _monoBeh.NameSetter.Set_UITK_Names(currentPage);

            if (_monoBeh.UITK_Converter != null)
            {
                await _monoBeh.UITK_Converter.Convert(virtualPage, currentPage, token);
            }
        }
    }
}
#endif