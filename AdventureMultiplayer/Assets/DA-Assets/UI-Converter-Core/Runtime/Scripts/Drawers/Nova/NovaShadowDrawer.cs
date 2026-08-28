#if UNITY_EDITOR
#if NOVA_UI_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Drawers.CanvasDrawers;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using Nova;
using System;
using System.Linq;

namespace DA_Assets.UCC.Drawers
{
    [Serializable]
    public class NovaShadowDrawer : FcuBase
    {
        public void Draw(Node fobject)
        {
            if (fobject.IsDownloadableType())
                return;

            if (!fobject.Data.GameObject.TryGetComponentSafe(out UIBlock2D uIBlock2D))
                return;

            if (fobject.Effects.IsEmpty())
                return;

            var visibleShadows = fobject.Effects.Where(x => x.IsVisible());

            if (visibleShadows.IsEmpty())
                return;

            var shadowDatas = visibleShadows.Select(x => monoBeh.CanvasDrawer.ShadowDrawer.GetShadowData(x));

            ShadowData highestAlphaShadow = shadowDatas.OrderByDescending(x => x.Color.a).First();
            AddShadow(uIBlock2D, highestAlphaShadow);
        }

        private void AddShadow(UIBlock2D uIBlock2D, ShadowData shadowData)
        {
            Shadow s = new Shadow();
            s.Enabled = true;

            if (shadowData.EffectType == EffectType.DROP_SHADOW)
            {
                s.Direction = ShadowDirection.Out;
            }
            else
            {
                s.Direction = ShadowDirection.In;
            }

            s.Color = shadowData.Color;
            s.Blur = shadowData.Radius;
            s.Width = shadowData.Spread;
            s.Offset = new Length2(shadowData.Offset.x, -shadowData.Offset.y);

            uIBlock2D.Shadow = s;
        }
    }
}
#endif
#endif