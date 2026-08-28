using System.Reflection;
using DA_Assets.Singleton;

namespace DA_Assets.UCC
{
    internal static class FuitkConfigReflectionHelper
    {
        private static object GetFuitkInstance()
        {
            var configAsset = FcuConfig.FuitkConfig;
            if (configAsset == null)
                return null;

            var instanceProp = configAsset.GetType().GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return instanceProp?.GetValue(null);
        }

        public static string GetProductVersion()
        {
            var instance = GetFuitkInstance();
            if (instance == null) return null;

            var prop = instance.GetType().GetProperty("ProductVersion");
            return prop?.GetValue(instance) as string;
        }

        public static void SetLanguage(DALanguage language)
        {
            var instance = GetFuitkInstance();
            if (instance == null) return;

            var locProp = instance.GetType().GetProperty("Localizator");
            var localizator = locProp?.GetValue(instance);
            if (localizator == null) return;

            var langProp = localizator.GetType().GetProperty("Language");
            langProp?.SetValue(localizator, language);
        }
    }
}