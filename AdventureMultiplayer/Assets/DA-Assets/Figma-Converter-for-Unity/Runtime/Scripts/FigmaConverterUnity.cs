#if UNITY_EDITOR
using DA_Assets.UCC;
using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    [AddComponentMenu("D.A. Assets/Figma Converter for Unity")]
    [DisallowMultipleComponent]
    public sealed class FigmaConverterUnity : ConverterBase
    {
        public override IConvConfig Config => FcuConfig.Instance;

        public AuthSettings AuthSettings = new AuthSettings();
        public Authorizer Authorizer = new Authorizer();
        public override IAuthProvider AuthProvider => Authorizer;
    }
}
#endif
