#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReasonGroupAttribute : Attribute
    {
        public string Group { get; }
        public ReasonGroupAttribute(string group) => Group = group;
    }
}
#endif