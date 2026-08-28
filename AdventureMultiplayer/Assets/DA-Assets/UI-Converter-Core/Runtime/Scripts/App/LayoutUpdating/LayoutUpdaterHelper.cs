#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class LayoutUpdaterHelper
    {
        public static async Task<List<Node>> ShowLayoutUpdaterWindow(
            ConverterBase monoBeh,
            SyncHelper[] syncHelpers,
            List<Node> currentPage,
            CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_import_show_difference_checker.Localize());

            monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

            LayoutUpdaterInput lui = await monoBeh.LayoutUpdateDataCreator
                .Create(currentPage, syncHelpers.ToList(), token);

            LayoutUpdaterOutput luo = default;

            await monoBeh.AssetTools.ReselectFcu(token);
            monoBeh.EditorDelegateHolder.ShowDifferenceChecker(lui, _ => luo = _);

            while (luo.IsDefault())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1000, token);
            }

            List<Node> tempPage = new List<Node>();

            foreach (string item in luo.ToImport)
            {
                token.ThrowIfCancellationRequested();

                foreach (Node fobject in currentPage)
                {
                    token.ThrowIfCancellationRequested();

                    if (item == fobject.Id)
                    {
                        tempPage.Add(fobject);
                    }
                }
            }

            await monoBeh.CanvasDrawer.GameObjectDrawer.DestroyMissing(luo.ToRemove, token);

            return tempPage;
        }
    }
}
#endif