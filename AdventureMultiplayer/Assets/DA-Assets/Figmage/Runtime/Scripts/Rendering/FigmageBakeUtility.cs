using System.IO;
using UnityEngine;

namespace DA_Assets.Figmage
{
    public static class FigmageBakeUtility
    {
        public static byte[] BakePngBytes(FigmageNode node, float scale = 1f, Rect? canvasBoundsOverride = null)
        {
            Rect canvasBounds = canvasBoundsOverride ?? FigmageBoundsUtility.CalculateRenderBounds(node);
            using (FigmageRenderer renderer = new FigmageRenderer())
            {
                return renderer.RenderToPngBytes(node, scale, string.Empty, canvasBounds);
            }
        }

        public static void BakePng(FigmageNode node, string outputPath, float scale = 1f, Rect? canvasBoundsOverride = null)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            Rect canvasBounds = canvasBoundsOverride ?? FigmageBoundsUtility.CalculateRenderBounds(node);
            using (FigmageRenderer renderer = new FigmageRenderer())
            {
                renderer.RenderToPng(node, scale, outputPath, canvasBounds);
            }
        }
    }
}
