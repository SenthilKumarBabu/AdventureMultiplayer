#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DA_Assets.UCC
{
    public delegate Task DrawByTag(Node fobject, FcuTag tag, Action onDraw);
    public delegate bool GetGameViewSize(out Vector2 size);

    [Serializable]
    public class EditorDelegateHolder : FcuBase
    {
        public Action<LayoutUpdaterInput, Action<LayoutUpdaterOutput>> ShowDifferenceChecker { get; set; }
        public Action<RateLimitWindowData, Action<RateLimitWindowResult>> ShowRateLimitWindow { get; set; }
        public Action<LineHeightAdjusterWindowData, Action<LineHeightAdjusterWindowResult>> ShowLineHeightAdjusterWindow { get; set; }
        public Action<SpriteDuplicateFinderRequest> ShowSpriteDuplicateFinder { get; set; }
        public Func<Vector2, bool> SetGameViewSize { get; set; }
        public Action<Sprite, Vector4> SetSpriteRects { get; set; }
        public Action<Object, ProgressBarCategory, int, bool> StartProgress { get; set; }
        public Action<Object, ProgressBarCategory, int> UpdateProgress { get; set; }
        public Action<Object, ProgressBarCategory> CompleteProgress { get; set; }
        public Action<Object> StopAllProgress { get; set; }
    }
}
#endif