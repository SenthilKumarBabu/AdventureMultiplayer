#if UNITY_EDITOR
using UnityEngine;

namespace DA_Assets.UCC
{
    public struct RectOffsetCustom
    {
        public int left;

        public int right;

        public int top;

        public int bottom;

        public RectOffsetCustom(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public RectOffset ToRectOffset()
        {
            return new RectOffset(left, right, top, bottom);
        }

        public override string ToString()
        {
            return $"RectOffsetCustom(Left: {left}, Right: {right}, Top: {top}, Bottom: {bottom})";
        }
    }
}
#endif