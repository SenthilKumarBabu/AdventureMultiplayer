#if UNITY_EDITOR
using System;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public struct TagConfig
    {
        [SerializeField] string label;
        [SerializeField] FcuTag fcuTag;
        [SerializeField] string figmaTag;
        [SerializeField] bool customButtonTag;
        [SerializeField] bool canBeDownloaded;
        [SerializeField] bool canBeInsideSingleImage;
        [SerializeField] bool hasComponent;

        public FcuTag FcuTag => fcuTag;
        public string FigmaTag => figmaTag;
        public bool CustomButtonTag => customButtonTag;
        public bool CanBeDownloaded => canBeDownloaded;
        public bool CanBeInsideSingleImage => canBeInsideSingleImage;
        public bool HasComponent => hasComponent;

        public void SetDefaultData(FcuTag fcuTag)
        {
            this.label = fcuTag.ToString();
            this.fcuTag = fcuTag;
        }
    }
}
#endif