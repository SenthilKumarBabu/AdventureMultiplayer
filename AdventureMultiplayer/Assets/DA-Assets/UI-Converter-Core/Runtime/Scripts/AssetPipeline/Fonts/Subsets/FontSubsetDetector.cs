#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    internal static class FontSubsetDetector
    {


        private static readonly Dictionary<FontSubset, HashSet<uint>> _ranges = BuildCache();

        private static Dictionary<FontSubset, HashSet<uint>> BuildCache()
        {
            var dict = new Dictionary<FontSubset, HashSet<uint>>();

            foreach (FontSubset subset in Enum.GetValues(typeof(FontSubset)))
            {
                if (FontSubsetUnicodeRanges.TryGet(subset, out uint[] range))
                    dict[subset] = new HashSet<uint>(range);
            }

            return dict;
        }

        public static Task<HashSet<FontSubset>> DetectFromNodes(List<Node> fobjects)
        {
            Debug.Log(FcuLocKey.log_detecting_font_subsets.Localize());

            return Task.Run(() =>
            {
                var result = new HashSet<FontSubset> { FontSubset.Latin };
                var resultLock = new object();

                Parallel.ForEach(fobjects, fobject =>
                {
                    if (!fobject.ContainsTag(FcuTag.Text))
                        return;

                    string characters = fobject.Characters;

                    if (string.IsNullOrEmpty(characters))
                        return;

                    foreach (char c in characters)
                    {
                        uint codePoint = c;

                        foreach (var pair in _ranges)
                        {
                            lock (resultLock)
                            {
                                if (result.Contains(pair.Key))
                                    continue;

                                if (pair.Value.Contains(codePoint))
                                    result.Add(pair.Key);
                            }
                        }
                    }
                });

                return result;
            });
        }
    }
}
#endif