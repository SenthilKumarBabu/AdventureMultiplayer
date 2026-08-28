#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public struct ReasonEntry : IEquatable<ReasonEntry>
    {
        public ReasonKey key;
        public FcuTag relatedTag;

        public ReasonEntry(ReasonKey key, FcuTag relatedTag = FcuTag.None)
        {
            this.key = key;
            this.relatedTag = relatedTag;
        }

        public bool Equals(ReasonEntry other) =>
            key == other.key && relatedTag == other.relatedTag;

        public override bool Equals(object obj) =>
            obj is ReasonEntry other && Equals(other);

        public override int GetHashCode() =>
            ((int)key * 397) ^ (int)relatedTag;
    }
}
#endif