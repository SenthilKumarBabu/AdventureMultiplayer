#if UNITY_EDITOR
using DA_Assets.DAI;
using System;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class NovaSettings : FcuBase
    {
        [SerializeField] public Texture InputTexture;
    }
}
#endif