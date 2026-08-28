#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITEXT
using LightSide;
using UniTextStyle = LightSide.Style;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public partial class UniTextDrawer : FcuBase
    {
#if UNITEXT

        private static readonly Type[] managedModifierTypes = new Type[]
        {
            typeof(LineHeightModifier),
            typeof(LetterSpacingModifier),
            typeof(SizeModifier),
            typeof(FontModifier),
            typeof(BoldModifier),
            typeof(ItalicModifier),
            typeof(UppercaseModifier),
            typeof(LowercaseModifier),
            typeof(SmallCapsModifier),
            typeof(ColorModifier),
            typeof(UnderlineModifier),
            typeof(StrikethroughModifier),
            typeof(LinkModifier),
            typeof(EllipsisModifier),
            typeof(TextBoxTrimModifier),
            typeof(ParagraphSpacingModifier),
            typeof(ListModifier),
            typeof(ShadowModifier),
            typeof(OutlineModifier),
            typeof(GradientModifier),
        };

        public UniText Draw(Node fobject)
        {
            fobject.Data.GameObject.TryAddGraphic(out UniText text);

            text.raycastTarget = monoBeh.Settings.UniTextSettings.RaycastTarget;

            UnregisterManagedModifiers(text);
            SetFont(text, fobject);
            SetStyle(text, fobject);

            text.HorizontalAlignment = fobject.GetUniTextHAlign();
            text.VerticalAlignment = fobject.GetUniTextVAlign();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(text);
#endif
            return text;
        }

        private void SetFont(UniText text, Node fobject)
        {
            FontMetadata defaultFont = fobject.Style.GetFontMetadata();
            UniTextFontStack fontStack = ResolveFontStack(fobject);
            UniTextFont primaryFont = ResolvePrimaryFont(defaultFont);

            text.Font = primaryFont;
            text.FontStack = fontStack;
        }

        private void SetStyle(UniText text, Node fobject)
        {
            text.AutoSize = monoBeh.Settings.UniTextSettings.AutoSize;
            text.MinFontSize = 1;
            text.MaxFontSize = fobject.Style.FontSize;
            text.WordWrap = monoBeh.Settings.UniTextSettings.WordWrap;

            text.RenderMode = GetRenderMode(fobject);


            FGraphic graphic = fobject.Data.Graphic;
            Color baseColor = default;

            if (graphic.Fill.HasSolid)
            {
                baseColor = graphic.Fill.SolidPaint.Color;
                text.color = baseColor;
            }
            else if (graphic.Fill.HasGradient)
            {
                List<GradientColorKey> gradientColorKeys = graphic.Fill.GradientPaint.ToGradientColorKeys();

                if (!gradientColorKeys.IsEmpty())
                {
                    baseColor = gradientColorKeys.First().color;
                    text.color = baseColor;
                }
            }

            PopulateVerticalTrim(text, fobject);
            PopulateLineHeightAndLetterSpacing(text, fobject);
            PopulateParagraphSpacing(text, fobject);
            PopulateLists(text, fobject);
            PopulateListSpacing(text, fobject);
            PopulateSize(text, fobject);
            PopulateFontModifier(text, fobject);
            PopulateBold(text, fobject);
            PopulateItalic(text, fobject);
            PopulateTextCase(text, fobject);

            PopulateColor(text, fobject, (Color32)baseColor);
            PopulateUnderline(text, fobject);
            PopulateStrikethrough(text, fobject);
            PopulateLink(text, fobject, (Color32)baseColor);
            PopulateEllipsis(text, fobject);
            PopulateShadow(text, fobject);
            PopulateOutline(text, fobject);
            PopulateGradient(text, fobject);




            string finalText = ApplyTitleCase(fobject.GetText(), fobject.Style.TextCase);
            text.Text = finalText;
            text.FontSize = fobject.Style.FontSize;
        }

        private static void UnregisterManagedModifiers(UniText text)
        {
            var toRemove = new List<UniTextStyle>();

            foreach (var style in text.Styles)
            {
                if (style.Modifier != null && Array.IndexOf(managedModifierTypes, style.Modifier.GetType()) >= 0)
                    toRemove.Add(style);
            }

            foreach (var style in toRemove)
                text.RemoveStyle(style);
        }

        private static RangeRule RegisterRangeRule(UniText text, BaseModifier modifier)
        {
            var rule = new RangeRule();
            var style = new UniTextStyle { Modifier = modifier, Rule = rule };
            text.AddStyle(style);
            return rule;
        }

        private static void RegisterIfHasData(UniText text, BaseModifier modifier, List<RangeRule.Data> data)
        {
            if (data.Count == 0)
                return;

            RegisterRangeRule(text, modifier).data.AddRange(data);
        }

#endif
    }
}
#endif
