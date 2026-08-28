#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;

#if UNITEXT
using LightSide;
using Style = DA_Assets.UCC.Model.Style;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateShadow(UniText text, Node fobject)
        {
            if (fobject.Effects == null)
                return;

            foreach (Effect effect in fobject.Effects)
            {
                if (effect.Type != EffectType.DROP_SHADOW && effect.Type != EffectType.INNER_SHADOW)
                    continue;

                if (effect.Visible.HasValue && !effect.Visible.Value)
                    continue;

                float dilate = effect.Spread ?? 0f;
                UnityEngine.Color32 color = effect.Color;
                float offsetX = effect.Offset.x;

                float offsetY = -effect.Offset.y;
                float softness = effect.Radius;
                string hex = Color32ToHex(color);
                string parameter = FormattableString.Invariant(
                    $"{dilate},{hex},{offsetX},{offsetY},{softness}");

                RegisterRangeRule(text, new ShadowModifier { FixedPixelSize = true })
                    .data.Add(new RangeRule.Data { range = string.Empty, parameter = parameter });


                break;
            }
        }
#endif
    }
}
#endif