#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class CurrentProject : FcuBase
    {
        public string GetInstanceKey(string id)
        {
            foreach (var item in this.FigmaProject.Components)
            {
                if (item.Key == id)
                {
                    return item.Value.Key;
                }
            }

            return null;
        }

        public bool TryGetById(string id, out Node fobject)
        {
            foreach (var item in this.CurrentPage)
            {
                if (item.Id == id)
                {
                    fobject = item;
                    return true;
                }
            }

            fobject = default;
            return false;
        }

        public bool TryGetByIndex(int index, out Node fobject)
        {
            if (index < 0)
            {
                fobject = default;
                return false;
            }

            fobject = this.CurrentPage[index];
            return true;
        }

        public bool TryGetParent(Node fobject, out Node parent)
        {
            if (fobject.Data.ParentIndex < 0)
            {
                parent = default;
                return false;
            }

            parent = this.CurrentPage[fobject.Data.ParentIndex];
            return true;
        }

        public Node GetRootFrame(Node fobject)
        {

            if (fobject.ContainsTag(FcuTag.Frame))
            {
                return fobject;
            }

            TryGetParent(fobject, out Node parent);


            return GetRootFrame(parent);
        }



        public bool HasLocalPrefab(SyncData fobject, out SyncHelper localPrefab)
        {
            foreach (SyncHelper lp in localPrefabs)
            {
                if (lp.Data.ObjectHash == fobject.ObjectHash)
                {
                    localPrefab = lp;
                    return true;
                }
            }

            foreach (SyncHelper lp in localPrefabs)
            {
                if (lp.Data.Id == fobject.Id && lp.Data.Names.FileName == fobject.Names.FileName)
                {
                    localPrefab = lp;
                    return true;
                }
            }

            localPrefab = null;
            return false;
        }

        public void LoadLocalPrefabs(CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_search_local_prefabs.Localize());
            localPrefabs = LoadAssetFromFolder<SyncHelper>(monoBeh.Settings.PrefabSettings.PrefabsPath, "t:Prefab", token);
            Debug.Log(FcuLocKey.log_local_prefabs_found.Localize(localPrefabs.Count));
        }

        public List<T> LoadAssetFromFolder<T>(string fontsPath, string customType, CancellationToken token) where T : UnityEngine.Object
        {
            List<string> pathes = new List<string>();
            List<T> loadedAssets = new List<T>();

            if (customType == null)
                customType = $"t:{typeof(T).Name}";

            token.ThrowIfCancellationRequested();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets(customType, new string[] { fontsPath.ToRelativePath() });

            foreach (string guid in guids)
            {
                token.ThrowIfCancellationRequested();

                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null)
                {
                    loadedAssets.Add(asset);
                }
            }
#endif

            return loadedAssets;
        }

        public void SetRootFrames(List<Node> fobjects, CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_set_root_frames.Localize());
            for (int i = 0; i < fobjects.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                if (fobjects[i].ContainsTag(FcuTag.Page))
                    continue;

                Node rootFrameNode = monoBeh.CurrentProject.GetRootFrame(fobjects[i]);
                fobjects[i].Data.RootFrame = rootFrameNode.Data;
            }
        }

        [SerializeField] List<SyncHelper> localPrefabs = new List<SyncHelper>();

        [SerializeField] string projectName;
        public string ProjectName { get => projectName; set => projectName = value; }

        public DesignProject FigmaProject { get; set; }

        public ZipProjectData ZipData { get; set; }

        public List<Node> CurrentPage { get; set; } = new List<Node>();
    }
}
#endif