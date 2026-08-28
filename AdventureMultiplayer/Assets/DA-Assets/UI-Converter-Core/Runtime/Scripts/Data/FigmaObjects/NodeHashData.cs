#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;

namespace DA_Assets.UCC
{
    public struct NodeHashData
    {
        private int indent;
        private Node fobject;
        private List<FieldHashData> hashes;
        public List<EffectHashData> effectDatas;

        public NodeHashData(Node fobject, List<FieldHashData> hashes, List<EffectHashData> effectDatas, int indent)
        {
            this.fobject = fobject;
            this.hashes = hashes;
            this.indent = indent;
            this.effectDatas = effectDatas;
        }

        public int Indent => indent;
        public Node Node => fobject;
        public List<FieldHashData> FieldHashes => hashes;
        public List<EffectHashData> EffectDatas => effectDatas;
    }
}
#endif