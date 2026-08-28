#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class JoshPuiSettings : BaseImageSettings
    {
        [SerializeField] float falloffDistance = 1f;
        public float FalloffDistance { get => falloffDistance; set => falloffDistance = value; }
    }
}
#endif