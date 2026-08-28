#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class TextDrawer : FcuBase
    {
        [SerializeField] List<Node> texts = new List<Node>();
        public List<Node> Texts => texts;

        public override void Init(ConverterBase monoBeh)
        {
            base.Init(monoBeh);

            UnityTextDrawer.Init(monoBeh);
            TextMeshDrawer.Init(monoBeh);
            UniTextDrawer.Init(monoBeh);
        }

        public void ClearTexts()
        {
            texts.Clear();
        }

        public void Draw(Node fobject)
        {
            if (fobject.Data.GameObject.IsPartOfAnyPrefab() == false)
            {
                if (fobject.Data.GameObject.TryGetComponentSafe(out Graphic oldGraphic))
                {
                    Type curType = monoBeh.GetCurrentTextType();

                    if (oldGraphic.GetType().Equals(curType) == false)
                    {
                        oldGraphic.Destroy();
                    }
                }
            }

            if (monoBeh.IsNova())
            {
                this.TextMeshDrawer.DrawNovaTMP(fobject);
            }
            else
            {
                switch (monoBeh.Settings.TextFontsSettings.TextComponent)
                {
                    case TextComponent.TextMeshPro:
                        this.TextMeshDrawer.DrawTMP(fobject);
                        break;
                    case TextComponent.RTL_TextMeshPro:
                        this.TextMeshDrawer.DrawRTL(fobject);
                        break;
                    case TextComponent.UnityEngine_UI_Text:
                        this.UnityTextDrawer.Draw(fobject);
                        break;
#if UNITEXT
                    case TextComponent.UniText:
                        this.UniTextDrawer.Draw(fobject);
                        break;
#endif
                }
            }

            texts.Add(fobject);
        }

        [SerializeField] public UnityTextDrawer UnityTextDrawer = new UnityTextDrawer();
        [SerializeField] public TextMeshDrawer TextMeshDrawer = new TextMeshDrawer();
        [SerializeField] public UniTextDrawer UniTextDrawer = new UniTextDrawer();
    }
}
#endif