#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.TextCore;
using TextCoreFontAsset = UnityEngine.TextCore.Text.FontAsset;
using TextCoreCharacter = UnityEngine.TextCore.Text.Character;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC
{
    public static class LineHeightAdjuster
    {
        private const float MetricsTolerance = 0.01f;

        public static void AdjustUitkLineHeight(TextCoreFontAsset fontAsset)
        {
            AdjustUitkLineHeight(fontAsset, LineHeightMode.CapHeightToBaseline);
        }

        public static void AdjustUitkLineHeight(TextCoreFontAsset fontAsset, LineHeightMode mode)
        {
            if (fontAsset == null || mode != LineHeightMode.CapHeightToBaseline)
                return;

            FaceInfo faceInfo = fontAsset.faceInfo;
            AdjustFaceInfo(ref faceInfo, ResolveCapLine(fontAsset, faceInfo, faceInfo.baseline));
            fontAsset.faceInfo = faceInfo;
        }

#if TextMeshPro
        public static void AdjustTmpLineHeight(TMP_FontAsset fontAsset)
        {
            AdjustTmpLineHeight(fontAsset, LineHeightMode.CapHeightToBaseline);
        }

        public static void AdjustTmpLineHeight(TMP_FontAsset fontAsset, LineHeightMode mode)
        {
            if (fontAsset == null || mode != LineHeightMode.CapHeightToBaseline)
                return;

            FaceInfo faceInfo = fontAsset.faceInfo;
            AdjustFaceInfo(ref faceInfo, ResolveCapLine(fontAsset, faceInfo, faceInfo.baseline));
            fontAsset.faceInfo = faceInfo;
        }
#endif

#if UNITY_EDITOR
        public static void ApplyAndSaveUitkLineHeight(TextCoreFontAsset fontAsset)
        {
            ApplyAndSaveUitkLineHeight(fontAsset, LineHeightMode.CapHeightToBaseline);
        }

        public static void ApplyAndSaveUitkLineHeight(TextCoreFontAsset fontAsset, LineHeightMode mode)
        {
            if (fontAsset == null || mode != LineHeightMode.CapHeightToBaseline)
            {
                return;
            }

            AdjustUitkLineHeight(fontAsset, mode);
            SaveAsset(fontAsset);
        }

#if TextMeshPro
        public static void ApplyAndSaveTmpLineHeight(TMP_FontAsset fontAsset)
        {
            ApplyAndSaveTmpLineHeight(fontAsset, LineHeightMode.CapHeightToBaseline);
        }

        public static void ApplyAndSaveTmpLineHeight(TMP_FontAsset fontAsset, LineHeightMode mode)
        {
            if (fontAsset == null || mode != LineHeightMode.CapHeightToBaseline)
            {
                return;
            }

            AdjustTmpLineHeight(fontAsset, mode);
            SaveAsset(fontAsset);
        }
#endif
#endif

        public static bool IsUitkLineHeightAdjusted(TextCoreFontAsset fontAsset, out string details)
        {
            return IsUitkLineHeightAdjusted(fontAsset, LineHeightMode.CapHeightToBaseline, out details);
        }

        public static bool IsUitkLineHeightAdjusted(TextCoreFontAsset fontAsset, LineHeightMode mode, out string details)
        {
            if (mode != LineHeightMode.CapHeightToBaseline)
            {
                details = string.Empty;
                return true;
            }

            return IsAdjusted(
                fontAsset,
                out details,
                x => x.faceInfo,
                (x, faceInfo) => ResolveCapLine(x, faceInfo, faceInfo.baseline));
        }

#if TextMeshPro
        public static bool IsTmpLineHeightAdjusted(TMP_FontAsset fontAsset, out string details)
        {
            return IsTmpLineHeightAdjusted(fontAsset, LineHeightMode.CapHeightToBaseline, out details);
        }

        public static bool IsTmpLineHeightAdjusted(TMP_FontAsset fontAsset, LineHeightMode mode, out string details)
        {
            if (mode != LineHeightMode.CapHeightToBaseline)
            {
                details = string.Empty;
                return true;
            }

            return IsAdjusted(
                fontAsset,
                out details,
                x => x.faceInfo,
                (x, faceInfo) => ResolveCapLine(x, faceInfo, faceInfo.baseline));
        }
#endif

        private static void AdjustFaceInfo(ref FaceInfo faceInfo, float capLine)
        {
            float ascender = faceInfo.ascentLine;
            float descender = faceInfo.descentLine;
            float originalLineHeight = faceInfo.lineHeight;
            float baseline = faceInfo.baseline;

            if (capLine <= baseline || ascender <= descender)
                return;

            faceInfo.capLine = capLine;
            faceInfo.ascentLine = capLine;
            faceInfo.descentLine = baseline;
            faceInfo.lineHeight = ResolveTrimmedLineHeight(ascender, descender, capLine, baseline, originalLineHeight);
        }

        private static bool IsAdjusted<TFontAsset>(
            TFontAsset fontAsset,
            out string details,
            System.Func<TFontAsset, FaceInfo> getFaceInfo,
            System.Func<TFontAsset, FaceInfo, float> resolveCapLine)
            where TFontAsset : UnityEngine.Object
        {
            details = string.Empty;

            if (fontAsset == null)
            {
                details = "Font asset is null.";
                return false;
            }

            FaceInfo faceInfo = getFaceInfo(fontAsset);
            float expectedCapLine = resolveCapLine(fontAsset, faceInfo);

            if (expectedCapLine <= faceInfo.baseline || faceInfo.ascentLine <= faceInfo.descentLine)
            {
                return true;
            }

            float expectedAscent = expectedCapLine;
            float expectedDescent = faceInfo.baseline;
            float expectedLineHeight = ResolveTrimmedLineHeight(
                faceInfo.ascentLine,
                faceInfo.descentLine,
                expectedCapLine,
                faceInfo.baseline,
                faceInfo.lineHeight);

            bool capOk = Approximately(faceInfo.capLine, expectedCapLine);
            bool ascentOk = Approximately(faceInfo.ascentLine, expectedAscent);
            bool descentOk = Approximately(faceInfo.descentLine, expectedDescent);
            bool lineHeightOk = Approximately(faceInfo.lineHeight, expectedLineHeight);

            if (capOk && ascentOk && descentOk && lineHeightOk)
            {
                return true;
            }

            details =
                $"Expected capLine/ascentLine/descentLine/lineHeight = " +
                $"{expectedCapLine:0.##} / {expectedAscent:0.##} / {expectedDescent:0.##} / {expectedLineHeight:0.##}, " +
                $"current = {faceInfo.capLine:0.##} / {faceInfo.ascentLine:0.##} / {faceInfo.descentLine:0.##} / {faceInfo.lineHeight:0.##}. baseline is unchanged.";

            return false;
        }

        private static float ResolveTrimmedLineHeight(
            float ascender,
            float descender,
            float capLine,
            float baseline,
            float originalLineHeight)
        {
            float contentArea = ascender - descender;
            float effectiveLineHeight = originalLineHeight > 0 ? originalLineHeight : contentArea;

            float topTrim = Mathf.Max(0, ascender - capLine);
            float bottomTrim = Mathf.Max(0, baseline - descender);

            float trimmedLineHeight = effectiveLineHeight - topTrim - bottomTrim;
            float minimumLineHeight = Mathf.Max(capLine - baseline, contentArea - topTrim - bottomTrim);

            return Mathf.Max(minimumLineHeight, trimmedLineHeight);
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= MetricsTolerance;
        }

        private static float ResolveCapLine(TextCoreFontAsset fontAsset, FaceInfo faceInfo, float baseline)
        {
            if (faceInfo.capLine > baseline)
            {
                return faceInfo.capLine;
            }

            float glyphCapLine = GetGlyphCapLine(fontAsset, baseline);
            if (glyphCapLine > baseline)
            {
                return glyphCapLine;
            }

            return faceInfo.ascentLine;
        }

#if TextMeshPro
        private static float ResolveCapLine(TMP_FontAsset fontAsset, FaceInfo faceInfo, float baseline)
        {
            if (faceInfo.capLine > baseline)
            {
                return faceInfo.capLine;
            }

            float glyphCapLine = GetGlyphCapLine(fontAsset, baseline);
            if (glyphCapLine > baseline)
            {
                return glyphCapLine;
            }

            return faceInfo.ascentLine;
        }
#endif

        private static float GetGlyphCapLine(TextCoreFontAsset fontAsset, float baseline)
        {
            if (fontAsset.characterLookupTable == null || fontAsset.characterLookupTable.Count == 0)
            {
                return baseline;
            }

            float capLine = baseline;

            foreach (TextCoreCharacter character in fontAsset.characterLookupTable.Values)
            {
                if (character == null || character.glyph == null)
                {
                    continue;
                }

                if (character.unicode <= char.MaxValue && char.IsWhiteSpace((char)character.unicode))
                {
                    continue;
                }

                capLine = Mathf.Max(capLine, character.glyph.metrics.horizontalBearingY);
            }

            return capLine;
        }

#if TextMeshPro
        private static float GetGlyphCapLine(TMP_FontAsset fontAsset, float baseline)
        {
            if (fontAsset.characterLookupTable == null || fontAsset.characterLookupTable.Count == 0)
            {
                return baseline;
            }

            float capLine = baseline;

            foreach (TMP_Character character in fontAsset.characterLookupTable.Values)
            {
                if (character == null || character.glyph == null)
                {
                    continue;
                }

                if (character.unicode <= char.MaxValue && char.IsWhiteSpace((char)character.unicode))
                {
                    continue;
                }

                capLine = Mathf.Max(capLine, character.glyph.metrics.horizontalBearingY);
            }

            return capLine;
        }
#endif

#if UNITY_EDITOR
        private static void SaveAsset(UnityEngine.Object fontAsset)
        {
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
#endif