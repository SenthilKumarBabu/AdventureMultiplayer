#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
    public class NodeAttribute : Attribute
    {
        public string Name { get; }

        public NodeAttribute(string name)
        {
            Name = name;
        }
    }
}
#endif