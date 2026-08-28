#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;

namespace DA_Assets.UCC
{
    public struct FrameGroup
    {
        public Node RootFrame { get; set; }
        public List<Node> Childs { get; set; }
    }
}
#endif