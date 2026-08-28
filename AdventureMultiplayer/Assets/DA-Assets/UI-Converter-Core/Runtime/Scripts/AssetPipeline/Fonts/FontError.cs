#if UNITY_EDITOR
namespace DA_Assets.UCC
{
    internal struct FontError
    {
        public FontStruct FontStruct { get; set; }
        public WebError Error { get; set; }
    }
}
#endif