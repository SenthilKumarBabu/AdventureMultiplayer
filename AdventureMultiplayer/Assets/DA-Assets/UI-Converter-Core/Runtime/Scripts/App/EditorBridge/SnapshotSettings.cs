#if UNITY_EDITOR
using System;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    [Serializable]
    public class SnapshotSettings : FcuBase
    {
        [SerializeField] public Transform RootFrame;
        [SerializeField] public string SelectedBaseline;
        [SerializeField] public string FigmaResponseLogPath;
    }
}
#endif