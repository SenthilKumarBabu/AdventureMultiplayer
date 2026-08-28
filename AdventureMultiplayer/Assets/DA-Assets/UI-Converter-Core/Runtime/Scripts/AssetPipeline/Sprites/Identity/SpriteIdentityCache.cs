#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DA_Assets.UCC
{
    public sealed class SpriteIdentityCache
    {

        private readonly Dictionary<Node, int> _renderKeys;


        private readonly Dictionary<int, List<Node>> _byRenderKey;


        private readonly List<Node> _uniqueRepresentatives;


        private Dictionary<int, string> _existingPathByKey;

        internal SpriteIdentityCache(
            Dictionary<Node, int> renderKeys,
            Dictionary<int, List<Node>> byRenderKey,
            List<Node> uniqueRepresentatives)
        {
            _renderKeys = renderKeys;
            _byRenderKey = byRenderKey;
            _uniqueRepresentatives = uniqueRepresentatives;
        }

        public int GetRenderKey(Node fobject) =>
            _renderKeys.TryGetValue(fobject, out int k) ? k : 0;

        public IReadOnlyList<Node> UniqueRepresentatives => _uniqueRepresentatives;

        public IReadOnlyList<Node> GetGroup(int renderKey) =>
            _byRenderKey.TryGetValue(renderKey, out var list) ? list : Array.Empty<Node>() as IReadOnlyList<Node>;

        internal void BuildExistingSpritePathLookup(string[] assetSpritePaths)
        {
            _existingPathByKey = new Dictionary<int, string>();

            foreach (string spritePath in assetSpritePaths)
            {
                if (!GuidMetaUtility.TryExtractData(spritePath + ".meta", out int hash))
                    continue;

                if (!_existingPathByKey.ContainsKey(hash))
                    _existingPathByKey[hash] = spritePath;
            }
        }

        internal bool TryGetExistingPath(int renderKey, out string spritePath)
        {
            if (_existingPathByKey != null && _existingPathByKey.TryGetValue(renderKey, out spritePath))
                return true;

            spritePath = null;
            return false;
        }
    }
}
#endif