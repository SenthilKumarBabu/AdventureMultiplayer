#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Logging;
using System;
using UnityEngine;

#pragma warning disable CS0162

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class ShadowSettings : FcuBase
    {

        [SerializeField] ShadowComponent shadowComponent = ShadowComponent.Baked;
        public ShadowComponent ShadowComponent
        {
            get => shadowComponent;
            set => shadowComponent = value;
        }

    }
}
#endif