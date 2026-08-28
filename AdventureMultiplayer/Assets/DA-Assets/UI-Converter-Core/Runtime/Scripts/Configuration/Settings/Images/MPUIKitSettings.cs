#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class MPUIKitSettings : BaseImageSettings
    {
        [SerializeField] float falloffDistance = 0.5f;
        public float FalloffDistance { get => falloffDistance; set => falloffDistance = value; }
    }
}
#endif