#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;

namespace DA_Assets.UCC
{
    public struct ScriptData
    {
        public Node Node { get; set; }
        public Type ComponentType { get; set; }

        public ScriptData(Node fobject, Type type)
        {
            this.Node = fobject;
            this.ComponentType = type;
        }
    }
}
#endif