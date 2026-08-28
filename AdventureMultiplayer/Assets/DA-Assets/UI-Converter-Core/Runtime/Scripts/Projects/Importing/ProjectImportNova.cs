#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public class ProjectImportNova : IProjectImportStrategy
    {
        private readonly ConverterBase _monoBeh;

        public UIFramework Framework => UIFramework.NOVA;

        public ProjectImportNova(ConverterBase monoBeh)
        {
            _monoBeh = monoBeh;
        }

        public async Task<List<Node>> ShowLayoutUpdaterWindow(
            SyncHelper[] syncHelpers,
            List<Node> currentPage,
            CancellationToken token)
        {
            return await LayoutUpdaterHelper.ShowLayoutUpdaterWindow(_monoBeh, syncHelpers, currentPage, token);
        }

        public Task LoadPrefabs(CancellationToken token)
        {
            _monoBeh.CurrentProject.LoadLocalPrefabs(token);
            return Task.CompletedTask;
        }

        public Task DrawGameObjects(Node virtualPage, CancellationToken token)
        {
            _monoBeh.CanvasDrawer.GameObjectDrawer.Draw(virtualPage, token);
            return Task.CompletedTask;
        }

        public async Task FinalSteps(Node virtualPage, List<Node> currentPage, CancellationToken token)
        {
#if NOVA_UI_EXISTS
            _monoBeh.NovaDrawer.SetupSpace();
            _monoBeh.gameObject.transform.localScale = Vector3.one;

            await _monoBeh.TransformSetter.SetTransformPos(currentPage);
            await _monoBeh.TransformSetter.RestoreParentsRect(currentPage);
            _monoBeh.TransformSetter.MoveNovaTransforms(currentPage);

            await _monoBeh.TransformSetter.RestoreParents(currentPage);
            await _monoBeh.TransformSetter.RestoreNovaFramePositions(currentPage, token);

            await _monoBeh.NovaDrawer.DrawToScene(currentPage, token);
            _monoBeh.NovaDrawer.EnableScreenSpaceComponent();
#else
            await Task.CompletedTask;
#endif
        }
    }
}
#endif