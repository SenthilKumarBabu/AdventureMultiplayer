#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class UnityTextDrawer : FcuBase
    {
        public Text Draw(Node fobject)
        {
            fobject.Data.GameObject.TryAddGraphic(out Text text);

            SetStyle(text, fobject);
            SetFont(text, fobject);

            text.fontStyle = fobject.GetFontWeight();
            text.alignment = fobject.GetTextAnchor();

            return text;
        }

        private void SetFont(Text text, Node fobject)
        {
            Font font = monoBeh.FontLoader.GetFontFromArray(fobject, monoBeh.FontLoader.TtfFonts);
            text.font = font;
        }

        private void SetStyle(Text text, Node fobject)
        {
            text.resizeTextForBestFit = monoBeh.Settings.UnityTextSettings.BestFit;
            text.text = fobject.GetText();
            text.resizeTextMinSize = 1;

            text.resizeTextMaxSize = Convert.ToInt32(fobject.Style.FontSize);
            text.fontSize = Convert.ToInt32(fobject.Style.FontSize);

            text.verticalOverflow = monoBeh.Settings.UnityTextSettings.VerticalWrapMode;
            text.horizontalOverflow = monoBeh.Settings.UnityTextSettings.HorizontalWrapMode;
            text.lineSpacing = monoBeh.Settings.UnityTextSettings.FontLineSpacing;

            FGraphic graphic = fobject.Data.Graphic;

            if (graphic.Fill.HasSolid)
            {
                text.color = graphic.Fill.SolidPaint.Color;
            }
            else if (graphic.Fill.HasGradient)
            {
                List<GradientColorKey> gradientColorKeys = graphic.Fill.GradientPaint.ToGradientColorKeys();

                if (!gradientColorKeys.IsEmpty())
                {
                    text.color = gradientColorKeys.First().color;
                }
            }
        }
    }
}
#endif