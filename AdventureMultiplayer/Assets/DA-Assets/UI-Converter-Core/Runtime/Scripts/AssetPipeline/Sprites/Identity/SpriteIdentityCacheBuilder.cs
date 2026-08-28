#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class SpriteIdentityCacheBuilder
    {
        public static SpriteIdentityCache Build(List<Node> fobjects)
        {


            Dictionary<Node, float> absoluteAngleCache = new Dictionary<Node, float>(fobjects.Count);

            foreach (Node fo in fobjects)
            {
                float parentAngle = 0f;

                if (fo.Data?.Parent != null && absoluteAngleCache.TryGetValue(fo.Data.Parent, out float p))
                    parentAngle = p;

                float localAngle = fo.Data != null && fo.Data.HasTransformComputationCache
                    ? fo.Data.CachedMatrixAngle
                    : fo.GetAngleFromMatrix();
                absoluteAngleCache[fo] = parentAngle + localAngle;
            }



            Dictionary<Node, int> renderKeys = new Dictionary<Node, int>(fobjects.Count);
            Dictionary<int, List<Node>> byRenderKey = new Dictionary<int, List<Node>>();
            List<Node> uniqueRepresentatives = new List<Node>();
            Dictionary<int, int> representativeIndexByRenderKey = new Dictionary<int, int>();

            foreach (Node fo in fobjects)
            {
                if (!(fo.IsDownloadableType() || fo.IsGenerativeType()))
                    continue;

                int key = ComputeRenderKey(fo, absoluteAngleCache);
                renderKeys[fo] = key;

                if (!byRenderKey.TryGetValue(key, out List<Node> group))
                {
                    group = new List<Node>();
                    byRenderKey[key] = group;
                    representativeIndexByRenderKey[key] = uniqueRepresentatives.Count;
                    uniqueRepresentatives.Add(fo);
                }
                else if (IsLargerSpriteSource(fo, uniqueRepresentatives[representativeIndexByRenderKey[key]]))
                {
                    uniqueRepresentatives[representativeIndexByRenderKey[key]] = fo;
                }

                group.Add(fo);
            }

            return new SpriteIdentityCache(renderKeys, byRenderKey, uniqueRepresentatives);
        }

        private static int ComputeRenderKey(Node fobject, Dictionary<Node, float> absoluteAngleCache)
        {
            if (ReferenceEquals(fobject, null) || fobject.Data == null)
                return 0;

            if (fobject.IsGenerativeType())
                return SpriteRenderKeyUtility.GetGenerativeSpriteRenderKey(fobject);

            if (!fobject.IsDownloadableType())
                return fobject.Data.RenderHash;

            if (fobject.IsRotationInvariantSolidCircle() ||
                fobject.IsScaleInvariantAutoSlice9CircleSource())
                return fobject.Data.RenderHash;

            absoluteAngleCache.TryGetValue(fobject, out float absoluteMatrixAngle);
            float roundedAngle = (float)Math.Round(absoluteMatrixAngle, FcuConfig.Rounding.Rotation);

            string key = $"{fobject.Data.RenderHash}|abs-matrix-angle:{roundedAngle}";
            return key.GetDeterministicHashCode();
        }

        private static bool IsLargerSpriteSource(Node candidate, Node current)
        {
            Vector2 candidateSize = SpriteRenderKeyUtility.GetSpriteSourceSize(candidate);
            Vector2 currentSize = SpriteRenderKeyUtility.GetSpriteSourceSize(current);

            float candidateArea = candidateSize.x * candidateSize.y;
            float currentArea = currentSize.x * currentSize.y;

            if (!Mathf.Approximately(candidateArea, currentArea))
                return candidateArea > currentArea;

            return Mathf.Max(candidateSize.x, candidateSize.y) > Mathf.Max(currentSize.x, currentSize.y);
        }
    }
}
#endif