#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace DA_Assets.UCC
{
    internal static class FontSubsetUnicodeRanges
    {
        private static readonly Dictionary<FontSubset, uint[]> _ranges = new Dictionary<FontSubset, uint[]>
        {
            { FontSubset.Latin,              BuildRange("0000-00FF, 0131, 0152-0153, 02BB-02BC, 02C6, 02DA, 02DC, 0304, 0308, 0329, 2000-206F, 20AC, 2122, 2191, 2193, 2212, 2215, FEFF, FFFD") },
            { FontSubset.LatinExt,           BuildRange("0100-02BA, 02BD-02C5, 02C7-02CC, 02CE-02D7, 02DD-02FF, 0304, 0308, 0329, 1D00-1DBF, 1E00-1E9F, 1EF2-1EFF, 2020, 20A0-20AB, 20AD-20C0, 2113, 2C60-2C7F, A720-A7FF") },
            { FontSubset.Cyrillic,           BuildRange("0301, 0400-045F, 0490-0491, 04B0-04B1, 2116") },
            { FontSubset.CyrillicExt,        BuildRange("0460-052F, 1C80-1C8A, 20B4, 2DE0-2DFF, A640-A69F, FE2E-FE2F") },
            { FontSubset.Greek,              BuildRange("0370-0377, 037A-037F, 0384-038A, 038C, 038E-03A1, 03A3-03FF") },
            { FontSubset.GreekExt,           BuildRange("1F00-1FFF") },
            { FontSubset.Vietnamese,         BuildRange("0102-0103, 0110-0111, 0128-0129, 0168-0169, 01A0-01A1, 01AF-01B0, 0300-0301, 0303-0304, 0308-0309, 0323, 0329, 1EA0-1EF9, 20AB") },
            { FontSubset.Arabic,             BuildRange("0600-06FF, 0750-077F, 08A0-08FF, FB50-FDFF, FE70-FEFF") },
            { FontSubset.Hebrew,             BuildRange("0590-05FF, FB00-FB4F") },
            { FontSubset.Devanagari,         BuildRange("0900-097F, 1CD0-1CF9, 200C-200D, 20A8, 20B9, 20F0, 25CC, A830-A839, A8E0-A8FF, 11B00-11B09") },
            { FontSubset.Bengali,            BuildRange("0951-0952, 0964-0965, 0980-09FE, 1CD0, 1CD2, 1CDA, 1CDB, 1CDE-1CDF, 1CE1, 1CE5-1CE6, 1CE9, 1CED, 1CEF-1CF3, 1CF7, 20B9, 25CC, A8F1") },
            { FontSubset.Gujarati,           BuildRange("0951-0952, 0964-0965, 0A80-0AFF, 200C-200D, 20B9, 25CC, A830-A839") },
            { FontSubset.Gurmukhi,           BuildRange("0900-0902, 093A, 0941-0948, 094D, 0950, 0964-0965, 0A01-0A76, 200C-200D, 20B9, 25CC, A830-A839") },
            { FontSubset.Kannada,            BuildRange("0951-0952, 0964-0965, 0C80-0CFF, 1CD0, 1CD2, 1CDA, 1CDB, 1CDE-1CDF, 1CE1, 1CE5-1CE6, 1CE9, 1CED, 1CEF-1CF3, 1CF7, 20B9, 25CC") },
            { FontSubset.Khmer,              BuildRange("1780-17FF, 19E0-19FF, 200C-200D") },
            { FontSubset.Malayalam,           BuildRange("0951-0952, 0964-0965, 0D00-0D7F, 1CDA, 1CDB, 200C-200D, 20B9, 25CC") },
            { FontSubset.Myanmar,            BuildRange("1000-109F, A92E") },
            { FontSubset.Odia,               BuildRange("0951-0952, 0964-0965, 0B01-0B77, 1CDA, 1CDB, 200C-200D, 20B9, 25CC") },
            { FontSubset.Sinhala,            BuildRange("0964-0965, 0D80-0DFF, 1CF2, 1CF4") },
            { FontSubset.Tamil,              BuildRange("0951-0952, 0964-0965, 0B82-0BFA, 1CDA, 200C-200D, 20B9, 25CC") },
            { FontSubset.Telugu,             BuildRange("0951-0952, 0964-0965, 0C00-0C7F, 1CDA, 1CDB, 200C-200D, 20B9, 25CC") },
            { FontSubset.Thai,               BuildRange("0E01-0E5B, 200C-200D, 2010") },
            { FontSubset.Georgian,           BuildRange("0589, 10A0-10FF, 1C90-1CBA, 1CBD-1CBF, 2D00-2D2F") },
            { FontSubset.Ethiopic,           BuildRange("1200-137F, 1380-139F, 2D80-2DDF, AB00-AB2F") },
            { FontSubset.Lao,                BuildRange("0E81-0EFF") },
            { FontSubset.Tibetan,            BuildRange("0F00-0FFF") },
            { FontSubset.Japanese,           BuildRange("3000-303F, 3040-309F, 30A0-30FF, 3400-4DBF, 4E00-9FFF, F900-FAFF, FF00-FFEF") },
            { FontSubset.ChineseSimplified,  BuildRange("0020-007E, 00A0-00A9, 00AB-00AE, 00B2-00B3, 00B5, 00B7-00B8, 00BA-00BB, 00BC-00BE, 00D7, 00F7, 2000-206F, 2070-209F, 20A0-20CF, 2100-214F, 3000-303F, 3400-4DBF, 4E00-9FFF, F900-FAFF, FE30-FE4F, FF00-FFEF") },
            { FontSubset.ChineseTraditional, BuildRange("0020-007E, 00A0-00FF, 2000-206F, 2070-209F, 20A0-20CF, 2100-214F, 2C60-2C7F, 3000-303F, 3400-4DBF, 4E00-9FFF, F900-FAFF, FE10-FE1F, FE30-FE4F, FF00-FFEF") },
            { FontSubset.Korean,             BuildRange("0020-007E, 00A0-00A9, 00AB-00AE, 00B2-00B3, 00B5, 00B7-00B8, 00BA-00BB, 00BC-00BE, 00D7, 00F7, 02C7, 02C9, 02CA, 02CB, 02D0, 2000-206F, 20A9, 2103, 2109, 2116, 2121, 212B, 2160-2183, 2190-2199, 25A0-25FF, 2600-26FF, 3000-303F, 3131-318E, 3200-32FF, AC00-D7AF") },
        };

        public static bool TryGet(FontSubset subset, out uint[] unicodes)
        {
            return _ranges.TryGetValue(subset, out unicodes);
        }

        private static uint[] BuildRange(string spec)
        {
            List<uint> result = new List<uint>();

            foreach (string part in spec.Split(','))
            {
                string t = part.Trim();

                if (string.IsNullOrWhiteSpace(t))
                    continue;

                int dashIndex = t.IndexOf('-');

                if (dashIndex >= 0)
                {
                    uint start = Convert.ToUInt32(t.Substring(0, dashIndex), 16);
                    uint end = Convert.ToUInt32(t.Substring(dashIndex + 1), 16);

                    for (uint c = start; c <= end; c++)
                        result.Add(c);
                }
                else
                {
                    result.Add(Convert.ToUInt32(t, 16));
                }
            }

            return result.ToArray();
        }
    }
}
#endif