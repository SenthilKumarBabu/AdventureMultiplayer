#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Figmage;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class FcuFigmageSpriteBaker
    {
        public static byte[] BakePngBytes(Node fobject, float scale)
        {
            return BakePngBytes(fobject, scale, null);
        }

        public static byte[] BakePngBytes(Node fobject, float scale, Rect? canvasBoundsOverride)
        {
            FigmageNode node = FcuFigmageNodeMapper.ToFigmageNode(fobject);
            return FigmageBakeUtility.BakePngBytes(node, scale, canvasBoundsOverride);
        }

        public static void BakePng(Node fobject, float scale, string outputPath, Rect? canvasBoundsOverride = null)
        {
            FigmageNode node = FcuFigmageNodeMapper.ToFigmageNode(fobject);
            FigmageBakeUtility.BakePng(node, outputPath, scale, canvasBoundsOverride);
        }
    }
}
#endif