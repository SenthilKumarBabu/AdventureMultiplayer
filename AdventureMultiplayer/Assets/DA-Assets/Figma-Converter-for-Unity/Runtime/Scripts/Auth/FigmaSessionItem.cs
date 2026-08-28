#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.UCC;

namespace DA_Assets.FCU
{
    public struct FigmaSessionItem
    {
        public AuthType AuthType { get; set; }
        public AuthResult AuthResult { get; set; }
        public FigmaUser User { get; set; }
    }
}
#endif
