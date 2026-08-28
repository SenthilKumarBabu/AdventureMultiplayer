#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC
{

    [Flags]
    public enum FontSubset
    {
        Latin = 1 << 0,
        LatinExt = 1 << 1,
        Sinhala = 1 << 2,
        Greek = 1 << 3,
        Hebrew = 1 << 4,
        Vietnamese = 1 << 5,
        Cyrillic = 1 << 6,
        CyrillicExt = 1 << 7,
        Devanagari = 1 << 8,
        Arabic = 1 << 9,
        Khmer = 1 << 10,
        Tamil = 1 << 11,
        GreekExt = 1 << 12,
        Thai = 1 << 13,
        Bengali = 1 << 14,
        Gujarati = 1 << 15,
        [Obsolete("Renamed to Odia per official Unicode/Google Fonts naming. Use Odia instead.")]
        Oriya = 1 << 16,
        Odia = 1 << 16,
        Malayalam = 1 << 17,
        Gurmukhi = 1 << 18,
        Kannada = 1 << 19,
        Telugu = 1 << 20,
        Myanmar = 1 << 21,
        Georgian = 1 << 22,
        Ethiopic = 1 << 23,
        Lao = 1 << 24,
        Tibetan = 1 << 25,
        Japanese = 1 << 26,
        ChineseSimplified = 1 << 27,
        ChineseTraditional = 1 << 28,
        Korean = 1 << 29,
    }
}
#endif