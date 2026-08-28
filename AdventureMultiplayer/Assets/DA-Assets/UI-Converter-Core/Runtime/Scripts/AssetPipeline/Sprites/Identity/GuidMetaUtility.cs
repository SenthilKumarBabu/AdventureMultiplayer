#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class GuidMetaUtility
    {
        public static void WriteGuid(string metaPath, int primaryKey)
        {
            Rewrite(metaPath, IntFloatGuid.Encode(primaryKey).Value);
        }

        public static bool TryExtractData(string metaPath, out int hash)
        {
            hash = 0;

            if (!File.Exists(metaPath))
            {
                return false;
            }

            foreach (string line in File.ReadAllLines(metaPath))
            {
                if (!line.TrimStart().StartsWith("guid:"))
                    continue;

                int colon = line.IndexOf(':');
                if (colon < 0)
                    return false;

                string tail = line.Substring(colon + 1).Trim();
                Guid guid;
                if (!Guid.TryParse(tail, out guid))
                {
                    return false;
                }

                hash = IntFloatGuid.Decode(guid);
                return true;
            }

            return false;
        }

        private static void Rewrite(string metaPath, Guid guid)
        {
            try
            {
                string[] lines = File.ReadAllLines(metaPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("guid:"))
                    {
                        lines[i] = $"guid: {guid:N}";
                        break;
                    }
                }

                File.WriteAllLines(metaPath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    internal static class SpriteRenderKeyUtility
    {
        public static int GetSpriteRenderKey(Node fobject)
        {
            if (ReferenceEquals(fobject, null) || fobject.Data == null)
                return 0;

            if (fobject.IsGenerativeType())
                return GetGenerativeSpriteRenderKey(fobject);

            if (!fobject.IsDownloadableType())
                return fobject.Data.RenderHash;

            if (fobject.IsRotationInvariantSolidCircle() ||
                fobject.IsScaleInvariantAutoSlice9CircleSource())
                return fobject.Data.RenderHash;

            float absoluteMatrixAngle = GetAbsoluteMatrixAngle(fobject);
            float roundedAngle = (float)Math.Round(absoluteMatrixAngle, FcuConfig.Rounding.Rotation);

            string key = $"{fobject.Data.RenderHash}|abs-matrix-angle:{roundedAngle}";
            return key.GetDeterministicHashCode();
        }

        internal static int GetGenerativeSpriteRenderKey(Node fobject)
        {
            Vector2 sourceSize = GetSpriteSourceSize(fobject);
            string key = FormattableString.Invariant($"{fobject.Data.RenderHash}|src:{sourceSize.x:F1}x{sourceSize.y:F1}");
            return key.GetDeterministicHashCode();
        }

        public static bool MatchesPackedGuid(int renderKey, int hash)
        {
            return hash == renderKey;
        }

        internal static Vector2 GetSpriteSourceSize(Node fobject)
        {
            fobject.GetBoundingSize(out Vector2 boundingSize);
            fobject.GetRenderSize(out Vector2 renderSize);

            float width = Mathf.Max(boundingSize.x, renderSize.x, fobject.Size.x);
            float height = Mathf.Max(boundingSize.y, renderSize.y, fobject.Size.y);

            return new Vector2(width, height);
        }

        private static float GetAbsoluteMatrixAngle(Node fobject)
        {
            float total = 0f;
            Node current = fobject;

            while (current.Data != null)
            {
                total += current.GetAngleFromMatrix();
                current = current.Data.Parent;
            }

            return total;
        }
    }
}
#endif