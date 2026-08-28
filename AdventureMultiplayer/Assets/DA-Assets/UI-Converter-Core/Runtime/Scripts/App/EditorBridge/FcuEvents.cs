#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using UnityEngine;
using UnityEngine.Events;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    [Serializable]
    public class FcuEvents : FcuBase
    {
        [SerializeField] public UnityEvent<ConverterBase> OnProjectDownloadFail;

        [SerializeField] public UnityEvent<ConverterBase> OnProjectDownloadStart;

        [SerializeField] public UnityEvent<ConverterBase> OnProjectDownloaded;

        [SerializeField] public UnityEvent<ConverterBase> OnImportStart;

        [SerializeField] public UnityEvent<ConverterBase> OnImportComplete;

        [SerializeField] public UnityEvent<ConverterBase> OnImportFail;

        [SerializeField] public UnityEvent<ConverterBase, Node> OnObjectInstantiate;

        [SerializeField] public UnityEvent<ConverterBase, Node, FcuTag> OnAddComponent;
    }
}
#endif