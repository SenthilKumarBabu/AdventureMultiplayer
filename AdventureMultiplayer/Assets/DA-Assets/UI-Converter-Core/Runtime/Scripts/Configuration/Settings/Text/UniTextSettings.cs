#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class UniTextSettings : FcuBase
    {
        [SerializeField] bool autoSize = true;
        public bool AutoSize { get => autoSize; set => autoSize = value; }

        [SerializeField] bool wordWrap = true;
        public bool WordWrap { get => wordWrap; set => wordWrap = value; }

        [SerializeField] bool raycastTarget = false;
        public bool RaycastTarget { get => raycastTarget; set => raycastTarget = value; }
    }
}
#endif