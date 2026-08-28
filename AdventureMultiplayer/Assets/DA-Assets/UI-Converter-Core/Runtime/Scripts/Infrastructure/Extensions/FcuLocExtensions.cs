#if UNITY_EDITOR
namespace DA_Assets.UCC
{
    public static class FcuLocExtensions
    {
        public static string Localize(this FcuLocKey key, params object[] args) =>
            FcuConfig.Instance.Localizator.GetLocalizedText(key, null, args);

        public static string Localize(this FcuLocKey key, DA_Assets.Singleton.DALanguage language, params object[] args) =>
            FcuConfig.Instance.Localizator.GetLocalizedText(key, language, args);
    }
}
#endif