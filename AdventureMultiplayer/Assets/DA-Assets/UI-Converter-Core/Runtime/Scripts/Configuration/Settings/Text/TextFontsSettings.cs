#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class TextFontsSettings : FcuBase
    {
        [SerializeField] TextComponent textComponent = TextComponent.UnityEngine_UI_Text;
        public TextComponent TextComponent { get => textComponent; set => textComponent = value; }

        [SerializeField] LineHeightMode lineHeightMode = LineHeightMode.Standard;
        public LineHeightMode LineHeightMode { get => lineHeightMode; set => lineHeightMode = value; }
    }
}
#endif