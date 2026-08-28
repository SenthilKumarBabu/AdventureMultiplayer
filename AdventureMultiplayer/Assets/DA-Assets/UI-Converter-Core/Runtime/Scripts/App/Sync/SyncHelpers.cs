#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class SyncHelpers : FcuBase
    {
        public void DestroySyncHelpers()
        {
            _ = DestroySyncHelpersAsync();
        }

        public async Task<int> DestroySyncHelpersAsync()
        {
            SyncHelper[] syncHelpers = GetAllSyncHelpers();

            for (int i = 0; i < syncHelpers.Length; i++)
            {
                syncHelpers[i].Destroy();
                await Task.Yield();
            }

            Debug.Log(FcuLocKey.log_current_canvas_metas_destroy.Localize(
                monoBeh.GetInstanceID(),
                syncHelpers.Length,
                nameof(SyncHelper)));

            return syncHelpers.Length;
        }

        public SyncHelper[] GetAllSyncHelpers()
        {
            List<SyncHelper> syncHelpers = new List<SyncHelper>();
            AddOwnedSyncHelpers(monoBeh.transform, true, syncHelpers);
            return syncHelpers.ToArray();
        }

        private static void AddOwnedSyncHelpers(Transform current, bool isRoot, List<SyncHelper> syncHelpers)
        {
            if (!isRoot && current.GetComponent<ConverterBase>() != null)
                return;

            for (int i = current.childCount - 1; i >= 0; i--)
            {
                AddOwnedSyncHelpers(current.GetChild(i), false, syncHelpers);
            }

            if (current.TryGetComponent(out SyncHelper syncHelper))
                syncHelpers.Add(syncHelper);
        }

        public bool IsExistsOnCurrentCanvas(Node fobject, out SyncHelper syncObject)
        {
            SyncHelper[] syncHelpers = GetAllSyncHelpers();

            foreach (SyncHelper sh in syncHelpers)
            {
                if (sh.Data.Id == fobject.Id)
                {
                    syncObject = sh;
                    return true;
                }
            }

            syncObject = null;
            return false;
        }

        public SyncData GetRootFrame(SyncData syncData)
        {
            GameObject currentGameObject = syncData.GameObject;

            while (currentGameObject != null)
            {
                SyncHelper syncHelper = currentGameObject.GetComponent<SyncHelper>();
                if (syncHelper != null && syncHelper.ContainsTag(FcuTag.Frame))
                {
                    return syncHelper.Data;
                }

                currentGameObject = currentGameObject.transform.parent?.gameObject;
            }

            return null;
        }

        public void RestoreRootFrames(SyncHelper[] syncHelpers)
        {
            List<SyncHelper> frames = new List<SyncHelper>();
            foreach (SyncHelper syncHelper in syncHelpers)
            {
                if (syncHelper.ContainsTag(FcuTag.Frame))
                {
                    frames.Add(syncHelper);
                }
            }

            foreach (SyncHelper frame in frames)
            {
                SetRootFrameToAllChilds(frame.gameObject, frame.Data);
            }
        }

        public void SetRootFrameToAllChilds(GameObject @object, SyncData rootFrame)
        {
            if (@object == null)
                return;

            foreach (Transform child in @object.transform)
            {
                if (child == null)
                    continue;

                if (child.TryGetComponentSafe(out SyncHelper syncObject))
                {
                    syncObject.Data.RootFrame = rootFrame;
                }

                SetRootFrameToAllChilds(child.gameObject, rootFrame);
            }
        }

        private CancellationTokenSource _on_click_set_fcu_to_all_sync_helpers_cts;

        public void SetFcuToAllSyncHelpers()
        {
            _on_click_set_fcu_to_all_sync_helpers_cts?.Cancel();
            _on_click_set_fcu_to_all_sync_helpers_cts = new CancellationTokenSource();

            _ = SetFcuToAllSyncHelpersAsync(_on_click_set_fcu_to_all_sync_helpers_cts.Token);
        }

        public async Task SetFcuToAllSyncHelpersAsync(CancellationToken token)
        {
            int counter = 0;
            SetFcuToAllChilds(monoBeh.gameObject, ref counter, token);

            await Task.Delay(100, token);

            Debug.Log(FcuLocKey.log_fcu_assigned.Localize(
                counter,
                nameof(ConverterBase),
                monoBeh.GetInstanceID()));
        }

        public void SetFcuToAllChilds(GameObject @object, ref int counter, CancellationToken token)
        {
            if (@object == null)
                return;

            foreach (Transform child in @object.transform)
            {
                token.ThrowIfCancellationRequested();

                if (child == null)
                    continue;

                if (child.GetComponent<ConverterBase>() != null)
                    continue;

                if (child.TryGetComponentSafe(out SyncHelper syncObject))
                {
                    counter++;
                    syncObject.Data.ConverterBase = monoBeh;
                }

                SetFcuToAllChilds(child.gameObject, ref counter, token);
            }
        }
    }
}
#endif
