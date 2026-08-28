#if UNITY_EDITOR
namespace DA_Assets.UCC
{
    public struct JFResult
    {
        public bool IsValid { get; set; }
        public string Json { get; set; }
        public bool MatchTargetType { get; set; }
    }
}
#endif